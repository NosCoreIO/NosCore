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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    public class ChannelController : Controller
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private readonly WebApiConfiguration _apiConfiguration;

        private int _id;

        public ChannelController(WebApiConfiguration apiConfiguration)
        {
            _apiConfiguration = apiConfiguration;
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Connect([FromBody]Channel data)
        {
            if (!ModelState.IsValid)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTHENTICATED_ERROR));
                return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_ERROR)));
            }

            if (data.MasterCommunication.Password != _apiConfiguration.Password)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTHENTICATED_ERROR));
                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_INCORRECT));
            }

            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Server"),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            });
            var keyByteArray = Encoding.Default.GetBytes(EncryptionHelper.Sha512(_apiConfiguration.Password));
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
            });

            _logger.Debug(string.Format(
                LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTHENTICATED_SUCCESS), _id.ToString()));

            if (MasterClientListSingleton.Instance.WorldServers == null)
            {
                MasterClientListSingleton.Instance.WorldServers = new List<WorldServerInfo>();
            }

            try
            {
                _id = MasterClientListSingleton.Instance.WorldServers.Select(s => s.Id).Max() + 1;
            }
            catch
            {
                _id = 0;
            }

            WorldServerInfo serv = null;
            var servtype = (ServerType)Enum.Parse(typeof(ServerType), data.ClientType.ToString());
            if (servtype == ServerType.WorldServer)
            {
                serv = new WorldServerInfo
                {
                    Name = data.ClientName,
                    Host = data.Host,
                    Port = data.Port,
                    Id = _id,
                    ConnectedAccountLimit = data.ConnectedAccountLimit,
                    WebApi = data.WebApi
                };

                MasterClientListSingleton.Instance.WorldServers.Add(serv);
                data.ChannelId = _id;
            }

            return Ok(new ConnectionInfo{ Token = handler.WriteToken(securityToken), ChannelInfo = data });
        }

        // GET api/channel
        [HttpGet]
        public List<WorldServerInfo> GetChannels(long? id)
        {
            if (id != null)
            {
                return MasterClientListSingleton.Instance.WorldServers.Where(s => s.Id == id).ToList();
            }

            return MasterClientListSingleton.Instance.WorldServers;
        }
    }
}