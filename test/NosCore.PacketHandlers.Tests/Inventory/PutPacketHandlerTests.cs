//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class PutPacketHandlerTests
    {
        private IItemGenerationService Item = null!;
        private PutPacketHandler PutPacketHandler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Item = TestHelpers.Instance.GenerateItemProvider();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            PutPacketHandler = new PutPacketHandler(TestHelpers.Instance.WorldConfiguration, TestHelpers.Instance.GameLanguageLocalizer);
        }

        [TestMethod]
        public async Task DroppingPartialStackShouldKeepRemaining()
        {
            await new Spec("Dropping partial stack should keep remaining")
                .Given(CharacterHasStackOf_Items, 999)
                .WhenAsync(Dropping_Items, 500)
                .Then(InventoryShouldHaveOneItem)
                .And(ItemAmountShouldBe_, 499)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DroppingNonDroppableItemShouldFail()
        {
            await new Spec("Dropping non droppable item should fail")
                .Given(CharacterHasNonDroppableItem)
                .WhenAsync(DroppingOneItem)
                .Then(ShouldReceiveCantDropMessage)
                .And(InventoryShouldNotBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DroppingItemShouldRemoveFromInventory()
        {
            await new Spec("Dropping item should remove from inventory")
                .Given(CharacterHasDroppableItem)
                .WhenAsync(DroppingOneItem)
                .Then(InventoryShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DroppingAtBadPositionShouldFail()
        {
            await new Spec("Dropping at bad position should fail")
                .Given(CharacterIsAtBadPosition)
                .And(CharacterHasDroppableItem)
                .WhenAsync(DroppingOneItem)
                .Then(ShouldReceiveCantDropMessage)
                .And(InventoryShouldNotBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DroppingOutOfBoundsShouldFail()
        {
            await new Spec("Dropping out of bounds should fail")
                .Given(CharacterIsOutOfBounds)
                .And(CharacterHasDroppableItem)
                .WhenAsync(DroppingOneItem)
                .Then(ShouldReceiveCantDropMessage)
                .And(InventoryShouldNotBeEmpty)
                .ExecuteAsync();
        }

        private void CharacterHasStackOf_Items(int value)
        {
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1012, 999), 0));
        }

        private void CharacterHasDroppableItem()
        {
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1012, 1), 0));
        }

        private void CharacterHasNonDroppableItem()
        {
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1013, 1), 0));
        }

        private void CharacterIsAtBadPosition()
        {
            Session.Character.PositionX = 2;
            Session.Character.PositionY = 2;
        }

        private void CharacterIsOutOfBounds()
        {
            Session.Character.PositionX = -1;
            Session.Character.PositionY = -1;
        }

        private async Task Dropping_Items(int value)
        {
            await PutPacketHandler.ExecuteAsync(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 500
            }, Session);
        }

        private async Task DroppingOneItem()
        {
            await PutPacketHandler.ExecuteAsync(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            }, Session);
        }

        private void InventoryShouldHaveOneItem()
        {
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        private void ItemAmountShouldBe_(int value)
        {
            Assert.AreEqual((short)value, Session.Character.InventoryService.FirstOrDefault().Value.ItemInstance?.Amount);
        }

        private void InventoryShouldBeEmpty()
        {
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }

        private void InventoryShouldNotBeEmpty()
        {
            Assert.IsTrue(Session.Character.InventoryService.Count > 0);
        }

        private void ShouldReceiveCantDropMessage()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player &&
                packet?.VisualId == Session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow &&
                packet?.Message == Game18NConstString.CantDropItem);
        }
    }
}
