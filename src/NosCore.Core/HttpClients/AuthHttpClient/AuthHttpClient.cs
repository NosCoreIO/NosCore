using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NosCore.Core.HttpClients.ChannelHttpClient;

namespace NosCore.Core.HttpClients.AuthHttpClient
{
    public class AuthHttpClient : NoscoreHttpClient, IAuthHttpClient
    {
        public AuthHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {

        }

        public bool IsAwaitingConnection(string name, string packetPassword, int clientSessionSessionId)
        {
            var client = Connect();
            var response = client.GetAsync($"api/auth?id={name}&token={packetPassword}&sessionId={clientSessionSessionId}").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<bool>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }
    }
}
