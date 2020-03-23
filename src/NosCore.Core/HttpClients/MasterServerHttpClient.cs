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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NosCore.Core.HttpClients.ChannelHttpClient;

namespace NosCore.Core.HttpClients
{
    public class MasterServerHttpClient
    {
        private readonly Channel _channel;
        private readonly IChannelHttpClient _channelHttpClient;
        protected readonly IHttpClientFactory _httpClientFactory;

        protected MasterServerHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
        {
            _httpClientFactory = httpClientFactory;
            _channel = channel;
            _channelHttpClient = channelHttpClient;
        }

        public virtual string ApiUrl { get; set; } = "";
        public virtual bool RequireConnection { get; set; }

        public virtual HttpClient Connect()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());

            if (RequireConnection)
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _channelHttpClient.GetOrRefreshToken());
            }

            return client;
        }

        public HttpClient Connect(int channelId)
        {
            using var client = _httpClientFactory.CreateClient();
            var channel = _channelHttpClient.GetChannel(channelId);
            if (channel == null)
            {
                return null;
            }

            client.BaseAddress = new Uri(channel.WebApi.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channel.Token);
            return client;
        }

        protected T Post<T>(object objectToPost)
        {
            var client = Connect();
            using var content = new StringContent(JsonConvert.SerializeObject(objectToPost),
                Encoding.Default, "application/json");
            var response = client.PostAsync(new Uri($"{client.BaseAddress}{ApiUrl}"), content).Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new WebException();
        }


        protected T Patch<T>(object id, object objectToPost)
        {
            var client = Connect();
            using var content = new StringContent(JsonConvert.SerializeObject(objectToPost),
                Encoding.Default, "application/json");
            var response = client.PatchAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={id}"), content).Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new WebException();
        }

        protected Task Post(object objectToPost)
        {
            var client = Connect();
            using var content = new StringContent(JsonConvert.SerializeObject(objectToPost),
                Encoding.Default, "application/json");
            return client.PostAsync(new Uri($"{client.BaseAddress}{ApiUrl}"), content);
        }

        [return: MaybeNull]
        protected T Get<T>()
        {
            return Get<T>(null);
        }

        [return: MaybeNull]
        protected T Get<T>(object? id)
        {
            var client = Connect();
            var response = client.GetAsync(new Uri($"{client.BaseAddress}{ApiUrl}{(id != null ? $"?id={id}" : "")}")).Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            }

            throw new WebException();
        }

        protected Task Delete(object id)
        {
            var client = Connect();
            return client.DeleteAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={id}"));
        }
    }
}