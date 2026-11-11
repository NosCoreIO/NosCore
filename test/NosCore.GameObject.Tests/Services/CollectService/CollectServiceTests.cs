//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.CollectService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Services.CollectService
{
    [TestClass]
    public class CollectServiceTests
    {
        private TestWorld World;
        private const int MapNpcId = 100;
        private const short ObjectVNum = 2004;
        private const short ItemVNum = 1012;
        private ClientSession _session;
        private Mock<Wolverine.IMessageBus> _messageBus;
        private GameObject.Services.CollectService.CollectService _service;
        private bool _started;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var catalog = new Mock<INpcCombatCatalog>();
            catalog.Setup(c => c.GetDrops(ObjectVNum)).Returns(new List<DropDto>
            {
                new() { VNum = ItemVNum, Amount = 1, MonsterVNum = ObjectVNum, DropChance = 100000 }
            });
            catalog.Setup(c => c.GetDrops(It.Is<short>(v => v != ObjectVNum))).Returns(new List<DropDto>());
            _messageBus = new Mock<Wolverine.IMessageBus>();
            _service = new GameObject.Services.CollectService.CollectService(catalog.Object,
                World.GenerateItemProvider(), World.Clock, _messageBus.Object);
        }

        [TestMethod]
        public async Task OpeningACollectableObjectStartsTheBar()
        {
            await new Spec("Opening a collectable map object starts a five second bar that sends guri 400 back")
                .Given(AMapObjectWhoseCollectFlagIs_, true)
                .WhenAsync(OpeningTheObject)
                .Then(TheCollectBarShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OpeningAnOrdinaryNpcStartsNoBar()
        {
            await new Spec("Opening an NPC that is not collectable starts no bar")
                .Given(AMapObjectWhoseCollectFlagIs_, false)
                .WhenAsync(OpeningTheObject)
                .Then(NoBarShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FinishingTheBarGivesTheItemAndHidesTheObject()
        {
            await new Spec("Once the bar has run the object's item is given and the object goes out until it respawns")
                .Given(AMapObjectWhoseCollectFlagIs_, true)
                .AndAsync(OpeningTheObject)
                .And(SecondsPass_, 5)
                .WhenAsync(SendingTheCollectGuri)
                .Then(ThePlayerShouldHave_OfTheItem, 1)
                .And(TheObjectShouldBeOut)
                .And(TheCollectShouldBePublished)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CollectingOntoAStackReportsTheAmountReceived()
        {
            await new Spec("Collecting an item the player already holds reports the one received, not what is left after stacking")
                .Given(AMapObjectWhoseCollectFlagIs_, true)
                .And(ThePlayerAlreadyHasOneOfTheItem)
                .AndAsync(OpeningTheObject)
                .And(SecondsPass_, 5)
                .WhenAsync(SendingTheCollectGuri)
                .Then(ThePlayerShouldHave_OfTheItem, 2)
                .And(TheReceivedMessageShouldSay_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACollectBeforeTheBarEndsIsIgnored()
        {
            await new Spec("A guri 400 sent two seconds into the bar gives nothing")
                .Given(AMapObjectWhoseCollectFlagIs_, true)
                .AndAsync(OpeningTheObject)
                .And(SecondsPass_, 2)
                .WhenAsync(SendingTheCollectGuri)
                .Then(ThePlayerShouldHave_OfTheItem, 0)
                .And(TheObjectShouldStillStand)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACollectWithoutABarIsIgnored()
        {
            await new Spec("A guri 400 the server never started a bar for gives nothing")
                .Given(AMapObjectWhoseCollectFlagIs_, true)
                .WhenAsync(SendingTheCollectGuri)
                .Then(ThePlayerShouldHave_OfTheItem, 0)
                .And(TheObjectShouldStillStand)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACollectedObjectCannotBeCollectedAgainBeforeItRespawns()
        {
            await new Spec("Opening an object that was just collected starts no new bar")
                .Given(AMapObjectWhoseCollectFlagIs_, true)
                .AndAsync(OpeningTheObject)
                .And(SecondsPass_, 5)
                .AndAsync(SendingTheCollectGuri)
                .WhenAsync(OpeningTheObject)
                .Then(TheOpeningShouldBeRefused)
                .ExecuteAsync();
        }

        private void AMapObjectWhoseCollectFlagIs_(bool canCollect) =>
            _session.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto>
                {
                    new() { MapNpcId = MapNpcId, MapId = _session.Character.MapInstance.Map.MapId, MapX = 1, MapY = 1, VNum = ObjectVNum }
                },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = ObjectVNum, CanCollect = canCollect, RespawnTime = 300 } });

        private async Task OpeningTheObject()
        {
            _session.LastPackets.Clear();
            _started = await _service.StartAsync(_session.Character, _session.Character.MapInstance.GetNpcById(MapNpcId).NotNull());
        }

        private Task SendingTheCollectGuri() =>
            new CollectItemHandler(_service).Handle(new GuriPacketReceivedEvent(_session,
                new ClientGuriPacket { Type = GuriPacketType.CollectItem, Argument = MapNpcId }));

        private void SecondsPass_(int seconds) => World.Clock.AdvanceSeconds(seconds);

        private void TheCollectBarShouldBeSent()
        {
            var delay = _session.LastPackets.OfType<DelayPacket>().Single();
            Assert.AreEqual(5000, delay.Delay);
            Assert.AreEqual(DelayPacketType.InUse, delay.Type);
            var guri = ((ClientGuriPacket)delay.Packet.NotNull()).NotNull();
            Assert.AreEqual(GuriPacketType.CollectItem, guri.Type);
            Assert.AreEqual(MapNpcId, guri.Argument);
        }

        private void NoBarShouldBeSent()
        {
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        private void ThePlayerAlreadyHasOneOfTheItem() =>
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(ItemVNum, 1), _session.Character.CharacterId));

        private void TheReceivedMessageShouldSay_(int amount)
        {
            var message = _session.LastPackets.OfType<SayiPacket>().Single(p => p.Message == Game18NConstString.ReceivedThisItem);
            Assert.AreEqual(amount, Convert.ToInt32(message.Game18NArguments[1]));
        }

        private void TheOpeningShouldBeRefused()
        {
            Assert.IsFalse(_started);
            NoBarShouldBeSent();
        }

        private void ThePlayerShouldHave_OfTheItem(int amount)
        {
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(ItemVNum));
        }

        private void TheObjectShouldBeOut()
        {
            Assert.IsFalse(_session.Character.MapInstance.GetNpcById(MapNpcId).NotNull().IsAlive);
        }

        private void TheObjectShouldStillStand()
        {
            Assert.IsTrue(_session.Character.MapInstance.GetNpcById(MapNpcId).NotNull().IsAlive);
        }

        private void TheCollectShouldBePublished()
        {
            _messageBus.Verify(b => b.PublishAsync(It.IsAny<MapEntityCollectedEvent>(), null), Times.Once);
        }
    }
}
