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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace NosCore.GameObject.Services.BlackListService
{
    public class BlacklistService(IPubSubHub pubSubHub, IChannelHub channelHub,
            IDao<CharacterRelationDto, Guid> characterRelationDao, IDao<CharacterDto, long> characterDao)
        : IBlacklistService
    {
        public async Task<LanguageKey> BlacklistPlayerAsync(long characterId, long secondCharacterId)
        {
            var servers = await channelHub.GetCommunicationChannels();
            var accounts = await pubSubHub.GetSubscribersAsync();
            var character = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == characterId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));
            var targetCharacter = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == secondCharacterId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));


            if ((character == null) || (targetCharacter == null))
            {
                throw new ArgumentException();
            }

            var relations = characterRelationDao.Where(s => s.CharacterId == characterId)?
                .ToList() ?? new List<CharacterRelationDto>();
            if (relations.Any(s =>
                (s.RelatedCharacterId == secondCharacterId) &&
                (s.RelationType != CharacterRelationType.Blocked)))
            {
                return LanguageKey.CANT_BLOCK_FRIEND;
            }

            if (relations.Any(s =>
                (s.RelatedCharacterId == secondCharacterId) &&
                (s.RelationType == CharacterRelationType.Blocked)))
            {
                return LanguageKey.ALREADY_BLACKLISTED;
            }

            var data = new CharacterRelationDto
            {
                CharacterId = character.ConnectedCharacter!.Id,
                RelatedCharacterId = targetCharacter.ConnectedCharacter!.Id,
                RelationType = CharacterRelationType.Blocked
            };

            await characterRelationDao.TryInsertOrUpdateAsync(data);
            return LanguageKey.BLACKLIST_ADDED;

        }

        public async Task<List<CharacterRelationStatus>> GetBlacklistedListAsync(long id)
        {
            var list = characterRelationDao
                .Where(s => (s.CharacterId == id) && (s.RelationType == CharacterRelationType.Blocked))?.ToList();
            if (list == null || list.Count == 0)
            {
                return new List<CharacterRelationStatus>();
            }

            var servers = await channelHub.GetCommunicationChannels();
            var worldServerIds = servers.Where(c => c.Type == ServerType.WorldServer).Select(c => c.Id).ToHashSet();
            var online = (await pubSubHub.GetSubscribersAsync())
                .Where(s => s.ConnectedCharacter != null && worldServerIds.Contains(s.ChannelId))
                .Select(s => s.ConnectedCharacter!.Id)
                .ToHashSet();
            var relatedIds = list.Select(s => s.RelatedCharacterId).Distinct().ToList();
            var names = characterDao.Where(s => relatedIds.Contains(s.CharacterId))?
                .ToDictionary(s => s.CharacterId, s => (string?)s.Name) ?? new Dictionary<long, string?>();

            return list.Select(rel => new CharacterRelationStatus
            {
                CharacterName = names.GetValueOrDefault(rel.RelatedCharacterId) ?? "",
                CharacterId = rel.RelatedCharacterId,
                IsConnected = online.Contains(rel.RelatedCharacterId),
                RelationType = rel.RelationType,
                CharacterRelationId = rel.CharacterRelationId
            }).ToList();
        }

        public async Task<bool> UnblacklistAsync(Guid id)
        {
            var rel = await characterRelationDao.FirstOrDefaultAsync(s =>
                (s.CharacterRelationId == id) && (s.RelationType == CharacterRelationType.Blocked));
            if (rel == null)
            {
                return false;
            }
            await characterRelationDao.TryDeleteAsync(rel.CharacterRelationId);
            return true;
        }
    }
}
