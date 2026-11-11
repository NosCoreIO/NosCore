//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Battle;

namespace NosCore.PacketHandlers.Battle
{
    // The client names a skill's extra targets just before the u_s that casts it; that cast hits them.
    public class MultiTargetListPacketHandler : PacketHandler<MultiTargetListPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(MultiTargetListPacket packet, ClientSession clientSession)
        {
            clientSession.Character.ListedTargets = packet.Targets is { Count: > 0 } targets && packet.TargetsAmount == targets.Count
                ? targets.Select(t => (t.VisualType, t.VisualId)).ToList()
                : null;
            return Task.CompletedTask;
        }
    }
}
