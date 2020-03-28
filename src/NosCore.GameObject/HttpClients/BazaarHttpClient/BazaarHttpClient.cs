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
using NosCore.Packets.Enumerations;
using Microsoft.AspNetCore.JsonPatch;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using System.Text.Json;
using System.Threading.Tasks;

namespace NosCore.GameObject.HttpClients.BazaarHttpClient
{
    public class BazaarHttpClient : MasterServerHttpClient, IBazaarHttpClient
    {
        public BazaarHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/bazaar";
            RequireConnection = true;
        }

        public async Task<List<BazaarLink>> GetBazaarLinks(int i, int packetIndex, int pagesize, BazaarListType packetTypeFilter,
            byte packetSubTypeFilter,
            byte packetLevelFilter, byte packetRareFilter, byte packetUpgradeFilter, long? sellerFilter)
        {
            var client = await Connect();
            var response = await client
                .GetAsync(
                    $"{ApiUrl}?id={i}&Index={packetIndex}&PageSize={pagesize}&TypeFilter={packetTypeFilter}&SubTypeFilter={packetSubTypeFilter}&LevelFilter={packetLevelFilter}&RareFilter={packetRareFilter}&UpgradeFilter={packetUpgradeFilter}&SellerFilter={sellerFilter}");
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<List<BazaarLink>>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            throw new ArgumentException();
        }

        public Task<LanguageKey?> AddBazaar(BazaarRequest bazaarRequest)
        {
            return Post<LanguageKey?>(bazaarRequest);
        }

        public async Task<BazaarLink?> GetBazaarLink(long bazaarId)
        {
            return (await Get<List<BazaarLink?>>(bazaarId)).FirstOrDefault();
        }

        public async Task<bool> Remove(long bazaarId, int count, string requestCharacterName)
        {
            var client = await Connect();
            var response = await client
                .DeleteAsync($"{ApiUrl}?id={bazaarId}&Count={count}&requestCharacterName={requestCharacterName}");
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<bool>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            throw new ArgumentException();
        }

        public Task<BazaarLink> Modify(long bazaarId, JsonPatchDocument<BazaarLink> patchBz)
        {
            return Patch<BazaarLink>(bazaarId, patchBz);
        }
    }
}