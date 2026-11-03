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
    public class FamilyHeadPacketHandler(IFamilyManagementService familyManagementService)
        : PacketHandler<FamilyHeadPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(FamilyHeadPacket packet, ClientSession session) =>
            familyManagementService.ChangeAuthorityAsync(session, packet.CharacterName,
                Data.Enumerations.Family.FamilyAuthority.Head);
    }
}
