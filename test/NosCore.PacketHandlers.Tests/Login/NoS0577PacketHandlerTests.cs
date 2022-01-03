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
using NosCore.Core.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.Networking;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;
using NosCore.PacketHandlers.Login;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;

namespace NosCore.PacketHandlers.Tests.Login
{
    [TestClass]
    public class NoS0577PacketHandlerTests
    {
        private readonly string _tokenGuid = Guid.NewGuid().ToString();
        private IHasher _encryption = null!;
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly Mock<IAuthHttpClient> _authHttpClient = new();
        private readonly Mock<IChannelHttpClient> _channelHttpClient = TestHelpers.Instance.ChannelHttpClient;
        private readonly Mock<IConnectedAccountHttpClient> _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
        private readonly IOptions<LoginConfiguration> _loginConfiguration = Options.Create(new LoginConfiguration
        {
            MasterCommunication = new WebApiConfiguration()
        });
        private NoS0577PacketHandler? _noS0577PacketHandler;
        private ClientSession? _session;

        private static string GuidToToken(string token)
        {
            return string.Join("", token.ToCharArray().Select(s => Convert.ToByte(s).ToString("x")));
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _noS0577PacketHandler = new NoS0577PacketHandler(new LoginService(_loginConfiguration,
                TestHelpers.Instance.AccountDao,
                _authHttpClient.Object, _channelHttpClient.Object, _connectedAccountHttpClient.Object, TestHelpers.Instance.CharacterDao, new SessionRefHolder()));
            var authController = new AuthController(Options.Create(_loginConfiguration.Value.MasterCommunication),
                TestHelpers.Instance.AccountDao, Logger, _encryption);
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session.Account.Name;
            _authHttpClient.Setup(s => s.GetAwaitingConnectionAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>())).ReturnsAsync((string a, string b, int c) =>
                (string?)((OkObjectResult)authController.GetExpectingConnection(a, b, c)).Value);
            SessionFactory.Instance.ReadyForAuth.Clear();
        }

        [TestMethod]
        public async Task LoginBCryptAsync()
        {
            _encryption = new BcryptHasher();
            _channelHttpClient.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> { new() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            _session!.Account.NewAuthSalt = BCrypt.Net.BCrypt.GenerateSalt();
            _session.Account.NewAuthPassword = _encryption.Hash(_tokenGuid, _session.Account.NewAuthSalt);

            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session.Account.Name;
            await _noS0577PacketHandler!.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session).ConfigureAwait(false);


            Assert.IsNotNull((NsTestPacket?)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public async Task LoginPbkdf2Async()
        {
            _encryption = new Pbkdf2Hasher();
            _channelHttpClient.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> { new() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            _session!.Account.NewAuthPassword = _encryption.Hash(_tokenGuid, "MY_SUPER_SECRET_HASH");
            _session.Account.NewAuthSalt = "MY_SUPER_SECRET_HASH";
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session.Account.Name;
            await _noS0577PacketHandler!.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session).ConfigureAwait(false);

            Assert.IsNotNull((NsTestPacket?)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public async Task LoginOldClientAsync()
        {
            _loginConfiguration.Value.ClientVersion = new ClientVersionSubPacket { Major = 1 };
            await _noS0577PacketHandler!.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session!).ConfigureAwait(false);

            Assert.IsTrue(((FailcPacket?)_session!.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.OldClient);
        }

        [TestMethod]
        public async Task LoginWrongTokenAsync()
        {
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session!.Account.Name;
            await _noS0577PacketHandler!.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(Guid.NewGuid().ToString()),
            }, _session).ConfigureAwait(false);

            Assert.IsTrue(((FailcPacket?)_session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AccountOrPasswordWrong);
        }

        [TestMethod]
        public async Task LoginAsync()
        {
            _channelHttpClient.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> { new() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session!.Account.Name;
            await _noS0577PacketHandler!.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session).ConfigureAwait(false);

            Assert.IsNotNull((NsTestPacket?)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public async Task LoginAlreadyConnectedAsync()
        {
            _channelHttpClient.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> { new() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>
                    {new() {Name = _session!.Account.Name}});
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session.Account.Name;
            await _noS0577PacketHandler!.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(((FailcPacket?)_session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AlreadyConnected);
        }

        [TestMethod]
        public async Task LoginNoServerAsync()
        {
            _channelHttpClient.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo>());
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session!.Account.Name;
            await _noS0577PacketHandler!.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(((FailcPacket?)_session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.CantConnect);
        }

        //[TestMethod]
        //public async Task LoginBanned()
        //{
        //    _handler.VerifyLogin(new NoS0575Packet
        //    {
        //        Password ="test".Sha512(),
        //        Name = Name,
        //    });
        //    Assert.IsTrue(_session.LastPacket is FailcPacket);
        //    Assert.IsTrue(((FailcPacket) _session.LastPacket).Type == LoginFailType.Banned);
        //}

        [TestMethod]
        public async Task LoginMaintenanceAsync()
        {
            _channelHttpClient.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> { new() { IsMaintenance = true } });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session!.Account.Name;
            await _noS0577PacketHandler!.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session).ConfigureAwait(false);

            Assert.IsTrue(((FailcPacket?)_session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.Maintenance);
        }

        [TestMethod]
        public async Task LoginMaintenanceGameMasterAsync()
        {
            _channelHttpClient.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> { new() { IsMaintenance = true } });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            _session!.Account.Authority = AuthorityType.GameMaster;
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(_session!.Account);
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session!.Account.Name;
            await _noS0577PacketHandler!.ExecuteAsync(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session).ConfigureAwait(false);

            Assert.IsNotNull((NsTestPacket?)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }
    }
}