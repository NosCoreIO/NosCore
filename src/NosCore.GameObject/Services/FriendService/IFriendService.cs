//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FriendService
{
    public interface IFriendService
    {
        Task<LanguageKey> AddFriendAsync(long characterId, long secondCharacterId, FinsPacketType friendsPacketType);

        Task<List<CharacterRelationStatus>> GetFriendsAsync(long id);

        Task<bool> DeleteAsync(Guid id);
    }
}
