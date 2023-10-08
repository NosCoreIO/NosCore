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

using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NosCore.Core.HttpClients.ChannelHttpClients;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

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

#pragma warning disable CA1056 // Uri properties should not be strings
        public virtual string ApiUrl { get; set; } = "";
#pragma warning restore CA1056 // Uri properties should not be strings
        public virtual bool RequireConnection { get; set; }

        public virtual async Task<HttpClient> ConnectAsync()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication!.ToString());

            if (RequireConnection)
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", await _channelHttpClient.GetOrRefreshTokenAsync().ConfigureAwait(false));
            }

            return client;
        }

        protected async Task<T> PostAsync<T>(object objectToPost)
        {
            var client = await ConnectAsync().ConfigureAwait(false);
            using var content = new StringContent(JsonSerializer.Serialize(objectToPost),
                Encoding.Default, "application/json");
            var response = await client.PostAsync(new Uri($"{client.BaseAddress}{ApiUrl}"), content).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<T>(await response.Content!.ReadAsStringAsync().ConfigureAwait(false),
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)) ?? throw new InvalidOperationException();
            }

            throw new WebException();
        }


        protected async Task<T> PatchAsync<T>(object id, object objectToPost)
        {
            var client = await ConnectAsync().ConfigureAwait(false);
            //todo replace when Json.Net support jsonpatch
            using var content = new StringContent(JsonSerializer.Serialize(objectToPost), Encoding.Default,
                "application/json-patch+json");

            var response = await client.PatchAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={id}"), content).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<T>(await response.Content!.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)) ?? throw new InvalidOperationException();
            }

            throw new WebException();
        }

        protected async Task<HttpResponseMessage> PostAsync(object objectToPost)
        {
            var client = await ConnectAsync().ConfigureAwait(false);
            using var content = new StringContent(JsonSerializer.Serialize(objectToPost, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)),
                Encoding.Default, "application/json");
            return await client.PostAsync(new Uri($"{client.BaseAddress}{ApiUrl}"), content).ConfigureAwait(false);
        }

        [return: MaybeNull]
        protected Task<T> GetAsync<T>()
        {
            return GetAsync<T>(null)!;
        }

        [return: MaybeNull]
        protected async Task<T> GetAsync<T>(object? id)
        {
            var client = await ConnectAsync().ConfigureAwait(false);
            var response = await client.GetAsync(new Uri($"{client.BaseAddress}{ApiUrl}{(id != null ? $"?id={id}" : "")}")).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<T>(await response.Content!.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? throw new InvalidOperationException();
            }

            throw new WebException();
        }

        protected async Task<HttpResponseMessage> DeleteAsync(object id)
        {
            var client = await ConnectAsync().ConfigureAwait(false);
            return await client.DeleteAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={id}")).ConfigureAwait(false);
        }
    }
}