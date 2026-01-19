//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ChannelService;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class ChangeChannelPacketHandler(IChannelService loginService) : PacketHandler<ChangeChannelPacket>,
        IWorldPacketHandler
    {
        public override Task ExecuteAsync(ChangeChannelPacket changeClassPacket, ClientSession session)
        {
            return loginService.MoveChannelAsync(session, changeClassPacket.ChannelId);
        }
    }
}
