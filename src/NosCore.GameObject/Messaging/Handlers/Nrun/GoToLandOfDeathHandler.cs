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
using NosCore.GameObject.Services.LandOfDeathService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class GoToLandOfDeathHandler(ILandOfDeathService landOfDeathService, INpcDialogService npcDialogService)
        : INrunEventHandler
    {
        public const short ConfirmType = 1;

        public NrunRunnerType Runner => NrunRunnerType.GoToLandOfDeath;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle npc)
            {
                return;
            }

            var character = session.Character;
            if (packet.Type == ConfirmType && npcDialogService.IsOpenWith(character, npc, NpcDialogWindow.LandOfDeathQuestion))
            {
                npcDialogService.Open(character, npc, NpcDialogWindow.Conversation);
                await landOfDeathService.EnterAsync(session);
                return;
            }

            if (!await landOfDeathService.CanEnterAsync(session))
            {
                return;
            }

            npcDialogService.Open(character, npc, NpcDialogWindow.LandOfDeathQuestion);
            await session.SendPacketAsync(new QnaiPacket
            {
                YesPacket = new NrunPacket
                {
                    Runner = NrunRunnerType.GoToLandOfDeath,
                    Type = ConfirmType,
                    VisualType = packet.VisualType,
                    VisualId = packet.VisualId,
                },
                Question = Game18NConstString.AskJoinLandOfDeath,
            });
        }
    }
}
