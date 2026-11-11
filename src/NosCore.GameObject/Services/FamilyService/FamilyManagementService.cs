//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyMemberRank = NosCore.Data.Enumerations.Family.FamilyMemberRank;
using FamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.Interfaces;
using System.Linq;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Dao.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.CharacterService;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyManagementService(IFamilyService familyService, IPubSubHub pubSubHub,
        IFamilyExperienceService familyExperienceService, IDaoTransactionScope daoTransactionScope,
        ISessionRegistry sessionRegistry, IFamilyWarehouseService familyWarehouseService,
        IFamilyLogService familyLogService) : IFamilyManagementService
    {
        private const int MessageMaxLength = 255;
        private const long CreationPrice = 200000;
        private const int FoundingMembers = 3;
        private const byte MaxSize = 50;
        private const byte NameMinLength = 3;
        private const byte NameMaxLength = 14;

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
                var targetName = (await familyService.GetMemberCharactersAsync(family))
                    .FirstOrDefault(s => s.CharacterId == targetId)?.Name ?? string.Empty;
                await familyLogService.AddAsync(family.FamilyId, FamilyLogType.AuthorityChanged,
                    session.Character.Name, (byte)authority, targetName);
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
                .Select(s => s.ConnectedCharacter?.Id)
                .OfType<long>()
                .ToHashSet();
            await session.SendPacketsAsync(new IPacket?[]
            {
                session.Character.GenerateGInfo(familyExperienceService),
                family.GenerateGmbr(characters, connected),
                family.GenerateGmsg(),
                family.GenerateGexp()
            }.OfType<IPacket>());
        }

        public async Task OfferCreationAsync(ClientSession session)
        {
            if (await GetFoundersAsync(session) == null)
            {
                return;
            }

            await session.SendPacketAsync(new InboxPacket(new CreateFamilyPacket { FamilyName = string.Empty })
            {
                MaxLength = NameMaxLength,
                Type = 1,
                Message = Game18NConstString.EnterFamilyName
            });
        }

        public async Task CreateAsync(ClientSession session, string? name)
        {
            var founders = await GetFoundersAsync(session);
            if (founders == null || name == null)
            {
                return;
            }

            if (session.Character.Gold < CreationPrice)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NeedMoneyForFamily });
                return;
            }

            if (name.Length is < NameMinLength or > NameMaxLength)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.FamilyNameTooShortOrTooLong });
                return;
            }

            if (!NameRules.HasOnlyAllowedCharacters(name))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.InvalidCharacterInName });
                return;
            }

            if (await familyService.IsNameTakenAsync(name))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.FamilyAlreadyExist });
                return;
            }

            long? familyId;
            await using (var transaction = daoTransactionScope.Begin())
            {
                familyId = await familyService.CreateAsync(name, MaxSize, session.Character.CharacterId,
                    founders.Where(s => s.CharacterId != session.Character.CharacterId).Select(s => s.CharacterId));
                if (familyId == null)
                {
                    await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.FamilyCreationFailed });
                    return;
                }

                await transaction.CommitAsync();
            }

            session.Character.RemoveGold(CreationPrice);
            await session.SendPacketAsync(session.Character.GenerateGold());
            var created = new Msgi2Packet
            {
                Type = MessageType.Default,
                Message = Game18NConstString.FamilyCreated,
                ArgumentType = 1,
                Game18NArguments = { name }
            };
            await Task.WhenAll(sessionRegistry.GetSessions(s => s.HasSelectedCharacter).Select(s => s.SendPacketAsync(created)));

            var changed = new FamilyChangedData { FamilyId = familyId.Value };
            changed.CharacterIds.AddRange(founders.Select(s => s.CharacterId));
            await pubSubHub.SendMessageAsync(changed);
        }

        public async Task OfferDisbandAsync(ClientSession session)
        {
            if (await GetDisbandableFamilyAsync(session) == null)
            {
                return;
            }

            await session.SendPacketAsync(new QnaiPacket
            {
                YesPacket = new FamilyDismissPacket(),
                Question = Game18NConstString.AreYouSureToDisbandFamily
            });
        }

        public async Task DisbandAsync(ClientSession session)
        {
            var family = await GetDisbandableFamilyAsync(session);
            if (family == null)
            {
                return;
            }

            await familyWarehouseService.EmptyAsync(family.FamilyId);
            await using (var transaction = daoTransactionScope.Begin())
            {
                if (!await familyService.DisbandAsync(family))
                {
                    await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.FailedToDisbandFamily });
                    return;
                }

                await transaction.CommitAsync();
            }

            await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.FamilyDisbanded });
            var changed = new FamilyChangedData { FamilyId = family.FamilyId };
            changed.CharacterIds.AddRange(family.Members.Select(s => s.CharacterId));
            await pubSubHub.SendMessageAsync(changed);
        }

        private async Task<Family?> GetDisbandableFamilyAsync(ClientSession session)
        {
            var family = session.Character.Family;
            if (family?.AuthorityOf(session.Character.CharacterId) == FamilyAuthority.Head)
            {
                return family;
            }

            await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.OnlyHeadCanDisbandFamily });
            return null;
        }

        private async Task<List<PlayerComponentBundle>?> GetFoundersAsync(ClientSession session)
        {
            var group = session.Character.Group;
            var founders = new List<PlayerComponentBundle>();
            if (group is { Type: GroupType.Group })
            {
                foreach (var member in group.Keys.Where(s => s.Item1 == VisualType.Player))
                {
                    if (sessionRegistry.TryGetCharacter(s => s.VisualId == member.Item2, out var founder))
                    {
                        founders.Add(founder);
                    }
                }
            }

            if (founders.Count != FoundingMembers)
            {
                await session.SendPacketAsync(new Infoi2Packet
                {
                    Message = Game18NConstString.NeedThreeMember,
                    ArgumentType = 4,
                    Game18NArguments = { FoundingMembers }
                });
                return null;
            }

            if (founders.Any(s => s.Family != null))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.OneMemberAlreadyHaveAFamily });
                return null;
            }

            if (founders.Any(s => s.Class == CharacterClassType.Adventurer))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.OneOrMoreMemberHaveToChangeClass });
                return null;
            }

            return founders;
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

            if (!await familyService.UpdateAsync(family.FamilyId, s => s.FamilyMessage = notice))
            {
                return;
            }

            family.FamilyMessage = notice;
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

            if (await familyLogService.HasWrittenDailyMessageTodayAsync(family.FamilyId, session.Character.Name))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.OnlyOncePerDay });
                return;
            }

            if (!await familyService.UpdateMemberAsync(membership.CharacterId, s => s.DailyMessage = dailyMessage))
            {
                return;
            }

            membership.DailyMessage = dailyMessage;
            await familyLogService.AddAsync(family.FamilyId, FamilyLogType.DailyMessage, session.Character.Name, dailyMessage);
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

            if (!await familyService.UpdateMemberAsync(membership.CharacterId, s => s.Rank = rank))
            {
                return;
            }

            membership.Rank = rank;
            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });
        }

        public async Task SetHeadGenderAsync(ClientSession session, GenderType gender)
        {
            var family = session.Character.Family;
            if (family == null || family.AuthorityOf(session.Character.CharacterId) != FamilyAuthority.Head)
            {
                return;
            }

            await using (var transaction = daoTransactionScope.Begin())
            {
                foreach (var member in family.Members)
                {
                    if (!await familyService.UpdateMemberAsync(member.CharacterId, s => s.Rank = FamilyMemberRank.Nothing))
                    {
                        return;
                    }
                }

                if (!await familyService.UpdateAsync(family.FamilyId, s => s.FamilyHeadGender = gender))
                {
                    return;
                }

                await transaction.CommitAsync();
            }

            foreach (var member in family.Members)
            {
                member.Rank = FamilyMemberRank.Nothing;
            }

            family.FamilyHeadGender = gender;

            await pubSubHub.SendMessageAsync(new FamilyChangedData { FamilyId = family.FamilyId });
        }
}
}
