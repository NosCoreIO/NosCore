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
    public class ConnectedAccountHttpClient : NoscoreHttpClient, IConnectedAccountHttpClient
    {
        private readonly IChannelHttpClient _channelHttpClient;
        public ConnectedAccountHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient) 
            : base(httpClientFactory, channel, channelHttpClient)
        {
            _channelHttpClient = channelHttpClient;
        }

        public void Disconnect(long connectedCharacterId)
        {
            var client = Connect();
            client.DeleteAsync($"api/blacklist?id={connectedCharacterId}");
        }

        public (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName)
        {
            foreach (var channel in _channelHttpClient.GetChannels().Where(c => c.Type == ServerType.WorldServer))
            {
                List<ConnectedAccount> accounts = GetConnectedAccount(channel.WebApi);
                var target = accounts.FirstOrDefault(s => s.ConnectedCharacter.Name == characterName || s.ConnectedCharacter.Id == characterId);

                if (target != null)
                {
                    return (channel.WebApi, target);
                }
            }

            return (null, null);
        }

        public List<ConnectedAccount> GetConnectedAccount(ServerConfiguration serverWebApi)
        {
            var client = Connect();
            var response = client.GetAsync($"api/connectedAccount").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<List<ConnectedAccount>>(response.Content.ReadAsStringAsync().Result);
            }

            return new List<ConnectedAccount>();
        }
    }
}
