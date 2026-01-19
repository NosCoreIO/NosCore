//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.SessionRef;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.Infrastructure;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class DacPacketHandlerTests
    {
        private static readonly Mock<ILogger> Logger = new();
        private DacPacketHandler DacPacketHandler = null!;
        private ClientSession Session = null!;
        private Mock<IAuthHub> AuthHttpClient = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<IChannelHub> ChannelHub = null!;
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
            ChannelHub = TestHelpers.Instance.ChannelHub;
            ChannelHub.Setup(o => o.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo>());
            PubSubHub.Setup(o => o.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            DacPacketHandler =
                new DacPacketHandler(TestHelpers.Instance.AccountDao, Logger.Object, AuthHttpClient.Object, TestHelpers.Instance.PubSubHub.Object, new SessionRefHolder(), TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task ConnectionWithAlreadyConnectedAccountShouldFail()
        {
            await new Spec("Connection with already connected account should fail")
                .Given(AccountIsAlreadyConnected)
                .WhenAsync(ConnectingWithAccount)
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
        public async Task ConnectionWithNotInWaitingAccountShouldFail()
        {
            await new Spec("Connection with not in waiting account should fail")
                .WhenAsync(ConnectingWithAccount)
                .Then(InvalidPasswordErrorShouldBeLogged)
                .And(AccountShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConnectionWithWaitingAccountShouldSucceed()
        {
            await new Spec("Connection with waiting account should succeed")
                .Given(AccountIsInWaitingState)
                .WhenAsync(ConnectingWithAccount)
                .Then(AccountShouldBeSet)
                .ExecuteAsync();
        }

        private void AccountIsAlreadyConnected()
        {
            ChannelHub.Setup(o => o.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo>()
            {
                new()
                {
                    Id = 1,
                }
            });
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

        private void AccountIsInWaitingState()
        {
            AuthHttpClient.Setup(authHttpClient => authHttpClient
                    .GetAwaitingConnectionAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync("123");
        }

        private async Task ConnectingWithAccount()
        {
            var packet = new DacPacket(AccountName)
            {
                Slot = 0,
            };
            await DacPacketHandler.ExecuteAsync(packet, Session);
        }

        private async Task ConnectingWithFakeAccount()
        {
            var packet = new DacPacket("fakeName")
            {
                Slot = 2,
            };
            await DacPacketHandler.ExecuteAsync(packet, Session);
        }

        private void AlreadyConnectedErrorShouldBeLogged()
        {
            Logger.Verify(o => o.Error(It.Is<string>(s => s == TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.ALREADY_CONNECTED]), It.Is<It.IsAnyType>((v, t) => true)), Times.Once);
        }

        private void InvalidAccountErrorShouldBeLogged()
        {
            Logger.Verify(o => o.Error(It.Is<string>(s => s == TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.INVALID_ACCOUNT]), It.Is<It.IsAnyType>((v, t) => true)), Times.Once);
        }

        private void InvalidPasswordErrorShouldBeLogged()
        {
            Logger.Verify(o => o.Error(It.Is<string>(s => s == TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.INVALID_PASSWORD]), It.Is<It.IsAnyType>((v, t) => true)), Times.Once);
        }

        private void AccountShouldBeNull()
        {
            Assert.IsNull(Session.Account);
        }

        private void AccountShouldBeSet()
        {
            Assert.IsNotNull(Session.Account);
        }
    }
}
