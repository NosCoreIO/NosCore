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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace NosCore.GameObject.Services.FriendService
{
    public class FriendService(ILogger<FriendService> logger, IDao<CharacterRelationDto, Guid> characterRelationDao,
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

                if (characterRelationDao.Where(s => (s.CharacterId == secondCharacterId) &&
                        (s.RelatedCharacterId == characterId) &&
                        (s.RelationType == CharacterRelationType.Blocked))?.Any() == true)
                {
                    return LanguageKey.BLACKLIST_BLOCKED;
                }

                if (relations.Any(s =>
                    (s.RelationType == CharacterRelationType.Friend) &&
                    (s.RelatedCharacterId == secondCharacterId)))
                {
                    return LanguageKey.ALREADY_FRIEND;
                }

                if (character.ConnectedCharacter?.FriendRequestBlocked == true ||
                    targetCharacter.ConnectedCharacter?.FriendRequestBlocked == true)
                {
                    return LanguageKey.FRIEND_REQUEST_BLOCKED;
                }

                if (!friendRequest.Any())
                {
                    var alreadyRequested = friendRequestRegistry.GetRequestsForCharacter(characterId)
                        .Any(s => s.Value.SenderId == characterId && s.Value.ReceiverId == secondCharacterId);
                    if (!alreadyRequested)
                    {
                        friendRequestRegistry.RegisterRequest(Guid.NewGuid(), characterId, secondCharacterId);
                    }

                    return LanguageKey.FRIEND_REQUEST_SENT;
                }

                switch (friendsPacketType)
                {
                    case FinsPacketType.Accepted:
                        var data = new CharacterRelationDto
                        {
                            CharacterId = characterId,
                            RelatedCharacterId = secondCharacterId,
                            RelationType = CharacterRelationType.Friend
                        };

                        await characterRelationDao.TryInsertOrUpdateAsync(data);
                        var data2 = new CharacterRelationDto
                        {
                            CharacterId = secondCharacterId,
                            RelatedCharacterId = characterId,
                            RelationType = CharacterRelationType.Friend
                        };

                        await characterRelationDao.TryInsertOrUpdateAsync(data2);
                        friendRequestRegistry.UnregisterRequest(friendRequest.First().Key);
                        return LanguageKey.FRIEND_ADDED;
                    case FinsPacketType.Rejected:
                        friendRequestRegistry.UnregisterRequest(friendRequest.First().Key);
                        return LanguageKey.FRIEND_REJECTED;
                    default:
                        logger.LogError(logLanguage[LogLanguageKey.INVITETYPE_UNKNOWN]);
                        friendRequestRegistry.UnregisterRequest(friendRequest.First().Key);
                        throw new ArgumentException();
                }
            }

            friendRequestRegistry.UnregisterRequest(friendRequest.First().Key);
            throw new ArgumentException();
        }

        public async Task<List<CharacterRelationStatus>> GetFriendsAsync(long id)
        {
            var list = characterRelationDao
                .Where(s => (s.CharacterId == id) && (s.RelationType != CharacterRelationType.Blocked))?.ToList();
            if (list == null || list.Count == 0)
            {
                return new List<CharacterRelationStatus>();
            }

            var servers = await channelHub.GetCommunicationChannels();
            var worldServerIds = servers.Where(c => c.Type == ServerType.WorldServer).Select(c => c.Id).ToHashSet();
            var online = (await pubSubHub.GetSubscribersAsync())
                .Where(s => worldServerIds.Contains(s.ChannelId))
                .Select(s => s.ConnectedCharacter?.Id)
                .OfType<long>()
                .ToHashSet();
            var relatedIds = list.Select(s => s.RelatedCharacterId).Distinct().ToList();
            var names = characterDao.Where(s => relatedIds.Contains(s.CharacterId))?
                .ToDictionary(s => s.CharacterId, s => (string?)s.Name) ?? new Dictionary<long, string?>();

            return list.Select(rel => new CharacterRelationStatus
            {
                CharacterName = names.GetValueOrDefault(rel.RelatedCharacterId),
                CharacterId = rel.RelatedCharacterId,
                IsConnected = online.Contains(rel.RelatedCharacterId),
                RelationType = rel.RelationType,
                CharacterRelationId = rel.CharacterRelationId
            }).ToList();
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
