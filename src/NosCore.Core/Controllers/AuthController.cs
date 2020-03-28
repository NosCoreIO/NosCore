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
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NosCore.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using Serilog;

namespace NosCore.Core.Controllers
{
    [ApiController]
    [Route("api/v1/auth/thin")]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IGenericDao<AccountDto> _accountDao;
        private readonly WebApiConfiguration _apiConfiguration;
        private readonly ILogger _logger;

        public AuthController(WebApiConfiguration apiConfiguration, IGenericDao<AccountDto> accountDao, ILogger logger)
        {
            _apiConfiguration = apiConfiguration;
            _accountDao = accountDao;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("sessions")]
        public IActionResult ConnectUser(ApiSession session)
        {
            if (!ModelState.IsValid || session == null)
            {
                return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_ERROR)));
            }

            var account = _accountDao.FirstOrDefault(s => s.Name == session.Identity);
            if (account == null)
            {
                return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_ERROR)));
            }

            switch (_apiConfiguration.HashingType)
            {
                case HashingType.BCrypt:
                    if (account?.NewAuthPassword != Encoding.Default
                            .GetString(Convert.FromBase64String(account!.NewAuthPassword))
                            .ToBcrypt(account.NewAuthSalt
                        ))
                    {
                        return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
                    }

                    break;
                case HashingType.Pbkdf2:
                    if (account.NewAuthPassword != Encoding.Default
                        .GetString(Convert.FromBase64String(account.NewAuthPassword))
                        .ToPbkdf2Hash(account.NewAuthSalt))
                    {
                        return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
                    }

                    break;
                case HashingType.Sha512:
                default:
                    if (account.Password.ToLower(CultureInfo.CurrentCulture) != (session.Password?.ToSha512() ?? ""))
                    {
                        return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
                    }

                    break;
            }

            account.Language = Enum.Parse<RegionType>(session.GfLang?.ToUpper(CultureInfo.CurrentCulture) ?? "");
            _accountDao.InsertOrUpdate(ref account);
            var platformGameAccountId = Guid.NewGuid();
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, session.Identity),
                new Claim(ClaimTypes.Sid, platformGameAccountId.ToString()),
                new Claim(ClaimTypes.Role, account.Authority.ToString())
            });
            var password = _apiConfiguration.HashingType switch
            {
                HashingType.BCrypt => _apiConfiguration.Password!.ToBcrypt(_apiConfiguration.Salt ?? ""),
                HashingType.Pbkdf2 => _apiConfiguration.Password!.ToPbkdf2Hash(_apiConfiguration.Salt ?? ""),
                HashingType.Sha512 => _apiConfiguration.Password!.ToSha512(),
                _ => _apiConfiguration.Password!.ToSha512()
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
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_API_SUCCESS),
                session.Identity, platformGameAccountId, session.Locale);
            return Ok(new
            {
                token = handler.WriteToken(securityToken),
                platformGameAccountId
            });
        }

        [HttpPost("codes")]
        public IActionResult GetAuthCode(ApiPlatformGameAccount platformGameAccount)
        {
            var identity = (ClaimsIdentity)User.Identity;
            if (!identity.Claims.Any(s =>
                (s.Type == ClaimTypes.Sid) && (s.Value == platformGameAccount.PlatformGameAccountId)))
            {
                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
            }

            var authCode = Guid.NewGuid();
            SessionFactory.Instance.AuthCodes[authCode.ToString()] = 
                identity.Claims.First(s => s.Type == ClaimTypes.NameIdentifier).Value;

            return Ok(new { code = authCode });
        }


        [HttpGet]
        public IActionResult GetExpectingConnection(string? id, string? token, long sessionId)
        {
            var sessionGuid = HexStringToString(token ?? "");
            if (!SessionFactory.Instance.AuthCodes.ContainsKey(sessionGuid))
            {
                return Ok(null);
            }

            var username = SessionFactory.Instance.AuthCodes[sessionGuid];
            SessionFactory.Instance.ReadyForAuth.AddOrUpdate(username, sessionId, (key, oldValue) => sessionId);
            return Ok(username);

            //if ((token != "thisisgfmode") && (SessionFactory.Instance.AuthCodes[id] == HexStringToString(token ?? "")))
            //{
            //    SessionFactory.Instance.ReadyForAuth.AddOrUpdate(id, sessionId, (key, oldValue) => sessionId);
            //    return Ok(true);
            //}

            //if (SessionFactory.Instance.ReadyForAuth.ContainsKey(id) &&
            //    (sessionId == SessionFactory.Instance.ReadyForAuth[id]))
            //{
            //    return Ok(true);
            //}
        }

        private static string HexStringToString(string hexString)
        {
            var bb = Enumerable.Range(0, hexString.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                .ToArray();
            return Encoding.UTF8.GetString(bb);
        }
    }

    [Serializable]
    public class ApiSession
    {
        public string? GfLang { get; set; }
        public string? Identity { get; set; }
        public string? Locale { get; set; }
        public string? Password { get; set; }
    }

    [Serializable]
    public class ApiPlatformGameAccount
    {
        public string? PlatformGameAccountId { get; set; }
    }
}