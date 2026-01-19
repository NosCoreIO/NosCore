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
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using Serilog;

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
