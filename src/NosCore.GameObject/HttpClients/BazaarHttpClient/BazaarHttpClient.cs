using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using ChickenAPI.Packets.Enumerations;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.BazaarHttpClient
{
    public class BazaarHttpClient : MasterServerHttpClient, IBazaarHttpClient
    {
        public BazaarHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/bazaar";
            RequireConnection = true;
        }

        public List<BazaarLink> GetBazaarLinks(int i, int packetIndex, int pagesize, BazaarListType packetTypeFilter, byte packetSubTypeFilter,
            byte packetLevelFilter, byte packetRareFilter, byte packetUpgradeFilter, long? sellerFilter)
        {
            var client = Connect();
            var response = client.GetAsync($"{ApiUrl}?id={i}&Index={packetIndex}&PageSize={pagesize}&TypeFilter={packetTypeFilter}&SubTypeFilter={packetSubTypeFilter}&LevelFilter={packetLevelFilter}&RareFilter={packetRareFilter}&UpgradeFilter={packetUpgradeFilter}&SellerFilter={sellerFilter}").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<List<BazaarLink>>(response.Content.ReadAsStringAsync().Result);
            }

            throw new ArgumentException();
        }

        public LanguageKey AddBazaar(BazaarRequest bazaarRequest)
        {
            return Post<LanguageKey>(bazaarRequest);
        }

        public BazaarLink GetBazaarLink(long bazaarId)
        {
           return Get<List<BazaarLink>>(bazaarId).FirstOrDefault();
        }

        public bool Remove(long bazaarId, int count, string requestCharacterName)
        {
            var client = Connect();
            var response = client.DeleteAsync($"{ApiUrl}?id={bazaarId}&Count={count}&requestCharacterName={requestCharacterName}").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<bool>(response.Content.ReadAsStringAsync().Result);
            }

            throw new ArgumentException();
        }

        public BazaarLink Modify(long bazaarId, JsonPatchDocument<BazaarLink> patchBz)
        {
            return Patch<BazaarLink>(bazaarId, patchBz);
        }
    }
}
