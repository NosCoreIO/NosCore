//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ShopService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.NpcDialogService;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // The NPC names the window it wants in the packet's Type. An upgrade window sends that type back
    // in its up_gr, so only windows whose upgrade is handled and offered by the NPC are opened; the
    // relict examination window answers with a guri instead.
    [UsedImplicitly]
    public sealed class ProbabilityWindowHandler(IEnumerable<IUpgradeOperation> operations,
        INpcDialogService npcDialogService, INpcServiceCatalog npcServices) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.ProbabilityUIs;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle npc || packet.Type is not { } type
                || DialogWindowFor(npc, type) is not { } window)
            {
                return Task.CompletedTask;
            }

            npcDialogService.Open(session.Character, npc, window);
            return session.SendPacketAsync(new WopenPacket
            {
                Type = (WindowType)type,
            });
        }

        private NpcDialogWindow? DialogWindowFor(NpcComponentBundle npc, short type)
        {
            if ((WindowType)type == WindowType.ExamineRelics)
            {
                return npcServices.Offers(npc, NpcService.RelictExamination) ? NpcDialogWindow.RelictExamination : null;
            }

            return operations.Any(o => (short)o.Kind == type && o.Window == NpcDialogWindow.Upgrade
                && (o.Service is not { } service || npcServices.Offers(npc, service)))
                ? NpcDialogWindow.Upgrade
                : null;
        }
    }
}
