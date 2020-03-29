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
using NosCore.Packets.Enumerations;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class BlacklistController : Controller
    {
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;

        public BlacklistController(IConnectedAccountHttpClient connectedAccountHttpClient,
            IGenericDao<CharacterRelationDto> characterRelationDao, IGenericDao<CharacterDto> characterDao)
        {
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _characterRelationDao = characterRelationDao;
            _characterDao = characterDao;
        }

        [HttpPost]
        public async Task<LanguageKey> AddBlacklist([FromBody] BlacklistRequest blacklistRequest)
        {
            var character = await _connectedAccountHttpClient.GetCharacter(blacklistRequest.CharacterId, null);
            var targetCharacter = await
                _connectedAccountHttpClient.GetCharacter(blacklistRequest.BlInsPacket?.CharacterId, null);
            if ((character.Item2 != null) && (targetCharacter.Item2 != null))
            {
                var relations = _characterRelationDao.Where(s => s.CharacterId == blacklistRequest.CharacterId)
                    .ToList();
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

                _characterRelationDao.InsertOrUpdate(ref data);
                return LanguageKey.BLACKLIST_ADDED;
            }

            throw new ArgumentException();
        }

        [HttpGet]
        public async Task<List<CharacterRelationStatus>> GetBlacklisted(long id)
        {
            var charList = new List<CharacterRelationStatus>();
            var list = _characterRelationDao
                .Where(s => (s.CharacterId == id) && (s.RelationType == CharacterRelationType.Blocked));
            foreach (var rel in list)
            {
                charList.Add(new CharacterRelationStatus
                {
                    CharacterName = _characterDao.FirstOrDefault(s => s.CharacterId == rel.RelatedCharacterId)?.Name ?? "",
                    CharacterId = rel.RelatedCharacterId,
                    IsConnected = (await _connectedAccountHttpClient.GetCharacter(rel.RelatedCharacterId, null)).Item1 != null,
                    RelationType = rel.RelationType,
                    CharacterRelationId = rel.CharacterRelationId
                });
            }

            return charList;
        }

        [HttpDelete]
        public IActionResult Delete(Guid id)
        {
            var rel = _characterRelationDao.FirstOrDefault(s =>
                (s.CharacterRelationId == id) && (s.RelationType == CharacterRelationType.Blocked));
            if (rel == null)
            {
                return NotFound();
            }
            _characterRelationDao.Delete(rel);
            return Ok();
        }
    }
}