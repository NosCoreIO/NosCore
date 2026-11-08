//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Tests.Shared.BDD.Steps;

namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class CBuyPacketHandlerTests
    {
        private static readonly ILogger<CBuyPacketHandler> Logger = new Mock<ILogger<CBuyPacketHandler>>().Object;
        private Mock<IBazaarHub> BazaarHttpClient = null!;
        private CBuyPacketHandler CbuyPacketHandler = null!;
        private Mock<IItemGenerationService> ItemProvider = null!;
        private ClientSession Session = null!;
        private readonly Mock<IDao<IItemInstanceDto?, Guid>> ItemInstanceDao = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.WithNpcAt(100, 1, 1);
            Session.WithWindowOpenedAt(100, NpcDialogWindow.Bazaar);
            BazaarHttpClient = new Mock<IBazaarHub>();
            ItemProvider = new Mock<IItemGenerationService>();
            CbuyPacketHandler = new CBuyPacketHandler(BazaarHttpClient.Object, ItemProvider.Object, Logger,
                TestHelpers.Instance.LogLanguageLocalizer, new Mock<NosCore.GameObject.Services.SaveService.ISaveService>().Object,
                ItemInstanceDao.Object, TestHelpers.Instance.NpcDialogService,
                new Mock<NosCore.GameObject.Services.ShellOptionService.IShellOptionService>().Object);
            ItemInstanceDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto { ItemVNum = 1012, Amount = 99 });

            BazaarHttpClient.Setup(b => b.GetBazaar(0, null, null, null, null, null, null, null, null)).ReturnsAsync(
                new List<BazaarLink> { new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 1 }
                }});
            BazaarHttpClient.Setup(b => b.GetBazaar(2, null, null, null, null, null, null, null, null)).ReturnsAsync(
                new List<BazaarLink> { new BazaarLink
                {
                    SellerName = Session.Character.Name,
                    BazaarItem = new BazaarItemDto { Price = 60, Amount = 1, SellerId = Session.Character.CharacterId },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012 }
                }});
            BazaarHttpClient.Setup(b => b.GetBazaar(3, null, null, null, null, null, null, null, null)).ReturnsAsync(
                new List<BazaarLink> { new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 99, IsPackage = true },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 99 }
                }});
            BazaarHttpClient.Setup(b => b.GetBazaar(1, null, null, null, null, null, null, null, null)).ReturnsAsync(new List<BazaarLink>());
            BazaarHttpClient.Setup(b => b.BuyBazaarAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<short>(), It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(true);
        }

        [TestMethod]
        public async Task BuyingWhileInShopShouldBeIgnored()
        {
            await new Spec("Buying while in shop should be ignored")
                .Given(CharacterIsInShop)
                .WhenAsync(BuyingFromBazaarViaMiddleware)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingNonExistentItemShouldShowOfferUpdated()
        {
            await new Spec("Buying non existent item should show offer updated")
                .WhenAsync(BuyingNonExistentItem)
                .Then(ShouldReceiveOfferUpdatedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingOwnItemShouldShowOfferUpdated()
        {
            await new Spec("Buying own item should show offer updated")
                .WhenAsync(BuyingOwnItem)
                .Then(ShouldReceiveOfferUpdatedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingAtWrongPriceShouldShowOfferUpdated()
        {
            await new Spec("Buying at wrong price should show offer updated")
                .WhenAsync(BuyingAtWrongPrice)
                .Then(ShouldReceiveOfferUpdatedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingWhenInventoryFullShouldShowNoSpace()
        {
            await new Spec("Buying when inventory full should show no space")
                .Given(InventoryIsFull)
                .WhenAsync(BuyingItemFromBazaar)
                .Then(ShouldReceiveNotEnoughSpaceMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingMoreThanAvailableShouldShowOfferUpdated()
        {
            await new Spec("Buying more than available should show offer updated")
                .Given(CharacterHasGold_, 5000L)
                .WhenAsync(BuyingMoreThanAvailable)
                .Then(ShouldReceiveOfferUpdatedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingPartialPackageShouldBeIgnored()
        {
            await new Spec("Buying partial package should be ignored")
                .Given(CharacterHasGold_, 5000L)
                .WhenAsync(BuyingPartialPackage)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFullPackageShouldSucceed()
        {
            await new Spec("Buying full package should succeed")
                .Given(CharacterHasGold_, 5000L)
                .And(ItemProviderIsConfigured)
                .WhenAsync(BuyingFullPackage)
                .Then(ShouldReceiveBoughtItemMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingWithoutEnoughGoldShouldFail()
        {
            await new Spec("Buying without enough gold should fail")
                .WhenAsync(BuyingItemFromBazaar)
                .Then(ShouldReceiveInsufficientGoldMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingItemShouldSucceed()
        {
            await new Spec("Buying item should succeed")
                .Given(CharacterHasGold_, 5000L)
                .And(ItemProviderIsConfigured)
                .WhenAsync(BuyingItemFromBazaar)
                .Then(ShouldReceiveBoughtItemMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingWithoutTheBazaarOpenIsIgnored()
        {
            await new Spec("Buying a listing without the bazaar window open does nothing")
                .Given(CharacterHasGold_, 5000L)
                .And(ItemProviderIsConfigured)
                .And(NoBazaarWindowIsOpen)
                .WhenAsync(BuyingItemFromBazaar)
                .Then(GoldShouldStayAt_, 5000L)
                .And(NoListingShouldBeClaimed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingAfterWalkingAwayFromTheBazaarNpcIsIgnored()
        {
            await new Spec("Buying a listing after walking away from the bazaar NPC does nothing")
                .Given(CharacterHasGold_, 5000L)
                .And(ItemProviderIsConfigured)
                .And(CharacterWalksTo_, (short)30, (short)30)
                .WhenAsync(BuyingItemFromBazaar)
                .Then(GoldShouldStayAt_, 5000L)
                .And(NoListingShouldBeClaimed)
                .ExecuteAsync();
        }

        private void NoBazaarWindowIsOpen() => Session.Character.NpcDialog = null;

        private void CharacterWalksTo_(short x, short y) => Session.WalkTo(x, y);

        private void GoldShouldStayAt_(long gold) => Assert.AreEqual(gold, Session.Character.Gold);

        private void NoListingShouldBeClaimed() =>
            BazaarHttpClient.Verify(b => b.BuyBazaarAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<short>(), It.IsAny<long>(), It.IsAny<long>()), Times.Never);

        private void CharacterIsInShop()
        {
            Session.Character.InShop = true;
        }

        private async Task BuyingFromBazaarViaMiddleware()
        {
            await Session.HandlePacketsAsync(new[]
            {
                new CBuyPacket { BazaarId = 1, Price = 50, Amount = 1, VNum = 1012 }
            });
        }

        private void NoPacketShouldBeSent()
        {
            Assert.IsNull(Session.LastPackets.FirstOrDefault());
        }

        private async Task BuyingNonExistentItem()
        {
            await CbuyPacketHandler.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 1, Price = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private void ShouldReceiveOfferUpdatedMessage()
        {
            var packet = (ModaliPacket?)Session.LastPackets.FirstOrDefault(s => s is ModaliPacket);
            Assert.IsTrue(packet?.Type == 1 && packet?.Message == Game18NConstString.OfferUpdated);
        }

        private async Task BuyingOwnItem()
        {
            await CbuyPacketHandler.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 2, Price = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private async Task BuyingAtWrongPrice()
        {
            await CbuyPacketHandler.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0, Price = 40, Amount = 1, VNum = 1012
            }, Session);
        }

        private void InventoryIsFull()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            Session.Character.InventoryService.AddItemToPocket(new InventoryItemInstance(new ItemInstance(new Item { VNum = 1012 }) { Amount = 999, Id = guid2 })
            {
                Id = guid2, Slot = 0, Type = NoscorePocketType.Main
            });
            Session.Character.InventoryService.AddItemToPocket(new InventoryItemInstance(new ItemInstance(new Item { VNum = 1012 }) { Amount = 999, Id = guid1 })
            {
                Id = guid1, Slot = 1, Type = NoscorePocketType.Main
            });
        }

        private async Task BuyingItemFromBazaar()
        {
            await CbuyPacketHandler.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0, Price = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private void ShouldReceiveNotEnoughSpaceMessage()
        {
            var packet = (InfoiPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.NotEnoughSpace);
        }

        private void CharacterHasGold_(long gold)
        {
            Session.Character.Gold = gold;
        }

        private async Task BuyingMoreThanAvailable()
        {
            await CbuyPacketHandler.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0, Price = 50, Amount = 2, VNum = 1012
            }, Session);
        }

        private async Task BuyingPartialPackage()
        {
            await CbuyPacketHandler.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 3, Price = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private void ItemProviderIsConfigured()
        {
            var item = new Item { Type = NoscorePocketType.Main, VNum = 1012 };
            ItemProvider.Setup(s => s.Convert(It.IsAny<IItemInstanceDto>()))
                .Returns(new ItemInstance(item) { Amount = 1, Item = item });
        }

        private async Task BuyingFullPackage()
        {
            await CbuyPacketHandler.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 3, Price = 50, Amount = 99, VNum = 1012
            }, Session);
        }

        private void ShouldReceiveBoughtItemMessage()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player &&
                packet?.VisualId == Session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow &&
                packet?.Message == Game18NConstString.BoughtItem);
        }

        private void ShouldReceiveInsufficientGoldMessage()
        {
            var packet = (ModaliPacket?)Session.LastPackets.FirstOrDefault(s => s is ModaliPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.InsufficientGoldAvailable);
        }
    
        [TestMethod]
        public async Task LosingTheRaceForAListingCostsNeitherGoldNorMakesAnItem()
        {
            // The listing lives on the master server and every channel reads it, so two buyers
            // can both pass the price and amount checks. Only one wins the claim.
            Session.Character.Gold = 500;
            ItemProviderIsConfigured();
            BazaarHttpClient!
                .Setup(b => b.BuyBazaarAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<short>(), It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(false);

            await CbuyPacketHandler!.ExecuteAsync(new CBuyPacket { BazaarId = 0, Amount = 1, Price = 50 }, Session);

            Assert.AreEqual(500, Session.Character.Gold, "the loser must keep their gold");
            Assert.AreEqual(0, Session.Character.InventoryService.Count, "the loser must not receive an item");
        }

    }
}
