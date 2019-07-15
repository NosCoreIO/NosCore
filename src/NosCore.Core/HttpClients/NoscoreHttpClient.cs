using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using NosCore.Core.HttpClients.ChannelHttpClient;

namespace NosCore.Core.HttpClients
{
    public abstract class NoscoreHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Channel _channel;
        private readonly IChannelHttpClient _channelHttpClient;

        public virtual string ApiUrl { get; set; }

        protected NoscoreHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
        {
            _httpClientFactory = httpClientFactory;
            _channel = channel;
            _channelHttpClient = channelHttpClient;
        }

        public virtual HttpClient Connect()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelHttpClient.GetOrRefreshToken());
            return client;
        }
    }
}
