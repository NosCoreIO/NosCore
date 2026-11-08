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
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class FamilyLeavePacketHandler(IFamilyService familyService, IPubSubHub pubSubHub,
            IFamilyLogService familyLogService)
        : PacketHandler<FamilyLeavePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FamilyLeavePacket packet, ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null)
            {
                return;
            }

            if (family.AuthorityOf(session.Character.CharacterId) == FamilyAuthority.Head)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.FamilyHeadCanNotLeave });
                return;
            }

            await familyService.RemoveMemberAsync(session.Character.CharacterId);
            await familyLogService.AddAsync(family.FamilyId, FamilyLogType.FamilyManaged, session.Character.Name);
            await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.LeftFamily });
            await pubSubHub.SendMessageAsync(new FamilyChangedData
            {
                FamilyId = family.FamilyId,
                CharacterIds = { session.Character.CharacterId }
            });
        }
    }
}
