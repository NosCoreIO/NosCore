//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Data.WebApi;
using NosCore.Shared.I18N;
using Polly;
using Serilog;

namespace NosCore.Core.Networking
{
    public class WebApiAccess
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private WebApiAccess()
        {
            if (BaseAddress == null)
            {
                throw new ArgumentNullException(nameof(BaseAddress));
            }
        }
        private static WebApiAccess _instance;

        private static Uri BaseAddress { get; set; }

        private static string Token { get; set; }

        public Dictionary<string, object> MockValues { get; set; } = new Dictionary<string, object>();

        public static WebApiAccess Instance => _instance ?? (_instance = new WebApiAccess());

        public static StringContent Content { get; private set; }

        public static void RegisterBaseAdress() => RegisterBaseAdress(null);
        public static void RegisterBaseAdress(Channel channel)
        {
            if (string.IsNullOrEmpty(channel?.MasterCommunication?.ToString()))
            {
                BaseAddress = new Uri("http://localhost");
                return;
            }

            BaseAddress = new Uri(channel.MasterCommunication.ToString());
            Content = new StringContent(JsonConvert.SerializeObject(channel),
                Encoding.Default, "application/json");
            var client = new HttpClient
            {
                BaseAddress = BaseAddress
            };

            var message = Policy
                 .Handle<Exception>()
                 .OrResult<HttpResponseMessage>(mess => !mess.IsSuccessStatusCode)
                 .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                     (_, __, timeSpan) =>
                         _logger.Error(string.Format(
                             LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_RETRY),
                             timeSpan.TotalSeconds))
                 ).ExecuteAsync(() => client.PostAsync(WebApiRoutes.ChannelRoute, Content));

            var result = JsonConvert.DeserializeObject<ConnectionInfo>(message.Result.Content.ReadAsStringAsync().Result);
            Token = result.Token;
            _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LanguageKey.REGISTRED_ON_MASTER));
            MasterClientListSingleton.Instance.ChannelId = result.ChannelInfo.ChannelId;
            Task.Run(() =>
            {
                Policy
                    .HandleResult<HttpStatusCode>(ping => ping == HttpStatusCode.OK)
                    .WaitAndRetryForever(retryAttempt => TimeSpan.FromSeconds(5),
                        (_, __, timeSpan) =>
                            _logger.Verbose(
                                LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_PING))
                    ).Execute(() => Instance.Patch<HttpStatusCode>(WebApiRoutes.ChannelRoute,
                        result.ChannelInfo.ChannelId));
                _logger.Error(
                    LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_PING_FAILED));
                Environment.Exit(0);
            });
        }

        public T Delete<T>(string route, ServerConfiguration webApi) => Delete<T>(route, webApi, null);

        public T Delete<T>(string route, object id) => Delete<T>(route, null, id);

        public T Delete<T>(string route) => Delete<T>(route, null, null);

        public T Delete<T>(string route, ServerConfiguration webApi, object id)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T)MockValues[route];
            }

            var client = new HttpClient
            {
                BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString())
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var response = client.DeleteAsync(route + "?id=" + id ?? "").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }

        public T Get<T>(string route, object id) => Get<T>(route, null, id);

        public T Get<T>(string route, ServerConfiguration webApi) => Get<T>(route, webApi, null);

        public T Get<T>(string route) => Get<T>(route, null, null);

        public T Get<T>(string route, ServerConfiguration webApi, object id)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T)MockValues[route];
            }

            var client = new HttpClient { BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString()) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var response = client.GetAsync(route + "?id=" + id ?? "").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }

        public T Post<T>(string route, ServerConfiguration webApi) => Post<T>(route, null, webApi);

        public T Post<T>(string route, object data) => Post<T>(route, data, null);

        public T Post<T>(string route, object data, ServerConfiguration webApi)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T)MockValues[route];
            }

            var client = new HttpClient
            {
                BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString())
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, "application/json");
            var postResponse = client.PostAsync(route, content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public T Put<T>(string route, ServerConfiguration webApi) => Put<T>(route, null, webApi);

        public T Put<T>(string route, object data) => Put<T>(route, data, null);

        public T Put<T>(string route, object data, ServerConfiguration webApi)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T)MockValues[route];
            }

            var client = new HttpClient
            {
                BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString())
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, "application/json");
            var postResponse = client.PutAsync(route, content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public T Patch<T>(string route, ServerConfiguration webApi) => Patch<T>(route, null, webApi);

        public T Patch<T>(string route, object data) => Patch<T>(route, data, null);

        public T Patch<T>(string route, object data, ServerConfiguration webApi)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T)MockValues[route];
            }

            var client = new HttpClient
            {
                BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString())
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, "application/json");
            var postResponse = client.PatchAsync(route, content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public void BroadcastPacket(PostedPacket packet, int channelId)
        {
            var channel = Instance.Get<List<ChannelInfo>>(WebApiRoutes.ChannelRoute, channelId).FirstOrDefault();
            if (channel != null)
            {
                Instance.Post<PostedPacket>(WebApiRoutes.PostedPacketRoute, packet, channel.WebApi);
            }
        }

        public void BroadcastPacket(PostedPacket packet)
        {
            foreach (var channel in Instance.Get<List<ChannelInfo>>(WebApiRoutes.ChannelRoute))
            {
                Instance.Post<PostedPacket>(WebApiRoutes.PostedPacketRoute, packet, channel.WebApi);
            }
        }

        public void BroadcastPackets(List<PostedPacket> packets)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet);
            }
        }

        public void BroadcastPackets(List<PostedPacket> packets, int channelId)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet, channelId);
            }
        }
    }
}