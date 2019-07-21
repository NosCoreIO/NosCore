using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using ChickenAPI.Packets.Enumerations;
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

        public List<BazaarLink> GetBazaarLinks(long bazaarId)
        {
           return Get<List<BazaarLink>>(bazaarId);
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
    }
}
