using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using Serilog;

namespace NosCore.MasterServer.GraphQl
{
    public class Mutation
    {
        private readonly ILogger _logger;

        public Mutation(ILogger logger)
        {
            _logger = logger;
        }

        public IActionResult Connect([FromBody] Channel data)
        {
            if (!ModelState.IsValid)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_ERROR));
                return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_ERROR)));
            }

            if (data.MasterCommunication.Password != _apiConfiguration.Password)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_ERROR));
                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
            }

            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Server"),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            });
            var keyByteArray = Encoding.Default.GetBytes(_apiConfiguration.Password.ToSha512());
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
            });

            _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_SUCCESS), _id.ToString(),
                data.ClientName);

            try
            {
                _id = ++MasterClientListSingleton.Instance.ConnectionCounter;
            }
            catch
            {
                _id = 0;
            }

            var serv = new ChannelInfo
            {
                Name = data.ClientName,
                Host = data.Host,
                Port = data.Port,
                Id = _id,
                ConnectedAccountLimit = data.ConnectedAccountLimit,
                WebApi = data.WebApi,
                LastPing = SystemTime.Now(),
                Type = data.ClientType
            };

            MasterClientListSingleton.Instance.Channels.Add(serv);
            data.ChannelId = _id;


            return Ok(new ConnectionInfo { Token = handler.WriteToken(securityToken), ChannelInfo = data });
        }

        public HttpStatusCode Ping(int id, DateTime data)
        {
            var chann = MasterClientListSingleton.Instance.Channels.FirstOrDefault(s => s.Id == id);
            if (chann != null)
            {
                if (chann.LastPing.AddSeconds(10) < SystemTime.Now() && !System.Diagnostics.Debugger.IsAttached)
                {
                    MasterClientListSingleton.Instance.Channels.RemoveAll(s => s.Id == _id);
                    _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CONNECTION_LOST),
                        _id.ToString());
                    return HttpStatusCode.RequestTimeout;
                }

                chann.LastPing = data;
                return HttpStatusCode.OK;
            }

            return HttpStatusCode.NotFound;
        }
    }
}
