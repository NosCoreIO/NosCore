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
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using Serilog;

namespace NosCore.FriendServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class BlacklistController : Controller
    {
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao;
        private readonly ILogger _logger;
        public BlacklistController(ILogger logger, IGenericDao<CharacterRelationDto> characterRelationDao)
        {
            _logger = logger;
            _characterRelationDao = characterRelationDao;
        }

        [HttpPost]
        public IActionResult AddBlacklist([FromBody] BlacklistRequest blacklistRequest)
        {
            ICharacterEntity character = Broadcaster.Instance.GetCharacter(s => s.VisualId == blacklistRequest.CharacterId);
            if (character != null)
            {
                var relations = _characterRelationDao.Where(s => s.CharacterId == blacklistRequest.CharacterId).ToList();
                if (relations.Any(s => s.RelatedCharacterId == blacklistRequest.BlInsPacket.CharacterId && s.RelationType != CharacterRelationType.Blocked))
                {
                    character.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_BLOCK_FRIEND,
                            character.AccountLanguage)
                    });
                    return Ok();
                }

                if (relations.Any(s => s.RelatedCharacterId == blacklistRequest.BlInsPacket.CharacterId && s.RelationType == CharacterRelationType.Blocked))
                {
                    character.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_BLACKLISTED,
                            character.AccountLanguage)
                    });
                    return Ok();
                }
                character.SendPacket(character.GenerateBlinit());
                var data = new CharacterRelationDto
                {
                    CharacterId = character.VisualId,
                    RelatedCharacterId = character.VisualId,
                    RelationType = CharacterRelationType.Blocked,
                };

                _characterRelationDao.InsertOrUpdate(ref data);
                character.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_ADDED,
                        character.AccountLanguage)
                });
            }
            return NotFound();
        }

        [HttpGet]
        public List<CharacterRelation> GetBlacklisted(long characterId)
        {
            var list = _characterRelationDao
                .Where(s => s.CharacterId == characterId && s.RelationType == CharacterRelationType.Blocked).Adapt<List<CharacterRelation>>();
            foreach (var rel in list)
            {
                rel.CharacterName = "TODO";
            }
            return list;
        }
    }
}