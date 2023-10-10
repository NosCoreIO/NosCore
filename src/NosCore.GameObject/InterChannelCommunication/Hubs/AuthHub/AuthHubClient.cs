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

using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub
{
    public class AuthHubClient(HubConnectionFactory hubConnectionFactory) : IAuthHub
    {
        private readonly HubConnection _hubConnection = hubConnectionFactory.Create(nameof(AuthHub));

        public async Task<string?> GetAwaitingConnectionAsync(string? name, string? packetPassword, int clientSessionSessionId)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<string?>(nameof(GetAwaitingConnectionAsync), name, packetPassword, clientSessionSessionId);
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task SetAwaitingConnectionAsync(long sessionId, string accountName)
        {
            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync(nameof(SetAwaitingConnectionAsync), sessionId, accountName);
            await _hubConnection.StopAsync();
        }
    }
}