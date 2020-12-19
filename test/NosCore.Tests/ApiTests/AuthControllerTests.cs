using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Core.Controllers;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Tests.Helpers;
using Serilog;
using TwoFactorAuthNet;

namespace NosCore.Tests.ApiTests
{
    [TestClass]
    public class AuthControllerTests
    {
        private readonly string _tokenGuid = Guid.NewGuid().ToString();
        private AuthController _controller = null!;
        private ClientSession _session = null!;
        private Mock<ILogger> _logger = null!;
        private IEncryption _encryption = null!;

        [TestInitialize]
        public async Task Setup()
        {
            SessionFactory.Instance.AuthCodes.Clear();
            SessionFactory.Instance.ReadyForAuth.Clear();
            SessionFactory.Instance.Sessions.Clear();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _logger = new Mock<ILogger>();
            _controller = new AuthController(Options.Create(new WebApiConfiguration()
            {
                Password = "123"
            }), TestHelpers.Instance.AccountDao, _logger.Object, _encryption);
        }

        [TestMethod]
        public async Task ConnectUser()
        {
            await _controller.ConnectUserAsync(new ApiSession
            {
                Identity = _session.Account.Name,
                GfLang = "EN",
                Password = "test",
                Locale = "en-GB"
            });

            _logger.Verify(o=>o.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_API_SUCCESS),
                _session.Account.Name, It.IsAny<Guid>(), "en-GB"), Times.Once());
        }

        [TestMethod]
        public async Task ConnectUserInvalidPassword()
        {
            var result = await _controller.ConnectUserAsync(new ApiSession
            {
                Identity = _session.Account.Name,
                GfLang = "EN",
                Password = "test2"
            });
            Assert.AreEqual(JsonSerializer.Serialize(new BadRequestObjectResult(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT))), JsonSerializer.Serialize((BadRequestObjectResult)result));
        }

        [TestMethod]
        public async Task ConnectUserAsyncWhenInvalidAccount()
        {
            var result = await _controller.ConnectUserAsync(new ApiSession
            {
                Identity = _session.Account.Name + "abc"
            });
            Assert.AreEqual(JsonSerializer.Serialize(new BadRequestObjectResult(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_ERROR))), JsonSerializer.Serialize((BadRequestObjectResult)result));
        }

        [TestMethod]
        public async Task ConnectUserAsyncWhenInvalidMfa()
        {
            var tfa = new TwoFactorAuth();
            _session.Account.MfaSecret = tfa.CreateSecret();
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(_session.Account);

            var result = await _controller.ConnectUserAsync(new ApiSession
            {
                Identity = _session.Account.Name,
                Mfa = tfa.GetCode(string.Concat(_session.Account.MfaSecret.Reverse())),
            });

            Assert.AreEqual(JsonSerializer.Serialize(new BadRequestObjectResult(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MFA_INCORRECT))), JsonSerializer.Serialize((BadRequestObjectResult)result));
        }

        [TestMethod]
        public async Task ConnectUserAsyncWhenNoMfa()
        {
            var result = await _controller.ConnectUserAsync(new ApiSession
            {
                Identity = _session.Account.Name
            });

            Assert.AreNotEqual(JsonSerializer.Serialize(new BadRequestObjectResult(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MFA_INCORRECT))), JsonSerializer.Serialize((BadRequestObjectResult)result));
        }

        [TestMethod]
        public async Task ConnectUserAsyncWhenValidMfa()
        {
            var tfa = new TwoFactorAuth();
            _session.Account.MfaSecret = tfa.CreateSecret();
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(_session.Account);

            var result = await _controller.ConnectUserAsync(new ApiSession
            {
                Identity = _session.Account.Name,
                Mfa = tfa.GetCode(_session.Account.MfaSecret),
            });

            Assert.AreNotEqual(JsonSerializer.Serialize(new BadRequestObjectResult(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MFA_INCORRECT))), JsonSerializer.Serialize((BadRequestObjectResult)result));
        }

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
