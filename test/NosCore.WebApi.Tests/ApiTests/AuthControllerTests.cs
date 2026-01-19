//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Encryption;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.AuthService;
using NosCore.Shared.Configuration;
using NosCore.Tests.Shared;
using NosCore.WebApi.Controllers;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using TwoFactorAuthNet;

namespace NosCore.WebApi.Tests.ApiTests
{
    [TestClass]
    public class AuthControllerTests
    {
        private readonly string TokenGuid = Guid.NewGuid().ToString();
        private AuthController Controller = null!;
        private ClientSession Session = null!;
        private Mock<ILogger<AuthController>> Logger = null!;
        private IAuthCodeService AuthCodeService = null!;
        private IActionResult? Result;

        [TestInitialize]
        public async Task Setup()
        {
            AuthCodeService = new AuthCodeService();
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Logger = new Mock<ILogger<AuthController>>();
            Controller = new AuthController(Options.Create(new WebApiConfiguration()
            {
                Password = "123"
            }), TestHelpers.Instance.AccountDao, Logger.Object, new Sha512Hasher(), TestHelpers.Instance.LogLanguageLocalizer,
               new AuthHub(AuthCodeService), AuthCodeService);
        }

        [TestMethod]
        public async Task ConnectUserShouldLogSuccess()
        {
            await new Spec("Connect user should log success")
                .WhenAsync(UserConnectsWithValidCredentials)
                .Then(SuccessShouldBeLogged)
                .ExecuteAsync();
        }

        private async Task UserConnectsWithValidCredentials()
        {
            await Controller.ConnectUserAsync(new ApiSession
            {
                Identity = Session.Account.Name,
                GfLang = "EN",
                Password = "test",
                Locale = "en-GB"
            });
        }

        private void SuccessShouldBeLogged()
        {
            Logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => string.Equals(string.Format(TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.AUTH_API_SUCCESS], Session.Account.Name, It.IsAny<Guid>(), "en-GB"), o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ConnectUserWithInvalidPasswordShouldFail()
        {
            await new Spec("Connect user with invalid password should fail")
                .WhenAsync(UserConnectsWithInvalidPassword)
                .Then(ShouldReturnAuthIncorrectError)
                .ExecuteAsync();
        }

        private async Task UserConnectsWithInvalidPassword()
        {
            Result = await Controller.ConnectUserAsync(new ApiSession
            {
                Identity = Session.Account.Name,
                GfLang = "EN",
                Password = "test2"
            });
        }

        private void ShouldReturnAuthIncorrectError()
        {
            Assert.AreEqual(JsonSerializer.Serialize(new BadRequestObjectResult(TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.AUTH_INCORRECT])), JsonSerializer.Serialize((BadRequestObjectResult)Result!));
        }

        [TestMethod]
        public async Task ConnectUserWithInvalidAccountShouldFail()
        {
            await new Spec("Connect user with invalid account should fail")
                .WhenAsync(UserConnectsWithInvalidAccount)
                .Then(ShouldReturnAuthError)
                .ExecuteAsync();
        }

        private async Task UserConnectsWithInvalidAccount()
        {
            Result = await Controller.ConnectUserAsync(new ApiSession
            {
                Identity = Session.Account.Name + "abc"
            });
        }

        private void ShouldReturnAuthError()
        {
            Assert.AreEqual(JsonSerializer.Serialize(new BadRequestObjectResult(TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.AUTH_ERROR])), JsonSerializer.Serialize((BadRequestObjectResult)Result!));
        }

        [TestMethod]
        public async Task ConnectUserWithInvalidMfaShouldFail()
        {
            await new Spec("Connect user with invalid MFA should fail")
                .GivenAsync(UserHasMfaEnabled)
                .WhenAsync(UserConnectsWithInvalidMfaCode)
                .Then(ShouldReturnMfaIncorrectError)
                .ExecuteAsync();
        }

        private TwoFactorAuth? Tfa;

        private async Task UserHasMfaEnabled()
        {
            Tfa = new TwoFactorAuth();
            Session.Account.MfaSecret = Tfa.CreateSecret();
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(Session.Account);
        }

        private async Task UserConnectsWithInvalidMfaCode()
        {
            Result = await Controller.ConnectUserAsync(new ApiSession
            {
                Identity = Session.Account.Name,
                Mfa = Tfa!.GetCode(string.Concat(Session.Account.MfaSecret!.Reverse())),
            });
        }

        private void ShouldReturnMfaIncorrectError()
        {
            Assert.AreEqual(JsonSerializer.Serialize(new BadRequestObjectResult(TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.MFA_INCORRECT])), JsonSerializer.Serialize((BadRequestObjectResult)Result!));
        }

        [TestMethod]
        public async Task ConnectUserWithoutMfaShouldSucceed()
        {
            await new Spec("Connect user without MFA should succeed")
                .WhenAsync(UserConnectsWithoutMfa)
                .Then(ShouldNotReturnMfaIncorrectError)
                .ExecuteAsync();
        }

        private async Task UserConnectsWithoutMfa()
        {
            Result = await Controller.ConnectUserAsync(new ApiSession
            {
                Identity = Session.Account.Name
            });
        }

        private void ShouldNotReturnMfaIncorrectError()
        {
            Assert.AreNotEqual(JsonSerializer.Serialize(new BadRequestObjectResult(TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.MFA_INCORRECT])), JsonSerializer.Serialize((BadRequestObjectResult)Result!));
        }

        [TestMethod]
        public async Task ConnectUserWithValidMfaShouldSucceed()
        {
            await new Spec("Connect user with valid MFA should succeed")
                .GivenAsync(UserHasMfaEnabled)
                .WhenAsync(UserConnectsWithValidMfaCode)
                .Then(ShouldNotReturnMfaIncorrectError)
                .ExecuteAsync();
        }

        private async Task UserConnectsWithValidMfaCode()
        {
            Result = await Controller.ConnectUserAsync(new ApiSession
            {
                Identity = Session.Account.Name,
                Mfa = Tfa!.GetCode(Session.Account.MfaSecret!),
            });
        }

        [TestMethod]
        public void GetAuthCodeShouldGenerateCodeWhenValidIdentity()
        {
            new Spec("Get auth code should generate code when valid identity")
                .Given(ControllerHasValidIdentity)
                .When(GettingAuthCode)
                .Then(ShouldReturnValidGuid)
                .Execute();
        }

        private void ControllerHasValidIdentity()
        {
            var claims = new List<Claim>()
            {
                new(ClaimTypes.Sid, "123"),
                new(ClaimTypes.NameIdentifier, Session.Account.Name),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            Controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        private void GettingAuthCode()
        {
            Result = Controller.GetAuthCode(new ApiPlatformGameAccount
            {
                PlatformGameAccountId = "123"
            });
        }

        private void ShouldReturnValidGuid()
        {
            var okResult = (OkObjectResult)Result!;
            Assert.IsNotNull(okResult.Value);
            var codeProperty = okResult.Value.GetType().GetProperty("code");
            Assert.IsNotNull(codeProperty);
            var codeValue = codeProperty.GetValue(okResult.Value);
            Assert.IsTrue(Guid.TryParse(codeValue?.ToString(), out _));
        }

        [TestMethod]
        public void GetAuthCodeShouldFailWhenInvalidIdentity()
        {
            new Spec("Get auth code should fail when invalid identity")
                .Given(ControllerHasInvalidIdentity)
                .When(GettingAuthCode)
                .Then(ShouldReturnAuthIncorrectError)
                .Execute();
        }

        private void ControllerHasInvalidIdentity()
        {
            var claims = new List<Claim>()
            {
                new(ClaimTypes.Sid, "124"),
                new(ClaimTypes.NameIdentifier, Session.Account.Name),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            Controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        [TestMethod]
        public async Task GetExpectingConnectionShouldReturnNullWhenTokenNull()
        {
            await new Spec("Get expecting connection should return null when token null")
                .WhenAsync(GettingExpectingConnectionWithNullToken)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        private async Task GettingExpectingConnectionWithNullToken()
        {
            Result = await Controller.GetExpectingConnection(Session.Account.Name, null, 1);
        }

        private void ShouldReturnNull()
        {
            Assert.AreEqual(null, ((OkObjectResult)Result!).Value);
        }

        [TestMethod]
        public async Task GetExpectingConnectionShouldReturnNullWhenNoAuthCode()
        {
            await new Spec("Get expecting connection should return null when no auth code")
                .WhenAsync(GettingExpectingConnectionWithInvalidAuthCode)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        private async Task GettingExpectingConnectionWithInvalidAuthCode()
        {
            Result = await Controller.GetExpectingConnection(Session.Account.Name, "A1A2A3", 1);
        }

        [TestMethod]
        public async Task GetExpectingConnectionShouldReturnAccountNameWhenAuthCode()
        {
            await new Spec("Get expecting connection should return account name when auth code")
                .Given(AuthCodeIsStored)
                .WhenAsync(GettingExpectingConnectionWithValidAuthCode)
                .Then(ShouldReturnAccountName)
                .ExecuteAsync();
        }

        private void AuthCodeIsStored()
        {
            AuthCodeService.StoreAuthCode(TokenGuid, Session.Account.Name);
        }

        private async Task GettingExpectingConnectionWithValidAuthCode()
        {
            Result = await Controller.GetExpectingConnection(Session.Account.Name, string.Join("", TokenGuid.ToCharArray().Select(s => Convert.ToByte(s).ToString("x"))), 1);
        }

        private void ShouldReturnAccountName()
        {
            Assert.AreEqual(Session.Account.Name, ((OkObjectResult)Result!).Value);
        }

        [TestMethod]
        public async Task GetExpectingConnectionShouldReturnNullWhenNoneSessionTicket()
        {
            await new Spec("Get expecting connection should return null when none session ticket")
                .WhenAsync(GettingExpectingConnectionWithNoneSessionTicket)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        private async Task GettingExpectingConnectionWithNoneSessionTicket()
        {
            Result = await Controller.GetExpectingConnection(Session.Account.Name, "NONE_SESSION_TICKET", 1);
        }

        [TestMethod]
        public async Task GetExpectingConnectionShouldReturnTrueWhenGfModeAndExpecting()
        {
            await new Spec("Get expecting connection should return true when gf mode and expecting")
                .Given(UserIsMarkedReadyForAuth)
                .WhenAsync(GettingExpectingConnectionInGfMode)
                .Then(ShouldReturnTrue)
                .ExecuteAsync();
        }

        private void UserIsMarkedReadyForAuth()
        {
            AuthCodeService.MarkReadyForAuth(Session.Account.Name, 1);
        }

        private async Task GettingExpectingConnectionInGfMode()
        {
            Result = await Controller.GetExpectingConnection(Session.Account.Name, "thisisgfmode", 1);
        }

        private void ShouldReturnTrue()
        {
            Assert.AreEqual("true", ((OkObjectResult)Result!).Value);
        }

        [TestMethod]
        public async Task GetExpectingConnectionShouldReturnFalseWhenGfModeButWrongSessionId()
        {
            await new Spec("Get expecting connection should return false when gf mode but wrong session id")
                .Given(UserIsMarkedReadyForAuth)
                .WhenAsync(GettingExpectingConnectionInGfModeWithWrongSession)
                .Then(ShouldReturnFalse)
                .ExecuteAsync();
        }

        private async Task GettingExpectingConnectionInGfModeWithWrongSession()
        {
            Result = await Controller.GetExpectingConnection(Session.Account.Name, "thisisgfmode", 2);
        }

        private void ShouldReturnFalse()
        {
            Assert.AreEqual("false", ((OkObjectResult)Result!).Value);
        }

        [TestMethod]
        public async Task GetExpectingConnectionShouldReturnFalseWhenGfModeAndNotExpecting()
        {
            await new Spec("Get expecting connection should return false when gf mode and not expecting")
                .WhenAsync(GettingExpectingConnectionInGfMode)
                .Then(ShouldReturnFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HasMfaEnabledShouldReturnTrueWhenMfaSecretExists()
        {
            await new Spec("Has MFA enabled should return true when MFA secret exists")
                .GivenAsync(UserHasMfaSecret)
                .WhenAsync(CheckingIfMfaEnabled)
                .Then(ShouldReturnTrueValue)
                .ExecuteAsync();
        }

        private async Task UserHasMfaSecret()
        {
            Session.Account.MfaSecret = "12345";
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(Session.Account);
        }

        private async Task CheckingIfMfaEnabled()
        {
            Result = await Controller.HasMfaEnabled(Session.Account.Name);
        }

        private void ShouldReturnTrueValue()
        {
            Assert.AreEqual(true, ((OkObjectResult)Result!).Value);
        }

        [TestMethod]
        public async Task HasMfaEnabledShouldReturnFalseWhenNoMfaSecret()
        {
            await new Spec("Has MFA enabled should return false when no MFA secret")
                .WhenAsync(CheckingIfMfaEnabled)
                .Then(ShouldReturnFalseValue)
                .ExecuteAsync();
        }

        private void ShouldReturnFalseValue()
        {
            Assert.AreEqual(false, ((OkObjectResult)Result!).Value);
        }
    }
}
