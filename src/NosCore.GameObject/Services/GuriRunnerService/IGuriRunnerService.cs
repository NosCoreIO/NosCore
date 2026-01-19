//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;

namespace NosCore.GameObject.Services.GuriRunnerService
{
    public interface IGuriRunnerService
    {
        void GuriLaunch(ClientSession clientSession, GuriPacket data);
    }
}
