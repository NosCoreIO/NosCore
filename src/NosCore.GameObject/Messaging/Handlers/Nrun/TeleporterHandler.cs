//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class TeleporterHandler(IMapChangeService mapChangeService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.Teleport;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle mapNpc
                || !((mapNpc.Dialog >= 439 && mapNpc.Dialog <= 441) || mapNpc.Dialog == 11
                    || mapNpc.Dialog == 16 || mapNpc.Dialog == 9768))
            {
                return Task.CompletedTask;
            }

            return packet.Type switch
            {
                1 => RemoveGoldAndTeleportAsync(session, 20, 1000, 7, 11, 90, 94),
                2 => RemoveGoldAndTeleportAsync(session, 145, 2000, 11, 15, 108, 112),
                _ => RemoveGoldAndTeleportAsync(session, 1, 0, 77, 82, 113, 119),
            };
        }

        private async Task RemoveGoldAndTeleportAsync(ClientSession session, short mapId, long goldToPay,
            short x1, short x2, short y1, short y2)
        {
            var character = session.Character;
            if (character.Gold >= goldToPay)
            {
                character.RemoveGold(goldToPay);
                await session.SendPacketAsync(character.GenerateGold());
                await mapChangeService.ChangeMapAsync(session, mapId,
                    (short)RandomHelper.Instance.RandomNumber(x1, x2),
                    (short)RandomHelper.Instance.RandomNumber(y1, y2));
                return;
            }

            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.NotEnoughGold
            });
        }
    }
}
