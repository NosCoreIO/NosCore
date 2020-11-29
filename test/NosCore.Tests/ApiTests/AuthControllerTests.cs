using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Core.Controllers;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.ApiTests
{
    [TestClass]
    public class AuthControllerTests
    {
        private readonly string _tokenGuid = Guid.NewGuid().ToString();
        private AuthController _controller = null!;
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task Setup()
        {
            SessionFactory.Instance.AuthCodes.Clear();
            SessionFactory.Instance.ReadyForAuth.Clear();
            SessionFactory.Instance.Sessions.Clear();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _controller = new AuthController(Options.Create(new WebApiConfiguration()), TestHelpers.Instance.AccountDao, new Mock<ILogger>().Object);
        }

        //[AllowAnonymous]
        //[HttpPost("sessions")]
        //public async Task<IActionResult> ConnectUserAsync(ApiSession session)
        //{
        //    if (!ModelState.IsValid || session == null)
        //    {
        //        return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_ERROR));
        //    }

        //    var account = await _accountDao.FirstOrDefaultAsync(s => s.Name == session.Identity).ConfigureAwait(false);
        //    if (account == null)
        //    {
        //        return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_ERROR));
        //    }
        //    var tfa = new TwoFactorAuth();
        //    if (account.MfaSecret != null && !tfa.VerifyCode(account.MfaSecret, session.Mfa))
        //    {
        //        return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MFA_INCORRECT));
        //    }

        //    switch (_apiConfiguration.Value.HashingType)
        //    {
        //        case HashingType.BCrypt:
        //            if (account.NewAuthPassword != Encoding.Default
        //                    .GetString(Convert.FromBase64String(account!.NewAuthPassword!))
        //                    .ToBcrypt(account.NewAuthSalt!
        //                ))
        //            {
        //                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
        //            }

        //            break;
        //        case HashingType.Pbkdf2:
        //            if (account.NewAuthPassword != Encoding.Default
        //                .GetString(Convert.FromBase64String(account.NewAuthPassword!))
        //                .ToPbkdf2Hash(account.NewAuthSalt!))
        //            {
        //                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
        //            }

        //            break;
        //        default:
        //            if (account.Password!.ToLower(CultureInfo.CurrentCulture) != (session.Password?.ToSha512() ?? ""))
        //            {
        //                return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
        //            }

        //            break;
        //    }

        //    account.Language = Enum.Parse<RegionType>(session.GfLang?.ToUpper(CultureInfo.CurrentCulture) ?? "");

        //    account = await _accountDao.TryInsertOrUpdateAsync(account).ConfigureAwait(false);
        //    var platformGameAccountId = Guid.NewGuid();
        //    var claims = new ClaimsIdentity(new[]
        //    {
        //        new Claim(ClaimTypes.NameIdentifier, session.Identity),
        //        new Claim(ClaimTypes.Sid, platformGameAccountId.ToString()),
        //        new Claim(ClaimTypes.Role, account.Authority.ToString())
        //    });
        //    var password = _apiConfiguration.Value.HashingType switch
        //    {
        //        HashingType.BCrypt => _apiConfiguration.Value.Password!.ToBcrypt(_apiConfiguration.Value.Salt ?? ""),
        //        HashingType.Pbkdf2 => _apiConfiguration.Value.Password!.ToPbkdf2Hash(_apiConfiguration.Value.Salt ?? ""),
        //        HashingType.Sha512 => _apiConfiguration.Value.Password!.ToSha512(),
        //        _ => _apiConfiguration.Value.Password!.ToSha512()
        //    };

        //    var keyByteArray = Encoding.Default.GetBytes(password);
        //    var signinKey = new SymmetricSecurityKey(keyByteArray);
        //    var handler = new JwtSecurityTokenHandler();
        //    var securityToken = handler.CreateToken(new SecurityTokenDescriptor
        //    {
        //        Subject = claims,
        //        Issuer = "Issuer",
        //        Audience = "Audience",
        //        SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
        //    });
        //    _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_API_SUCCESS),
        //        session.Identity, platformGameAccountId, session.Locale);
        //    return Ok(new
        //    {
        //        token = handler.WriteToken(securityToken),
        //        platformGameAccountId
        //    });
        //}

        [TestMethod]
        public void GetAuthCodeGenerateAuthCodeWhenValidIdentity()
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Sid, "123"),
                new Claim(ClaimTypes.NameIdentifier, _session.Account.Name),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            var result = _controller.GetAuthCode(new ApiPlatformGameAccount
            {
                PlatformGameAccountId = "123"
            });
            Assert.AreEqual(JsonSerializer.Serialize(new OkObjectResult(new { code = SessionFactory.Instance.AuthCodes.FirstOrDefault().Key })), JsonSerializer.Serialize(((OkObjectResult)result)));
        }

        [TestMethod]
        public void GetAuthCodeDoesNotGenerateAuthCodeWhenInvalidIdentity()
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Sid, "124"),
                new Claim(ClaimTypes.NameIdentifier, _session.Account.Name),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
           
            var result = _controller.GetAuthCode(new ApiPlatformGameAccount
            {
                PlatformGameAccountId = "123"
            });
            Assert.AreEqual(JsonSerializer.Serialize(new BadRequestObjectResult(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT))), JsonSerializer.Serialize(((BadRequestObjectResult)result)));
        }

        [TestMethod]
        public void GetExpectingConnectionReturnNullWhenTokenNull()
        {
            var result = _controller.GetExpectingConnection(_session.Account.Name, null, 1);
            Assert.AreEqual(null, ((OkObjectResult)result).Value);
        }

        [TestMethod]
        public void GetExpectingConnectionReturnNullWhenNotAuthCode()
        {
            var result = _controller.GetExpectingConnection(_session.Account.Name, "A1A2A3", 1);
            Assert.AreEqual(null, ((OkObjectResult)result).Value);
        }

        [TestMethod]
        public void GetExpectingConnectionReturnAccountNameWhenAuthCode()
        {
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session.Account.Name;
            var result = _controller.GetExpectingConnection(_session.Account.Name, string.Join("", _tokenGuid.ToCharArray().Select(s => Convert.ToByte(s).ToString("x"))), 1);
            Assert.AreEqual(_session.Account.Name, ((OkObjectResult)result).Value);
        }

        [TestMethod]
        public void GetExpectingConnectionReturnNullWhenTokenNoneSessionTicket()
        {
            var result = _controller.GetExpectingConnection(_session.Account.Name, "NONE_SESSION_TICKET", 1);
            Assert.AreEqual(null, ((OkObjectResult)result).Value);
        }

        [TestMethod]
        public void GetExpectingConnectionReturnTrueWhenGfModeAndExpecting()
        {
            SessionFactory.Instance.ReadyForAuth[_session.Account.Name] = 1;
            var result = _controller.GetExpectingConnection(_session.Account.Name, "thisisgfmode", 1);
            Assert.AreEqual(true, ((OkObjectResult)result).Value);
        }

        [TestMethod]
        public void GetExpectingConnectionReturnTrueWhenGfModeAndExpectingButWrongSessionId()
        {
            SessionFactory.Instance.ReadyForAuth[_session.Account.Name] = 1;
            var result = _controller.GetExpectingConnection(_session.Account.Name, "thisisgfmode", 2);
            Assert.AreEqual(false, ((OkObjectResult)result).Value);
        }

        [TestMethod]
        public void GetExpectingConnectionReturnFalseWhenGfModeAndNotExpecting()
        {
            var result = _controller.GetExpectingConnection(_session.Account.Name, "thisisgfmode", 1);
            Assert.AreEqual(false, ((OkObjectResult)result).Value);
        }


        [TestMethod]
        public async Task HasMfaEnabledReturnTrueWhenTokenNotNull()
        {
            _session.Account.MfaSecret = null;
            var result = await _controller.HasMfaEnabled(_session.Account.Name);
            Assert.AreEqual(true, ((OkObjectResult)result).Value);
        }

        [TestMethod]
        public async Task HasMfaEnabledReturnFalseWhenTokenNull()
        {
            var result = await _controller.HasMfaEnabled(_session.Account.Name);
            Assert.AreEqual(true, ((OkObjectResult)result).Value);
        }

    }
}
