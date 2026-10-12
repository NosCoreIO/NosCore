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
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub
{
    public class FriendHubClient(IMessageBus bus) : IFriendHub
    {
        public Task<LanguageKey> AddFriendAsync(FriendShipRequest friendPacket) =>
            bus.RequestAsync<LanguageKey>(new AddFriendRequest(friendPacket));

        public Task<List<CharacterRelationStatus>> GetFriendsAsync(long id) =>
            bus.RequestAsync<List<CharacterRelationStatus>>(new GetFriendsRequest(id));

        public Task<bool> DeleteAsync(Guid id) => bus.RequestAsync<bool>(new DeleteFriendRequest(id));
    }
}
