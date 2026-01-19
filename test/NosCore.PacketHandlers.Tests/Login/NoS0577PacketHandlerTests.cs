//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

namespace NosCore.PacketHandlers.Tests.Login
{
    [TestClass]
    public class NoS0577PacketHandlerSpecs
    {
        private readonly string TokenGuid = Guid.NewGuid().ToString();
        private IHasher Encryption = null!;
        private readonly Mock<IAuthHub> AuthHttpClient = new();
        private readonly Mock<IPubSubHub> PubSubHub = TestHelpers.Instance.PubSubHub;
        private readonly IOptions<LoginConfiguration> LoginConfiguration = Options.Create(new LoginConfiguration
        {
            MasterCommunication = new WebApiConfiguration()
        });

        private NoS0577PacketHandler NoS0577PacketHandler = null!;
        private ClientSession Session = null!;
        private Mock<IChannelHub> ChannelHub = null!;
        private SessionRefHolder SessionRefHolder = null!;

        private static string GuidToToken(string token)
        {
            return string.Join("", token.ToCharArray().Select(s => Convert.ToByte(s).ToString("x")));
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            ChannelHub = new Mock<IChannelHub>();
            SessionRefHolder = new SessionRefHolder();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            SessionRefHolder[Session.Channel!.Id] = new RegionTypeMapping(Session.SessionId, RegionType.EN);
            AuthHttpClient.Setup(s => s.GetAwaitingConnectionAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>())).ReturnsAsync((string a, string b, int c) =>
                (string?)((new OkObjectResult(Session.Account.Name)).Value));
            NoS0577PacketHandler = new NoS0577PacketHandler(new LoginService(LoginConfiguration,
                TestHelpers.Instance.AccountDao,
                AuthHttpClient.Object, PubSubHub.Object, ChannelHub.Object, TestHelpers.Instance.CharacterDao, SessionRefHolder));
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
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() });
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
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() });
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
            ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo> { new() { IsMaintenance = true } });
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>());
        }

        private async Task AccountIsGameMaster()
        {
            Session.Account.Authority = AuthorityType.GameMaster;
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(Session.Account);
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
