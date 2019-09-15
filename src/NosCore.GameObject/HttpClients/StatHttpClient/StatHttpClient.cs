using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.WebApi;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace NosCore.GameObject.HttpClients.StatHttpClient
{
    public class StatHttpClient : IStatHttpClient
    {
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        const string ApiUrl = "api/stat";
        public StatHttpClient(IHttpClientFactory httpClientFactory, IChannelHttpClient channelHttpClient)
        {
            _channelHttpClient = channelHttpClient;
            _httpClientFactory = httpClientFactory;
        }

        public void ChangeStat(StatData data, ServerConfiguration item1)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(item1.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelHttpClient.GetOrRefreshToken());

            var content = new StringContent(JsonConvert.SerializeObject(data),
                Encoding.Default, "application/json");
            client.PostAsync(ApiUrl, content);
        }
    }
}
