//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.LoginService;
using NosCore.Networking;
using NosCore.Networking.SessionRef;
using NosCore.PacketHandlers.Login;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Login
{
    [TestClass]
    public class NoS0575PacketHandlerSpecs
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private string Password = null!;
        private Mock<IAuthHub> AuthHttpClient = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private IOptions<LoginConfiguration> LoginConfiguration = null!;
        private NoS0575PacketHandler NoS0575PacketHandler = null!;
        private ClientSession Session = null!;
        private Mock<IChannelHub> ChannelHub = null!;
        private SessionRefHolder SessionRefHolder = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Password = new Sha512Hasher().Hash("test");
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            AuthHttpClient = new Mock<IAuthHub>();
            PubSubHub = TestHelpers.Instance.PubSubHub;
            LoginConfiguration = Options.Create(new LoginConfiguration());
            SessionRefHolder = new SessionRefHolder();
            ChannelHub = new Mock<IChannelHub>();
            SessionRefHolder[Session.Channel!.Id] = new RegionTypeMapping(Session.SessionId, RegionType.EN);
            NoS0575PacketHandler = new NoS0575PacketHandler(new LoginService(LoginConfiguration,
                    TestHelpers.Instance.AccountDao,
                    AuthHttpClient.Object, PubSubHub.Object, ChannelHub.Object, TestHelpers.Instance.CharacterDao, SessionRefHolder),
                LoginConfiguration, Logger, TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task LoginWithOldClientShouldFail()
        {
            await new Spec("Login with old client should fail")
                .Given(ClientVersionIsSet)
                .WhenAsync(LoggingInWithUppercaseNameAsync)
                .Then(ShouldReceiveOldClientError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithOldAuthWhenNewEnforcedShouldFail()
        {
            await new Spec("Login with old auth when new enforced should fail")
                .Given(NewAuthIsEnforced)
                .WhenAsync(LoggingInWithUppercaseNameAsync)
                .Then(NoPacketsShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithNoAccountShouldFail()
        {
            await new Spec("Login with no account should fail")
                .WhenAsync(LoggingInWithFakeAccountAsync)
                .Then(ShouldReceiveAccountWrongError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithWrongCapsShouldFail()
        {
            await new Spec("Login with wrong caps should fail")
                .WhenAsync(LoggingInWithUppercaseNameAsync)
                .Then(ShouldReceiveWrongCapsError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithWrongPasswordShouldFail()
        {
            await new Spec("Login with wrong password should fail")
                .WhenAsync(LoggingInWithWrongPasswordAsync)
                .Then(ShouldReceiveAccountWrongError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithCorrectCredentialsShouldSucceed()
        {
            await new Spec("Login with correct credentials should succeed")
                .Given(ServerIsAvailable)
                .WhenAsync(LoggingInCorrectlyAsync)
                .Then(ShouldReceiveNsTestPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWhenAlreadyConnectedShouldFail()
        {
            await new Spec("Login when already connected should fail")
                .Given(AccountIsAlreadyConnected)
                .WhenAsync(LoggingInCorrectlyAsync)
                .Then(ShouldReceiveAlreadyConnectedError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithNoServerShouldFail()
        {
            await new Spec("Login with no server should fail")
                .Given(NoServerIsAvailable)
                .WhenAsync(LoggingInCorrectlyAsync)
                .Then(ShouldReceiveCantConnectError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginDuringMaintenanceShouldFail()
        {
            await new Spec("Login during maintenance should fail")
                .Given(ServerIsInMaintenance)
                .WhenAsync(LoggingInCorrectlyAsync)
                .Then(ShouldReceiveMaintenanceError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginDuringMaintenanceAsGmShouldSucceed()
        {
            await new Spec("Login during maintenance as gm should succeed")
                .Given(ServerIsInMaintenance)
                .AndAsync(AccountIsGameMasterAsync)
                .WhenAsync(LoggingInCorrectlyAsync)
                .Then(ShouldReceiveNsTestPacket)
                .ExecuteAsync();
        }

        private void ClientVersionIsSet()
        {
            LoginConfiguration.Value.ClientVersion = new ClientVersionSubPacket { Major = 1 };
        }

        private void NewAuthIsEnforced()
        {
            LoginConfiguration.Value.EnforceNewAuth = true;
        }

        private void ServerIsAvailable()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() });
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>());
        }

        private void AccountIsAlreadyConnected()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() });
            PubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(
                new List<Subscriber>
                    { new() { Name = Session.Account.Name } });
        }

        private void NoServerIsAvailable()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo>());
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>());
        }

        private void ServerIsInMaintenance()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() { IsMaintenance = true } });
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>());
        }

        private async Task AccountIsGameMasterAsync()
        {
            Session.Account.Authority = AuthorityType.GameMaster;
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(Session.Account);
        }

        private async Task LoggingInWithUppercaseNameAsync()
        {
            await NoS0575PacketHandler.ExecuteAsync(new NoS0575Packet
            {
                Password = Password,
                Username = Session.Account.Name.ToUpperInvariant()
            }, Session);
        }

        private async Task LoggingInWithFakeAccountAsync()
        {
            await NoS0575PacketHandler.ExecuteAsync(new NoS0575Packet
            {
                Password = Password,
                Username = "noaccount"
            }, Session);
        }

        private async Task LoggingInWithWrongPasswordAsync()
        {
            var encryption = new Sha512Hasher();
            await NoS0575PacketHandler.ExecuteAsync(new NoS0575Packet
            {
                Password = encryption.Hash("test1"),
                Username = Session.Account.Name
            }, Session);
        }

        private async Task LoggingInCorrectlyAsync()
        {
            await NoS0575PacketHandler.ExecuteAsync(new NoS0575Packet
            {
                Password = Password,
                Username = Session.Account.Name
            }, Session);
        }

        private void ShouldReceiveOldClientError()
        {
            Assert.IsTrue(((FailcPacket?)Session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.OldClient);
        }

        private void NoPacketsShouldBeSent()
        {
            Assert.IsTrue(Session.LastPackets.Count == 0);
        }

        private void ShouldReceiveAccountWrongError()
        {
            Assert.IsTrue(((FailcPacket?)Session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AccountOrPasswordWrong);
        }

        private void ShouldReceiveWrongCapsError()
        {
            Assert.IsTrue(((FailcPacket?)Session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.WrongCaps);
        }

        private void ShouldReceiveNsTestPacket()
        {
            Assert.IsNotNull((NsTestPacket?)Session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        private void ShouldReceiveAlreadyConnectedError()
        {
            Assert.IsTrue(((FailcPacket?)Session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AlreadyConnected);
        }

        private void ShouldReceiveCantConnectError()
        {
            Assert.IsTrue(((FailcPacket?)Session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.CantConnect);
        }

        private void ShouldReceiveMaintenanceError()
        {
            Assert.IsTrue(((FailcPacket?)Session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.Maintenance);
        }
    }
}
