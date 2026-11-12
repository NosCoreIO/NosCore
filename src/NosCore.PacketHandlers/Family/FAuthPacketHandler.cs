//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using FamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;
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
    public class FAuthPacketHandler(IFamilyService familyService, IPubSubHub pubSubHub,
            IFamilyLogService familyLogService)
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
            var change = (packet.MemberType, packet.AuthorityId) switch
            {
                (Packets.Enumerations.FamilyAuthority.Manager, 0) => new FamilyChange { ManagerCanInvite = enabled },
                (Packets.Enumerations.FamilyAuthority.Manager, 1) => new FamilyChange { ManagerCanNotice = enabled },
                (Packets.Enumerations.FamilyAuthority.Manager, 2) => new FamilyChange { ManagerCanShout = enabled },
                (Packets.Enumerations.FamilyAuthority.Manager, 3) => new FamilyChange { ManagerCanGetHistory = enabled },
                (Packets.Enumerations.FamilyAuthority.Manager, 4) => new FamilyChange { ManagerAuthorityType = (FamilyAuthorityType)packet.Value },
                (Packets.Enumerations.FamilyAuthority.Member, 0) => new FamilyChange { MemberCanGetHistory = enabled },
                (Packets.Enumerations.FamilyAuthority.Member, 1) => new FamilyChange { MemberAuthorityType = (FamilyAuthorityType)packet.Value },
                _ => null
            };
            if (change == null || !await familyService.UpdateAsync(family.FamilyId, change, []))
            {
                return;
            }

            change.ApplyTo(family);
            // The history numbers rights from one where f_auth numbers them from zero; unverified in a client.
            await familyLogService.AddAsync(family.FamilyId, FamilyLogType.RightChanged, session.Character.Name,
                (byte)packet.MemberType, packet.AuthorityId + 1, packet.Value);
            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });
        }
    }
}
