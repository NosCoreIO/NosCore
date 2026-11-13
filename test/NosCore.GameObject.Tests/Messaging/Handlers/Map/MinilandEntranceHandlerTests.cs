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
using NosCore.Data.Enumerations;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
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
        private TestWorld World;
        private ClientSession _session;
        private Mock<IMinilandService> _minilandService;
        private MinilandEntranceHandler _handler;
        private Miniland _miniland;
        private int _initialDailyVisitCount;
        private int _initialVisitCount;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _minilandService = World.Freeze<Mock<IMinilandService>>();
            _handler = World.Create<MinilandEntranceHandler>();
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
        public async Task AVisitorIsToldWhereTheObjectsArePlaced()
        {
            await new Spec("A visitor receives mltobj with each placed object's vnum, slot and position")
                .Given(MinilandIsRegisteredWithOwner_, false)
                .And(AQuarryIsPlacedInSlot_At_, (short)3, (short)6, (short)8)
                .WhenAsync(HandlingEnterEvent)
                .Then(MltobjShouldList_, "3117.3.6.8")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheOwnerGetsNoMltobj()
        {
            await new Spec("The owner fills the client table from mlobjlst and receives no mltobj")
                .Given(MinilandIsRegisteredWithOwner_, true)
                .And(AQuarryIsPlacedInSlot_At_, (short)3, (short)6, (short)8)
                .WhenAsync(HandlingEnterEvent)
                .Then(NoMltobjShouldBeSent)
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

        private void AQuarryIsPlacedInSlot_At_(short slot, short x, short y)
        {
            var quarry = new ItemInstance(new Item { VNum = 3117, Type = NoscorePocketType.Miniland }) { Amount = 1 };
            var placed = InventoryItemInstance.Create(quarry, _session.Character.CharacterId);
            placed.Slot = slot;
            placed.Type = NoscorePocketType.Miniland;
            _session.Character.MapInstance.MapDesignObjects[placed.Id] = new MapDesignObject
            {
                MapX = x,
                MapY = y,
                Slot = slot,
                InventoryItemInstance = placed,
            };
        }

        private void MltobjShouldList_(string objects)
        {
            var packet = _session.LastPackets.OfType<MltobjPacket>().Single();
            Assert.AreEqual(objects, string.Join(' ', (packet.MltobjSubPacket ?? []).Select(o => $"{o?.VNum}.{o?.Slot}.{o?.MapX}.{o?.MapY}")));
        }

        private void NoMltobjShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<MltobjPacket>().Any());

        private void NoPacketShouldBeSent() =>
            Assert.AreEqual(0, _session.LastPackets.Count);
    }
}
