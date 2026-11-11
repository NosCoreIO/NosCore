//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using FamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using NosCore.Data.Dto;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using System;
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
            Action<FamilyDto>? change = (packet.MemberType, packet.AuthorityId) switch
            {
                (Packets.Enumerations.FamilyAuthority.Manager, 0) => s => s.ManagerCanInvite = enabled,
                (Packets.Enumerations.FamilyAuthority.Manager, 1) => s => s.ManagerCanNotice = enabled,
                (Packets.Enumerations.FamilyAuthority.Manager, 2) => s => s.ManagerCanShout = enabled,
                (Packets.Enumerations.FamilyAuthority.Manager, 3) => s => s.ManagerCanGetHistory = enabled,
                (Packets.Enumerations.FamilyAuthority.Manager, 4) => s => s.ManagerAuthorityType = (FamilyAuthorityType)packet.Value,
                (Packets.Enumerations.FamilyAuthority.Member, 0) => s => s.MemberCanGetHistory = enabled,
                (Packets.Enumerations.FamilyAuthority.Member, 1) => s => s.MemberAuthorityType = (FamilyAuthorityType)packet.Value,
                _ => null
            };
            if (change == null || !await familyService.UpdateAsync(family.FamilyId, change))
            {
                return;
            }

            change(family);
            // The history numbers rights from one where f_auth numbers them from zero; unverified in a client.
            await familyLogService.AddAsync(family.FamilyId, FamilyLogType.RightChanged, session.Character.Name,
                (byte)packet.MemberType, packet.AuthorityId + 1, packet.Value);
            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });
        }
    }
}
