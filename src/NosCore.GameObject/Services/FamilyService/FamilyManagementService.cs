//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyMemberRank = NosCore.Data.Enumerations.Family.FamilyMemberRank;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.Interfaces;
using System.Linq;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Algorithm.FamilyExperienceService;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyManagementService(IFamilyService familyService, IPubSubHub pubSubHub,
        IFamilyExperienceService familyExperienceService) : IFamilyManagementService
    {
        private const int MessageMaxLength = 255;

        public async Task ChangeAuthorityAsync(ClientSession session, string? characterName, FamilyAuthority authority)
        {
            if (session.Character.Family == null || string.IsNullOrWhiteSpace(characterName))
            {
                return;
            }

            var target = await familyService.FindCharacterAsync(characterName);
            if (target == null || await ChangeAuthorityAsync(session, target.CharacterId, authority) == FamilyAuthorityChange.NotInFamily)
            {
                await session.SendPacketAsync(new Infoi2Packet
                {
                    Message = Game18NConstString.CanNotFind,
                    ArgumentType = 1,
                    Game18NArguments = { characterName }
                });
            }
        }

        public async Task<FamilyAuthorityChange> ChangeAuthorityAsync(ClientSession session, long targetId, FamilyAuthority authority)
        {
            var family = session.Character.Family;
            if (family == null)
            {
                return FamilyAuthorityChange.NotInFamily;
            }

            var result = await familyService.ChangeAuthorityAsync(family, session.Character.CharacterId, targetId, authority);
            Game18NConstString? message = result switch
            {
                FamilyAuthorityChange.NotAuthorized => Game18NConstString.NotAuthorized,
                FamilyAuthorityChange.OnlyAssistantCanBecomeHead => Game18NConstString.OnlyFamilyDeputyCanBecomeHEad,
                FamilyAuthorityChange.TooManyAssistants => Game18NConstString.CanNotAppointMoreDeputy,
                FamilyAuthorityChange.Changed => authority switch
                {
                    FamilyAuthority.Head => Game18NConstString.FamilyHeadChanged,
                    FamilyAuthority.Assistant => Game18NConstString.NewFamilyDeputyAppointed,
                    FamilyAuthority.Manager => Game18NConstString.NewFamilyKeeperAppointed,
                    _ => null
                },
                _ => null
            };
            if (message != null)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = message.Value });
            }

            if (result == FamilyAuthorityChange.Changed)
            {
                var changed = new FamilyChangedData { FamilyId = family.FamilyId, CharacterIds = { targetId } };
                if (authority == FamilyAuthority.Head)
                {
                    changed.CharacterIds.Add(session.Character.CharacterId);
                }

                await pubSubHub.SendMessageAsync(changed);
            }

            return result;
        }
            public async Task SendMemberListAsync(ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null)
            {
                return;
            }

            var characters = await familyService.GetMemberCharactersAsync(family);
            var connected = (await pubSubHub.GetSubscribersAsync())
                .Where(s => s.ConnectedCharacter != null)
                .Select(s => s.ConnectedCharacter!.Id)
                .ToHashSet();
            await session.SendPacketsAsync(new IPacket?[]
            {
                session.Character.GenerateGInfo(familyExperienceService),
                family.GenerateGmbr(characters, connected),
                family.GenerateGmsg(),
                family.GenerateGexp()
            }.OfType<IPacket>());
        }

        public async Task SetNoticeAsync(ClientSession session, string? message)
        {
            var family = session.Character.Family;
            if (family == null)
            {
                return;
            }

            var canChange = family.AuthorityOf(session.Character.CharacterId) switch
            {
                FamilyAuthority.Head or FamilyAuthority.Assistant => true,
                FamilyAuthority.Manager => family.ManagerCanNotice,
                _ => false
            };
            if (!canChange)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotAuthorizedToChangeNoticed });
                return;
            }

            var notice = message?.Trim() ?? string.Empty;
            if (notice.Length > MessageMaxLength)
            {
                return;
            }

            family.FamilyMessage = notice;
            await familyService.SaveAsync(family);
            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });
        }

        public async Task SetDailyMessageAsync(ClientSession session, string? message)
        {
            var family = session.Character.Family;
            var membership = family?.Members.FirstOrDefault(s => s.CharacterId == session.Character.CharacterId);
            var dailyMessage = message?.Trim() ?? string.Empty;
            if (family == null || membership == null || dailyMessage.Length > MessageMaxLength)
            {
                return;
            }

            membership.DailyMessage = dailyMessage;
            await familyService.SaveMembersAsync(new[] { membership });
            await session.SendPacketAsync(family.GenerateGmsg());
            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });
        }

        public async Task SetTitleAsync(ClientSession session, string? characterName, FamilyMemberRank rank)
        {
            var family = session.Character.Family;
            if (family == null || string.IsNullOrWhiteSpace(characterName)
                || family.AuthorityOf(session.Character.CharacterId) != FamilyAuthority.Head)
            {
                return;
            }

            var target = await familyService.FindCharacterAsync(characterName);
            var membership = target == null ? null : family.Members.FirstOrDefault(s => s.CharacterId == target.CharacterId);
            if (membership == null)
            {
                await session.SendPacketAsync(new Infoi2Packet
                {
                    Message = Game18NConstString.CanNotFind,
                    ArgumentType = 1,
                    Game18NArguments = { characterName }
                });
                return;
            }

            membership.Rank = rank;
            await familyService.SaveMembersAsync(new[] { membership });
            await SendMemberListAsync(session);
            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });
        }

        public async Task SetHeadGenderAsync(ClientSession session, GenderType gender)
        {
            var family = session.Character.Family;
            if (family == null || family.AuthorityOf(session.Character.CharacterId) != FamilyAuthority.Head)
            {
                return;
            }

            foreach (var member in family.Members)
            {
                member.Rank = FamilyMemberRank.Nothing;
            }

            family.FamilyHeadGender = gender;
            await familyService.SaveMembersAsync(family.Members);
            await familyService.SaveAsync(family);
            await SendMemberListAsync(session);
            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });
        }
}
}
