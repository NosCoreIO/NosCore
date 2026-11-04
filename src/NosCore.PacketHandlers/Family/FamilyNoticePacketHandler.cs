//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Commands;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class FamilyNoticePacketHandler(IFamilyManagementService familyManagementService)
        : PacketHandler<FamilyNoticePacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(FamilyNoticePacket packet, ClientSession session) =>
            familyManagementService.SetNoticeAsync(session, packet.Message);
    }
}
