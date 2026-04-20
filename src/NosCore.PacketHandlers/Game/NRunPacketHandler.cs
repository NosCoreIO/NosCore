//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.PacketHandlers.Game
{
    public sealed class NRunPacketHandler(
            ILogger logger,
            IRespawnService respawnService)
        : PacketHandler<NrunPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(NrunPacket packet, ClientSession session)
        {
            switch (packet.Runner)
            {
                case NrunRunnerType.SetPlaceOfRevival:
                    await HandleSetPlaceOfRevivalAsync(packet, session).ConfigureAwait(false);
                    return;

                default:
                    logger.Debug("Unhandled n_run runner {Runner}", packet.Runner);
                    return;
            }
        }

        private async Task HandleSetPlaceOfRevivalAsync(NrunPacket packet, ClientSession session)
        {
            if (packet.Type == 2)
            {
                await session.SendPacketAsync(new QnaPacket
                {
                    YesPacket = new NrunPacket
                    {
                        Runner = NrunRunnerType.SetPlaceOfRevival,
                        Type = 1,
                        VisualType = packet.VisualType,
                        VisualId = packet.VisualId,
                    },
                    Question = "#n_run^15^1^1 Do you want to save this location as your respawn point?",
                }).ConfigureAwait(false);
                return;
            }

            var character = session.Character;
            if (packet.VisualType != VisualType.Npc
                || !packet.VisualId.HasValue
                || character.MapInstance == null)
            {
                return;
            }

            var npc = character.MapInstance.FindNpc(n => n.VisualId == packet.VisualId.Value);
            if (npc is null)
            {
                return;
            }

            respawnService.SetRespawnPoint(character, character.MapInstance.Map.MapId,
                npc.Value.PositionX, npc.Value.PositionY);
        }
    }
}
