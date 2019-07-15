using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using Polly;
using Serilog;

namespace NosCore.Core.HttpClients.ChannelHttpClient
{
    public class ChannelHttpClient: IChannelHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Channel _channel;
        private readonly ILogger _logger;
        private string _token;
        public ChannelHttpClient(IHttpClientFactory httpClientFactory, Channel channel, ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _channel = channel;
            _logger = logger;
        }

        public void Connect()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());

            var content = new StringContent(JsonConvert.SerializeObject(_channel),
                Encoding.Default, "application/json");
          
            var message = Policy
                .Handle<Exception>()
                .OrResult<HttpResponseMessage>(mess => !mess.IsSuccessStatusCode)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (_, __, timeSpan) =>
                        _logger.Error(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_RETRY),
                            timeSpan.TotalSeconds))
                ).ExecuteAsync(() => client.PostAsync($"api/channel", content));

            var result =
                JsonConvert.DeserializeObject<ConnectionInfo>(message.Result.Content.ReadAsStringAsync().Result);
            _token = result.Token;
            _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.REGISTRED_ON_MASTER));
            MasterClientListSingleton.Instance.ChannelId = result.ChannelInfo.ChannelId;
            Task.Run(() =>
            {
                Policy
                    .HandleResult<HttpStatusCode>(ping => ping == HttpStatusCode.OK)
                    .WaitAndRetryForever(retryAttempt => TimeSpan.FromSeconds(1),
                        (_, __, timeSpan) =>
                            _logger.Verbose(
                                LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_PING))
                    ).Execute(Ping);
                _logger.Error(
                    LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_PING_FAILED));
                Environment.Exit(0);
            });
        }

        public HttpStatusCode Ping()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetOrRefreshToken());
            var content = new StringContent(JsonConvert.SerializeObject(SystemTime.Now()), Encoding.Default, "application/json");

            var postResponse = client.PatchAsync($"api/channel?id=" + MasterClientListSingleton.Instance.ChannelId ?? "", content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<HttpStatusCode>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public string GetOrRefreshToken()
        {
            //todo refresh before end
            return _token;
        }

        public List<ChannelInfo> GetChannels()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetOrRefreshToken());
            var channels = MasterClientListSingleton.Instance.Channels;
            if (!MasterClientListSingleton.Instance.Channels.Any())
            {
                var response = client.GetAsync($"api/channel").Result;
                if (response.IsSuccessStatusCode)
                {
                    channels = JsonConvert.DeserializeObject<List<ChannelInfo>>(response.Content.ReadAsStringAsync().Result);
                }
            }

            return channels;
        }
    }
}
