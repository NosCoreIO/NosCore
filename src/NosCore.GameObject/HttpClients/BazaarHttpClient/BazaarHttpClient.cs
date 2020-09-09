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
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using System.Text.Json;
using System.Threading.Tasks;
using NosCore.Core.HttpClients.ChannelHttpClients;

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

        public async Task<List<BazaarLink>> GetBazaarLinksAsync(int i, int packetIndex, int pagesize, BazaarListType packetTypeFilter,
            byte packetSubTypeFilter,
            byte packetLevelFilter, byte packetRareFilter, byte packetUpgradeFilter, long? sellerFilter)
        {
            var client = await ConnectAsync().ConfigureAwait(false);
            var response = await client
                .GetAsync(
                    $"{ApiUrl}?id={i}&Index={packetIndex}&PageSize={pagesize}&TypeFilter={packetTypeFilter}&SubTypeFilter={packetSubTypeFilter}&LevelFilter={packetLevelFilter}&RareFilter={packetRareFilter}&UpgradeFilter={packetUpgradeFilter}&SellerFilter={sellerFilter}").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (list == null)
                {
                    return new List<BazaarLink>();
                }

                return JsonSerializer.Deserialize<List<BazaarLink>>(list, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new List<BazaarLink>();
            }

            throw new ArgumentException();
        }

        public Task<LanguageKey?> AddBazaarAsync(BazaarRequest bazaarRequest)
        {
            return PostAsync<LanguageKey?>(bazaarRequest);
        }

        public async Task<BazaarLink?> GetBazaarLinkAsync(long bazaarId)
        {
            return (await GetAsync<List<BazaarLink?>>(bazaarId)!.ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<bool> RemoveAsync(long bazaarId, int count, string requestCharacterName)
        {
            var client = await ConnectAsync().ConfigureAwait(false);
            var response = await client
                .DeleteAsync($"{ApiUrl}?id={bazaarId}&Count={count}&requestCharacterName={requestCharacterName}").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<bool>(await response.Content.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            throw new ArgumentException();
        }

        public Task<BazaarLink> ModifyAsync(long bazaarId, JsonPatchDocument<BazaarLink> patchBz)
        {
            return PatchAsync<BazaarLink>(bazaarId, patchBz);
        }
    }
}