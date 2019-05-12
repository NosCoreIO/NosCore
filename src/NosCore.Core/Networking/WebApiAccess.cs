//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL.Common.Request;
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.GraphQL;
using NosCore.Data.WebApi;
using Polly;
using Serilog;

namespace NosCore.Core.Networking
{
    public class WebApiAccess
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private static WebApiAccess _instance;

        private WebApiAccess()
        {
            if (BaseAddress == null)
            {
                throw new ArgumentNullException(nameof(BaseAddress));
            }
        }

        public static Dictionary<WebApiRoute, string> WebApiRoutes { get; set; }

        private static Uri BaseAddress { get; set; }

        private static string Token { get; set; }

        public Dictionary<WebApiRoute, object> MockValues { get; set; } = new Dictionary<WebApiRoute, object>();

        public static WebApiAccess Instance => _instance ?? (_instance = new WebApiAccess());

        public static StringContent Content { get; private set; }

        public T Delete<T>(WebApiRoute route, ServerConfiguration webApi) => Delete<T>(route, webApi, null);

        public T Delete<T>(WebApiRoute route, object id) => Delete<T>(route, null, id);

        public T Delete<T>(WebApiRoute route) => Delete<T>(route, null, null);

        public T Delete<T>(WebApiRoute route, ServerConfiguration webApi, object id)
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
            var response = client.DeleteAsync(WebApiRoutes[route] + "?id=" + id ?? "").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }

        public T Get<T>(WebApiRoute route, object id) => Get<T>(route, null, id);

        public T Get<T>(WebApiRoute route, ServerConfiguration webApi) => Get<T>(route, webApi, null);

        public T Get<T>(WebApiRoute route) => Get<T>(route, null, null);

        public T Get<T>(WebApiRoute route, ServerConfiguration webApi, object id)
        {
            if (MockValues.ContainsKey(route))
            {
                return (T)MockValues[route];
            }

            var client = new HttpClient { BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString()) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var response = client.GetAsync(WebApiRoutes[route] + "?id=" + id ?? "").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }

        public T Post<T>(WebApiRoute route, ServerConfiguration webApi) => Post<T>(route, null, webApi);

        public T Post<T>(WebApiRoute route, object data) => Post<T>(route, data, null);

        public T Post<T>(WebApiRoute route, object data, ServerConfiguration webApi)
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
            var postResponse = client.PostAsync(WebApiRoutes[route], content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public T Put<T>(WebApiRoute route, ServerConfiguration webApi) => Put<T>(route, null, webApi);

        public T Put<T>(WebApiRoute route, object data) => Put<T>(route, data, null);

        public T Put<T>(WebApiRoute route, object data, ServerConfiguration webApi)
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
            var postResponse = client.PutAsync(WebApiRoutes[route], content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public T Patch<T>(WebApiRoute route, object id, ServerConfiguration webApi) =>
            Patch<T>(route, id, null, webApi);

        public T Patch<T>(WebApiRoute route, object id, object data) => Patch<T>(route, id, data, null);

        public T Patch<T>(WebApiRoute route, object id, object data, ServerConfiguration webApi)
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
            var postResponse = client.PatchAsync(WebApiRoutes[route] + "?id=" + id ?? "", content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        //TODO move to graphql files after
        public void BroadcastPacket(PostedPacket packet, int channelId)
        {
            var channelRequest = new GraphQLRequest
            {
                Query =
                    $"{{ channels(id:\"{channelId}\") {{ id, name, host, port, connectedAccountLimit, webApi {{host, port, language}}, lastPing, type  }} }}"
            };
            var graphQlClient = new GraphQLHttpClient($"{BaseAddress}/graphql");
            var graphQlResponse = graphQlClient.SendQueryAsync(channelRequest).Result;
            var channels = graphQlResponse.GetDataFieldAs<List<ChannelInfo>>("channels");
            var channel = channels.FirstOrDefault();
            if (channel != null)
            {
                Instance.Post<PostedPacket>(WebApiRoute.Packet, packet, channel.WebApi);
            }
        }

        public void BroadcastPacket(PostedPacket packet)
        {
            var channelRequest = new GraphQLRequest
            {
                Query =
                        $"{{ channels {{ id, name, host, port, connectedAccountLimit, webApi {{host, port, language}}, lastPing, type  }} }}"
            };
            var graphQlClient = new GraphQLHttpClient($"{BaseAddress}/graphql");
            var graphQlResponse = graphQlClient.SendQueryAsync(channelRequest).Result;
            var channels = graphQlResponse.GetDataFieldAs<List<ChannelInfo>>("channels");

            foreach (var channel in channels.Where(c => c.Type == ServerType.WorldServer))
            {
                Instance.Post<PostedPacket>(WebApiRoute.Packet, packet, channel.WebApi);
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
        public static void RegisterBaseAdress() => RegisterBaseAdress(null);

        public static void RegisterBaseAdress(Channel channel)
        {
            if (string.IsNullOrEmpty(channel?.MasterCommunication?.ToString()))
            {
                BaseAddress = new Uri("http://localhost");
                return;
            }

            WebApiRoutes = channel.MasterCommunication.Routes;
            BaseAddress = new Uri(channel.MasterCommunication.ToString());
            Content = new StringContent(JsonConvert.SerializeObject(channel),
                Encoding.Default, "application/json");
            var client = new HttpClient
            {
                BaseAddress = BaseAddress
            };
            var channelRequest = new GraphQLRequest
            {
                Query =
                    $"{{ channels {{ id, name, host, port, connectedAccountLimit, webApi {{host, port, language}}, lastPing, type  }} }}"
            };
            var graphQlClient = new GraphQLHttpClient($"{BaseAddress}/graphql");
            var graphQlResponse = graphQlClient.SendQueryAsync(channelRequest).Result;

            var message = Policy
                .Handle<Exception>()
                .OrResult<HttpResponseMessage>(mess => !mess.IsSuccessStatusCode)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (_, __, timeSpan) =>
                        _logger.Error(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_RETRY),
                            timeSpan.TotalSeconds))
                ).ExecuteAsync(() => client.PostAsync(WebApiRoutes[WebApiRoute.Channel], Content));

            var result =
                JsonConvert.DeserializeObject<ConnectionInfo>(message.Result.Content.ReadAsStringAsync().Result);
            Token = result.Token;
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
                    ).Execute(() => Instance.Patch<HttpStatusCode>(WebApiRoute.Channel,
                        result.ChannelInfo.ChannelId, SystemTime.Now()));
                _logger.Error(
                    LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_PING_FAILED));
                Environment.Exit(0);
            });
        }
    }
}