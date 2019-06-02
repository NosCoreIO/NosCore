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
using ChickenAPI.Packets.Enumerations;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using Serilog;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class FriendController : Controller
    {
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao;
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly ILogger _logger;
        private readonly IWebApiAccess _webApiAccess;
        private readonly FriendRequestHolder _friendRequestHolder;

        public FriendController(ILogger logger, IGenericDao<CharacterRelationDto> characterRelationDao,
            IGenericDao<CharacterDto> characterDao, FriendRequestHolder friendRequestHolder, IWebApiAccess webApiAccess)
        {
            _logger = logger;
            _characterRelationDao = characterRelationDao;
            _characterDao = characterDao;
            _friendRequestHolder = friendRequestHolder;
            _webApiAccess = webApiAccess;
        }


        [HttpPost]
        public IActionResult AddFriend([FromBody] FriendShipRequest friendPacket)
        {
            var character = _webApiAccess.GetCharacter(friendPacket.CharacterId, null);
            var targetCharacter = _webApiAccess.GetCharacter(friendPacket.FinsPacket.CharacterId, null);
            var friendRequest = _friendRequestHolder.FriendRequestCharacters.Where(s =>
              s.Value.Item1 == character.Item2.ConnectedCharacter.Id && s.Value.Item2 == targetCharacter.Item2.ConnectedCharacter.Id).ToList();
            if (character.Item2 != null && targetCharacter.Item2 != null)
            {
                if (character.Item2.ChannelId != targetCharacter.Item2.ChannelId)
                {
                    throw new ArgumentException();
                }

                var relations = _characterRelationDao.Where(s => s.CharacterId == friendPacket.CharacterId).ToList();
                if (relations.Count(s => s.RelationType == CharacterRelationType.Friend) >= 80)
                {
                    return Ok(LanguageKey.FRIENDLIST_FULL);
                }

                if (relations.Any(s =>
                    s.RelationType == CharacterRelationType.Blocked &&
                    s.RelatedCharacterId == friendPacket.FinsPacket.CharacterId))
                {
                    return Ok(LanguageKey.BLACKLIST_BLOCKED);
                }

                if (relations.Any(s =>
                    s.RelationType == CharacterRelationType.Friend &&
                    s.RelatedCharacterId == friendPacket.FinsPacket.CharacterId))
                {
                    return Ok(LanguageKey.ALREADY_FRIEND);
                }

                if (character.Item2.ConnectedCharacter.FriendRequestBlocked || targetCharacter.Item2.ConnectedCharacter.FriendRequestBlocked)
                {
                    return Ok(LanguageKey.FRIEND_REQUEST_BLOCKED);
                }
          
                if (!friendRequest.Any())
                {
                    _friendRequestHolder.FriendRequestCharacters[Guid.NewGuid()] = new Tuple<long, long>(character.Item2.ConnectedCharacter.Id, targetCharacter.Item2.ConnectedCharacter.Id);
                    return Ok(LanguageKey.FRIEND_REQUEST_SENT);
                }

                switch (friendPacket.FinsPacket.Type)
                {
                    case FinsPacketType.Accepted:
                        var data = new CharacterRelationDto
                        {
                            CharacterId = character.Item2.ConnectedCharacter.Id,
                            RelatedCharacterId = targetCharacter.Item2.ConnectedCharacter.Id,
                            RelationType = CharacterRelationType.Friend,
                        };

                        _characterRelationDao.InsertOrUpdate(ref data);
                        var data2 = new CharacterRelationDto
                        {
                            CharacterId = targetCharacter.Item2.ConnectedCharacter.Id,
                            RelatedCharacterId = character.Item2.ConnectedCharacter.Id,
                            RelationType = CharacterRelationType.Friend,
                        };

                        _characterRelationDao.InsertOrUpdate(ref data2);
                        _friendRequestHolder.FriendRequestCharacters.TryRemove(friendRequest.First().Key, out _);
                        return Ok(LanguageKey.FRIEND_ADDED);
                    case FinsPacketType.Rejected:
                        _friendRequestHolder.FriendRequestCharacters.TryRemove(friendRequest.First().Key, out _);
                        return Ok(LanguageKey.FRIEND_REJECTED);
                    default:
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVITETYPE_UNKNOWN));
                        _friendRequestHolder.FriendRequestCharacters.TryRemove(friendRequest.First().Key, out _);
                        throw new ArgumentException();

                }

            }

            _friendRequestHolder.FriendRequestCharacters.TryRemove(friendRequest.First().Key, out _);
            throw new ArgumentException();
        }

        [HttpGet]
        public List<CharacterRelationStatus> GetFriends(long id)
        {
            var charList = new List<CharacterRelationStatus>();
            var list = _characterRelationDao
                .Where(s => s.CharacterId == id && s.RelationType != CharacterRelationType.Blocked);
            foreach (var rel in list)
            {
                charList.Add(new CharacterRelationStatus
                {
                    CharacterName = _characterDao.FirstOrDefault(s => s.CharacterId == rel.RelatedCharacterId).Name,
                    CharacterId = rel.RelatedCharacterId,
                    IsConnected = _webApiAccess.GetCharacter(rel.RelatedCharacterId, null).Item1 != null,
                    RelationType = rel.RelationType,
                    CharacterRelationId = rel.CharacterRelationId,
                });
            }
            return charList;
        }

        [HttpDelete]
        public IActionResult Delete(Guid id)
        {
            var rel = _characterRelationDao.FirstOrDefault(s => s.CharacterRelationId == id && s.RelationType == CharacterRelationType.Friend);
            _characterRelationDao.Delete(rel);
            return Ok();
        }
    }
}