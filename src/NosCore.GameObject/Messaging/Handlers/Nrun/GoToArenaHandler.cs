//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ArenaService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class GoToArenaHandler(IMapChangeService mapChangeService, IMapInstanceRegistry mapInstanceRegistry,
        IClock clock) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.GoToArena;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle
                || session.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
            {
                return;
            }

            var type = packet.Type ?? 0;
            var mapId = Arena.MapIdForType(type);
            var arena = mapId == null ? null : mapInstanceRegistry.GetAll()
                .FirstOrDefault(s => s.MapInstanceType == MapInstanceType.ArenaInstance && s.Map.MapId == mapId);
            if (arena == null)
            {
                return;
            }

            var character = session.Character;
            var now = clock.GetCurrentInstant();
            if ((now - character.LastPortal).TotalSeconds < 4)
            {
                await SayAsync(session, Game18NConstString.WillMoveShortly);
                return;
            }

            var cost = Arena.EntryCost(type);
            if (character.Gold < cost)
            {
                await SayAsync(session, Game18NConstString.NotEnoughGold);
                return;
            }

            var cell = Arena.RandomFightingCell(arena.Map);
            if (cell == null)
            {
                return;
            }

            character.RemoveGold(cost);
            character.LastPortal = now;
            await session.SendPacketAsync(character.GenerateGold());
            await mapChangeService.ChangeMapInstanceAsync(session, arena.MapInstanceId, cell.Value.X, cell.Value.Y);
        }

        private static Task SayAsync(ClientSession session, Game18NConstString message) =>
            session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = message,
            });
    }
}
