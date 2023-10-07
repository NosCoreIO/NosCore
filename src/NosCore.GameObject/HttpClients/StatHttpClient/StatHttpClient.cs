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

using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NosCore.GameObject.HttpClients.StatHttpClient
{
    public class StatHttpClient(IHttpClientFactory httpClientFactory, IChannelHttpClient channelHttpClient)
        : IStatHttpClient
    {
        private const string ApiUrl = "api/stat";

        public async Task ChangeStatAsync(StatData data, ServerConfiguration item1)
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(item1.ToString());
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await channelHttpClient.GetOrRefreshTokenAsync().ConfigureAwait(false));

            var content = new StringContent(JsonSerializer.Serialize(data, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)),
                Encoding.Default, "application/json");
            await client.PostAsync(ApiUrl, content).ConfigureAwait(false);
        }
    }
}