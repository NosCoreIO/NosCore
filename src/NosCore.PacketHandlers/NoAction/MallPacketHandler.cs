//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.NoAction
{
    public class MallPacketHandler : PacketHandler<MallPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(MallPacket packet, ClientSession clientSession)
        {
            return Task.CompletedTask;
        }
    }
}
