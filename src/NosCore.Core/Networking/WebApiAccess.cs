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
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using Polly;
using Serilog;

namespace NosCore.Core.Networking
{
    public class WebApiAccess : IWebApiAccess
    {
        private readonly ILogger _logger;

        public WebApiAccess(ILogger logger)
        {
            _logger = logger;
        }

        private Uri BaseAddress { get; set; }

        public string Token { get; set; }

        public StringContent Content { get; private set; }

        public void RegisterBaseAdress(Channel channel)
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
            var name = WebApiRoute.Channel.ToString();
            var message = Policy
                .Handle<Exception>()
                .OrResult<HttpResponseMessage>(mess => !mess.IsSuccessStatusCode)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (_, __, timeSpan) =>
                        _logger.Error(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_RETRY),
                            timeSpan.TotalSeconds))
                ).ExecuteAsync(() => client.PostAsync($"api/{char.ToLowerInvariant(name[0]) + name.Substring(1)}", Content));

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
                    ).Execute(() => Patch<HttpStatusCode>(WebApiRoute.Channel,
                        result.ChannelInfo.ChannelId, SystemTime.Now()));
                _logger.Error(
                    LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_PING_FAILED));
                Environment.Exit(0);
            });
        }

        public T Delete<T>(WebApiRoute route, ServerConfiguration webApi) => Delete<T>(route, webApi, null);

        public T Delete<T>(WebApiRoute route, object id) => Delete<T>(route, null, id);

        public T Delete<T>(WebApiRoute route) => Delete<T>(route, null, null);

        public T Delete<T>(WebApiRoute route, ServerConfiguration webApi, object id)
        {
            var client = new HttpClient
            {
                BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString())
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var name = route.ToString();
            var response = client.DeleteAsync($"api/{char.ToLowerInvariant(name[0]) + name.Substring(1)}?id=" + id ?? "").Result;
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
            var client = new HttpClient { BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString()) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var name = route.ToString();
            var response = client.GetAsync($"api/{char.ToLowerInvariant(name[0]) + name.Substring(1)}?id=" + id ?? "").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }

        public T Post<T>(WebApiRoute route, object data) => Post<T>(route, data, null);

        public T Post<T>(WebApiRoute route, object data, ServerConfiguration webApi)
        {
            var client = new HttpClient
            {
                BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString())
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, "application/json");
            var name = route.ToString();
            var postResponse = client.PostAsync($"api/{char.ToLowerInvariant(name[0]) + name.Substring(1)}", content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public T Put<T>(WebApiRoute route, object data) => Put<T>(route, data, null);

        public T Put<T>(WebApiRoute route, object data, ServerConfiguration webApi)
        {
            var client = new HttpClient
            {
                BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString())
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, "application/json");
            var name = route.ToString();
            var postResponse = client.PutAsync($"api/{char.ToLowerInvariant(name[0]) + name.Substring(1)}", content).Result;
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
            var client = new HttpClient
            {
                BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString())
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, "application/json");
            var name = route.ToString();
            var postResponse = client.PatchAsync($"api/{char.ToLowerInvariant(name[0]) + name.Substring(1)}?id=" + id ?? "", content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public void BroadcastPacket(PostedPacket packet, int channelId)
        {
            var channel = Get<List<ChannelInfo>>(WebApiRoute.Channel, channelId).FirstOrDefault();
            if (channel != null)
            {
                Post<PostedPacket>(WebApiRoute.Packet, packet, channel.WebApi);
            }
        }

        public void BroadcastPacket(PostedPacket packet)
        {
            foreach (var channel in Get<List<ChannelInfo>>(WebApiRoute.Channel)
                ?.Where(c => c.Type == ServerType.WorldServer) ?? new List<ChannelInfo>())
            {
                Post<PostedPacket>(WebApiRoute.Packet, packet, channel.WebApi);
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

        public (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName)
        {
            var channels = MasterClientListSingleton.Instance.Channels ?? Get<List<ChannelInfo>>(WebApiRoute.Channel);
            foreach (var channel in (channels ?? new List<ChannelInfo>()).Where(c => c.Type == ServerType.WorldServer))
            {
                var accounts = Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, channel.WebApi);

                var target = accounts.FirstOrDefault(s => s.ConnectedCharacter.Name == characterName || s.ConnectedCharacter.Id == characterId);

                if (target != null)
                {
                    return (channel.WebApi, target);
                }
            }

            return (null, null);
        }
    }
}