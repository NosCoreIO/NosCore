//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
    // The NPC names the window it wants in the packet's Type, which is also the up_gr type the
    // window sends back, so only windows whose upgrade is handled are opened.
    [UsedImplicitly]
    public sealed class ProbabilityWindowHandler(IEnumerable<IUpgradeOperation> operations,
        INpcDialogService npcDialogService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.ProbabilityUIs;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle npc || packet.Type is not { } type
                || operations.All(o => (short)o.Kind != type))
            {
                return Task.CompletedTask;
            }

            npcDialogService.Open(session.Character, npc, NpcDialogWindow.Upgrade);
            return session.SendPacketAsync(new WopenPacket
            {
                Type = (WindowType)type,
            });
        }
    }
}
