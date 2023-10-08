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

using Json.More;
using Json.Patch;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NosCore.Core.HttpClients.ChannelHttpClients
{
    public class ChannelHttpClient(IHttpClientFactory httpClientFactory, Channel channel, ILogger logger, IClock clock,
            IIdService<ChannelInfo> channelIdService, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : IChannelHttpClient
    {
        private Instant _lastUpdateToken;
        private string? _token;

        public async Task ConnectAsync()
        {
            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(channel.MasterCommunication!.ToString());

            using var content = new StringContent(JsonSerializer.Serialize(channel, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)),
                Encoding.Default, "application/json");

            var message = Policy
                .Handle<Exception>()
                .OrResult<HttpResponseMessage>(mess => !mess.IsSuccessStatusCode)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (_, __, timeSpan) =>
                        logger.Error(
                            logLanguage[LogLanguageKey.MASTER_SERVER_RETRY],
                            timeSpan.TotalSeconds)
                ).ExecuteAsync(() => client.PostAsync(new Uri($"{client.BaseAddress}api/channel"), content));

            var result =
                JsonSerializer.Deserialize<ConnectionInfo>(await (await message.ConfigureAwait(false)).Content.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
            _token = result?.Token;
            _lastUpdateToken = clock.GetCurrentInstant();
            logger.Debug(logLanguage[LogLanguageKey.REGISTRED_ON_MASTER]);
            channel.ChannelId = result?.ChannelInfo?.ChannelId ?? 0;

            await Policy
                .HandleResult<HttpStatusCode>(ping => ping == HttpStatusCode.OK)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(1),
                    (_, __, timeSpan) =>
                        logger.Verbose(
                            logLanguage[LogLanguageKey.MASTER_SERVER_PING])
                ).ExecuteAsync(() =>
                {
                    var jsonPatch = new JsonPatch(PatchOperation.Replace(Json.Pointer.JsonPointer.Parse("/LastPing"), JsonDocument.Parse(JsonSerializer.Serialize(clock.GetCurrentInstant(), new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))).RootElement.AsNode()));
                    return PatchAsync(channel.ChannelId, jsonPatch);
                }).ConfigureAwait(false);
            logger.Error(
                logLanguage[LogLanguageKey.MASTER_SERVER_PING_FAILED]);
            Environment.Exit(0);
        }

        public async Task<HttpStatusCode> PatchAsync(long channelId, JsonPatch patch)
        {
            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(channel.MasterCommunication!.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetOrRefreshTokenAsync().ConfigureAwait(false));
            //todo replace when System.Text.Json support jsonpatch
            using var content = new StringContent(JsonSerializer.Serialize(patch, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)), Encoding.Default,
                "application/json-patch+json");

            var postResponse = await client
                .PatchAsync(new Uri($"{client.BaseAddress}api/channel?id={channelId}"), content).ConfigureAwait(false);
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<HttpStatusCode>(await postResponse.Content.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))!;
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public async Task<string?> GetOrRefreshTokenAsync()
        {
            if (_lastUpdateToken.Plus(Duration.FromMinutes(25)) >= clock.GetCurrentInstant())
            {
                return _token;
            }

            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(channel.MasterCommunication!.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            using var content = new StringContent(JsonSerializer.Serialize(channel, new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)),
                Encoding.Default, "application/json");
            var message = client.PutAsync(new Uri($"{client.BaseAddress}api/channel"), content);
            var result =
                JsonSerializer.Deserialize<ConnectionInfo>(await (await message.ConfigureAwait(false)).Content.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
            _token = result?.Token;
            _lastUpdateToken = clock.GetCurrentInstant();
            logger.Information(logLanguage[LogLanguageKey.SECURITY_TOKEN_UPDATED]);

            return _token;
        }

        public async Task<List<ChannelInfo>> GetChannelsAsync()
        {
            var channels = channelIdService.Items.Values.ToList();
            if (channels.Any())
            {
                return channels;
            }

            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await GetOrRefreshTokenAsync().ConfigureAwait(false));

            var response = await client.GetAsync(new Uri($"{channel.MasterCommunication}/api/channel")).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var chan = JsonSerializer.Deserialize<List<ChannelInfo>>(await response.Content.ReadAsStringAsync()
                    , new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
                if (chan != null)
                {
                    channels = chan;
                    return channels;
                }
            }

            throw new HttpRequestException();
        }

        public async Task<ChannelInfo?> GetChannelAsync(long channelId)
        {
            var channels = channelIdService.Items.Values.ToList();
            if (channels.Any())
            {
                return channels?.FirstOrDefault(s => s.Id == channelId);
            }

            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(channel.MasterCommunication!.ToString());
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await GetOrRefreshTokenAsync().ConfigureAwait(false));

            var response = await client.GetAsync(new Uri($"{channel.MasterCommunication}/api/channel?id={channelId}")).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                channels = JsonSerializer.Deserialize<List<ChannelInfo>>(await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                    , new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
            }

            return channels?.FirstOrDefault(s => s.Id == channelId);
        }
    }
}