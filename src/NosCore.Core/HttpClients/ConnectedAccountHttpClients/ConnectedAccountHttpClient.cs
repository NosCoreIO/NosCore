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
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.Rpc;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClients
{
    public class ConnectedAccountHttpClient : IConnectedAccountHttpClient
    {
        private readonly HubConnection _hubConnection;
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IOptions<WorldConfiguration> _worldConfOptions;
        private readonly SecurityTokenProvider _securityTokenProvider;

        public ConnectedAccountHttpClient(HubConnection hubConnection, IChannelHttpClient channelHttpClient, IOptions<WorldConfiguration> worldConfOptions, SecurityTokenProvider securityTokenProvider)
        {
            _hubConnection = hubConnection;
            _channelHttpClient = channelHttpClient;
            _worldConfOptions = worldConfOptions;
            _securityTokenProvider = securityTokenProvider;
        }

        public async Task DisconnectAsync(long connectedCharacterId)
        {
            await _hubConnection.SendAsync("Kick", connectedCharacterId);
        }

        public async Task<Tuple<ServerConfiguration?, ConnectedAccount?>> GetCharacterAsync(long? characterId, string? characterName)
        {
            var characters = new Tuple<ServerConfiguration?, ConnectedAccount?>(null, null);
            var channels = await _channelHttpClient.GetChannelsAsync();
            foreach (var channel in channels)
            {
                var hub = new HubConnectionBuilder()
                    .WithUrl($"{channel.WebApi}/{nameof(WorldHub)}",
                        options => options.AccessTokenProvider = () => Task.FromResult(_securityTokenProvider.GenerateSecurityToken(_worldConfOptions.Value.MasterCommunication.Password!, _worldConfOptions.Value.MasterCommunication.Salt)))
                    .Build();
                var result = await hub.InvokeAsync<Tuple<ServerConfiguration?, ConnectedAccount?>?>("GetCharacter", characterId, characterName);
                if (result != null)
                {
                    return result;
                }
            }
            return characters;
        }

        public async Task<List<ConnectedAccount>> GetConnectedAccountAsync(ChannelInfo? channel)
        {
            var result = new List<ConnectedAccount>();
            var channels = await _channelHttpClient.GetChannelsAsync();
            if (channel != null)
            {
                channels = channels.Where(o => o.Id == channel.Id).ToList();
            }
            foreach (var chan in channels)
            {
                var hub = new HubConnectionBuilder()
                    .WithUrl($"{chan.WebApi}/{nameof(WorldHub)}",
                        options => options.AccessTokenProvider = () => Task.FromResult(_securityTokenProvider.GenerateSecurityToken(_worldConfOptions.Value.MasterCommunication.Password!, _worldConfOptions.Value.MasterCommunication.Salt)))
                    .Build();
                result.AddRange(await hub.InvokeAsync<List<ConnectedAccount>>("GetCharacters"));
            }

            return result;
        }
    }
}