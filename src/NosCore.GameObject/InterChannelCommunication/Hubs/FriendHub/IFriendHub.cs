//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;

public interface IFriendHub
{
    Task<LanguageKey> AddFriendAsync(FriendShipRequest friendPacket);
    Task<List<CharacterRelationStatus>> GetFriendsAsync(long id);
    Task<bool> DeleteAsync(Guid id);
}
