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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using Polly;
using Serilog;

namespace NosCore.Core.HttpClients.ChannelHttpClient
{
    public class ChannelHttpClient : IChannelHttpClient
    {
        private readonly Channel _channel;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private DateTime _lastUpdateToken;
        private string? _token;

        public ChannelHttpClient(IHttpClientFactory httpClientFactory, Channel channel, ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _channel = channel;
            _logger = logger;
        }

        public void Connect()
        {
            using var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());

            using var content = new StringContent(JsonConvert.SerializeObject(_channel),
                Encoding.Default, "application/json");

            var message = Policy
                .Handle<Exception>()
                .OrResult<HttpResponseMessage>(mess => !mess.IsSuccessStatusCode)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (_, __, timeSpan) =>
                        _logger.Error(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_RETRY),
                            timeSpan.TotalSeconds))
                ).ExecuteAsync(() => client.PostAsync(new Uri($"{client.BaseAddress}api/channel"), content));

            var result =
                JsonConvert.DeserializeObject<ConnectionInfo>(message.Result.Content.ReadAsStringAsync().Result);
            _token = result.Token;
            _lastUpdateToken = SystemTime.Now();
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
            using var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetOrRefreshToken());
            using var content = new StringContent(JsonConvert.SerializeObject(SystemTime.Now()), Encoding.Default,
                "application/json");

            var postResponse = client
                .PatchAsync(new Uri($"{client.BaseAddress}api/channel?id=" + MasterClientListSingleton.Instance.ChannelId ?? ""), content).Result;
            if (postResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<HttpStatusCode>(postResponse.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(postResponse.Headers.ToString());
        }

        public string GetOrRefreshToken()
        {
            if (_lastUpdateToken.AddMinutes(25) < SystemTime.Now())
            {
                using var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                string password;
                switch (_channel.MasterCommunication.HashingType)
                {
                    case HashingType.BCrypt:
                        password = _channel.MasterCommunication.Password.ToBcrypt(_channel.MasterCommunication.Salt);
                        break;
                    case HashingType.Pbkdf2:
                        password = _channel.MasterCommunication.Password.ToPbkdf2Hash(_channel.MasterCommunication
                            .Salt);
                        break;
                    case HashingType.Sha512:
                    default:
                        password = _channel.MasterCommunication.Password.ToSha512();
                        break;
                }

                var keyByteArray = Encoding.Default.GetBytes(password);
                var signinKey = new SymmetricSecurityKey(keyByteArray);
                var handler = new JwtSecurityTokenHandler();
                var claims = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "Server"),
                    new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
                });
                var securityToken = handler.CreateToken(new SecurityTokenDescriptor
                {
                    Subject = claims,
                    Issuer = "Issuer",
                    Audience = "Audience",
                    SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
                });
                _channel.Token = handler.WriteToken(securityToken);
                using var content = new StringContent(JsonConvert.SerializeObject(_channel),
                    Encoding.Default, "application/json");
                var message = client.PutAsync(new Uri($"{client.BaseAddress}api/channel"), content);
                var result =
                    JsonConvert.DeserializeObject<ConnectionInfo>(message.Result.Content.ReadAsStringAsync().Result);
                _token = result.Token;
                _lastUpdateToken = SystemTime.Now();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SECURITY_TOKEN_UPDATED));
            }

            return _token;
        }

        public List<ChannelInfo> GetChannels()
        {
            var channels = MasterClientListSingleton.Instance.Channels;
            if (!MasterClientListSingleton.Instance.Channels.Any())
            {
                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", GetOrRefreshToken());

                var response = client.GetAsync(new Uri($"{_channel.MasterCommunication}/api/channel")).Result;
                if (response.IsSuccessStatusCode)
                {
                    channels = JsonConvert.DeserializeObject<List<ChannelInfo>>(response.Content.ReadAsStringAsync()
                        .Result);
                }
            }

            return channels;
        }

        public ChannelInfo GetChannel(int channelId)
        {
            var channels = MasterClientListSingleton.Instance.Channels;
            if (!MasterClientListSingleton.Instance.Channels.Any())
            {
                using var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_channel.MasterCommunication.ToString());
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", GetOrRefreshToken());

                var response = client.GetAsync(new Uri($"{_channel.MasterCommunication}/api/channel?id={channelId}")).Result;
                if (response.IsSuccessStatusCode)
                {
                    channels = JsonConvert.DeserializeObject<List<ChannelInfo>>(response.Content.ReadAsStringAsync()
                        .Result);
                }
            }

            return channels.FirstOrDefault(s => s.Id == channelId);
        }
    }
}