//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.Services.AuthService;
using SpecLight;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.InterChannelCommunication
{
    [TestClass]
    public class AuthHubTests
    {
        private const string AccountName = "Account";
        private readonly string TokenGuid = Guid.NewGuid().ToString();
        private IAuthCodeService AuthCodeService = null!;
        private AuthHub Hub = null!;
        private string? Result;

        [TestInitialize]
        public void Setup()
        {
            AuthCodeService = new AuthCodeService();
            Hub = new AuthHub(AuthCodeService);
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnNullWhenTokenNull()
        {
            await new Spec("Get awaiting connection should return null when token null")
                .WhenAsync(GettingAwaitingConnectionWithNullToken)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnNullWhenNoAuthCode()
        {
            await new Spec("Get awaiting connection should return null when no auth code")
                .WhenAsync(GettingAwaitingConnectionWithInvalidAuthCode)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnAccountNameWhenAuthCode()
        {
            await new Spec("Get awaiting connection should return account name when auth code")
                .Given(AuthCodeIsStored)
                .WhenAsync(GettingAwaitingConnectionWithValidAuthCode)
                .Then(ShouldReturnAccountName)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnNullWhenNoneSessionTicket()
        {
            await new Spec("Get awaiting connection should return null when none session ticket")
                .WhenAsync(GettingAwaitingConnectionWithNoneSessionTicket)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnTrueWhenGfModeAndExpecting()
        {
            await new Spec("Get awaiting connection should return true when gf mode and expecting")
                .Given(UserIsMarkedReadyForAuth)
                .WhenAsync(GettingAwaitingConnectionInGfMode)
                .Then(ShouldReturnTrue)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnFalseWhenGfModeButWrongSessionId()
        {
            await new Spec("Get awaiting connection should return false when gf mode but wrong session id")
                .Given(UserIsMarkedReadyForAuth)
                .WhenAsync(GettingAwaitingConnectionInGfModeWithWrongSession)
                .Then(ShouldReturnFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnFalseWhenGfModeAndNotExpecting()
        {
            await new Spec("Get awaiting connection should return false when gf mode and not expecting")
                .WhenAsync(GettingAwaitingConnectionInGfMode)
                .Then(ShouldReturnFalse)
                .ExecuteAsync();
        }

        private void AuthCodeIsStored()
        {
            AuthCodeService.StoreAuthCode(TokenGuid, AccountName);
        }

        private void UserIsMarkedReadyForAuth()
        {
            AuthCodeService.MarkReadyForAuth(AccountName, 1);
        }

        private async Task GettingAwaitingConnectionWithNullToken()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, null, 1);
        }

        private async Task GettingAwaitingConnectionWithInvalidAuthCode()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, "A1A2A3", 1);
        }

        private async Task GettingAwaitingConnectionWithValidAuthCode()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, string.Join("", TokenGuid.ToCharArray().Select(s => Convert.ToByte(s).ToString("x"))), 1);
        }

        private async Task GettingAwaitingConnectionWithNoneSessionTicket()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, "NONE_SESSION_TICKET", 1);
        }

        private async Task GettingAwaitingConnectionInGfMode()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, "thisisgfmode", 1);
        }

        private async Task GettingAwaitingConnectionInGfModeWithWrongSession()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, "thisisgfmode", 2);
        }

        private void ShouldReturnNull()
        {
            Assert.IsNull(Result);
        }

        private void ShouldReturnAccountName()
        {
            Assert.AreEqual(AccountName, Result);
        }

        private void ShouldReturnTrue()
        {
            Assert.AreEqual("true", Result);
        }

        private void ShouldReturnFalse()
        {
            Assert.AreEqual("false", Result);
        }
    }
}
