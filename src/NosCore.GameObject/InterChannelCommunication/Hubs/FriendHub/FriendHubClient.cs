//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub
{
    public class FriendHubClient(HubConnectionFactory hubConnectionFactory, ILogger logger)
        : BaseHubClient(hubConnectionFactory, nameof(FriendHub), logger), IFriendHub
    {
        public Task<LanguageKey> AddFriendAsync(FriendShipRequest friendPacket) =>
            InvokeAsync<LanguageKey>(nameof(AddFriendAsync), friendPacket);

        public Task<List<CharacterRelationStatus>> GetFriendsAsync(long id) =>
            InvokeAsync<List<CharacterRelationStatus>>(nameof(GetFriendsAsync), id);

        public Task<bool> DeleteAsync(Guid id) =>
            InvokeAsync<bool>(nameof(DeleteAsync), id);
    }
}
