//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class GListPacketHandler(IFamilyManagementService familyManagementService)
        : PacketHandler<GListPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(GListPacket packet, ClientSession session) =>
            packet.Type == GListPacketType.MemberList ? familyManagementService.SendMemberListAsync(session) : Task.CompletedTask;
    }
}
