//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Auction;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Tests.Shared.BDD.Steps;

namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class CBListPacketHandlerTests
    {
        private Mock<IBazaarHub> BazaarHttpClient = null!;
        private CBListPacketHandler CblistPacketHandler = null!;
        private ClientSession Session = null!;
        private BazaarSearch? Search;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.WithNpcAt(100, 1, 1);
            Session.WithWindowOpenedAt(100, NpcDialogWindow.Bazaar);
            BazaarHttpClient = new Mock<IBazaarHub>();
            BazaarHttpClient.Setup(b => b.SearchBazaarAsync(It.IsAny<BazaarSearch>()))
                .Callback((BazaarSearch search) => Search = search)
                .ReturnsAsync(new List<BazaarLink>());
            CblistPacketHandler = new CBListPacketHandler(BazaarHttpClient.Object, TestHelpers.Instance.Clock, TestHelpers.Instance.NpcDialogService);
        }

        [TestMethod]
        public async Task ListingShouldReturnEmptyWhenNoItems()
        {
            await new Spec("Listing should return empty when no items")
                .WhenAsync(ListingBazaarItems)
                .Then(ShouldReceiveEmptyList)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ListingShouldSendTheReturnedPage()
        {
            await new Spec("Listing should send the returned page")
                .Given(TheMasterReturnsOneListing)
                .WhenAsync(ListingBazaarItems)
                .Then(ShouldReceiveTheListing)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ListingShouldAskTheMasterForOnlyTheRequestedPage()
        {
            await new Spec("Listing should ask the master for only the requested page")
                .WhenAsync(ListingTheSecondPageWithFilters)
                .Then(TheSearchShouldCarryThePageAndFilters)
                .And(TheWholeBazaarShouldNotBeFetched)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ListingWithoutAnItemFilterShouldSearchEveryItem()
        {
            await new Spec("Listing without an item filter should search every item")
                .WhenAsync(ListingWithoutAnItemFilter)
                .Then(TheSearchShouldHaveNoItemFilter)
                .And(ShouldReceiveEmptyList)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ListingWithoutTheBazaarOpenSendsNothing()
        {
            await new Spec("Listing the bazaar without its window open sends no list")
                .Given(TheMasterReturnsOneListing)
                .And(NoBazaarWindowIsOpen)
                .WhenAsync(ListingBazaarItems)
                .Then(NoListShouldBeSent)
                .ExecuteAsync();
        }

        private void NoBazaarWindowIsOpen() => Session.Character.NpcDialog = null;

        private void NoListShouldBeSent() => Assert.IsFalse(Session.LastPackets.Any(s => s is RcbListPacket));

        private async Task ListingBazaarItems()
        {
            await CblistPacketHandler.ExecuteAsync(new CBListPacket { ItemVNumFilter = new List<short>() }, Session);
        }

        private async Task ListingTheSecondPageWithFilters()
        {
            await CblistPacketHandler.ExecuteAsync(new CBListPacket
            {
                Index = 1,
                TypeFilter = BazaarListType.Weapon,
                SubTypeFilter = 2,
                LevelFilter = 3,
                RareFilter = 4,
                UpgradeFilter = 5,
                OrderFilter = 1,
                ItemVNumFilter = new List<short> { 1012 }
            }, Session);
        }

        private async Task ListingWithoutAnItemFilter()
        {
            await CblistPacketHandler.ExecuteAsync(new CBListPacket { ItemVNumFilter = null }, Session);
        }

        private void TheMasterReturnsOneListing()
        {
            BazaarHttpClient.Setup(b => b.SearchBazaarAsync(It.IsAny<BazaarSearch>()))
                .ReturnsAsync(new List<BazaarLink>
                {
                    new()
                    {
                        SellerName = "test",
                        BazaarItem = new BazaarItemDto
                        {
                            BazaarItemId = 7, SellerId = 3, Price = 50, Amount = 1,
                            DateStart = TestHelpers.Instance.Clock.GetCurrentInstant(), Duration = 2
                        },
                        ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 4 }
                    }
                });
        }

        private void ShouldReceiveEmptyList()
        {
            var packet = (RcbListPacket?)Session.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(packet?.Items?.Count == 0);
        }

        private void ShouldReceiveTheListing()
        {
            var packet = (RcbListPacket?)Session.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            var listing = packet!.Items!.Single()!;
            Assert.AreEqual(7, listing.AuctionId);
            Assert.AreEqual(3, listing.OwnerId);
            Assert.AreEqual("test", listing.OwnerName);
            Assert.AreEqual(1012, listing.ItemId);
            Assert.AreEqual(4, listing.Amount);
            Assert.AreEqual(50, listing.Price);
            Assert.AreEqual((long)Duration.FromHours(2).TotalMinutes, listing.MinutesLeft);
        }

        private void TheSearchShouldCarryThePageAndFilters()
        {
            Assert.IsNotNull(Search);
            Assert.AreEqual(1, Search.Index);
            Assert.AreEqual(30, Search.PageSize);
            Assert.AreEqual(BazaarListType.Weapon, Search.TypeFilter);
            Assert.AreEqual(2, Search.SubTypeFilter);
            Assert.AreEqual(3, Search.LevelFilter);
            Assert.AreEqual(4, Search.RareFilter);
            Assert.AreEqual(5, Search.UpgradeFilter);
            Assert.AreEqual(1, Search.OrderFilter);
            CollectionAssert.AreEqual(new List<short> { 1012 }, Search.ItemVNumFilter);
            Assert.AreEqual(Session.Account.Language, Search.Language);
        }

        private void TheWholeBazaarShouldNotBeFetched()
        {
            BazaarHttpClient.Verify(b => b.GetBazaar(It.IsAny<long>(), It.IsAny<byte?>(), It.IsAny<byte?>(),
                It.IsAny<BazaarListType?>(), It.IsAny<byte?>(), It.IsAny<byte?>(), It.IsAny<byte?>(),
                It.IsAny<byte?>(), It.IsAny<long?>()), Times.Never);
        }

        private void TheSearchShouldHaveNoItemFilter()
        {
            Assert.AreEqual(0, Search!.ItemVNumFilter.Count);
        }
    }
}
