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
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using Serilog;

namespace NosCore.GameObject.Services.FriendService
{
    public class FriendService : IFriendService
    {
        private readonly IDao<CharacterDto, long> _characterDao;
        private readonly IDao<CharacterRelationDto, Guid> _characterRelationDao;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly FriendRequestHolder _friendRequestHolder;
        private readonly ILogger _logger;

        public FriendService(ILogger logger, IDao<CharacterRelationDto, Guid> characterRelationDao,
            IDao<CharacterDto, long> characterDao, FriendRequestHolder friendRequestHolder,
            IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            _logger = logger;
            _characterRelationDao = characterRelationDao;
            _characterDao = characterDao;
            _friendRequestHolder = friendRequestHolder;
            _connectedAccountHttpClient = connectedAccountHttpClient;
        }

        public async Task<LanguageKey> AddFriendAsync(FriendShipRequest friendPacket)
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

                var relations = _characterRelationDao.Where(s => s.CharacterId == friendPacket.CharacterId)?.ToList() ?? new List<CharacterRelationDto>();
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

                        await _characterRelationDao.TryInsertOrUpdateAsync(data).ConfigureAwait(false);
                        var data2 = new CharacterRelationDto
                        {
                            CharacterId = targetCharacter.Item2.ConnectedCharacter.Id,
                            RelatedCharacterId = character.Item2.ConnectedCharacter.Id,
                            RelationType = CharacterRelationType.Friend
                        };

                        await _characterRelationDao.TryInsertOrUpdateAsync(data2).ConfigureAwait(false);
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

        public async Task<List<CharacterRelationStatus>> GetFriendsAsync(long id)
        {
            var charList = new List<CharacterRelationStatus>();
            var list = _characterRelationDao
                .Where(s => (s.CharacterId == id) && (s.RelationType != CharacterRelationType.Blocked));
            if (list == null)
            {
                return charList;
            }
            foreach (var rel in list)
            {
                charList.Add(new CharacterRelationStatus
                {
                    CharacterName = (await _characterDao.FirstOrDefaultAsync(s => s.CharacterId == rel.RelatedCharacterId).ConfigureAwait(false))?.Name,
                    CharacterId = rel.RelatedCharacterId,
                    IsConnected = (await _connectedAccountHttpClient.GetCharacterAsync(rel.RelatedCharacterId, null).ConfigureAwait(false)).Item1 != null,
                    RelationType = rel.RelationType,
                    CharacterRelationId = rel.CharacterRelationId
                });
            }

            return charList;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var rel = await _characterRelationDao.FirstOrDefaultAsync(s =>
                (s.CharacterRelationId == id) && (s.RelationType == CharacterRelationType.Friend)).ConfigureAwait(false);
            if (rel == null)
            {
                return false;
            }
            var rel2 = await _characterRelationDao.FirstOrDefaultAsync(s =>
                (s.CharacterId == rel.RelatedCharacterId) && (s.RelatedCharacterId == rel.CharacterId) &&
                (s.RelationType == CharacterRelationType.Friend)).ConfigureAwait(false);
            if (rel2 == null)
            {
                return false;
            }
            await _characterRelationDao.TryDeleteAsync(rel.CharacterRelationId).ConfigureAwait(false);
            await _characterRelationDao.TryDeleteAsync(rel2.CharacterRelationId).ConfigureAwait(false);
            return true;
        }
    }
}