using System.Net.Http;
using Newtonsoft.Json;
using NosCore.Core.HttpClients.ChannelHttpClient;

namespace NosCore.Core.HttpClients.AuthHttpClient
{
    public class AuthHttpClient : MasterServerHttpClient, IAuthHttpClient
    {
        public AuthHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/auth";
            RequireConnection = true;
        }

        public bool IsAwaitingConnection(string name, string packetPassword, int clientSessionSessionId)
        {
            var client = Connect();
            var response = client.GetAsync($"{ApiUrl}?id={name}&token={packetPassword}&sessionId={clientSessionSessionId}").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<bool>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }
    }
}
