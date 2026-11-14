//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Instance;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class EscapePacketHandler(IInstanceExitService instanceExitService)
        : PacketHandler<EscapePacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(EscapePacket packet, ClientSession session) =>
            instanceExitService.LeaveAsync(session);
    }
}
