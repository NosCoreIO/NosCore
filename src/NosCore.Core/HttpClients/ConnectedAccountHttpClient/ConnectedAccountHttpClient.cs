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
using NosCore.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
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

        public async Task Disconnect(long connectedCharacterId)
        {
            await Delete(connectedCharacterId).ConfigureAwait(false);
        }

        public async Task<Tuple<ServerConfiguration?, ConnectedAccount?>> GetCharacter(long? characterId, string? characterName)
        {
            foreach (var channel in (await _channelHttpClient.GetChannels().ConfigureAwait(false)).Where(c => c.Type == ServerType.WorldServer))
            {
                var accounts = await GetConnectedAccount(channel).ConfigureAwait(false);
                var target = accounts.FirstOrDefault(s =>
                    (s.ConnectedCharacter.Name == characterName) || (s.ConnectedCharacter.Id == characterId));

                if (target != null)
                {
                    return new Tuple<ServerConfiguration?, ConnectedAccount?>(channel.WebApi, target);
                }
            }

            return new Tuple<ServerConfiguration?, ConnectedAccount?>(null, null);
        }

        public async Task<List<ConnectedAccount>> GetConnectedAccount(ChannelInfo channel)
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
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
            }

            return new List<ConnectedAccount>();
        }
    }
}