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
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class CScalcPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Mock<IBazaarHub> BazaarHttpClient = null!;
        private CScalcPacketHandler CScalcPacketHandler = null!;
        private Mock<IDao<IItemInstanceDto?, Guid>> ItemInstanceDao = null!;
        private Mock<IItemGenerationService> ItemProvider = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            BazaarHttpClient = new Mock<IBazaarHub>();
            ItemProvider = new Mock<IItemGenerationService>();
            ItemInstanceDao = new Mock<IDao<IItemInstanceDto?, Guid>>();
            CScalcPacketHandler = new CScalcPacketHandler(TestHelpers.Instance.WorldConfiguration,
                BazaarHttpClient.Object, ItemProvider.Object, Logger, ItemInstanceDao.Object, TestHelpers.Instance.LogLanguageLocalizer);

            BazaarHttpClient.Setup(b => b.GetBazaar(0, null, null, null, null, null, null, null, null)).ReturnsAsync(
                new List<BazaarLink>() {new BazaarLink
                {
                    SellerName = Session.Character.Name,
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 0 }
                }});
            BazaarHttpClient.Setup(b => b.GetBazaar(2, null, null, null, null, null, null, null, null)).ReturnsAsync(
                new List<BazaarLink>() {new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto { Price = 60, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 0 }
                }});
            BazaarHttpClient.Setup(b => b.GetBazaar(1, null, null, null, null, null, null, null, null)).ReturnsAsync(new List<BazaarLink>());
            BazaarHttpClient.Setup(b => b.DeleteBazaarAsync(It.IsAny<long>(), It.IsAny<short>(), It.IsAny<string>(), It.IsAny<long?>())).ReturnsAsync(true);
            ItemProvider.Setup(s => s.Convert(It.IsAny<IItemInstanceDto>())).Returns(new ItemInstance(new Item() { VNum = 1012 })
            {
                Amount = 0, Item = new Item()
            });
        }

        [TestMethod]
        public async Task RetrievingWhileInShopShouldBeIgnored()
        {
            await new Spec("Retrieving while in shop should be ignored")
                .Given(CharacterIsInShop)
                .WhenAsync(RetrievingFromBazaarViaMiddleware)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RetrievingNonExistentItemShouldReturnZeroPrice()
        {
            await new Spec("Retrieving non existent item should return zero price")
                .WhenAsync(RetrievingNonExistentItem)
                .Then(ShouldReceiveZeroPriceResponse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RetrievingOtherSellersItemShouldReturnZeroPrice()
        {
            await new Spec("Retrieving other sellers item should return zero price")
                .WhenAsync(RetrievingOtherSellersItem)
                .Then(ShouldReceiveZeroPriceResponse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RetrievingWhenInventoryFullShouldStillReturnPrice()
        {
            await new Spec("Retrieving when inventory full should still return price")
                .Given(InventoryIsFull)
                .WhenAsync(RetrievingOwnItem)
                .Then(ShouldReceivePriceOf_, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RetrievingWhenAtMaxGoldShouldShowError()
        {
            await new Spec("Retrieving when at max gold should show error")
                .Given(CharacterHasMaxGold)
                .WhenAsync(RetrievingOwnItem)
                .Then(ShouldReceiveMaxGoldMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RetrievingSoldItemShouldSucceed()
        {
            await new Spec("Retrieving sold item should succeed")
                .Given(ItemInstanceDaoIsConfigured)
                .WhenAsync(RetrievingOwnItem)
                .Then(ShouldReceiveTotalOf_, 50)
                .ExecuteAsync();
        }

        private void CharacterIsInShop()
        {
            Session.Character.InShop = true;
        }

        private async Task RetrievingFromBazaarViaMiddleware()
        {
            await Session.HandlePacketsAsync(new[]
            {
                new CScalcPacket { BazaarId = 1, Price = 50, Amount = 1, VNum = 1012 }
            });
        }

        private void NoPacketShouldBeSent()
        {
            Assert.IsNull(Session.LastPackets.FirstOrDefault());
        }

        private async Task RetrievingNonExistentItem()
        {
            await CScalcPacketHandler.ExecuteAsync(new CScalcPacket
            {
                BazaarId = 1, Price = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private void ShouldReceiveZeroPriceResponse()
        {
            var packet = (RCScalcPacket?)Session.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(0, packet?.Price);
        }

        private async Task RetrievingOtherSellersItem()
        {
            await CScalcPacketHandler.ExecuteAsync(new CScalcPacket
            {
                BazaarId = 2, Price = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private void InventoryIsFull()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            Session.Character.InventoryService.AddItemToPocket(
                new InventoryItemInstance(new ItemInstance(new Item() { VNum = 1012 }) { Amount = 999, Id = guid2 })
                {
                    Id = guid2, Slot = 0, Type = NoscorePocketType.Main,
                });
            Session.Character.InventoryService.AddItemToPocket(new InventoryItemInstance(
                new ItemInstance(new Item() { VNum = 1012 }) { Amount = 999, Id = guid1 })
            {
                Id = guid1, Slot = 1, Type = NoscorePocketType.Main
            });
        }

        private async Task RetrievingOwnItem()
        {
            await CScalcPacketHandler.ExecuteAsync(new CScalcPacket
            {
                BazaarId = 0, Price = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private void ShouldReceivePriceOf_(int value)
        {
            var packet = (RCScalcPacket?)Session.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(value, packet?.Price);
        }

        private void CharacterHasMaxGold()
        {
            Session.Character.Gold = TestHelpers.Instance.WorldConfiguration.Value.MaxGoldAmount;
        }

        private void ShouldReceiveMaxGoldMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(Game18NConstString.MaxGoldReached, packet?.Message);
        }

        private void ItemInstanceDaoIsConfigured()
        {
            ItemInstanceDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto { ItemVNum = 1012, Amount = 0 });
        }

        private void ShouldReceiveTotalOf_(int value)
        {
            var packet = (RCScalcPacket?)Session.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(value, packet?.Total);
        }
    }
}
