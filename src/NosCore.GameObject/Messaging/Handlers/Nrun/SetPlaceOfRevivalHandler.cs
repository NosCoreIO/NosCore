//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class SetPlaceOfRevivalHandler(IRespawnService respawnService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.SetPlaceOfRevival;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
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
            if (target is not NpcComponentBundle npc || character.MapInstance == null)
            {
                return;
            }

            respawnService.SetRespawnPoint(character, character.MapInstance.Map.MapId,
                npc.PositionX, npc.PositionY);

#pragma warning disable CS0618
            await session.SendPacketAsync(new MsgPacket
            {
                Type = MessageType.Default,
                Message = "Your respawn location has been changed."
            }).ConfigureAwait(false);
#pragma warning restore CS0618
        }
    }
}
