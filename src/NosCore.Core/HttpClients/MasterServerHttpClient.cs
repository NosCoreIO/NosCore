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
using System.Text.Json;
using NosCore.Core.HttpClients.ChannelHttpClient;

namespace NosCore.Core.HttpClients
{
    public class MasterServerHttpClient
    {
        private readonly Channel _channel;
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IHttpClientFactory _httpClientFactory;

        protected MasterServerHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
        {
            _httpClientFactory = httpClientFactory;
            _channel = channel;
            _channelHttpClient = channelHttpClient;
        }

        public virtual string ApiUrl { get; set; } = "";
        public virtual bool RequireConnection { get; set; }

        protected HttpClient CreateClient()
        {
            return _httpClientFactory.CreateClient();
        }

        public virtual async Task<HttpClient?> Connect()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());

            if (RequireConnection)
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", await _channelHttpClient.GetOrRefreshToken().ConfigureAwait(false));
            }

            return client;
        }

        public async Task<HttpClient?> Connect(int channelId)
        {
            using var client = _httpClientFactory.CreateClient();
            var channel = await _channelHttpClient.GetChannel(channelId).ConfigureAwait(false);
            if (channel == null)
            {
                return null;
            }

            client.BaseAddress = new Uri(channel.WebApi.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channel.Token);
            return client;
        }

        protected async Task<T> Post<T>(object objectToPost)
        {
            var client = await Connect().ConfigureAwait(false);
            using var content = new StringContent(JsonSerializer.Serialize(objectToPost),
                Encoding.Default, "application/json");
            var response = await client.PostAsync(new Uri($"{client.BaseAddress}{ApiUrl}"), content).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
            }

            throw new WebException();
        }


        protected async Task<T> Patch<T>(object id, object objectToPost)
        {
            var client = await Connect().ConfigureAwait(false);
            using var content = new StringContent(JsonSerializer.Serialize(objectToPost),
                Encoding.Default, "application/json");
            var response = await client.PatchAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={id}"), content).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            throw new WebException();
        }

        protected async Task<HttpResponseMessage> Post(object objectToPost)
        {
            var client = await Connect().ConfigureAwait(false);
            using var content = new StringContent(JsonSerializer.Serialize(objectToPost),
                Encoding.Default, "application/json");
            return await client.PostAsync(new Uri($"{client.BaseAddress}{ApiUrl}"), content).ConfigureAwait(false);
        }

        [return: MaybeNull]
        protected async Task<T> Get<T>()
        {
            return await Get<T>(null).ConfigureAwait(false);
        }

        [return: MaybeNull]
        protected async Task<T> Get<T>(object? id)
        {
            var client = await Connect().ConfigureAwait(false);
            var response = await client.GetAsync(new Uri($"{client.BaseAddress}{ApiUrl}{(id != null ? $"?id={id}" : "")}")).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            throw new WebException();
        }

        protected async Task<HttpResponseMessage> Delete(object id)
        {
            var client = await Connect().ConfigureAwait(false);
            return await client.DeleteAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={id}")).ConfigureAwait(false);
        }
    }
}