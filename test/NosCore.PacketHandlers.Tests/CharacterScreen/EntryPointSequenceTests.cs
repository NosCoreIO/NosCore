//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.WebApi;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.AuthService;
using NosCore.Networking;
using NosCore.Networking.SessionRef;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class EntryPointSequenceTests
    {
        private const int SessionId = 70;
        private ClientSession Session = null!;
        private IAuthHub AuthHub = null!;
        private WorldPacketHandlingStrategy Strategy = null!;
        private string AccountName = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            AuthHub = new AuthHub(new AuthCodeService(TestHelpers.Instance.Clock), new MasterClientList());
            var sessionRefHolder = new SessionRefHolder();
            var pubSubHub = TestHelpers.Instance.PubSubHub;
            pubSubHub.Setup(o => o.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            pubSubHub.Setup(o => o.SubscribeAsync(It.IsAny<Subscriber>())).ReturnsAsync(true);
            Session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler>
            {
                new EntryPointPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.AccountDao,
                    TestHelpers.Instance.MateDao, NullLogger<EntryPointPacketHandler>.Instance, AuthHub,
                    TestHelpers.Instance.WorldConfiguration, sessionRefHolder,
                    TestHelpers.Instance.LogLanguageLocalizer)
            });
            AccountName = Session.Account.Name;
            Session.ClearPlayerEntity();
            Session.Account = null!;
            sessionRefHolder[Session.SessionKey] = new RegionTypeMapping(SessionId, RegionType.EN);
            Strategy = new WorldPacketHandlingStrategy(NullLogger<WorldPacketHandlingStrategy>.Instance,
                TestHelpers.Instance.LogLanguageLocalizer, sessionRefHolder);
        }

        [TestMethod]
        public async Task PasswordLoginEntersTheWorld()
        {
            await new Spec("Password login enters the world")
                .WhenAsync(ClientSendsItsPasswordAsync)
                .Then(AccountShouldHaveEntered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PasswordLoginWithAWrongPasswordIsRefused()
        {
            await new Spec("Password login with a wrong password is refused")
                .WhenAsync(ClientSendsAWrongPasswordAsync)
                .Then(AccountShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LauncherLoginEntersWithTheSessionTheLoginServerAnnounced()
        {
            await new Spec("Launcher login enters with the session the login server announced")
                .GivenAsync(LoginServerAnnouncedTheSessionAsync)
                .WhenAsync(ClientSendsTheLauncherMarkerAsync)
                .Then(AccountShouldHaveEntered)
                .ExecuteAsync();
        }

        private async Task LoginServerAnnouncedTheSessionAsync()
        {
            var authCode = Guid.NewGuid().ToString();
            await AuthHub.StoreAuthCodeAsync(authCode, AccountName);
            await AuthHub.GetAwaitingConnectionAsync(AccountName, authCode, SessionId);
        }

        private Task ClientSendsItsPasswordAsync() => EnteringTheWorldAsync("test");

        private Task ClientSendsAWrongPasswordAsync() => EnteringTheWorldAsync("wrong");

        private Task ClientSendsTheLauncherMarkerAsync() => EnteringTheWorldAsync("thisisgfmode");

        private async Task EnteringTheWorldAsync(string passwordSlot)
        {
            foreach (var packet in new IPacket[]
                     {
                         new UnresolvedPacket { KeepAliveId = 5000, Header = SessionId.ToString(CultureInfo.InvariantCulture), Body = "" },
                         new UnresolvedPacket { KeepAliveId = 5001, Header = AccountName, Body = "" },
                         new UnresolvedPacket { KeepAliveId = 5002, Header = passwordSlot, Body = "" },
                     })
            {
                await Strategy.HandlePacketAsync(packet, Session, true);
            }
        }

        private void AccountShouldHaveEntered()
        {
            Assert.AreEqual(AccountName, Session.Account?.Name);
        }

        private void AccountShouldBeRefused()
        {
            Assert.IsNull(Session.Account);
        }
    }
}
