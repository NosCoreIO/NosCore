using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.FriendHttpClient
{
    public class FriendHttpClient : NoscoreHttpClient, IFriendHttpClient
    {
        public FriendHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {

        }

        public LanguageKey AddFriend(FriendShipRequest friendShipRequest)
        {
            var client = Connect();
            var content = new StringContent(JsonConvert.SerializeObject(friendShipRequest),
                Encoding.Default, "application/json");
            var response = client.PostAsync("api/friend", content).Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<LanguageKey>(response.Content.ReadAsStringAsync().Result);
            }

            throw new ArgumentException();
        }

        public List<CharacterRelationStatus> GetListFriends(long visualEntityVisualId)
        {
            var client = Connect();
            var response = client.GetAsync($"api/friend?id={visualEntityVisualId}").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<List<CharacterRelationStatus>>(response.Content.ReadAsStringAsync().Result);
            }

            throw new ArgumentException();
        }

        public void Delete(Guid characterRelationId)
        {
            var client = Connect();
            client.DeleteAsync($"api/friend?id={characterRelationId}");
        }
    }
}
