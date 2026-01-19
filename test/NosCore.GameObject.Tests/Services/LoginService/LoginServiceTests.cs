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

using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.LoginService;
using NosCore.Networking.SessionRef;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.LoginService
{
    [TestClass]
    public class LoginServiceTests
    {
        private ILoginService Service = null!;
        private Mock<IAuthHub> AuthHub = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<IChannelHub> ChannelHub = null!;
        private SessionRefHolder SessionRefHolder = null!;
        private IOptions<LoginConfiguration> LoginConfig = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            SessionRefHolder = new SessionRefHolder();

            AuthHub = new Mock<IAuthHub>();
            PubSubHub = new Mock<IPubSubHub>();
            ChannelHub = new Mock<IChannelHub>();

            LoginConfig = Options.Create(new LoginConfiguration());

            Service = new GameObject.Services.LoginService.LoginService(
                LoginConfig,
                TestHelpers.Instance.AccountDao,
                AuthHub.Object,
                PubSubHub.Object,
                ChannelHub.Object,
                TestHelpers.Instance.CharacterDao,
                SessionRefHolder);
        }

        [TestMethod]
        public async Task ServiceCanBeConstructed()
        {
            await new Spec("Service can be constructed")
                .Then(ServiceShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ServiceImplementsInterface()
        {
            await new Spec("Service implements interface")
                .Then(ServiceShouldImplementILoginService)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoginConfigCanBeSet()
        {
            await new Spec("Login config can be set")
                .Given(LoginConfigWithClientVersion)
                .Then(ConfigShouldHaveClientVersion)
                .ExecuteAsync();
        }

        private void ServiceShouldNotBeNull()
        {
            Assert.IsNotNull(Service);
        }

        private void ServiceShouldImplementILoginService()
        {
            Assert.IsInstanceOfType(Service, typeof(ILoginService));
        }

        private void LoginConfigWithClientVersion()
        {
            LoginConfig = Options.Create(new LoginConfiguration
            {
                ClientVersion = new ClientVersionSubPacket { Major = 1 },
                Md5String = "testmd5"
            });
        }

        private void ConfigShouldHaveClientVersion()
        {
            Assert.IsNotNull(LoginConfig.Value.ClientVersion);
            Assert.AreEqual(1, LoginConfig.Value.ClientVersion.Major);
        }
    }
}
