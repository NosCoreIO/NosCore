using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NosCore.Core.HttpClients.ChannelHttpClient;
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

        public void DeleteIncommingMail(int channelId, long id, short mailId, byte postType)
        {
            var client = Connect(channelId);
            client.DeleteAsync($"{ApiUrl}?id={id}&mailId={mailId}&postType={postType}").Wait();
        }

        public void NotifyIncommingMail(int channelId, MailData mailRequest)
        {
            var client = Connect(channelId);
            var content = new StringContent(JsonConvert.SerializeObject(mailRequest), Encoding.Default, "application/json");
            client.PostAsync(ApiUrl, content).Wait();
        }

        public void OpenIncommingMail(int channelId, MailData mailData)
        {
            throw new NotImplementedException();
        }
    }
}
