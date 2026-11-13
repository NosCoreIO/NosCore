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
using NosCore.GameObject.Services.PenaltyService;
using NosCore.Networking;
using NosCore.Networking.SessionRef;
using NosCore.PacketHandlers.Login;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Login
{
    [TestClass]
    public class NoS0575PacketHandlerSpecs
    {
        private TestWorld World;
        private static readonly ILogger<NoS0575PacketHandler> Logger = new Mock<ILogger<NoS0575PacketHandler>>().Object;
        private string Password;
        private Mock<IAuthHub> AuthHttpClient;
        private Mock<IPubSubHub> PubSubHub;
        private IOptions<LoginConfiguration> LoginConfiguration;
        private NoS0575PacketHandler NoS0575PacketHandler;
        private ClientSession Session;
        private Mock<IChannelHub> ChannelHub;
        private SessionRefHolder SessionRefHolder;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Password = new Sha512Hasher().Hash("test").ToUpperInvariant();
            Session = await World.CreateSessionAsync();
            AuthHttpClient = new Mock<IAuthHub>();
            PubSubHub = World.PubSubHub;
            LoginConfiguration = Options.Create(new LoginConfiguration { Database = new(), MasterCommunication = new() });
            SessionRefHolder = new SessionRefHolder();
            ChannelHub = new Mock<IChannelHub>();
            SessionRefHolder[Session.Channel.NotNull().Id] = new RegionTypeMapping(Session.SessionId, RegionType.EN);
            NoS0575PacketHandler = new NoS0575PacketHandler(new LoginService(LoginConfiguration,
                    World.AccountDao,
                    AuthHttpClient.Object, PubSubHub.Object, ChannelHub.Object, World.CharacterDao, SessionRefHolder,
                    World.Create<PenaltyService>()),
                LoginConfiguration, Logger, World.LogLanguageLocalizer);
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
        public async Task LoginWithWrongCapsGetsTheSameAnswerAsAnUnknownAccount()
        {
            await new Spec("Login with wrong caps gets the same answer as an unknown account")
                .WhenAsync(LoggingInWithUppercaseNameAsync)
                .Then(ShouldReceiveAccountWrongError)
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
        public async Task LoginDuringATimedBanShouldFail()
        {
            await new Spec("Login during a timed ban should fail")
                .Given(ServerIsAvailable)
                .AndAsync(TheAccountIsBannedFor_Days, 3)
                .WhenAsync(LoggingInCorrectlyAsync)
                .Then(ShouldReceiveBannedError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginAfterATimedBanShouldSucceed()
        {
            await new Spec("Login after a timed ban should succeed")
                .Given(ServerIsAvailable)
                .AndAsync(TheAccountIsBannedFor_Days, 3)
                .And(Time_Passes, NodaTime.Duration.FromDays(3))
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
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() { Host = "127.0.0.1", Type = ServerType.WorldServer } });
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>());
        }

        private void AccountIsAlreadyConnected()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() { Host = "127.0.0.1", Type = ServerType.WorldServer } });
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
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() { Host = "127.0.0.1", Type = ServerType.WorldServer, IsMaintenance = true } });
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>());
        }

        private async Task AccountIsGameMasterAsync()
        {
            Session.Account.Authority = AuthorityType.GameMaster;
            await World.AccountDao.TryInsertOrUpdateAsync(Session.Account);
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
                Password = encryption.Hash("test1").ToUpperInvariant(),
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

        private Task TheAccountIsBannedFor_Days(int days)
        {
            var now = World.Clock.GetCurrentInstant();
            return World.PenaltyLogDao.TryInsertOrUpdateAsync(new NosCore.Data.Dto.PenaltyLogDto
            {
                AccountId = Session.Account.AccountId,
                Penalty = NosCore.Data.Enumerations.Interaction.PenaltyType.Banned,
                DateStart = now,
                DateEnd = now.Plus(NodaTime.Duration.FromDays(days)),
                AdminName = "admin",
                Reason = "test",
            });
        }

        private void Time_Passes(NodaTime.Duration time) => World.Clock.Advance(time);

        private void ShouldReceiveBannedError()
        {
            Assert.IsTrue(((FailcPacket?)Session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.Banned);
            Assert.IsNull(Session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
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
