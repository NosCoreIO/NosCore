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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;
using NosCore.PacketHandlers.Login;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class NoS0575PacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly string _password = "test".ToSha512();
        private Mock<IAuthHttpClient>? _authHttpClient;
        private Mock<IChannelHttpClient>? _channelHttpClient;
        private Mock<IConnectedAccountHttpClient>? _connectedAccountHttpClient;
        private IOptions<LoginConfiguration>? _loginConfiguration;
        private NoS0575PacketHandler? _noS0575PacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _authHttpClient = new Mock<IAuthHttpClient>();
            _channelHttpClient = TestHelpers.Instance.ChannelHttpClient;
            _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
            _loginConfiguration = Options.Create(new LoginConfiguration());
            _noS0575PacketHandler = new NoS0575PacketHandler(new LoginService(_loginConfiguration,
                    TestHelpers.Instance.AccountDao,
                    _authHttpClient.Object, _channelHttpClient.Object, _connectedAccountHttpClient.Object),
                _loginConfiguration, Logger);
        }

        [TestMethod]
        public async Task LoginOldClientAsync()
        {
            _loginConfiguration!.Value.ClientVersion = new ClientVersionSubPacket {Major = 1};
            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = _password,
                Username = _session!.Account.Name.ToUpperInvariant()
            }, _session).ConfigureAwait(false);

            Assert.IsTrue(((FailcPacket?) _session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.OldClient);
        }

        [TestMethod]
        public async Task LoginOldAuthWithNewAuthEnforcedAsync()
        {
            _loginConfiguration!.Value.EnforceNewAuth = true;
            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = _password,
                Username = _session!.Account.Name.ToUpperInvariant()
            }, _session).ConfigureAwait(false);

            Assert.IsTrue(_session.LastPackets.Count == 0);
        }

        [TestMethod]
        public async Task LoginNoAccountAsync()
        {
            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = _password,
                Username = "noaccount"
            }, _session!).ConfigureAwait(false);

            Assert.IsTrue(((FailcPacket?) _session!.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AccountOrPasswordWrong);
        }

        [TestMethod]
        public async Task LoginWrongCapsAsync()
        {
            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = _password,
                Username = _session!.Account.Name.ToUpperInvariant()
            }, _session).ConfigureAwait(false);

            Assert.IsTrue(((FailcPacket?) _session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.WrongCaps);
        }

        [TestMethod]
        public async Task LoginWrongPAsswordAsync()
        {
            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = "test1".ToSha512(),
                Username = _session!.Account.Name
            }, _session).ConfigureAwait(false);

            Assert.IsTrue(((FailcPacket?) _session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AccountOrPasswordWrong);
        }

        [TestMethod]
        public async Task LoginAsync()
        {
            _channelHttpClient!.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> {new ChannelInfo()});
            _connectedAccountHttpClient!.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = _password,
                Username = _session!.Account.Name
            }, _session).ConfigureAwait(false);

            Assert.IsNotNull((NsTestPacket?) _session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public async Task LoginAlreadyConnectedAsync()
        {
            _channelHttpClient!.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> {new ChannelInfo()});
            _connectedAccountHttpClient!.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>())).ReturnsAsync(
                new List<ConnectedAccount>
                    {new ConnectedAccount {Name = _session!.Account.Name}});
            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = _password,
                Username = _session.Account.Name
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(((FailcPacket?) _session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.AlreadyConnected);
        }

        [TestMethod]
        public async Task LoginNoServerAsync()
        {
            _channelHttpClient!.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo>());
            _connectedAccountHttpClient!.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());

            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = _password,
                Username = _session!.Account.Name
            }, _session).ConfigureAwait(false);
            Assert.IsTrue(((FailcPacket?) _session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
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


        [TestMethod]
        public async Task LoginMaintenanceAsync()
        {
            _channelHttpClient!.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> { new ChannelInfo { IsMaintenance = true } });
            _connectedAccountHttpClient!.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = _password,
                Username = _session!.Account.Name
            }, _session).ConfigureAwait(false);

            Assert.IsTrue(((FailcPacket?)_session.LastPackets.FirstOrDefault(s => s is FailcPacket))?.Type ==
                LoginFailType.Maintenance);
        }

        [TestMethod]
        public async Task LoginMaintenanceGameMasterAsync()
        {
            _channelHttpClient!.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelInfo> { new ChannelInfo { IsMaintenance = true } });
            _connectedAccountHttpClient!.Setup(s => s.GetConnectedAccountAsync(It.IsAny<ChannelInfo>()))
                .ReturnsAsync(new List<ConnectedAccount>());
            _session!.Account.Authority = AuthorityType.GameMaster;
            await TestHelpers.Instance.AccountDao.TryInsertOrUpdateAsync(_session!.Account);
            await _noS0575PacketHandler!.ExecuteAsync(new NoS0575Packet
            {
                Password = _password,
                Username = _session!.Account.Name
            }, _session).ConfigureAwait(false);

            Assert.IsNotNull((NsTestPacket?)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }
    }
}