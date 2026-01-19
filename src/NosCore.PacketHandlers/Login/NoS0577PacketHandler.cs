//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.LoginService;
using NosCore.Packets.ClientPackets.Login;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Login
{
    public class NoS0577PacketHandler(ILoginService loginService) : PacketHandler<NoS0577Packet>, ILoginPacketHandler
    {
        public override Task ExecuteAsync(NoS0577Packet packet, ClientSession clientSession)
        {
            return loginService.LoginAsync(null, packet.Md5String!, packet.ClientVersion!, clientSession,
                packet.AuthToken!, true, packet.RegionType);
        }
    }
}
