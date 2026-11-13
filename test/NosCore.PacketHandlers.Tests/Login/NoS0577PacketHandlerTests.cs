//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.Mvc;
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
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Login
{
    [TestClass]
    public class NoS0577PacketHandlerSpecs
    {
        private TestWorld World;
        private readonly string TokenGuid = Guid.NewGuid().ToString();
        private IHasher Encryption;
        private readonly Mock<IAuthHub> AuthHttpClient = new();
        private Mock<IPubSubHub> PubSubHub => World.PubSubHub;
        private readonly IOptions<LoginConfiguration> LoginConfiguration = Options.Create(new LoginConfiguration
        {
            Database = new(),
            MasterCommunication = new WebApiConfiguration()
        });

        private NoS0577PacketHandler NoS0577PacketHandler;
        private ClientSession Session;
        private Mock<IChannelHub> ChannelHub;
        private SessionRefHolder SessionRefHolder;

        private static string GuidToToken(string token)
        {
            return string.Join("", token.ToCharArray().Select(s => Convert.ToByte(s).ToString("x")));
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            ChannelHub = new Mock<IChannelHub>();
            SessionRefHolder = new SessionRefHolder();
            Session = await World.CreateSessionAsync();
            SessionRefHolder[Session.Channel.NotNull().Id] = new RegionTypeMapping(Session.SessionId, RegionType.EN);
            AuthHttpClient.Setup(s => s.GetAwaitingConnectionAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>())).ReturnsAsync((string a, string b, int c) =>
                (string?)((new OkObjectResult(Session.Account.Name)).Value));
            NoS0577PacketHandler = new NoS0577PacketHandler(new LoginService(LoginConfiguration,
                World.AccountDao,
                AuthHttpClient.Object, PubSubHub.Object, ChannelHub.Object, World.CharacterDao, SessionRefHolder,
                World.Create<PenaltyService>()));
        }

        [TestMethod]
        public async Task LoginWithBcryptShouldSucceed()
        {
            await new Spec("Login with bcrypt should succeed")
                .Given(ServerIsAvailable)
                .And(AccountUsesBcrypt)
                .WhenAsync(LoggingInWithToken)
                .Then(ShouldReceiveNsTestPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithPbkdf2ShouldSucceed()
        {
            await new Spec("Login with pbkdf2 should succeed")
                .Given(ServerIsAvailable)
                .And(AccountUsesPbkdf_, 2)
                .WhenAsync(LoggingInWithToken)
                .Then(ShouldReceiveNsTestPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithOldClientShouldFail()
        {
            await new Spec("Login with old client should fail")
                .Given(ClientVersionIsSet)
                .WhenAsync(LoggingInWithToken)
                .Then(ShouldReceiveOldClientError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithWrongTokenShouldFail()
        {
            await new Spec("Login with wrong token should fail")
                .Given(TokenIsInvalid)
                .WhenAsync(LoggingInWithWrongToken)
                .Then(ShouldReceiveAccountWrongError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithCorrectCredentialsShouldSucceed()
        {
            await new Spec("Login with correct credentials should succeed")
                .Given(ServerIsAvailable)
                .WhenAsync(LoggingInWithToken)
                .Then(ShouldReceiveNsTestPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWhenAlreadyConnectedShouldFail()
        {
            await new Spec("Login when already connected should fail")
                .Given(AccountIsAlreadyConnected)
                .WhenAsync(LoggingInWithToken)
                .Then(ShouldReceiveAlreadyConnectedError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginWithNoServerShouldFail()
        {
            await new Spec("Login with no server should fail")
                .Given(NoServerIsAvailable)
                .WhenAsync(LoggingInWithToken)
                .Then(ShouldReceiveCantConnectError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginDuringMaintenanceShouldFail()
        {
            await new Spec("Login during maintenance should fail")
                .Given(ServerIsInMaintenance)
                .WhenAsync(LoggingInWithToken)
                .Then(ShouldReceiveMaintenanceError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginDuringMaintenanceAsGmShouldSucceed()
        {
            await new Spec("Login during maintenance as gm should succeed")
                .Given(ServerIsInMaintenance)
                .AndAsync(AccountIsGameMaster)
                .WhenAsync(LoggingInWithToken)
                .Then(ShouldReceiveNsTestPacket)
                .ExecuteAsync();
        }

        private void ServerIsAvailable()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() { Host = "127.0.0.1" } });
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>());
        }

        private void AccountUsesBcrypt()
        {
            Encryption = new BcryptHasher();
            Session.Account.NewAuthSalt = BCrypt.Net.BCrypt.GenerateSalt();
            Session.Account.NewAuthPassword = Encryption.Hash(TokenGuid, Session.Account.NewAuthSalt);
        }

        private void AccountUsesPbkdf_(int value)
        {
            Encryption = new Pbkdf2Hasher();
            Session.Account.NewAuthPassword = Encryption.Hash(TokenGuid, "MYSUPERSECRETHASH");
            Session.Account.NewAuthSalt = "MYSUPERSECRETHASH";
        }

        private void ClientVersionIsSet()
        {
            LoginConfiguration.Value.ClientVersion = new ClientVersionSubPacket { Major = 1 };
        }

        private void TokenIsInvalid()
        {
            AuthHttpClient.Setup(s => s.GetAwaitingConnectionAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>())).ReturnsAsync((string a, string b, int c) =>
                (string?)((new OkObjectResult(null)).Value));
        }

        private void AccountIsAlreadyConnected()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() { Host = "127.0.0.1" } });
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>
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
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() { Host = "127.0.0.1", IsMaintenance = true } });
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>());
        }

        private async Task AccountIsGameMaster()
        {
            Session.Account.Authority = AuthorityType.GameMaster;
            await World.AccountDao.TryInsertOrUpdateAsync(Session.Account);
        }

        private async Task LoggingInWithToken()
        {
            await NoS0577PacketHandler.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(TokenGuid),
            }, Session);
        }

        private async Task LoggingInWithWrongToken()
        {
            await NoS0577PacketHandler.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(Guid.NewGuid().ToString()),
            }, Session);
        }

        private void ShouldReceiveNsTestPacket()
        {
            Assert.IsNotNull((NsTestPacket?)Session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        private void ShouldReceiveOldClientError()
        {
            Assert.IsTrue(((FailcPacket?)Session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.OldClient);
        }

        private void ShouldReceiveAccountWrongError()
        {
            Assert.IsTrue(((FailcPacket?)Session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AccountOrPasswordWrong);
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
