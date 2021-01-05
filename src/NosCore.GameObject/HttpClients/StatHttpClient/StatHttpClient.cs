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
    public class StatHttpClient : IStatHttpClient
    {
        private const string ApiUrl = "api/stat";
        private readonly IHttpClientFactory _httpClientFactory;

        public StatHttpClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task ChangeStatAsync(StatData data, ServerConfiguration item1)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(item1.ToString());

            var content = new StringContent(JsonSerializer.Serialize(data),
                Encoding.Default, "application/json");
            await client.PostAsync(ApiUrl, content).ConfigureAwait(false);
        }
    }
}