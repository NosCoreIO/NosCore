//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GroupService;
using NosCore.Packets.ClientPackets.Groups;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Group
{
    public class PleavePacketHandler(IGroupLeaveService groupLeaveService) : PacketHandler<PleavePacket>,
        IWorldPacketHandler
    {
        public override Task ExecuteAsync(PleavePacket bIPacket, ClientSession clientSession) =>
            groupLeaveService.LeaveAsync(clientSession.Character);
    }
}
