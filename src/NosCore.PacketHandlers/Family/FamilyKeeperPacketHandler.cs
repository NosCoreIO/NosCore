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
    public class FamilyKeeperPacketHandler(IFamilyManagementService familyManagementService)
        : PacketHandler<FamilyKeeperPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(FamilyKeeperPacket packet, ClientSession session) =>
            familyManagementService.ChangeAuthorityAsync(session, packet.CharacterName,
                Data.Enumerations.Family.FamilyAuthority.Manager);
    }
}
