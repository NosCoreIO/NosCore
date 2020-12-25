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
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClients
{
    public class ConnectedAccountHttpClient: IConnectedAccountHttpClient
    {
        private readonly HubConnection _hubConnection;

        public ConnectedAccountHttpClient(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;
        }

        public Task DisconnectAsync(long connectedCharacterId)
        {
            return _hubConnection.SendAsync("Kick", connectedCharacterId);
        }

        public Task<Tuple<ServerConfiguration?, ConnectedAccount?>> GetCharacterAsync(long? characterId, string? characterName)
        {
            return _hubConnection.InvokeAsync<Tuple<ServerConfiguration?, ConnectedAccount?>>("GetCharacter", characterId, characterName);
        }

        public Task<List<ConnectedAccount>> GetConnectedAccountAsync(ChannelInfo channel)
        {
            return _hubConnection.InvokeAsync<List<ConnectedAccount>>("GetCharacters", channel);
        }
    }
}