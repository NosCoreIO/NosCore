//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Infrastructure;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.NoAction
{
    public class ZeroPacketHandler : PacketHandler<ZeroPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(ZeroPacket packet, ClientSession clientSession)
        {
            return Task.CompletedTask;
        }
    }
}
