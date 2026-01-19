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
            var charList = new List<CharacterRelationStatus>();
            var list = characterRelationDao
                .Where(s => (s.CharacterId == id) && (s.RelationType == CharacterRelationType.Blocked));

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
                    CharacterName = (await characterDao.FirstOrDefaultAsync(s => s.CharacterId == rel.RelatedCharacterId))?.Name ?? "",
                    CharacterId = rel.RelatedCharacterId,
                    IsConnected = character != null,
                    RelationType = rel.RelationType,
                    CharacterRelationId = rel.CharacterRelationId
                });
            }

            return charList;
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
