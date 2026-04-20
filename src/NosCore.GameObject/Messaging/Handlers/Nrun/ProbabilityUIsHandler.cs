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
    // n_run 12 — generic "probability" NPC that can open any of the upgrade-style windows
    // (sum, rarify, rarify-protected, upgrade-protected, sp variants, fusion, ...). The
    // window type is carried in packet.Type and maps 1:1 to WindowType / UpgradePacketType.
    //
    // This NPC is how a tester reaches Sum / Rarify variants in-game: the client sends
    // n_run 12 8 → wopen 8 0 (sum window) → up_gr 8 ... → SumUpgradeOperation, etc.
    [UsedImplicitly]
    public sealed class ProbabilityUIsHandler
    {
        [UsedImplicitly]
        public Task Handle(NrunRequestedEvent evt)
        {
            if (evt.Packet.Runner != NrunRunnerType.ProbabilityUIs
                || evt.Target == null
                || evt.Packet.Type is null)
            {
                return Task.CompletedTask;
            }

            return evt.ClientSession.SendPacketAsync(new WopenPacket
            {
                Type = (WindowType)evt.Packet.Type.Value,
                Unknown = 0,
                Unknown2 = 0,
            });
        }
    }
}
