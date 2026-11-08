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
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class SetPlaceOfRevivalHandler(IRespawnService respawnService, INpcDialogService npcDialogService)
        : INrunEventHandler
    {
        private const short AskType = 2;
        private const short ConfirmType = 1;

        public NrunRunnerType Runner => NrunRunnerType.SetPlaceOfRevival;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            var character = session.Character;
            if (target is not NpcComponentBundle npc)
            {
                return;
            }

            if (packet.Type == AskType)
            {
                npcDialogService.Open(character, npc, NpcDialogWindow.RevivalQuestion);
                await session.SendPacketAsync(new QnaPacket
                {
                    YesPacket = new NrunPacket
                    {
                        Runner = NrunRunnerType.SetPlaceOfRevival,
                        Type = ConfirmType,
                        VisualType = packet.VisualType,
                        VisualId = packet.VisualId,
                    },
                    Question = "#n_run^15^1^1 Do you want to save this location as your respawn point?",
                });
                return;
            }

            if (packet.Type != ConfirmType
                || !npcDialogService.IsOpenWith(character, npc, NpcDialogWindow.RevivalQuestion))
            {
                return;
            }

            npcDialogService.Open(character, npc, NpcDialogWindow.Conversation);
            respawnService.SetRespawnPoint(character, character.MapInstance.Map.MapId,
                npc.PositionX, npc.PositionY);

            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.RevivalPlaceChanged
            });
        }
    }
}
