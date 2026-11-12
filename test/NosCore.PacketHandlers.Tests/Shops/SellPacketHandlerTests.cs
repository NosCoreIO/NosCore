//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Tests.Shared.BDD.Steps;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class SellPacketHandlerTests
    {
        private TestWorld World;
        private SellPacketHandler SellPacketHandler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.WithNpcAt(100, 1, 1);
            Session.WithWindowOpenedAt(100, NpcDialogWindow.Shop);
            SellPacketHandler = World.Create<SellPacketHandler>();
        }

        [TestMethod]
        public async Task UserCannotSellInExchange()
        {
            await new Spec("User cannot sell in exchange")
                .Given(CharacterIsInShop)
                .And(CharacterHasTradableItems)
                .WhenAsync(SellingItem)
                .Then(GoldShouldNotIncrease)
                .And(ItemShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCannotSellNonSoldableItem()
        {
            await new Spec("User cannot sell non soldable item")
                .Given(CharacterHasNonSoldableItems)
                .WhenAsync(SellingItem)
                .Then(ShouldReceiveCannotSellError)
                .And(GoldShouldNotIncrease)
                .And(ItemShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCanSellItem()
        {
            await new Spec("User can sell item")
                .Given(CharacterHasSoldableItems)
                .WhenAsync(SellingItem)
                .Then(GoldShouldIncrease)
                .And(ItemShouldBeRemoved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SellingLastEquipmentClearsItsSlot()
        {
            await new Spec("Selling the last equipment clears its inventory slot")
                .Given(CharacterHasSoldableEquipment)
                .WhenAsync(SellingEquipment)
                .Then(EquipmentShouldBeRemoved)
                .And(EquipmentSlotShouldBeCleared)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SellingPartOfAStackKeepsTheRest()
        {
            await new Spec("Selling part of a stack updates the remaining amount")
                .Given(CharacterHasAStackOfThree)
                .WhenAsync(SellingItem)
                .Then(SlotZeroShouldShowTwoLeft)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SellingRejectsOnPriceAmountOverflow()
        {
            await new Spec("Selling rejects silently when price*amount would overflow long")
                .Given(CharacterHasExtremePricedItem)
                .WhenAsync(SellingTwoOfSlotZero)
                .Then(GoldShouldNotIncrease)
                .And(ItemShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SellingRejectsWhenSaleWouldBreachMaxGold()
        {
            await new Spec("Selling rejects with MaxGoldReached when the payout would exceed MaxGoldAmount")
                .Given(CharacterHasSoldableItems)
                .And(CharacterIsAtMaxGold)
                .WhenAsync(SellingItem)
                .Then(ShouldReceiveMaxGoldReachedMessage)
                .And(ItemShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SellingWithoutAnOpenNpcShopIsIgnored()
        {
            await new Spec("Selling without an NPC shop window open does nothing")
                .Given(CharacterHasSoldableItems)
                .And(NoShopWindowIsOpen)
                .WhenAsync(SellingItem)
                .Then(GoldShouldNotIncrease)
                .And(ItemShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SellingAfterWalkingAwayFromTheShopIsIgnored()
        {
            await new Spec("Selling after walking away from the NPC whose shop is open does nothing")
                .Given(CharacterHasSoldableItems)
                .And(CharacterWalksTo_, (short)30, (short)30)
                .WhenAsync(SellingItem)
                .Then(GoldShouldNotIncrease)
                .And(ItemShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SellingWhileTalkingToANonShopWindowIsIgnored()
        {
            await new Spec("Selling while the NPC's dialog, not its shop, is open does nothing")
                .Given(CharacterHasSoldableItems)
                .And(OnlyTheNpcsDialogIsOpen)
                .WhenAsync(SellingItem)
                .Then(GoldShouldNotIncrease)
                .And(ItemShouldStillExist)
                .ExecuteAsync();
        }

        private void NoShopWindowIsOpen() => Session.Character.NpcDialog = null;

        private void OnlyTheNpcsDialogIsOpen() => Session.WithWindowOpenedAt(100, NpcDialogWindow.Conversation);

        private void CharacterWalksTo_(short x, short y) => Session.WalkTo(x, y);

        private void CharacterHasExtremePricedItem()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, ItemType = ItemType.Sell, Price = long.MaxValue }
            };
            var itemBuilder = new ItemGenerationService(items, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 5), 0),
                NoscorePocketType.Etc, 0);
        }

        private void CharacterHasSoldableEquipment()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, IsSoldable = true, Price = 500 }
            };
            var itemBuilder = new ItemGenerationService(items, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 1), 0),
                NoscorePocketType.Equipment, 0);
        }

        private async Task SellingEquipment()
        {
            await SellPacketHandler.ExecuteAsync(
                new SellPacket { Slot = 0, Amount = 1, Data = (short)NoscorePocketType.Equipment },
                Session);
        }

        private void EquipmentShouldBeRemoved()
        {
            Assert.IsNull(Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Equipment));
        }

        private void EquipmentSlotShouldBeCleared()
        {
            var ivn = Session.LastPackets.OfType<IvnPacket>().Last();
            var sub = ivn.IvnSubPackets.NotNull().Single();
            Assert.AreEqual(PocketType.Equipment, ivn.Type);
            Assert.AreEqual(0, sub.Slot);
            Assert.AreEqual(0, sub.VNum);
        }

        private void CharacterHasAStackOfThree()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 500 }
            };
            var itemBuilder = new ItemGenerationService(items, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 3), 0),
                NoscorePocketType.Etc, 0);
        }

        private void SlotZeroShouldShowTwoLeft()
        {
            var sub = Session.LastPackets.OfType<IvnPacket>().Last().IvnSubPackets.NotNull().Single();
            Assert.AreEqual(0, sub.Slot);
            Assert.AreEqual(1, sub.VNum);
            Assert.AreEqual(2, sub.RareAmount);
        }

        private async Task SellingTwoOfSlotZero()
        {
            await SellPacketHandler.ExecuteAsync(
                new SellPacket { Slot = 0, Amount = 2, Data = (short)NoscorePocketType.Etc },
                Session);
        }

        private void CharacterIsAtMaxGold()
        {
            Session.Character.Gold = World.WorldConfiguration.Value.MaxGoldAmount;
        }

        private void ShouldReceiveMaxGoldReachedMessage()
        {
            var packet = Session.LastPackets.OfType<NosCore.Packets.ServerPackets.UI.MsgiPacket>()
                .FirstOrDefault(m => m.Message == Game18NConstString.MaxGoldReached);
            Assert.IsNotNull(packet);
        }

        private void CharacterIsInShop()
        {
            Session.Character.InShop = true;
        }

        private void CharacterHasTradableItems()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1, IsTradable = true }
            };
            var itemBuilder = new ItemGenerationService(items, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 1), 0),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 2), 0),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 3), 0),
                NoscorePocketType.Etc, 2);

        }

        private void CharacterHasNonSoldableItems()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = false }
            };
            var itemBuilder = new ItemGenerationService(items, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 1), 0),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 2), 0),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 3), 0),
                NoscorePocketType.Etc, 2);

        }

        private void CharacterHasSoldableItems()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000 }
            };
            var itemBuilder = new ItemGenerationService(items, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 1), 0),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 2), 0),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 3), 0),
                NoscorePocketType.Etc, 2);

        }

        private async Task SellingItem()
        {
            await SellPacketHandler.ExecuteAsync(new SellPacket { Slot = 0, Amount = 1, Data = (short)NoscorePocketType.Etc },
                Session);
        }

        private void GoldShouldNotIncrease()
        {
            Assert.IsTrue(Session.Character.Gold == 0);
        }

        private void GoldShouldIncrease()
        {
            Assert.IsTrue(Session.Character.Gold > 0);
        }

        private void ItemShouldStillExist()
        {
            Assert.IsNotNull(Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Etc));
        }

        private void ItemShouldBeRemoved()
        {
            Assert.IsNull(Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Etc));
        }

        private void ShouldReceiveCannotSellError()
        {
            var packet = (SMemoiPacket?)Session.LastPackets.FirstOrDefault(s => s is SMemoiPacket);
            Assert.IsTrue(packet?.Type == SMemoType.FailNpc && packet?.Message == Game18NConstString.ItemCanNotBeSold);
        }
    }
}
