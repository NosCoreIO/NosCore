using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public class IncommingMailHttpClient : MasterServerHttpClient, IIncommingMailHttpClient
    {
        private readonly IChannelHttpClient _channelHttpClient;
        public IncommingMailHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/incommingMail";
            RequireConnection = true;
            _channelHttpClient = channelHttpClient;
        }

        public void NotifyIncommingMail(int channelId, MailData mailRequest)
        {
            var client = _httpClientFactory.CreateClient();
            var channel = _channelHttpClient.GetChannel(channelId);
            if(channel == null)
            {
                return;
            }
            client.BaseAddress = new Uri(channel.WebApi.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channel.Token);
            var content = new StringContent(JsonConvert.SerializeObject(mailRequest), Encoding.Default, "application/json");
            client.PostAsync(ApiUrl, content).Wait();
        }
    }
}
