//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class FamilyInvitePacketHandler(ISessionRegistry sessionRegistry, IBlacklistHub blacklistHub, IClock clock)
        : PacketHandler<FamilyInvitePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FamilyInvitePacket packet, ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || string.IsNullOrWhiteSpace(packet.CharacterName))
            {
                return;
            }

            if (!family.CanInvite(session.Character.CharacterId))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotAuthorizedToAcceptFamily });
                return;
            }

            if (!sessionRegistry.TryGetCharacter(s => s.Name == packet.CharacterName, out var target)
                || target.CharacterId == session.Character.CharacterId)
            {
                await session.SendPacketAsync(new Infoi2Packet
                {
                    Message = Game18NConstString.CanNotFind,
                    ArgumentType = 1,
                    Game18NArguments = { packet.CharacterName }
                });
                return;
            }

            if (await blacklistHub.IsBlacklistedEitherWayAsync(session.Character.VisualId, target.CharacterId))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.AlreadyBlacklisted });
                return;
            }

            if (target.FamilyRequestBlocked)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.PlayerFamilyBlocked });
                return;
            }

            if (family.Members.Count >= family.MaxSize)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.FamilyReachedMaxCapacity });
                return;
            }

            if (target.Family != null)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.PlayerAlreadyInAnotherFamily });
                return;
            }

            if (!await session.Character.TryStartInviteAsync(InviteType.Family, target.CharacterId, clock))
            {
                return;
            }

            session.Character.FamilyRequestCharacterIds[target.CharacterId] = family.FamilyId;
            await session.SendPacketAsync(new Infoi2Packet
            {
                Message = Game18NConstString.YouInvitedToGroup,
                ArgumentType = 1,
                Game18NArguments = { target.Name }
            });
            await target.SendPacketAsync(new Dlgi2Packet
            {
                YesPacket = new JoinFamilyPacket { Type = FamilyInviteAnswerType.Accept, CharacterId = session.Character.CharacterId },
                NoPacket = new JoinFamilyPacket { Type = FamilyInviteAnswerType.Decline, CharacterId = session.Character.CharacterId },
                Question = Game18NConstString.AskMemberFamily,
                ArgumentType = 1,
                Game18NArguments = { family.Name }
            });
        }
    }
}
