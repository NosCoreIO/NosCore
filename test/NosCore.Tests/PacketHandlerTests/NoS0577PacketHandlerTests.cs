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
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.AuthHttpClient;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;
using NosCore.PacketHandlers.Login;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class NoS0577PacketHandlerTests
    {
        private readonly string _tokenGuid = Guid.NewGuid().ToString();
        private static readonly ILogger Logger = Core.I18N.Logger.GetLoggerConfiguration().CreateLogger();
        private readonly Mock<IAuthHttpClient> _authHttpClient = new Mock<IAuthHttpClient>();
        private readonly Mock<IChannelHttpClient> _channelHttpClient = TestHelpers.Instance.ChannelHttpClient;
        private readonly Mock<IConnectedAccountHttpClient> _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
        private readonly LoginConfiguration _loginConfiguration = new LoginConfiguration
        {
            MasterCommunication = new WebApiConfiguration()
        };
        private NoS0577PacketHandler? _noS0577PacketHandler;
        private ClientSession? _session;

        private string GuidToToken(string token)
        {
            return string.Join("", token.ToCharArray().Select(s => Convert.ToByte(s).ToString("x")));
        }

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _noS0577PacketHandler = new NoS0577PacketHandler(new LoginService(_loginConfiguration,
                TestHelpers.Instance.AccountDao,
                _authHttpClient.Object, _channelHttpClient.Object, _connectedAccountHttpClient.Object));
            var authController = new AuthController(_loginConfiguration.MasterCommunication!,
                TestHelpers.Instance.AccountDao, Logger);
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session.Account.Name;
            _authHttpClient.Setup(s => s.GetAwaitingConnection(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>())).ReturnsAsync((string a, string b, int c) =>
                (string?)((OkObjectResult)authController.GetExpectingConnection(a, b, c)).Value);
            SessionFactory.Instance.ReadyForAuth.Clear();
        }

        [TestMethod]
        public async Task LoginBCrypt()
        {
            _loginConfiguration.MasterCommunication!.HashingType = HashingType.BCrypt;
            _channelHttpClient.Setup(s => s.GetChannels()).ReturnsAsync(new List<ChannelInfo> { new ChannelInfo() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            _session!.Account.NewAuthSalt = BCrypt.Net.BCrypt.GenerateSalt();
            _session.Account.NewAuthPassword = _tokenGuid.ToBcrypt(_session.Account.NewAuthSalt);

            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session.Account.Name;
            await _noS0577PacketHandler!.Execute(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session);


            Assert.IsNotNull((NsTestPacket?)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public async Task LoginPbkdf2()
        {
            _loginConfiguration.MasterCommunication!.HashingType = HashingType.Pbkdf2;
            _channelHttpClient.Setup(s => s.GetChannels()).ReturnsAsync(new List<ChannelInfo> { new ChannelInfo() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            _session!.Account.NewAuthPassword = _tokenGuid.ToPbkdf2Hash("MY_SUPER_SECRET_HASH");
            _session.Account.NewAuthSalt = "MY_SUPER_SECRET_HASH";
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session.Account.Name;
            await _noS0577PacketHandler!.Execute(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session);

            Assert.IsNotNull((NsTestPacket?)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public async Task LoginOldClient()
        {
            _loginConfiguration.ClientVersion = new ClientVersionSubPacket { Major = 1 };
            await _noS0577PacketHandler!.Execute(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session!);

            Assert.IsTrue(((FailcPacket?)_session!.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.OldClient);
        }

        [TestMethod]
        public async Task LoginWrongToken()
        {
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session!.Account.Name;
            await _noS0577PacketHandler!.Execute(new NoS0577Packet
            {
                AuthToken = GuidToToken(Guid.NewGuid().ToString()),
            }, _session);

            Assert.IsTrue(((FailcPacket?)_session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AccountOrPasswordWrong);
        }

        [TestMethod]
        public async Task LoginAsync()
        {
            _channelHttpClient.Setup(s => s.GetChannels()).ReturnsAsync(new List<ChannelInfo> { new ChannelInfo() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session!.Account.Name;
            await _noS0577PacketHandler!.Execute(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session);

            Assert.IsNotNull((NsTestPacket?)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public async Task LoginAlreadyConnected()
        {
            _channelHttpClient.Setup(s => s.GetChannels()).ReturnsAsync(new List<ChannelInfo> { new ChannelInfo() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>
                    {new ConnectedAccount {Name = _session!.Account.Name}});
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session.Account.Name;
            await _noS0577PacketHandler!.Execute(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session);
            Assert.IsTrue(((FailcPacket?)_session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AlreadyConnected);
        }

        [TestMethod]
        public async Task LoginNoServer()
        {
            _channelHttpClient.Setup(s => s.GetChannels()).ReturnsAsync(new List<ChannelInfo>());
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            SessionFactory.Instance.AuthCodes[_tokenGuid] = _session!.Account.Name;
            await _noS0577PacketHandler!.Execute(new NoS0577Packet
            {
                AuthToken = GuidToToken(_tokenGuid),
            }, _session);
            Assert.IsTrue(((FailcPacket?)_session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.CantConnect);
        }

        //[TestMethod]
        //public void LoginBanned()
        //{
        //    _handler.VerifyLogin(new NoS0575Packet
        //    {
        //        Password ="test".Sha512(),
        //        Name = Name,
        //    });
        //    Assert.IsTrue(_session.LastPacket is FailcPacket);
        //    Assert.IsTrue(((FailcPacket) _session.LastPacket).Type == LoginFailType.Banned);
        //}

        //[TestMethod]
        //public void LoginMaintenance()
        //{
        //    _handler.VerifyLogin(new NoS0575Packet
        //    {
        //        Password ="test".Sha512(),
        //        Name = Name,
        //    });
        //    Assert.IsTrue(_session.LastPacket is FailcPacket);
        //    Assert.IsTrue(((FailcPacket)_session.LastPacket.FirstOrDefault(s => s is FailcPacket)).Type == LoginFailType.Maintenance);
        //}
    }
}