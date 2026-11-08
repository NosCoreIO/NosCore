//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // An NPC's teleport menu: the option index picks one of the destinations recorded for that NPC
    // under this menu's runner, which also sets the fare.
    public abstract class NpcTeleportHandler(IMapChangeService mapChangeService, List<TeleporterDto> teleporters)
        : INrunEventHandler
    {
        public abstract NrunRunnerType Runner { get; }

        public bool RequiresNpc => true;

        protected abstract long Cost(short index);

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle npc)
            {
                return;
            }

            var index = packet.Type ?? 0;
            var destination = teleporters.FirstOrDefault(s =>
                s.MapNpcId == npc.VisualId && s.Runner == Runner && s.Index == index);
            if (destination == null)
            {
                return;
            }

            var character = session.Character;
            var cost = Cost(index);
            if (character.Gold < cost)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.NotEnoughGold
                });
                return;
            }

            if (cost > 0)
            {
                character.RemoveGold(cost);
                await session.SendPacketAsync(character.GenerateGold());
            }

            await mapChangeService.ChangeMapAsync(session, destination.MapId, destination.MapX, destination.MapY);
        }
    }
}
