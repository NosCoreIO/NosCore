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
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using Serilog;

namespace NosCore.FriendServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class FriendController : Controller
    {
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao;
        private readonly ILogger _logger;
        private readonly FriendRequestHolder _friendRequestHolder;
        public FriendController(ILogger logger, IGenericDao<CharacterRelationDto> characterRelationDao, FriendRequestHolder friendRequestHolder)
        {
            _logger = logger;
            _characterRelationDao = characterRelationDao;
            _friendRequestHolder = friendRequestHolder;
        }

        [HttpPost]
        public IActionResult AddFriend([FromBody] FriendShipRequest friendPacket)
        {
            ICharacterEntity character = Broadcaster.Instance.GetCharacter(s => s.VisualId == friendPacket.FinsPacket.CharacterId);
            ICharacterEntity targetCharacter = Broadcaster.Instance.GetCharacter(s => s.VisualId == friendPacket.CharacterId);
            if (character != null && targetCharacter != null)
            {
                var relations = _characterRelationDao.Where(s => s.CharacterId == friendPacket.CharacterId).ToList();
                if (relations.Count(s => s.RelationType == CharacterRelationType.Friend) >= 80)
                {
                    character.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIENDLIST_FULL,
                            character.AccountLanguage)
                    });
                    return Ok();
                }

                if (relations.Any(s =>
                    s.RelationType == CharacterRelationType.Blocked &&
                    s.RelatedCharacterId == friendPacket.FinsPacket.CharacterId))
                {
                    character.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                            character.AccountLanguage)
                    });
                    return Ok();
                }

                if (relations.Any(s =>
                    s.RelationType == CharacterRelationType.Friend &&
                    s.RelatedCharacterId == friendPacket.FinsPacket.CharacterId))
                {
                    character.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_FRIEND,
                            character.AccountLanguage)
                    });
                    return Ok();
                }

                if (character.FriendRequestBlocked || targetCharacter.FriendRequestBlocked)
                {
                    character.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_BLOCKED,
                            character.AccountLanguage)
                    });
                    return Ok();
                }

                var friendRequest = _friendRequestHolder.FriendRequestCharacters.Where(s =>
                    s.Value.Item1 == character.VisualId && s.Value.Item2 == targetCharacter.VisualId).ToList();
                if (!friendRequest.Any())
                {
                    character.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_SENT,
                            character.AccountLanguage)
                    });

                    targetCharacter.SendPacket(new DlgPacket
                    {
                        Question = string.Format(
                            Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADD, character.AccountLanguage),
                            character.Name),
                        YesPacket = new FinsPacket
                        { Type = FinsPacketType.Accepted, CharacterId = character.VisualId },
                        NoPacket = new FinsPacket
                        { Type = FinsPacketType.Rejected, CharacterId = character.VisualId }
                    });
                    _friendRequestHolder.FriendRequestCharacters[Guid.NewGuid()] = new Tuple<long,long>(character.VisualId, targetCharacter.VisualId);
                    return Ok();
                }

                switch (friendPacket.FinsPacket.Type)
                {
                    case FinsPacketType.Accepted:
                        character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                character.AccountLanguage)
                        });
                        targetCharacter.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                character.AccountLanguage)
                        });

                        targetCharacter.SendPacket(targetCharacter.GenerateFinit());
                        character.SendPacket(character.GenerateFinit());
                        var data = new CharacterRelationDto
                        {
                            CharacterId = character.VisualId,
                            RelatedCharacterId = targetCharacter.VisualId,
                            RelationType = CharacterRelationType.Friend,
                        };

                        _characterRelationDao.InsertOrUpdate(ref data);
                        break;
                    case FinsPacketType.Rejected:
                        targetCharacter.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REJECTED,
                                character.AccountLanguage)
                        });

                        break;
                    default:
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVITETYPE_UNKNOWN));
                        return NotFound();

                }

                _friendRequestHolder.FriendRequestCharacters.TryRemove(friendRequest.First().Key, out _);

            }
            return NotFound();
        }
    }
}