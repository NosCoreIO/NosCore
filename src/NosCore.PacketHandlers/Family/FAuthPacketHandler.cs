//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class FAuthPacketHandler(IFamilyService familyService, IPubSubHub pubSubHub)
        : PacketHandler<FAuthPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FAuthPacket packet, ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || family.AuthorityOf(session.Character.CharacterId) != FamilyAuthority.Head)
            {
                return;
            }

            var enabled = packet.Value == 1;
            switch (packet.MemberType, packet.AuthorityId)
            {
                case (Packets.Enumerations.FamilyAuthority.Manager, 0): family.ManagerCanInvite = enabled; break;
                case (Packets.Enumerations.FamilyAuthority.Manager, 1): family.ManagerCanNotice = enabled; break;
                case (Packets.Enumerations.FamilyAuthority.Manager, 2): family.ManagerCanShout = enabled; break;
                case (Packets.Enumerations.FamilyAuthority.Manager, 3): family.ManagerCanGetHistory = enabled; break;
                case (Packets.Enumerations.FamilyAuthority.Manager, 4): family.ManagerAuthorityType = (FamilyAuthorityType)packet.Value; break;
                case (Packets.Enumerations.FamilyAuthority.Member, 0): family.MemberCanGetHistory = enabled; break;
                case (Packets.Enumerations.FamilyAuthority.Member, 1): family.MemberAuthorityType = (FamilyAuthorityType)packet.Value; break;
                default: return;
            }

            await familyService.SaveAsync(family);
            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });
        }
    }
}
