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
using Microsoft.AspNetCore.SignalR.Client;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub
{
    public class FriendHubClient(HubConnectionFactory hubConnectionFactory) : IFriendHub
    {
        private readonly HubConnection _hubConnection = hubConnectionFactory.Create(nameof(FriendHub));

        public async Task<LanguageKey> AddFriendAsync(FriendShipRequest friendPacket)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<LanguageKey>(nameof(AddFriendAsync), friendPacket);
            await _hubConnection.StopAsync();
            return result;
        }
        public async Task<List<CharacterRelationStatus>> GetFriendsAsync(long id)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<List<CharacterRelationStatus>>(nameof(GetFriendsAsync), id);
            await _hubConnection.StopAsync();
            return result;
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<bool>(nameof(DeleteAsync), id);
            await _hubConnection.StopAsync();
            return result;
        }
    }
}