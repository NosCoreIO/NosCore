//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class ShopTests : SpecBase
    {
        private IMapInstanceAccessorService InstanceProvider = null!;
        private ItemGenerationService ItemBuilder = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            TestHelpers.Instance.WorldConfiguration.Value.BackpackSize = 3;
            InstanceProvider = TestHelpers.Instance.MapInstanceAccessorService;
        }

        [TestMethod]
        public async Task BuyingFromNonExistentSlotShouldFail()
        {
            await new Spec("Buying from non existent slot should fail")
                .Given(CharacterHasGold_, 9999999999L)
                .WhenAsync(AttemptingToBuyFromWrongSlotAsync)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingMoreThanShopQuantityShouldFail()
        {
            await new Spec("Buying more than shop quantity should fail")
                .Given(CharacterHasGold_, 9999999999L)
                .WhenAsync(AttemptingToBuyMoreThanAvailableAsync)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingWithoutEnoughGoldShouldFail()
        {
            await new Spec("Buying without enough gold should fail")
                .Given(CharacterHasGold_, 500000L)
                .WhenAsync(AttemptingToBuy_ItemsAsync, 99)
                .Then(ShouldReceiveNotEnoughGoldMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingWithoutEnoughReputationShouldFail()
        {
            await new Spec("Buying without enough reputation should fail")
                .Given(CharacterHasReputation_, 500000L)
                .WhenAsync(AttemptingToBuyReputationItemAsync)
                .Then(ShouldReceiveReputationError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingWithoutInventorySpaceShouldFail()
        {
            await new Spec("Buying without inventory space should fail")
                .Given(CharacterHasGoldButFullInventory)
                .WhenAsync(AttemptingToBuyWithFullInventoryAsync)
                .Then(ShouldReceiveNotEnoughSpaceMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SuccessfulPurchaseShouldUpdateGoldAndInventory()
        {
            await new Spec("Successful purchase should update gold and inventory")
                .Given(CharacterHasGoldAndPartialInventory)
                .WhenAsync(Buying998ItemsAt1GoldEachAsync)
                .Then(AllInventorySlotsShouldHave_Items, 999)
                .And(GoldShouldBeDeducted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SuccessfulReputationPurchaseShouldUpdateReputation()
        {
            await new Spec("Successful reputation purchase should update reputation")
                .Given(CharacterHasReputationAndPartialInventory)
                .WhenAsync(Buying998ItemsAt1ReputationEachAsync)
                .Then(AllInventorySlotsShouldHave_Items, 999)
                .And(ReputationShouldBeDeducted)
                .ExecuteAsync();
        }

        private Shop CreateShop(ItemGenerationService itemBuilder, short amount = -1, long? price = null, byte slot = 0)
        {
            var list = new ConcurrentDictionary<int, ShopItem>();
            var shopItem = new ShopItem
            {
                Slot = slot,
                ItemInstance = itemBuilder.Create(1, amount),
                Type = 0
            };
            if (amount > 0)
            {
                shopItem.Amount = amount;
            }
            if (price.HasValue)
            {
                shopItem.Price = price.Value;
            }
            list.TryAdd(slot, shopItem);
            return new Shop { ShopItems = list };
        }

        private ItemGenerationService CreateItemBuilder(long price = 500000, long reputPrice = 0)
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = price, ReputPrice = reputPrice }
            };
            return new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                    new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()),
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        private void CharacterHasGold_(long gold)
        {
            CharacterHasGold(gold);
        }

        private void CharacterHasReputation_(long reput)
        {
            Session.Character.Reput = reput;
        }

        private async Task AttemptingToBuyFromWrongSlotAsync()
        {
            var itemBuilder = CreateItemBuilder();
            var shop = CreateShop(itemBuilder);
            await Session.Character.BuyAsync(shop, 1, 99);
        }


        private async Task AttemptingToBuyMoreThanAvailableAsync()
        {
            var itemBuilder = CreateItemBuilder();
            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem
            {
                Slot = 0,
                ItemInstance = itemBuilder.Create(1, -1),
                Type = 0,
                Amount = 98
            });
            var shop = new Shop { ShopItems = list };
            await Session.Character.BuyAsync(shop, 0, 99);
        }

        private async Task AttemptingToBuy_ItemsAsync(int value)
        {
            var itemBuilder = CreateItemBuilder();
            var shop = CreateShop(itemBuilder);

            await Session.Character.BuyAsync(shop, 0, 99);
        }

        private void ShouldReceiveNotEnoughGoldMessage()
        {
            var packet = GetLastPacket<SMemoiPacket>();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.NotEnoughGold5, packet.Message);
        }

        private async Task AttemptingToBuyReputationItemAsync()
        {
            var itemBuilder = CreateItemBuilder(0, 500000);
            var shop = CreateShop(itemBuilder);
            await Session.Character.BuyAsync(shop, 0, 99);
        }

        private void ShouldReceiveReputationError()
        {
            var packet = GetLastPacket<SMemoiPacket>();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.ReputationNotHighEnough, packet.Message);
        }

        private void CharacterHasGoldButFullInventory()
        {
            CharacterHasGold(500000);
            ItemBuilder = CreateItemBuilder(1);
            Session.Character.ItemProvider = ItemBuilder;
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemBuilder.Create(1, 999), Session.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemBuilder.Create(1, 999), Session.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemBuilder.Create(1, 999), Session.Character.CharacterId),
                NoscorePocketType.Etc, 2);
        }

        private async Task AttemptingToBuyWithFullInventoryAsync()
        {
            var shop = CreateShop(ItemBuilder, -1, 1);
            await Session.Character.BuyAsync(shop, 0, 999);
        }

        private void ShouldReceiveNotEnoughSpaceMessage()
        {
            ShouldReceiveMessage(Game18NConstString.NotEnoughSpace);
        }

        private void CharacterHasGoldAndPartialInventory()
        {
            CharacterHasGold(500000);
            ItemBuilder = CreateItemBuilder(1);
            Session.Character.ItemProvider = ItemBuilder;
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemBuilder.Create(1, 999), Session.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemBuilder.Create(1, 999), Session.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemBuilder.Create(1, 1), Session.Character.CharacterId),
                NoscorePocketType.Etc, 2);
        }

        private async Task Buying998ItemsAt1GoldEachAsync()
        {
            var shop = CreateShop(ItemBuilder);
            await Session.Character.BuyAsync(shop, 0, 998);
        }

        private void AllInventorySlotsShouldHave_Items(int value)
        {
            Assert.IsTrue(Session.Character.InventoryService.All(s => s.Value.ItemInstance?.Amount == 999));
        }

        private void GoldShouldBeDeducted()
        {
            Assert.AreEqual(499002, Session.Character.Gold);
        }

        private void CharacterHasReputationAndPartialInventory()
        {
            Session.Character.Reput = 500000;
            ItemBuilder = CreateItemBuilder(0, 1);
            Session.Character.ItemProvider = ItemBuilder;
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemBuilder.Create(1, 999), Session.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemBuilder.Create(1, 999), Session.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemBuilder.Create(1, 1), Session.Character.CharacterId),
                NoscorePocketType.Etc, 2);
        }

        private async Task Buying998ItemsAt1ReputationEachAsync()
        {
            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem { Slot = 0, ItemInstance = ItemBuilder.Create(1), Type = 0 });
            var shop = new Shop { ShopItems = list };
            await Session.Character.BuyAsync(shop, 0, 998);
        }

        private void ReputationShouldBeDeducted()
        {
            Assert.AreEqual(499002, Session.Character.Reput);
        }
    }
}
