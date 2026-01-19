//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class SetMaintenancePacketHandler
        (IChannelHub channelHttpClient) : PacketHandler<SetMaintenancePacket>,
            IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetMaintenancePacket setMaintenancePacket, ClientSession session)
        {
            await channelHttpClient.SetMaintenance(setMaintenancePacket.IsGlobal, setMaintenancePacket.MaintenanceMode);
        }
    }
}
