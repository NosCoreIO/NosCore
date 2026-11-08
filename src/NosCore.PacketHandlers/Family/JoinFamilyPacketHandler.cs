//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using FamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class JoinFamilyPacketHandler(ISessionRegistry sessionRegistry, IFamilyService familyService, IPubSubHub pubSubHub,
            IFamilyLogService familyLogService)
        : PacketHandler<JoinFamilyPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(JoinFamilyPacket packet, ClientSession session)
        {
            if (!sessionRegistry.TryGetCharacter(s => s.VisualId == packet.CharacterId, out var inviter)
                || !inviter.FamilyRequestCharacterIds.TryRemove(session.Character.CharacterId, out var invitedFamilyId))
            {
                return;
            }

            if (packet.Type != FamilyInviteAnswerType.Accept)
            {
                await inviter.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.PlayerRefusedFamilyInvite });
                return;
            }

            var family = inviter.Family;
            if (family == null || family.FamilyId != invitedFamilyId || !family.CanInvite(inviter.CharacterId)
                || session.Character.Family != null)
            {
                return;
            }

            if (family.Members.Count >= family.MaxSize)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.FamilyReachedMaxCapacity });
                return;
            }

            await familyService.AddMemberAsync(family.FamilyId, session.Character.CharacterId);
            await familyLogService.AddAsync(family.FamilyId, FamilyLogType.UserManaged, inviter.Name, session.Character.Name);
            await pubSubHub.SendMessageAsync(new FamilyChangedData
            {
                FamilyId = family.FamilyId,
                CharacterIds = { session.Character.CharacterId },
                JoinedCharacterName = session.Character.Name
            });
        }
    }
}
