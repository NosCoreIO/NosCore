//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Player;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.NoAction
{
    public class SnapPacketHandler : PacketHandler<SnapPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SnapPacket packet, ClientSession clientSession)
        {
            return Task.CompletedTask;
        }
    }
}
