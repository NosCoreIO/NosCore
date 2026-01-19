//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GuriRunnerService;
using NosCore.Packets.ClientPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class GuriPacketHandler(IGuriRunnerService guriProvider) : PacketHandler<GuriPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(GuriPacket guriPacket, ClientSession session)
        {
            guriProvider.GuriLaunch(session, guriPacket);
            return Task.CompletedTask;
        }
    }
}
