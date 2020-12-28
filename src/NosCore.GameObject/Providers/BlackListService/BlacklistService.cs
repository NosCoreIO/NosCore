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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Providers.BlackListService
{
    public class BlacklistService : IBlacklistService
    {
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IDao<CharacterRelationDto, Guid> _characterRelationDao;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;

        public BlacklistService(IConnectedAccountHttpClient connectedAccountHttpClient,
            IDao<CharacterRelationDto, Guid> characterRelationDao, IDao<CharacterDto, long> characterDao)
        {
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _characterRelationDao = characterRelationDao;
            _characterDao = characterDao;
        }

        public async Task<LanguageKey> BlacklistPlayerAsync(BlacklistRequest blacklistRequest)
        {
            var character = await _connectedAccountHttpClient.GetCharacterAsync(blacklistRequest.CharacterId, null).ConfigureAwait(false);
            var targetCharacter = await
                _connectedAccountHttpClient.GetCharacterAsync(blacklistRequest.BlInsPacket?.CharacterId, null).ConfigureAwait(false);
            if ((character.Item2 == null) || (targetCharacter.Item2 == null))
            {
                throw new ArgumentException();
            }

            var relations = _characterRelationDao.Where(s => s.CharacterId == blacklistRequest.CharacterId)?
                .ToList() ?? new List<CharacterRelationDto>();
            if (relations.Any(s =>
                (s.RelatedCharacterId == blacklistRequest.BlInsPacket?.CharacterId) &&
                (s.RelationType != CharacterRelationType.Blocked)))
            {
                return LanguageKey.CANT_BLOCK_FRIEND;
            }

            if (relations.Any(s =>
                (s.RelatedCharacterId == blacklistRequest.BlInsPacket?.CharacterId) &&
                (s.RelationType == CharacterRelationType.Blocked)))
            {
                return LanguageKey.ALREADY_BLACKLISTED;
            }

            var data = new CharacterRelationDto
            {
                CharacterId = character.Item2.ConnectedCharacter!.Id,
                RelatedCharacterId = targetCharacter.Item2.ConnectedCharacter!.Id,
                RelationType = CharacterRelationType.Blocked
            };

            await _characterRelationDao.TryInsertOrUpdateAsync(data).ConfigureAwait(false);
            return LanguageKey.BLACKLIST_ADDED;

        }

        public async Task<List<CharacterRelationStatus>> GetBlacklistedListAsync(long id)
        {
            var charList = new List<CharacterRelationStatus>();
            var list = _characterRelationDao
                .Where(s => (s.CharacterId == id) && (s.RelationType == CharacterRelationType.Blocked));
            if (list == null)
            {
                return charList;
            }
            foreach (var rel in list)
            {
                charList.Add(new CharacterRelationStatus
                {
                    CharacterName = (await _characterDao.FirstOrDefaultAsync(s => s.CharacterId == rel.RelatedCharacterId).ConfigureAwait(false))?.Name ?? "",
                    CharacterId = rel.RelatedCharacterId,
                    IsConnected = (await _connectedAccountHttpClient.GetCharacterAsync(rel.RelatedCharacterId, null).ConfigureAwait(false)).Item1 != null,
                    RelationType = rel.RelationType,
                    CharacterRelationId = rel.CharacterRelationId
                });
            }

            return charList;
        }

        public async Task<bool> UnblacklistAsync(Guid id)
        {
            var rel = await _characterRelationDao.FirstOrDefaultAsync(s =>
                (s.CharacterRelationId == id) && (s.RelationType == CharacterRelationType.Blocked)).ConfigureAwait(false);
            if (rel == null)
            {
                return false;
            }
            await _characterRelationDao.TryDeleteAsync(rel.CharacterRelationId).ConfigureAwait(false);
            return true;
        }
    }
}