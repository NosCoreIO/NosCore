//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class MviPacketHandlerTests : SpecBase
    {
        private MviPacketHandler MviPacketHandler = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            MviPacketHandler = new MviPacketHandler();
        }

        [TestMethod]
        public async Task MovingFullStackShouldMoveItemToNewSlot()
        {
            await new Spec("Moving full stack should move item to new slot")
                .Given(CharacterHasStackOf999ItemsInSlot0)
                .WhenAsync(MovingFullStackFromSlot0ToSlot1)
                .Then(ItemShouldBeInSlot1WithAmount999)
                .And(Slot0ShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingPartialStackShouldSplitItems()
        {
            await new Spec("Moving partial stack should split items")
                .Given(CharacterHasStackOf999ItemsInSlot0)
                .WhenAsync(Moving500ItemsFromSlot0ToSlot1)
                .Then(Slot1ShouldHave500Items)
                .And(Slot0ShouldHave499Items)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SwappingItemsBetweenSlotsShouldWork()
        {
            await new Spec("Swapping items between slots should work")
                .Given(CharacterHasDifferentItemsInSlot0And1)
                .WhenAsync(MovingItemFromSlot0ToSlot1)
                .Then(ItemsShouldBeSwapped)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingItemShouldSendTwoPocketChangePackets()
        {
            await new Spec("Moving item should send two pocket change packets")
                .Given(CharacterHasStackOf999ItemsInSlot0)
                .WhenAsync(MovingFullStackFromSlot0ToSlot1)
                .Then(ShouldReceiveTwoPocketChangePackets)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StackingItemsOntoExistingStackShouldMerge()
        {
            await new Spec("Stacking items onto existing stack should merge")
                .Given(CharacterHas100ItemsInSlot0And200InSlot1)
                .WhenAsync(Moving100ItemsFromSlot0ToSlot1)
                .Then(Slot1ShouldHave300Items)
                .And(Slot0ShouldBeEmpty)
                .ExecuteAsync();
        }

        private void CharacterHasStackOf999ItemsInSlot0()
        {
            var item = ItemProvider.Create(1012, 999);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, Session.Character.CharacterId),
                NoscorePocketType.Main, 0);
        }

        private void CharacterHasDifferentItemsInSlot0And1()
        {
            var item1 = ItemProvider.Create(1012, 100);
            var item2 = ItemProvider.Create(1013, 50);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item1, Session.Character.CharacterId),
                NoscorePocketType.Main, 0);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item2, Session.Character.CharacterId),
                NoscorePocketType.Main, 1);
        }

        private void CharacterHas100ItemsInSlot0And200InSlot1()
        {
            var item1 = ItemProvider.Create(1012, 100);
            var item2 = ItemProvider.Create(1012, 200);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item1, Session.Character.CharacterId),
                NoscorePocketType.Main, 0);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item2, Session.Character.CharacterId),
                NoscorePocketType.Main, 1);
        }

        private async Task MovingFullStackFromSlot0ToSlot1()
        {
            await MviPacketHandler.ExecuteAsync(new MviPacket
            {
                Slot = 0,
                InventoryType = PocketType.Main,
                Amount = 999,
                DestinationSlot = 1
            }, Session);
        }

        private async Task Moving500ItemsFromSlot0ToSlot1()
        {
            await MviPacketHandler.ExecuteAsync(new MviPacket
            {
                Slot = 0,
                InventoryType = PocketType.Main,
                Amount = 500,
                DestinationSlot = 1
            }, Session);
        }

        private async Task MovingItemFromSlot0ToSlot1()
        {
            await MviPacketHandler.ExecuteAsync(new MviPacket
            {
                Slot = 0,
                InventoryType = PocketType.Main,
                Amount = 100,
                DestinationSlot = 1
            }, Session);
        }

        private async Task Moving100ItemsFromSlot0ToSlot1()
        {
            await MviPacketHandler.ExecuteAsync(new MviPacket
            {
                Slot = 0,
                InventoryType = PocketType.Main,
                Amount = 100,
                DestinationSlot = 1
            }, Session);
        }

        private void ItemShouldBeInSlot1WithAmount999()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(1, NoscorePocketType.Main);
            Assert.IsNotNull(item);
            Assert.AreEqual(1012, item.ItemInstance.ItemVNum);
            Assert.AreEqual(999, item.ItemInstance.Amount);
        }

        private void Slot0ShouldBeEmpty()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Main);
            Assert.IsNull(item);
        }

        private void Slot1ShouldHave500Items()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(1, NoscorePocketType.Main);
            Assert.IsNotNull(item);
            Assert.AreEqual(500, item.ItemInstance.Amount);
        }

        private void Slot0ShouldHave499Items()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Main);
            Assert.IsNotNull(item);
            Assert.AreEqual(499, item.ItemInstance.Amount);
        }

        private void Slot1ShouldHave300Items()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(1, NoscorePocketType.Main);
            Assert.IsNotNull(item);
            Assert.AreEqual(300, item.ItemInstance.Amount);
        }

        private void ItemsShouldBeSwapped()
        {
            var slot0Item = Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Main);
            var slot1Item = Session.Character.InventoryService.LoadBySlotAndType(1, NoscorePocketType.Main);
            Assert.IsNotNull(slot0Item);
            Assert.IsNotNull(slot1Item);
            Assert.AreEqual(1013, slot0Item.ItemInstance.ItemVNum);
            Assert.AreEqual(1012, slot1Item.ItemInstance.ItemVNum);
        }

        private void ShouldReceiveTwoPocketChangePackets()
        {
            var packets = Session.LastPackets.OfType<IvnPacket>().ToList();
            Assert.AreEqual(2, packets.Count);
        }
    }
}
