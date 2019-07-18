using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public class ConnectedAccountHttpClient : MasterServerHttpClient, IConnectedAccountHttpClient
    {
        private readonly IChannelHttpClient _channelHttpClient;
        public ConnectedAccountHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            _channelHttpClient = channelHttpClient;
            ApiUrl = "api/connectedAccount";
            RequireConnection = true;
        }

        public void Disconnect(long connectedCharacterId)
        {
            Delete(connectedCharacterId);
        }

        public (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName)
        {
            foreach (var channel in _channelHttpClient.GetChannels().Where(c => c.Type == ServerType.WorldServer))
            {
                List<ConnectedAccount> accounts = GetConnectedAccount(channel);
                var target = accounts.FirstOrDefault(s => s.ConnectedCharacter.Name == characterName || s.ConnectedCharacter.Id == characterId);

                if (target != null)
                {
                    return (channel.WebApi, target);
                }
            }

            return (null, null);
        }

        public List<ConnectedAccount> GetConnectedAccount(ChannelInfo channel)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(channel.WebApi.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channel.Token);

            var response = client.GetAsync($"api/connectedAccount").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<List<ConnectedAccount>>(response.Content.ReadAsStringAsync().Result);
            }

            return new List<ConnectedAccount>();
        }
    }
}
