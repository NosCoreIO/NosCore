using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.BlacklistHttpClient
{
    public class BlacklistHttpClient : NoscoreHttpClient, IBlacklistHttpClient
    {
        public BlacklistHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient) 
            : base(httpClientFactory, channel, channelHttpClient)
        {

        }

        public List<CharacterRelationStatus> GetBlackLists(long characterVisualId)
        {
            var client = Connect();
            var response = client.GetAsync($"api/blacklist?id={characterVisualId}").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<List<CharacterRelationStatus>>(response.Content.ReadAsStringAsync().Result);
            }

            throw new ArgumentException();
        }

        public LanguageKey AddToBlacklist(BlacklistRequest blacklistRequest)
        {
            var client = Connect();
            var content = new StringContent(JsonConvert.SerializeObject(blacklistRequest),
                Encoding.Default, "application/json");
            var response = client.PostAsync("api/blacklist", content).Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<LanguageKey>(response.Content.ReadAsStringAsync().Result);
            }

            throw new ArgumentException();
        }

        public void DeleteFromBlacklist(Guid characterRelationId)
        {
            var client = Connect();
            client.DeleteAsync($"api/blacklist?id={characterRelationId}");
        }
    }
}
