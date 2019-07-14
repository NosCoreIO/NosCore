using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using NosCore.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.ConnectedAccountHttpClient;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public class ConnectedAccountHttpClient : IConnectedAccountHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Channel _channel;
        private readonly IChannelHttpClient _channelHttpClient;

        public ConnectedAccountHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
        {
            _httpClientFactory = httpClientFactory;
            _channel = channel;
            _channelHttpClient = channelHttpClient;
        }

        public void Disconnect(ServerConfiguration receiverItem1, long connectedCharacterId)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelHttpClient.GetOrRefreshToken());
        }

        public (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelHttpClient.GetOrRefreshToken());
            //var channels = MasterClientListSingleton.Instance.Channels ?? Get<List<ChannelInfo>>(WebApiRoute.Channel);
            //foreach (var channel in (channels ?? new List<ChannelInfo>()).Where(c => c.Type == ServerType.WorldServer))
            //{
            //    var accounts = Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, channel.WebApi);

            //    var target = accounts.FirstOrDefault(s => s.ConnectedCharacter.Name == characterName || s.ConnectedCharacter.Id == characterId);

            //    if (target != null)
            //    {
            //        return (channel.WebApi, target);
            //    }
            //}

            return (null, null);
        }

        public List<ConnectedAccount> GetConnectedAccount(ServerConfiguration serverWebApi)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(serverWebApi.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelHttpClient.GetOrRefreshToken());
        }
    }
}
