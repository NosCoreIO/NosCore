//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.NoAction
{
    public class PdtClosePacketHandler : PacketHandler<PdtClosePacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(PdtClosePacket packet, ClientSession clientSession)
        {
            return Task.CompletedTask;
        }
    }
}
