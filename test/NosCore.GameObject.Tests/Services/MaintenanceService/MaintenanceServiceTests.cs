//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.MaintenanceService
{
    [TestClass]
    public class MaintenanceServiceTests
    {
        private TestWorld World;
        private GameObject.Services.MaintenanceService.MaintenanceService _service;
        private Mock<IChannelHub> _channelHub;
        private ClientSession _player;
        private ClientSession _gameMaster;
        private readonly List<ClientSession> _disconnected = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _player = await World.CreateSessionAsync();
            _gameMaster = await World.CreateSessionAsync();
            _gameMaster.Account.Authority = AuthorityType.GameMaster;
            _gameMaster.Account.Language = RegionType.FR;
            TrackDisconnection(_player);
            TrackDisconnection(_gameMaster);

            _channelHub = World.Freeze<Mock<IChannelHub>>();
            var localizer = World.Freeze<Mock<IGameLanguageLocalizer>>();
            localizer.Setup(x => x[It.IsAny<LanguageKey>(), It.IsAny<RegionType>(), It.IsAny<object[]>()])
                .Returns((LanguageKey key, RegionType region, object[] arguments) =>
                {
                    var text = $"{key} {region} {string.Join(" ", arguments)}";
                    return new LocalizedString(text, text, false);
                });
            _service = World.Create<GameObject.Services.MaintenanceService.MaintenanceService>();
        }

        [TestMethod]
        public async Task PlayersAreWarnedFiveMinutesBefore()
        {
            await new Spec("Five minutes before the shutdown every player is told, in their own language")
                .GivenAsync(AShutdownIsScheduledIn_Seconds, 600)
                .WhenAsync(_SecondsPass, 300)
                .Then(_ShouldBeWarnedOf_Minutes, nameof(_player), 5)
                .And(_ShouldBeWarnedOf_Minutes, nameof(_gameMaster), 5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NobodyIsWarnedEarlier()
        {
            await new Spec("More than five minutes before the shutdown nothing is announced")
                .GivenAsync(AShutdownIsScheduledIn_Seconds, 600)
                .WhenAsync(_SecondsPass, 299)
                .Then(NoWarningShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AShortDelayIsAnnouncedAtOnce()
        {
            await new Spec("A shutdown less than five minutes away is announced at once with the minutes left")
                .GivenAsync(AShutdownIsScheduledIn_Seconds, 90)
                .WhenAsync(_SecondsPass, 1)
                .Then(_ShouldBeWarnedOf_Minutes, nameof(_player), 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheLastThirtySecondsAreCountedDown()
        {
            await new Spec("The last thirty seconds are counted down every five seconds")
                .GivenAsync(AShutdownIsScheduledIn_Seconds, 40)
                .WhenAsync(_SecondsPass, 39)
                .Then(TheCountdownShouldBe)
                .And(TheChannelShouldNotBeInMaintenance)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AShortDelayStartsTheCountdownWhereItStands()
        {
            await new Spec("A shutdown twelve seconds away starts the countdown at ten")
                .GivenAsync(AShutdownIsScheduledIn_Seconds, 12)
                .WhenAsync(_SecondsPass, 3)
                .Then(TheOnlyCountdownStepShouldBe_Seconds, 10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheShutdownDisconnectsThePlayers()
        {
            await new Spec("At the shutdown the channel goes into maintenance and every player but the game masters is disconnected")
                .GivenAsync(AShutdownIsScheduledIn_Seconds, 40)
                .WhenAsync(_SecondsPass, 41)
                .Then(TheChannelShouldBeInMaintenance)
                .And(ThePlayerShouldBeDisconnected)
                .And(TheGameMasterShouldStayConnected)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AShutdownCanBeCalledOff()
        {
            await new Spec("Lifting the maintenance calls off a pending shutdown")
                .GivenAsync(AShutdownIsScheduledIn_Seconds, 40)
                .WhenAsync(TheMaintenanceIsLifted)
                .AndAsync(_SecondsPass, 41)
                .Then(NoCountdownShouldBeSent)
                .And(NobodyShouldBeDisconnected)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGlobalShutdownIsScheduledOnEveryChannel()
        {
            await new Spec("A shutdown for every channel is sent to all of them and starts no maintenance yet")
                .WhenAsync(AGlobalShutdownIsScheduledIn_Seconds, 300)
                .Then(EveryChannelShouldBeToldToShutDownIn_Seconds, 300)
                .And(TheChannelShouldNotBeInMaintenance)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutADelayMaintenanceStartsAtOnce()
        {
            await new Spec("Without a delay the maintenance starts at once and nobody is warned")
                .WhenAsync(AGlobalMaintenanceIsSetWithoutDelay)
                .Then(EveryChannelShouldBeInMaintenance)
                .And(NoWarningShouldBeSent)
                .ExecuteAsync();
        }

        private void TrackDisconnection(ClientSession session) =>
            World.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = session.ConnectedChannel.Id,
                SessionId = session.SessionId,
                Sender = session,
                AccountName = session.Account.Name,
                Disconnect = () =>
                {
                    _disconnected.Add(session);
                    return Task.CompletedTask;
                },
            });

        private Task AShutdownIsScheduledIn_Seconds(int seconds) =>
            _service.SetMaintenanceAsync(false, true, Duration.FromSeconds(seconds));

        private Task AGlobalShutdownIsScheduledIn_Seconds(int seconds) =>
            _service.SetMaintenanceAsync(true, true, Duration.FromSeconds(seconds));

        private Task AGlobalMaintenanceIsSetWithoutDelay() => _service.SetMaintenanceAsync(true, true, null);

        private Task TheMaintenanceIsLifted() => _service.SetMaintenanceAsync(false, false, null);

        private async Task _SecondsPass(int seconds)
        {
            for (var i = 0; i < seconds; i++)
            {
                World.Clock.AdvanceSeconds(1);
                await _service.TickAsync();
            }
        }

        private ClientSession Session(string name) => name == nameof(_player) ? _player : _gameMaster;

        private static List<MsgiPacket> Warnings(ClientSession session) => session.LastPackets.OfType<MsgiPacket>()
            .Where(p => p.Message == Game18NConstString.ServerWillDisconnectShortly).ToList();

        private void NoWarningShouldBeSent() => Assert.AreEqual(0, Warnings(_player).Count);

        private void _ShouldBeWarnedOf_Minutes(string name, int minutes)
        {
            PacketAssert.AreEqual(new MsgiPacket
            {
                Type = MessageType.Shout,
                Message = Game18NConstString.ServerWillDisconnectShortly,
                ArgumentType = 4,
                Game18NArguments = { minutes },
            }, Warnings(Session(name)).Single());
        }

        private List<MsgiPacket> Countdown() => _player.LastPackets.OfType<MsgiPacket>()
            .Where(p => p.Message != Game18NConstString.ServerWillDisconnectShortly).ToList();

        private void TheCountdownShouldBe()
        {
            var countdown = Countdown();
            CollectionAssert.AreEqual(
                new[]
                {
                    Game18NConstString.ServerWillBeDisonnected, Game18NConstString.ServerWillBeDisonnected,
                    Game18NConstString.ServerWillBeDisonnected, Game18NConstString.ServerWillBeDisonnected,
                    Game18NConstString.ServerWillBeDisonnected, Game18NConstString.ThanksForParticipating,
                },
                countdown.Select(p => p.Message).ToArray());
            CollectionAssert.AreEqual(new object[] { 30, 25, 20, 15, 10 },
                countdown.Take(5).Select(p => p.Game18NArguments.First()).ToArray());
            Assert.IsTrue(countdown.All(p => p.Type == MessageType.Default));
        }

        private void TheOnlyCountdownStepShouldBe_Seconds(int seconds)
        {
            var step = Countdown().Single();
            PacketAssert.AreEqual(new MsgiPacket
            {
                Message = Game18NConstString.ServerWillBeDisonnected,
                ArgumentType = 4,
                Game18NArguments = { seconds },
            }, step);
        }

        private void NoCountdownShouldBeSent() => Assert.AreEqual(0, Countdown().Count);

        private void TheChannelShouldBeInMaintenance() => _channelHub.Verify(x => x.SetMaintenance(false, true), Times.Once);

        private void EveryChannelShouldBeInMaintenance() => _channelHub.Verify(x => x.SetMaintenance(true, true), Times.Once);

        private void TheChannelShouldNotBeInMaintenance() =>
            _channelHub.Verify(x => x.SetMaintenance(It.IsAny<bool>(), true), Times.Never);

        private void ThePlayerShouldBeDisconnected() => CollectionAssert.Contains(_disconnected, _player);

        private void TheGameMasterShouldStayConnected() => CollectionAssert.DoesNotContain(_disconnected, _gameMaster);

        private void NobodyShouldBeDisconnected() => Assert.AreEqual(0, _disconnected.Count);

        private void EveryChannelShouldBeToldToShutDownIn_Seconds(int seconds)
        {
            var expected = World.Clock.GetCurrentInstant() + Duration.FromSeconds(seconds);
            World.PubSubHub.Verify(x => x.SendMessageAsync(It.Is<MaintenanceScheduleData>(m => m.ShutdownAt == expected)),
                Times.Once);
        }
    }
}
