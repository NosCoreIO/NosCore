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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace NosCore.Core.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.Root)]
    public class ChannelController(IOptions<WebApiConfiguration> apiConfiguration, ILogger logger, IHasher hasher,
            IClock clock, IIdService<ChannelInfo> channelInfoIdService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : Controller
    {
        private long _id;

        private string GenerateToken()
        {
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Server"),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            });
            var password = hasher.Hash(apiConfiguration.Value.Password!, apiConfiguration.Value.Salt);

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
                logger.Error(logLanguage[LogLanguageKey.AUTHENTICATED_ERROR]);
                return BadRequest(BadRequest(logLanguage[LogLanguageKey.AUTH_ERROR]));
            }

            if (data.MasterCommunication!.Password != apiConfiguration.Value.Password)
            {
                logger.Error(logLanguage[LogLanguageKey.AUTHENTICATED_ERROR]);
                return BadRequest(logLanguage[LogLanguageKey.AUTH_INCORRECT]);
            }

            _id = channelInfoIdService.GetNextId();
            logger.Debug(logLanguage[LogLanguageKey.AUTHENTICATED_SUCCESS], _id.ToString(CultureInfo.CurrentCulture),
                data.ClientName);

            var serv = new ChannelInfo
            {
                Name = data.ClientName,
                Host = data.Host,
                Port = data.Port,
                DisplayPort = (ushort?)data.DisplayPort,
                DisplayHost = data.DisplayHost,
                IsMaintenance = data.StartInMaintenance,
                ServerId = data.ServerId,
                Id = _id,
                ConnectedAccountLimit = data.ConnectedAccountLimit,
                WebApi = data.WebApi,
                LastPing = clock.GetCurrentInstant(),
                Type = data.ClientType,
            };

            channelInfoIdService.Items[_id] = serv;
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

            var channel = channelInfoIdService.Items.Values.First(s =>
                (s.Name == data.ClientName) && (s.Host == data.Host) && (s.Port == data.Port));
            logger.Debug(logLanguage[LogLanguageKey.TOKEN_UPDATED], channel.Id.ToString(CultureInfo.CurrentCulture),
                data.ClientName);
            return Ok(new ConnectionInfo { Token = GenerateToken(), ChannelInfo = data });
        }

        // GET api/channel
        [HttpGet]
#pragma warning disable CA1822 // Mark members as static
        public List<ChannelInfo> GetChannels(long? id)
#pragma warning restore CA1822 // Mark members as static
        {
            return id != null ? channelInfoIdService.Items.Values.Where(s => s.Id == id).ToList() : channelInfoIdService.Items.Values.ToList();
        }
    }
}