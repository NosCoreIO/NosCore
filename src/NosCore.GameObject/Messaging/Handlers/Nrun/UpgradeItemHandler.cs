//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 2 — basic upgrade NPC. Opens the equipment-upgrade window so the client can
    // then send up_gr packets that UpgradePacketHandler dispatches into IUpgradeOperation.
    [UsedImplicitly]
    public sealed class UpgradeItemHandler
    {
        [UsedImplicitly]
        public Task Handle(NrunRequestedEvent evt)
        {
            if (evt.Packet.Runner != NrunRunnerType.UpgradeItem || evt.Target == null)
            {
                return Task.CompletedTask;
            }

            return evt.ClientSession.SendPacketAsync(new WopenPacket
            {
                Type = WindowType.UpgradeItem,
                Unknown = 0,
                Unknown2 = 0,
            });
        }
    }
}
