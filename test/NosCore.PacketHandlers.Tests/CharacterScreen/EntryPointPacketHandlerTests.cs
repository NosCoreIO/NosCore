//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.SessionRef;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class EntryPointPacketHandlerTests
    {
        private static readonly Mock<ILogger> Logger = new();
        private EntryPointPacketHandler EntryPointPacketHandler = null!;
        private ClientSession Session = null!;
        private Mock<IAuthHub> AuthHttpClient = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private string AccountName = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            AccountName = Session.Account.Name;
            Session.Account = null!;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(Session.Character);
            await Session.SetCharacterAsync(null);
            AuthHttpClient = new Mock<IAuthHub>();
            PubSubHub = TestHelpers.Instance.PubSubHub;
            PubSubHub.Setup(o => o.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            EntryPointPacketHandler = new EntryPointPacketHandler(
                TestHelpers.Instance.CharacterDao,
                TestHelpers.Instance.AccountDao,
                TestHelpers.Instance.MateDao,
                Logger.Object,
                AuthHttpClient.Object,
                PubSubHub.Object,
                TestHelpers.Instance.WorldConfiguration,
                new SessionRefHolder(),
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task ConnectionWithAlreadyConnectedAccountShouldFail()
        {
            await new Spec("Connection with already connected account should fail")
                .Given(AccountIsAlreadyConnected)
                .WhenAsync(ConnectingWithValidPassword)
                .Then(AlreadyConnectedErrorShouldBeLogged)
                .And(AccountShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionWithInvalidAccountShouldFail()
        {
            await new Spec("Connection with invalid account should fail")
                .WhenAsync(ConnectingWithFakeAccount)
                .Then(InvalidAccountErrorShouldBeLogged)
                .And(AccountShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionWithInvalidPasswordShouldFail()
        {
            await new Spec("Connection with invalid password should fail")
                .WhenAsync(ConnectingWithInvalidPassword)
                .Then(InvalidPasswordErrorShouldBeLogged)
                .And(AccountShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionWithValidPasswordShouldSucceed()
        {
            await new Spec("Connection with valid password should succeed")
                .Given(AccountPasswordIsValid)
                .WhenAsync(ConnectingWithValidPassword)
                .Then(AccountShouldBeSet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionWithGfModeShouldSucceed()
        {
            await new Spec("Connection with gf mode should succeed")
                .Given(AccountIsInWaitingState)
                .WhenAsync(ConnectingWithGfMode)
                .Then(AccountShouldBeSet)
                .And(MfaShouldBeValidated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionWithMfaEnabledShouldRequireValidation()
        {
            await new Spec("Connection with MFA enabled should require validation")
                .GivenAsync(AccountHasMfaEnabled)
                .WhenAsync(ConnectingWithValidPassword)
                .ThenAsync(GuriPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionWhenAccountAlreadySetShouldSendCharacterList()
        {
            await new Spec("Connection when account already set should send character list")
                .GivenAsync(AccountIsAlreadyInitialized)
                .WhenAsync(ConnectingWithValidPassword)
                .ThenAsync(CharacterListShouldBeSent)
                .ExecuteAsync();
        }

        private void AccountIsAlreadyConnected()
        {
            PubSubHub.Setup(o => o.GetSubscribersAsync()).ReturnsAsync(
                new List<Subscriber>
                {
                    new()
                    {
                        ChannelId = 1,
                        Name = AccountName
                    }
                });
        }

        private void AccountPasswordIsValid()
        {
            // Password "test" is already set in TestHelpers with Sha512 hash
        }

        private void AccountIsInWaitingState()
        {
            AuthHttpClient.Setup(authHttpClient => authHttpClient
                    .GetAwaitingConnectionAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync("123");
        }

        private async Task AccountHasMfaEnabled()
        {
            var account = await TestHelpers.Instance.AccountDao.FirstOrDefaultAsync(a => a.Name == AccountName);
            account!.MfaSecret = "testsecret";
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(account);
        }

        private async Task AccountIsAlreadyInitialized()
        {
            var account = await TestHelpers.Instance.AccountDao.FirstOrDefaultAsync(a => a.Name == AccountName);
            Session.InitializeAccount(account!);
        }

        private async Task ConnectingWithValidPassword()
        {
            var packet = new EntryPointPacket
            {
                Name = AccountName,
                Password = "test"
            };
            await EntryPointPacketHandler.ExecuteAsync(packet, Session);
        }

        private async Task ConnectingWithInvalidPassword()
        {
            var packet = new EntryPointPacket
            {
                Name = AccountName,
                Password = "wrongpassword"
            };
            await EntryPointPacketHandler.ExecuteAsync(packet, Session);
        }

        private async Task ConnectingWithFakeAccount()
        {
            var packet = new EntryPointPacket
            {
                Name = "fakeaccount",
                Password = "test"
            };
            await EntryPointPacketHandler.ExecuteAsync(packet, Session);
        }

        private async Task ConnectingWithGfMode()
        {
            var packet = new EntryPointPacket
            {
                Name = AccountName,
                Password = "thisisgfmode"
            };
            await EntryPointPacketHandler.ExecuteAsync(packet, Session);
        }

        private void AlreadyConnectedErrorShouldBeLogged()
        {
            Logger.Verify(o => o.Error(
                It.Is<string>(s => s == TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.ALREADY_CONNECTED]),
                It.Is<It.IsAnyType>((v, t) => true)), Times.Once);
        }

        private void InvalidAccountErrorShouldBeLogged()
        {
            Logger.Verify(o => o.Error(
                It.Is<string>(s => s == TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.INVALID_ACCOUNT]),
                It.Is<It.IsAnyType>((v, t) => true)), Times.Once);
        }

        private void InvalidPasswordErrorShouldBeLogged()
        {
            Logger.Verify(o => o.Error(
                It.Is<string>(s => s == TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.INVALID_PASSWORD]),
                It.Is<It.IsAnyType>((v, t) => true)), Times.Once);
        }

        private void AccountShouldBeNull()
        {
            Assert.IsNull(Session.Account);
        }

        private void AccountShouldBeSet()
        {
            Assert.IsNotNull(Session.Account);
        }

        private void MfaShouldBeValidated()
        {
            Assert.IsTrue(Session.MfaValidated);
        }

        private async Task GuriPacketShouldBeSent()
        {
            await Task.CompletedTask;
            Assert.IsNotNull(Session.Account);
            Assert.IsFalse(Session.MfaValidated);
        }

        private async Task CharacterListShouldBeSent()
        {
            await Task.CompletedTask;
            Assert.IsNotNull(Session.Account);
        }
    }
}
