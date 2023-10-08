//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.Core.MessageQueue;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.BlackListService
{
    public class BlacklistService(IPubSubHub pubSubHub,
            IDao<CharacterRelationDto, Guid> characterRelationDao, IDao<CharacterDto, long> characterDao)
        : IBlacklistService
    {
        public async Task<LanguageKey> BlacklistPlayerAsync(long characterId, long secondCharacterId)
        {
            var servers = await pubSubHub.GetCommunicationChannels();
            var accounts = await pubSubHub.GetSubscribersAsync();
            var character = accounts.FirstOrDefault(s =>s.ConnectedCharacter?.Id == characterId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));
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

            await characterRelationDao.TryInsertOrUpdateAsync(data).ConfigureAwait(false);
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

                var servers = await pubSubHub.GetCommunicationChannels();
                var accounts = await pubSubHub.GetSubscribersAsync();
                var character = accounts.FirstOrDefault(s => s.ConnectedCharacter?.Id == rel.RelatedCharacterId && servers.Where(c => c.Type == ServerType.WorldServer).Any(x => x.Id == s.ChannelId));

                charList.Add(new CharacterRelationStatus
                {
                    CharacterName = (await characterDao.FirstOrDefaultAsync(s => s.CharacterId == rel.RelatedCharacterId).ConfigureAwait(false))?.Name ?? "",
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
                (s.CharacterRelationId == id) && (s.RelationType == CharacterRelationType.Blocked)).ConfigureAwait(false);
            if (rel == null)
            {
                return false;
            }
            await characterRelationDao.TryDeleteAsync(rel.CharacterRelationId).ConfigureAwait(false);
            return true;
        }
    }
}