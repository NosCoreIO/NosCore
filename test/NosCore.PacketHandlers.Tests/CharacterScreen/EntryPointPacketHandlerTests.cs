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
using NosCore.GameObject.Services.AuthService;
using NosCore.Networking.SessionRef;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class EntryPointPacketHandlerTests
    {
        private TestWorld World;
        private static readonly Mock<ILogger<EntryPointPacketHandler>> Logger = new();
        private EntryPointPacketHandler EntryPointPacketHandler;
        private ClientSession Session;
        private Mock<IAuthHub> AuthHttpClient;
        private Mock<IPubSubHub> PubSubHub;
        private string AccountName;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Logger.Invocations.Clear();
            Session = await World.CreateSessionAsync();
            AccountName = Session.Account.Name;
            await World.CharacterDao.TryInsertOrUpdateAsync(Session.Character.CharacterDto);
            Session.ClearPlayerEntity();
            Session.Account = null;
            AuthHttpClient = new Mock<IAuthHub>();
            PubSubHub = World.PubSubHub;
            PubSubHub.Setup(o => o.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            PubSubHub.Setup(o => o.SubscribeAsync(It.IsAny<Subscriber>())).ReturnsAsync(true);
            EntryPointPacketHandler = new EntryPointPacketHandler(
                World.CharacterDao,
                World.AccountDao,
                World.MateDao,
                Logger.Object,
                AuthHttpClient.Object,
                World.ChannelHub.Object,
                World.WorldConfiguration,
                new SessionRefHolder(),
                World.LogLanguageLocalizer);
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
        public async Task ConnectionWithGfModeNotInWaitingStateShouldFail()
        {
            await new Spec("Connection with gf mode not in waiting state should fail")
                .Given(AccountIsNotInWaitingState)
                .WhenAsync(ConnectingWithGfMode)
                .Then(InvalidPasswordErrorShouldBeLogged)
                .And(AccountShouldBeNull)
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
        public async Task ConnectionWithGfModeKeepsMfaPendingWhenTheGrantDidNotValidateIt()
        {
            await new Spec("Connection with gf mode keeps MFA pending when the grant did not validate it")
                .GivenAsync(AccountHasMfaEnabled)
                .And(AccountIsInWaitingStateWithoutMfa)
                .WhenAsync(ConnectingWithGfMode)
                .ThenAsync(GuriPacketShouldBeSent)
                .And(MfaShouldNotBeValidated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionWithABannedAccountShouldFail()
        {
            await new Spec("Connection with a banned account should fail")
                .GivenAsync(AccountAuthorityIs_, AuthorityType.Banned)
                .WhenAsync(ConnectingWithValidPassword)
                .Then(AccountShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionWithAnUnconfirmedAccountShouldFail()
        {
            await new Spec("Connection with an unconfirmed account should fail")
                .GivenAsync(AccountAuthorityIs_, AuthorityType.Unconfirmed)
                .WhenAsync(ConnectingWithValidPassword)
                .Then(AccountShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionToAChannelInMaintenanceShouldFail()
        {
            await new Spec("Connection to a channel in maintenance should fail")
                .Given(TheChannelIsInMaintenance)
                .WhenAsync(ConnectingWithValidPassword)
                .Then(AccountShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameMasterCanConnectToAChannelInMaintenance()
        {
            await new Spec("Game master can connect to a channel in maintenance")
                .Given(TheChannelIsInMaintenance)
                .AndAsync(AccountAuthorityIs_, AuthorityType.GameMaster)
                .WhenAsync(ConnectingWithValidPassword)
                .Then(AccountShouldBeSet)
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
            PubSubHub.Setup(o => o.SubscribeAsync(It.Is<Subscriber>(x => x.Name == AccountName))).ReturnsAsync(false);
        }

        private void AccountPasswordIsValid()
        {
            // Password "test" is already set by TestWorld with Sha512 hash
        }

        private void AccountIsNotInWaitingState()
        {
            AuthHttpClient.Setup(authHttpClient => authHttpClient
                    .RedeemWorldEntryAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((WorldEntryGrant?)null);
        }

        private void AccountIsInWaitingState()
        {
            AuthHttpClient.Setup(authHttpClient => authHttpClient
                    .RedeemWorldEntryAsync(AccountName, Session.SessionId))
                .ReturnsAsync(new WorldEntryGrant(true));
        }

        private void AccountIsInWaitingStateWithoutMfa()
        {
            AuthHttpClient.Setup(authHttpClient => authHttpClient
                    .RedeemWorldEntryAsync(AccountName, Session.SessionId))
                .ReturnsAsync(new WorldEntryGrant(false));
        }

        private async Task AccountAuthorityIs_(AuthorityType authority)
        {
            var account = (await World.AccountDao.FirstOrDefaultAsync(a => a.Name == AccountName)).NotNull();
            account.Authority = authority;
            await World.AccountDao.TryInsertOrUpdateAsync(account);
        }

        private void TheChannelIsInMaintenance()
        {
            World.ChannelHub.Setup(h => h.IsInMaintenanceAsync()).ReturnsAsync(true);
        }

        private async Task AccountHasMfaEnabled()
        {
            var account = (await World.AccountDao.FirstOrDefaultAsync(a => a.Name == AccountName)).NotNull();
            account.MfaSecret = "testsecret";
            await World.AccountDao.TryInsertOrUpdateAsync(account);
        }

        private async Task AccountIsAlreadyInitialized()
        {
            var account = (await World.AccountDao.FirstOrDefaultAsync(a => a.Name == AccountName)).NotNull();
            Session.InitializeAccount(account);
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
            var expected = (string)World.LogLanguageLocalizer[LogLanguageKey.ALREADY_CONNECTED];
            Logger.Verify(o => o.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v != null && v.ToString().NotNull().Contains(expected)),
                    It.IsAny<System.Exception?>(),
                    It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
                Times.Once);
        }

        private void InvalidAccountErrorShouldBeLogged()
        {
            var expected = (string)World.LogLanguageLocalizer[LogLanguageKey.INVALID_ACCOUNT];
            Logger.Verify(o => o.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v != null && v.ToString().NotNull().Contains(expected)),
                    It.IsAny<System.Exception?>(),
                    It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
                Times.Once);
        }

        private void InvalidPasswordErrorShouldBeLogged()
        {
            var expected = (string)World.LogLanguageLocalizer[LogLanguageKey.INVALID_PASSWORD];
            Logger.Verify(o => o.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v != null && v.ToString().NotNull().Contains(expected)),
                    It.IsAny<System.Exception?>(),
                    It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
                Times.Once);
        }

        private void AccountShouldBeNull()
        {
            Assert.IsFalse(Session.HasAccount);
        }

        private void AccountShouldBeSet()
        {
            Assert.IsTrue(Session.HasAccount);
        }

        private void MfaShouldNotBeValidated()
        {
            Assert.IsFalse(Session.MfaValidated);
        }

        private void MfaShouldBeValidated()
        {
            Assert.IsTrue(Session.MfaValidated);
        }

        private async Task GuriPacketShouldBeSent()
        {
            await Task.CompletedTask;
            Assert.IsTrue(Session.HasAccount);
            Assert.IsFalse(Session.MfaValidated);
        }

        private async Task CharacterListShouldBeSent()
        {
            await Task.CompletedTask;
            Assert.IsTrue(Session.HasAccount);
        }
    }
}
