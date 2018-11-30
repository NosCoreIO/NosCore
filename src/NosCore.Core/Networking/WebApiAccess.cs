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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Data.WebApi;

namespace NosCore.Core.Networking
{
    public class WebApiAccess
    {
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

        public static void RegisterBaseAdress() => RegisterBaseAdress(null,null);
        public static void RegisterBaseAdress(string address, string token)
        {
            if (address == null)
            {
                BaseAddress = new Uri("http://localhost");
                return;
            }

            BaseAddress = new Uri(address);
            Content = new StringContent(JsonConvert.SerializeObject(new WebApiToken { ServerToken = token }),
                Encoding.Default, "application/json");
        }

        private void AssignToken(ref HttpClient client)
        {
            if (Token == null)
            {
                HttpResponseMessage response = client.PostAsync("api/token/connectserver", Content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(response.Headers.ToString());
                }

                Token = response.Content.ReadAsStringAsync().Result;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
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
            AssignToken(ref client);
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

            var client = new HttpClient {BaseAddress = webApi == null ? BaseAddress : new Uri(webApi.ToString())};
            AssignToken(ref client);
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
            AssignToken(ref client);
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
            AssignToken(ref client);
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
            AssignToken(ref client);
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
            var channel = Instance.Get<List<WorldServerInfo>>("api/channels", channelId).FirstOrDefault();
            if (channel != null)
            {
                Instance.Post<PostedPacket>("api/packet", packet, channel.WebApi);
            }
        }

        public void BroadcastPacket(PostedPacket packet)
        {
            foreach (var channel in Instance.Get<List<WorldServerInfo>>("api/channels"))
            {
                Instance.Post<PostedPacket>("api/packet", packet, channel.WebApi);
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