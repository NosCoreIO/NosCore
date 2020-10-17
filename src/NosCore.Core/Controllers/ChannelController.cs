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
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using Serilog;

namespace NosCore.Core.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.Root)]
    public class ChannelController : Controller
    {
        private readonly IOptions<WebApiConfiguration> _apiConfiguration;
        private readonly ILogger _logger;
        private int _id;

        public ChannelController(IOptions<WebApiConfiguration> apiConfiguration, ILogger logger)
        {
            _logger = logger;
            _apiConfiguration = apiConfiguration;
        }

        private string GenerateToken()
        {
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Server"),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            });
            var password = _apiConfiguration.Value.HashingType switch
            {
                HashingType.BCrypt => _apiConfiguration.Value.Password!.ToBcrypt(_apiConfiguration.Value.Salt ?? ""),
                HashingType.Pbkdf2 => _apiConfiguration.Value.Password!.ToPbkdf2Hash(_apiConfiguration.Value.Salt ?? ""),
                HashingType.Sha512 => _apiConfiguration.Value.Password!.ToSha512(),
                _ => _apiConfiguration.Value.Password!.ToSha512()
            };

            var keyByteArray = Encoding.Default.GetBytes(password);
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
            });
            return handler.WriteToken(securityToken);
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Connect([FromBody] Channel data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!ModelState.IsValid)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_ERROR));
                return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_ERROR)));
            }

            if (data.MasterCommunication!.Password != _apiConfiguration.Value.Password)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_ERROR));
                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
            }

            _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_SUCCESS), _id.ToString(CultureInfo.CurrentCulture),
                data.ClientName);


            _id = ++MasterClientListSingleton.Instance.ConnectionCounter;

            var serv = new ChannelInfo
            {
                Name = data.ClientName,
                Host = data.Host,
                Port = data.Port,
                DisplayPort = data.DisplayPort,
                DisplayHost = data.DisplayHost,
                IsMaintenance = data.StartInMaintenance,
                Id = _id,
                ConnectedAccountLimit = data.ConnectedAccountLimit,
                WebApi = data.WebApi,
                LastPing = SystemTime.Now(),
                Type = data.ClientType,
                Token = data.Token
            };

            MasterClientListSingleton.Instance.Channels.Add(serv);
            data.ChannelId = _id;


            return Ok(new ConnectionInfo { Token = GenerateToken(), ChannelInfo = data });
        }

        [HttpPut]
        public IActionResult UpdateToken([FromBody] Channel data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var channel = MasterClientListSingleton.Instance.Channels.First(s =>
                (s.Name == data.ClientName) && (s.Host == data.Host) && (s.Port == data.Port));
            _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.TOKEN_UPDATED), channel.Id.ToString(CultureInfo.CurrentCulture),
                data.ClientName);
            channel.Token = data.Token;
            return Ok(new ConnectionInfo { Token = GenerateToken(), ChannelInfo = data });
        }

        // GET api/channel
        [HttpGet]
#pragma warning disable CA1822 // Mark members as static
        public List<ChannelInfo> GetChannels(long? id)
#pragma warning restore CA1822 // Mark members as static
        {
            return id != null ? MasterClientListSingleton.Instance.Channels.Where(s => s.Id == id).ToList() : MasterClientListSingleton.Instance.Channels;
        }

        [HttpPatch]
        public HttpStatusCode PingUpdate(int id, [FromBody] JsonPatchDocument<ChannelInfo?> data)
        {
            var chann = MasterClientListSingleton.Instance.Channels.FirstOrDefault(s => s.Id == id);
            if (chann == null)
            {
                return HttpStatusCode.NotFound;
            }

            if ((chann.LastPing.AddSeconds(10) < SystemTime.Now()) && !Debugger.IsAttached)
            {
                MasterClientListSingleton.Instance.Channels.RemoveAll(s => s.Id == _id);
                _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CONNECTION_LOST),
                    _id.ToString(CultureInfo.CurrentCulture));
                return HttpStatusCode.RequestTimeout;
            }

            data?.ApplyTo(chann);
            return HttpStatusCode.OK;
        }
    }
}