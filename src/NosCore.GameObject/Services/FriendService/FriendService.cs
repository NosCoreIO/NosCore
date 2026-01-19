//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace NosCore.GameObject.Services.FriendService
{
    public class FriendService(ILogger logger, IDao<CharacterRelationDto, Guid> characterRelationDao,
            IDao<CharacterDto, long> characterDao, IFriendRequestRegistry friendRequestRegistry,
            IPubSubHub pubSubHub, IChannelHub channelHub, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : IFriendService
    {
        public async Task<LanguageKey> AddFriendAsync(long characterId, long secondCharacterId, FinsPacketType friendsPacketType)
        {
            var servers = await channelHub.GetCommunicationChannels();
            var accounts = await pubSubHub.GetSubscribersAsync();
            var character = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == characterId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));
            var targetCharacter = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == secondCharacterId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));

            var friendRequest = friendRequestRegistry.GetRequestsForCharacter(character?.ConnectedCharacter?.Id ?? 0)
                .Where(s => s.Value.ReceiverId == character?.ConnectedCharacter?.Id &&
                           s.Value.SenderId == targetCharacter?.ConnectedCharacter?.Id).ToList();
            if ((character != null) && (targetCharacter != null))
            {
                if (character.ChannelId != targetCharacter.ChannelId)
                {
                    throw new ArgumentException();
                }

                var relations = characterRelationDao.Where(s => s.CharacterId == characterId)?.ToList() ?? new List<CharacterRelationDto>();
                if (relations.Count(s => s.RelationType == CharacterRelationType.Friend) >= 80)
                {
                    return LanguageKey.FRIENDLIST_FULL;
                }

                if (relations.Any(s =>
                    (s.RelationType == CharacterRelationType.Blocked) &&
                    (s.RelatedCharacterId == secondCharacterId)))
                {
                    return LanguageKey.BLACKLIST_BLOCKED;
                }

                if (relations.Any(s =>
                    (s.RelationType == CharacterRelationType.Friend) &&
                    (s.RelatedCharacterId == secondCharacterId)))
                {
                    return LanguageKey.ALREADY_FRIEND;
                }

                if (character.ConnectedCharacter!.FriendRequestBlocked ||
                    targetCharacter.ConnectedCharacter!.FriendRequestBlocked)
                {
                    return LanguageKey.FRIEND_REQUEST_BLOCKED;
                }

                if (!friendRequest.Any())
                {
                    friendRequestRegistry.RegisterRequest(Guid.NewGuid(),
                        character.ConnectedCharacter.Id,
                        targetCharacter.ConnectedCharacter.Id);
                    return LanguageKey.FRIEND_REQUEST_SENT;
                }

                switch (friendsPacketType)
                {
                    case FinsPacketType.Accepted:
                        var data = new CharacterRelationDto
                        {
                            CharacterId = character.ConnectedCharacter.Id,
                            RelatedCharacterId = targetCharacter.ConnectedCharacter.Id,
                            RelationType = CharacterRelationType.Friend
                        };

                        await characterRelationDao.TryInsertOrUpdateAsync(data);
                        var data2 = new CharacterRelationDto
                        {
                            CharacterId = targetCharacter.ConnectedCharacter.Id,
                            RelatedCharacterId = character.ConnectedCharacter.Id,
                            RelationType = CharacterRelationType.Friend
                        };

                        await characterRelationDao.TryInsertOrUpdateAsync(data2);
                        friendRequestRegistry.UnregisterRequest(friendRequest.First().Key);
                        return LanguageKey.FRIEND_ADDED;
                    case FinsPacketType.Rejected:
                        friendRequestRegistry.UnregisterRequest(friendRequest.First().Key);
                        return LanguageKey.FRIEND_REJECTED;
                    default:
                        logger.Error(logLanguage[LogLanguageKey.INVITETYPE_UNKNOWN]);
                        friendRequestRegistry.UnregisterRequest(friendRequest.First().Key);
                        throw new ArgumentException();
                }
            }

            friendRequestRegistry.UnregisterRequest(friendRequest.First().Key);
            throw new ArgumentException();
        }

        public async Task<List<CharacterRelationStatus>> GetFriendsAsync(long id)
        {
            var charList = new List<CharacterRelationStatus>();
            var list = characterRelationDao
                .Where(s => (s.CharacterId == id) && (s.RelationType != CharacterRelationType.Blocked));
            if (list == null)
            {
                return charList;
            }
            foreach (var rel in list)
            {
                var servers = await channelHub.GetCommunicationChannels();
                var accounts = await pubSubHub.GetSubscribersAsync();
                var character = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == rel.RelatedCharacterId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));

                charList.Add(new CharacterRelationStatus
                {
                    CharacterName = (await characterDao.FirstOrDefaultAsync(s => s.CharacterId == rel.RelatedCharacterId))?.Name,
                    CharacterId = rel.RelatedCharacterId,
                    IsConnected = character != null,
                    RelationType = rel.RelationType,
                    CharacterRelationId = rel.CharacterRelationId
                });
            }

            return charList;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var rel = await characterRelationDao.FirstOrDefaultAsync(s =>
                (s.CharacterRelationId == id) && (s.RelationType == CharacterRelationType.Friend));
            if (rel == null)
            {
                return false;
            }
            var rel2 = await characterRelationDao.FirstOrDefaultAsync(s =>
                (s.CharacterId == rel.RelatedCharacterId) && (s.RelatedCharacterId == rel.CharacterId) &&
                (s.RelationType == CharacterRelationType.Friend));
            if (rel2 == null)
            {
                return false;
            }
            await characterRelationDao.TryDeleteAsync(rel.CharacterRelationId);
            await characterRelationDao.TryDeleteAsync(rel2.CharacterRelationId);
            return true;
        }
    }
}
