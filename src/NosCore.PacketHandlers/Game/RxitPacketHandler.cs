//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class RxitPacketHandler(IInstanceExitService instanceExitService)
        : PacketHandler<RxitPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(RxitPacket packet, ClientSession session) =>
            packet.State == RxitPacketType.LeaveInstance
                ? instanceExitService.LeaveAsync(session)
                : Task.CompletedTask;
    }
}
