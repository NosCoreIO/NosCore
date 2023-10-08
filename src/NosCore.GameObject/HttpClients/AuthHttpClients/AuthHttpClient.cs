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
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Mapster;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.AuthHttpClients
{
    public class AuthHttpClient : MasterServerHttpClient, IAuthHttpClient
    {
        public AuthHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/auth";
            RequireConnection = true;
        }

        public async Task<string?> GetAwaitingConnectionAsync(string? name, string packetPassword,
            int clientSessionSessionId)
        {
            var client = await ConnectAsync().ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }
            var response = await client
                .GetAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={name}&token={packetPassword}&sessionId={clientSessionSessionId}"))
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(response.Headers.ToString());
            }

            var result = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            return result.Adapt<string?>();

        }

        public async Task SetAwaitingConnectionAsync(long sessionId, string accountName)
        {
            var client = await ConnectAsync().ConfigureAwait(false);
            if (client == null)
            {
                return;
            }

            var intent = new AuthIntent()
            {
                SessionId = -1,
                AccountName = accountName,
            };
            using var content = new StringContent(JsonSerializer.Serialize(intent, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)), Encoding.Default,
                "application/json");
            await client.PostAsync(new Uri($"{client.BaseAddress}{ApiUrl}"), content).ConfigureAwait(false);
        }
    }
}