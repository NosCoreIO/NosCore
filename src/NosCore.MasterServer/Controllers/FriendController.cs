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
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.MasterServer.DataHolders;
using Serilog;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class FriendController : Controller
    {
        private readonly IGenericDao<CharacterDto> _characterDao;
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly FriendRequestHolder _friendRequestHolder;
        private readonly ILogger _logger;

        public FriendController(ILogger logger, IGenericDao<CharacterRelationDto> characterRelationDao,
            IGenericDao<CharacterDto> characterDao, FriendRequestHolder friendRequestHolder,
            IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            _logger = logger;
            _characterRelationDao = characterRelationDao;
            _characterDao = characterDao;
            _friendRequestHolder = friendRequestHolder;
            _connectedAccountHttpClient = connectedAccountHttpClient;
        }


        [HttpPost]
        public async Task<LanguageKey> AddFriend([FromBody] FriendShipRequest friendPacket)
        {
            var character = await _connectedAccountHttpClient.GetCharacterAsync(friendPacket.CharacterId, null).ConfigureAwait(false);
            var targetCharacter = await _connectedAccountHttpClient.GetCharacterAsync(friendPacket.FinsPacket?.CharacterId, null).ConfigureAwait(false);
            var friendRequest = _friendRequestHolder.FriendRequestCharacters.Where(s =>
                (s.Value.Item2 == character.Item2?.ConnectedCharacter?.Id) &&
                (s.Value.Item1 == targetCharacter.Item2?.ConnectedCharacter?.Id)).ToList();
            if ((character.Item2 != null) && (targetCharacter.Item2 != null))
            {
                if (character.Item2.ChannelId != targetCharacter.Item2.ChannelId)
                {
                    throw new ArgumentException();
                }

                var relations = _characterRelationDao.Where(s => s.CharacterId == friendPacket.CharacterId).ToList();
                if (relations.Count(s => s.RelationType == CharacterRelationType.Friend) >= 80)
                {
                    return LanguageKey.FRIENDLIST_FULL;
                }

                if (relations.Any(s =>
                    (s.RelationType == CharacterRelationType.Blocked) &&
                    (s.RelatedCharacterId == friendPacket.FinsPacket!.CharacterId)))
                {
                    return LanguageKey.BLACKLIST_BLOCKED;
                }

                if (relations.Any(s =>
                    (s.RelationType == CharacterRelationType.Friend) &&
                    (s.RelatedCharacterId == friendPacket.FinsPacket!.CharacterId)))
                {
                    return LanguageKey.ALREADY_FRIEND;
                }

                if (character.Item2.ConnectedCharacter!.FriendRequestBlocked ||
                    targetCharacter.Item2.ConnectedCharacter!.FriendRequestBlocked)
                {
                    return LanguageKey.FRIEND_REQUEST_BLOCKED;
                }

                if (!friendRequest.Any())
                {
                    _friendRequestHolder.FriendRequestCharacters[Guid.NewGuid()] =
                        new Tuple<long, long>(character.Item2.ConnectedCharacter.Id,
                            targetCharacter.Item2.ConnectedCharacter.Id);
                    return LanguageKey.FRIEND_REQUEST_SENT;
                }

                switch (friendPacket.FinsPacket!.Type)
                {
                    case FinsPacketType.Accepted:
                        var data = new CharacterRelationDto
                        {
                            CharacterId = character.Item2.ConnectedCharacter.Id,
                            RelatedCharacterId = targetCharacter.Item2.ConnectedCharacter.Id,
                            RelationType = CharacterRelationType.Friend
                        };

                        _characterRelationDao.InsertOrUpdate(ref data);
                        var data2 = new CharacterRelationDto
                        {
                            CharacterId = targetCharacter.Item2.ConnectedCharacter.Id,
                            RelatedCharacterId = character.Item2.ConnectedCharacter.Id,
                            RelationType = CharacterRelationType.Friend
                        };

                        _characterRelationDao.InsertOrUpdate(ref data2);
                        _friendRequestHolder.FriendRequestCharacters.TryRemove(friendRequest.First().Key, out _);
                        return LanguageKey.FRIEND_ADDED;
                    case FinsPacketType.Rejected:
                        _friendRequestHolder.FriendRequestCharacters.TryRemove(friendRequest.First().Key, out _);
                        return LanguageKey.FRIEND_REJECTED;
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
        public async Task<List<CharacterRelationStatus>> GetFriends(long id)
        {
            var charList = new List<CharacterRelationStatus>();
            var list = _characterRelationDao
                .Where(s => (s.CharacterId == id) && (s.RelationType != CharacterRelationType.Blocked));
            foreach (var rel in list)
            {
                charList.Add(new CharacterRelationStatus
                {
                    CharacterName = _characterDao.FirstOrDefault(s => s.CharacterId == rel.RelatedCharacterId)?.Name,
                    CharacterId = rel.RelatedCharacterId,
                    IsConnected = (await _connectedAccountHttpClient.GetCharacterAsync(rel.RelatedCharacterId, null).ConfigureAwait(false)).Item1 != null,
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
                (s.CharacterRelationId == id) && (s.RelationType == CharacterRelationType.Friend));
            if (rel == null)
            {
                return NotFound();
            }
            var rel2 = _characterRelationDao.FirstOrDefault(s =>
                (s.CharacterId == rel.RelatedCharacterId) && (s.RelatedCharacterId == rel.CharacterId) &&
                (s.RelationType == CharacterRelationType.Friend));
            if (rel2 == null)
            {
                return NotFound();
            }
            _characterRelationDao.Delete(rel);
            _characterRelationDao.Delete(rel2);
            return Ok();
        }
    }
}