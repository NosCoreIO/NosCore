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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClients
{
    public class ConnectedAccountHttpClient : MasterServerHttpClient, IConnectedAccountHttpClient
    {
        private readonly IChannelHttpClient _channelHttpClient;

        public ConnectedAccountHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            _channelHttpClient = channelHttpClient;
            ApiUrl = "api/connectedAccount";
            RequireConnection = true;
        }

        public Task DisconnectAsync(long connectedCharacterId)
        {
            return DeleteAsync(connectedCharacterId);
        }

        public async Task<Tuple<ServerConfiguration?, ConnectedAccount?>> GetCharacterAsync(long? characterId, string? characterName)
        {
            foreach (var channel in (await _channelHttpClient.GetChannelsAsync().ConfigureAwait(false)).Where(c => c.Type == ServerType.WorldServer))
            {
                var accounts = await GetConnectedAccountAsync(channel).ConfigureAwait(false);
                var target = accounts.FirstOrDefault(s =>
                    (s.ConnectedCharacter?.Name == characterName) || (s.ConnectedCharacter?.Id == characterId));

                if (target != null)
                {
                    return new Tuple<ServerConfiguration?, ConnectedAccount?>(channel.WebApi, target);
                }
            }

            return new Tuple<ServerConfiguration?, ConnectedAccount?>(null, null);
        }

        public async Task<List<ConnectedAccount>> GetConnectedAccountAsync(ChannelInfo channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            using var client = CreateClient();
            client.BaseAddress = new Uri(channel.WebApi?.ToString() ?? "");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channel.Token);

            var response = await client.GetAsync(new Uri($"{client.BaseAddress}{ApiUrl}")).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<List<ConnectedAccount>>(
                    await response.Content!.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })!;
            }

            return new List<ConnectedAccount>();
        }
    }
}