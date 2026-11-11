//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Data.Dto;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class GetPacketHandlerTests
    {
        private TestWorld World;
        private const short ItemVNum = 1012;
        private ClientSession Session;
        private IItemGenerationService ItemProvider;
        private GetPacketHandler Handler;
        private InlineOrDeferredMessageBus<MapItemPickedUpEvent> Bus;
        private long MapItemVisualId;
        private Guid DroppedInstanceId;
        private ClientSession Owner;
        private const short GoldVNum = 1046;
        private const short GoldAmount = 500;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            ItemProvider = World.GenerateItemProvider();
            var dropHandler = World.Create<DropHandler>();
            var goldHandler = World.Create<GoldDropHandler>();
            var spChargerHandler = World.Create<SpChargerHandler>();
            Bus = new InlineOrDeferredMessageBus<MapItemPickedUpEvent>(async evt =>
            {
                await dropHandler.Handle(evt);
                await goldHandler.Handle(evt);
                await spChargerHandler.Handle(evt);
            });
            Handler = new GetPacketHandler(NullLogger<GetPacketHandler>.Instance, World.DistanceCalculator,
                World.GroupLootService, World.LogLanguageLocalizer, Bus.Object);
        }

        [TestMethod]
        public async Task TheSamePickUpSentTwiceGrantsTheItemOnce()
        {
            await new Spec("The same pick-up sent twice grants the item once")
                .Given(AnItemLiesNextToTheCharacter)
                .WhenAsync(TheSamePickUpIsSentTwiceConcurrently)
                .Then(TheCharacterHolds_Items, 1)
                .And(TheItemIsNoLongerOnTheMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APickedUpItemGetsANewInstanceId()
        {
            await new Spec("A picked-up item gets a new instance id")
                .Given(AnItemLiesNextToTheCharacter)
                .WhenAsync(TheSamePickUpIsSentTwiceConcurrently)
                .Then(TheHeldItemHasANewInstanceId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroupMemberPicksUpTheOwnersDropWhenTheGroupSharesLoot()
        {
            await new Spec("A group member picks up another member's drop during the owner window when the party shares loot")
                .GivenAsync(TheCharacterIsInAPartyWithTheOwner)
                .And(TheMasterSharesLootAsAGroup)
                .And(AnItemOwnedByTheOtherMemberLiesNextToTheCharacter)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheCharacterHolds_Items, 1)
                .And(TheItemIsNoLongerOnTheMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGroupMemberCannotTakeAnotherMembersTurn()
        {
            await new Spec("A group member cannot take a drop handed to another member when the party loots in turn")
                .GivenAsync(TheCharacterIsInAPartyWithTheOwner)
                .And(AnItemOwnedByTheOtherMemberLiesNextToTheCharacter)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheCharacterHolds_Items, 0)
                .And(TheCharacterIsToldItCannotPickThatUp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACharacterOutsideTheGroupCannotTakeItsDrop()
        {
            await new Spec("A character outside the party cannot take its drop during the owner window")
                .GivenAsync(TheOwnerIsInAPartyThatSharesLootWithoutTheCharacter)
                .And(AnItemOwnedByTheOtherMemberLiesNextToTheCharacter)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheCharacterHolds_Items, 0)
                .And(TheCharacterIsToldItCannotPickThatUp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnyoneTakesADropOnceTheOwnerWindowHasPassed()
        {
            await new Spec("Anyone may take a drop once its owner window has passed")
                .GivenAsync(AnotherCharacterOwnsTheDrop)
                .And(AnItemOwnedByTheOtherMemberLiesNextToTheCharacter)
                .And(_SecondsPass, 30)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheCharacterHolds_Items, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SharedGoldIsNotSplit()
        {
            await new Spec("Gold a group member picks up is not split with the party")
                .GivenAsync(TheCharacterIsInAPartyWithTheOwner)
                .And(TheMasterSharesLootAsAGroup)
                .And(GoldOwnedByTheOtherMemberLiesNextToTheCharacter)
                .WhenAsync(TheCharacterPicksItUp)
                .Then(TheCharacterHas_Gold, (long)GoldAmount)
                .And(TheOwnerHas_Gold, 0L)
                .ExecuteAsync();
        }

        private async Task AnotherCharacterOwnsTheDrop() => Owner = await World.CreateSessionAsync();

        private async Task TheCharacterIsInAPartyWithTheOwner()
        {
            await AnotherCharacterOwnsTheDrop();
            var group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Group, World.SessionGroupFactory);
            Owner.Character.JoinGroup(group);
            Session.Character.JoinGroup(group);
        }

        private async Task TheOwnerIsInAPartyThatSharesLootWithoutTheCharacter()
        {
            await AnotherCharacterOwnsTheDrop();
            var third = await World.CreateSessionAsync();
            var group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Group, World.SessionGroupFactory);
            Owner.Character.JoinGroup(group);
            third.Character.JoinGroup(group);
            TheMasterSharesLootAsAGroup();
        }

        private void TheMasterSharesLootAsAGroup() => Owner.Character.GroupSharing = true;

        private void AnItemOwnedByTheOtherMemberLiesNextToTheCharacter() =>
            MapItemVisualId = World.MapItemProvider.NotNull().Create(Session.Character.MapInstance,
                ItemProvider.Create(ItemVNum, 1), Session.Character.PositionX, Session.Character.PositionY,
                Owner.Character.CharacterId).VisualId;

        private void GoldOwnedByTheOtherMemberLiesNextToTheCharacter()
        {
            World.ItemList.Add(new ItemDto
            {
                VNum = GoldVNum, Type = NoscorePocketType.Main, Name = new I18NString { [RegionType.EN] = "Gold" }
            });
            Session.Character.Gold = 0;
            Owner.Character.Gold = 0;
            MapItemVisualId = World.MapItemProvider.NotNull().Create(Session.Character.MapInstance,
                ItemProvider.Create(GoldVNum, GoldAmount), Session.Character.PositionX, Session.Character.PositionY,
                Owner.Character.CharacterId).VisualId;
        }

        private void _SecondsPass(int seconds) => World.Clock.AdvanceSeconds(seconds);

        private async Task TheCharacterPicksItUp()
        {
            await Handler.ExecuteAsync(new GetPacket { PickerType = VisualType.Player, VisualId = MapItemVisualId }, Session);
            await Bus.DeliverPublishedAsync();
        }

        private void TheCharacterIsToldItCannotPickThatUp()
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().LastOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.UnableToPickUp, packet.Message);
            Assert.IsNotNull(Session.Character.MapInstance.TryGetMapItem(MapItemVisualId));
        }

        private void TheCharacterHas_Gold(long gold) => Assert.AreEqual(gold, Session.Character.Gold);

        private void TheOwnerHas_Gold(long gold) => Assert.AreEqual(gold, Owner.Character.Gold);

        private void AnItemLiesNextToTheCharacter()
        {
            var inv = Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(ItemVNum, 1), Session.Character.CharacterId)).NotNull().First();
            var mapItem = Session.Character.MapInstance.PutItem(1, inv.ItemInstance, Session).NotNull();
            MapItemVisualId = mapItem.VisualId;
            DroppedInstanceId = mapItem.ItemInstance.NotNull().Id;
            Assert.AreEqual(0, Session.Character.InventoryService.CountItem(ItemVNum));
        }

        private async Task TheSamePickUpIsSentTwiceConcurrently()
        {
            var packet = new GetPacket { PickerType = VisualType.Player, VisualId = MapItemVisualId };
            await Task.WhenAll(Handler.ExecuteAsync(packet, Session), Handler.ExecuteAsync(packet, Session));
            await Bus.DeliverPublishedAsync();
        }

        private void TheCharacterHolds_Items(int amount) =>
            Assert.AreEqual(amount, Session.Character.InventoryService.CountItem(ItemVNum));

        private void TheItemIsNoLongerOnTheMap() =>
            Assert.IsNull(Session.Character.MapInstance.TryGetMapItem(MapItemVisualId));

        private void TheHeldItemHasANewInstanceId()
        {
            var held = Session.Character.InventoryService.Values.Single(i => i.ItemInstance.ItemVNum == ItemVNum);
            Assert.AreNotEqual(DroppedInstanceId, held.ItemInstance.Id);
        }
    }
}
