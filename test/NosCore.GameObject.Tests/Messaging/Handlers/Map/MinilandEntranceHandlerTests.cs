//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

#pragma warning disable 618
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Map
{
    [TestClass]
    public class MinilandEntranceHandlerTests
    {
        private ClientSession _session = null!;
        private Mock<IMinilandService> _minilandService = null!;
        private MinilandEntranceHandler _handler = null!;
        private Miniland _miniland = null!;
        private int _initialDailyVisitCount;
        private int _initialVisitCount;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _minilandService = new Mock<IMinilandService>();
            _handler = new MinilandEntranceHandler(_minilandService.Object);
        }

        [TestMethod]
        public async Task EnteringOwnMinilandSendsMlinfoAndDoesNotIncrementVisitors()
        {
            await new Spec("Owner entering their own miniland receives mlinfo + mlobjlst; visit counters unchanged")
                .Given(MinilandIsRegisteredWithOwner_, true)
                .WhenAsync(HandlingEnterEvent)
                .Then(MlinfoPacketShouldHaveBeenSent)
                .And(MlobjlstPacketShouldHaveBeenSent)
                .And(DailyVisitCountShouldStillBe_, 0)
                .And(VisitCountShouldStillBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringSomeoneElsesMinilandSendsWelcomeMessageAndIncrementsBothCounters()
        {
            await new Spec("Non-owner entering a miniland gets the welcome message and increments both visit counters by 1")
                .Given(MinilandIsRegisteredWithOwner_, false)
                .WhenAsync(HandlingEnterEvent)
                .Then(WelcomeMessagePacketShouldHaveBeenSent)
                .And(DailyVisitCountShouldStillBe_, 1)
                .And(VisitCountShouldStillBe_, 1)
                .And(TotalVisitorsSayPacketShouldCarryVisitCount_, 1)
                .And(TodayVisitorsSayPacketShouldCarryDailyCount_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringANonMinilandInstanceIsANoOp()
        {
            await new Spec("Entering a map that is not registered as a miniland is a no-op")
                .Given(NoMinilandRegisteredForThisMapInstance)
                .WhenAsync(HandlingEnterEvent)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void MinilandIsRegisteredWithOwner_(bool ownedByThisCharacter)
        {
            _initialDailyVisitCount = 0;
            _initialVisitCount = 0;
            var mapInstanceId = _session.Character.MapInstanceId;
            var ownerId = ownedByThisCharacter
                ? _session.Character.CharacterId
                : _session.Character.CharacterId + 1;
            _miniland = new Miniland
            {
                MapInstanceId = mapInstanceId,
                OwnerId = ownerId,
                CharacterName = "OwnerName",
                MinilandMessage = "Welcome",
                DailyVisitCount = _initialDailyVisitCount,
                VisitCount = _initialVisitCount,
            };
            _minilandService
                .Setup(s => s.GetMinilandFromMapInstanceId(mapInstanceId))
                .Returns(_miniland);
        }

        private void NoMinilandRegisteredForThisMapInstance()
        {
            _minilandService
                .Setup(s => s.GetMinilandFromMapInstanceId(It.IsAny<Guid>()))
                .Returns((Miniland?)null);
        }

        private async Task HandlingEnterEvent()
        {
            await _handler.Handle(new MapInstanceEnteredEvent(_session, _session.Character.MapInstance));
        }

        private void MlinfoPacketShouldHaveBeenSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<MlinfoPacket>().Any());

        private void MlobjlstPacketShouldHaveBeenSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<MlobjlstPacket>().Any());

        private void WelcomeMessagePacketShouldHaveBeenSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgPacket>().Any());

        private void DailyVisitCountShouldStillBe_(int expected) =>
            Assert.AreEqual(expected, _miniland.DailyVisitCount);

        private void VisitCountShouldStillBe_(int expected) =>
            Assert.AreEqual(expected, _miniland.VisitCount);

        private void TotalVisitorsSayPacketShouldCarryVisitCount_(int expected)
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.TotalVisitors);
            Assert.IsNotNull(say);
            Assert.AreEqual(expected, say.Game18NArguments[0]);
        }

        private void TodayVisitorsSayPacketShouldCarryDailyCount_(int expected)
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.TodayVisitors);
            Assert.IsNotNull(say);
            Assert.AreEqual(expected, say.Game18NArguments[0]);
        }

        private void NoPacketShouldBeSent() =>
            Assert.AreEqual(0, _session.LastPackets.Count);
    }
}
