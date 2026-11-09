//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Tests.Services.BazaarService
{
    [TestClass]
    public class BazaarListTests
    {
        private const short Armor = 1;
        private const short Boots = 2;
        private const short Necklace = 3;

        private readonly FakeClock Clock = new(Instant.FromUtc(2021, 01, 01, 01, 01, 01));
        private readonly List<BazaarLink> Links = new();
        private GameObject.Services.BazaarService.BazaarService Service = null!;
        private BazaarSearch Search = null!;
        private List<BazaarLink> Page = new();

        [TestInitialize]
        public void Setup()
        {
            var items = new List<ItemDto>
            {
                Named(Necklace, "Necklace", "Halskette"),
                Named(Armor, "Armor", "Rüstung"),
                Named(Boots, "Boots", "Stiefel"),
            };
            var registry = new Mock<GameObject.Services.BazaarService.IBazaarRegistry>();
            registry.Setup(s => s.GetAll()).Returns(Links);
            Service = new GameObject.Services.BazaarService.BazaarService(registry.Object,
                new Mock<IDao<BazaarItemDto, long>>().Object, new Mock<IDao<IItemInstanceDto?, Guid>>().Object,
                new Mock<IDao<BazaarOperationDto, Guid>>().Object, new Mock<IDaoTransactionScope>().Object, Clock, items);
            Search = new BazaarSearch { Language = RegionType.EN, PageSize = 50 };
        }

        [TestMethod]
        public void ListingsAreSortedByNameThenByPrice()
        {
            new Spec("Listings are sorted by name then by price")
                .Given(MixedListings)
                .When(SearchingByAscendingPrice)
                .Then(PageShouldBe_, new long[] { 3, 2, 4, 1 })
                .Execute();
        }

        [TestMethod]
        public void ListingsAreSortedByNameThenByDescendingPrice()
        {
            new Spec("Listings are sorted by name then by descending price")
                .Given(MixedListings)
                .When(SearchingByDescendingPrice)
                .Then(PageShouldBe_, new long[] { 2, 3, 4, 1 })
                .Execute();
        }

        [TestMethod]
        public void NamesFollowTheSearchersLanguage()
        {
            new Spec("Names follow the searcher's language")
                .Given(MixedListings)
                .When(SearchingInGerman)
                .Then(PageShouldBe_, new long[] { 1, 3, 2, 4 })
                .Execute();
        }

        [TestMethod]
        public void ExpiredAndEmptyListingsAreLeftOut()
        {
            new Spec("Expired and empty listings are left out")
                .Given(MixedListings)
                .And(AnExpiredListing)
                .And(AnEmptyListing)
                .When(SearchingByAscendingPrice)
                .Then(PageShouldBe_, new long[] { 3, 2, 4, 1 })
                .Execute();
        }

        [TestMethod]
        public void ItemFilterKeepsOnlyTheListedItems()
        {
            new Spec("Item filter keeps only the listed items")
                .Given(MixedListings)
                .When(SearchingForBootsAndNecklaces)
                .Then(PageShouldBe_, new long[] { 4, 1 })
                .Execute();
        }

        [TestMethod]
        public void OnlyTheRequestedPageIsReturned()
        {
            new Spec("Only the requested page is returned")
                .Given(SixtyArmorsPricedByTheirId)
                .When(SearchingTheSecondPage)
                .Then(PageShouldBe_, Enumerable.Range(51, 10).Select(i => (long)i).ToArray())
                .Execute();
        }

        private static ItemDto Named(short vnum, string english, string german)
        {
            var item = new Item { VNum = vnum };
            item.Name[RegionType.EN] = english;
            item.Name[RegionType.DE] = german;
            return item;
        }

        private void List(long id, short vnum, long price, short amount = 1, short durationHours = 24)
        {
            Links.Add(new BazaarLink
            {
                BazaarItem = new BazaarItemDto
                {
                    BazaarItemId = id, Price = price, Amount = amount, DateStart = Clock.GetCurrentInstant(),
                    Duration = durationHours
                },
                ItemInstance = new ItemInstanceDto { ItemVNum = vnum, Amount = amount }
            });
        }

        private void MixedListings()
        {
            List(1, Necklace, 10);
            List(2, Armor, 30);
            List(3, Armor, 20);
            List(4, Boots, 5);
        }

        private void AnExpiredListing()
        {
            Links.Add(new BazaarLink
            {
                BazaarItem = new BazaarItemDto
                {
                    BazaarItemId = 5, Price = 1, Amount = 1, DateStart = Clock.GetCurrentInstant() - Duration.FromHours(25),
                    Duration = 24
                },
                ItemInstance = new ItemInstanceDto { ItemVNum = Armor, Amount = 1 }
            });
        }

        private void AnEmptyListing() => List(6, Armor, 1, amount: 0);

        private void SixtyArmorsPricedByTheirId()
        {
            foreach (var id in Enumerable.Range(1, 60).Reverse())
            {
                List(id, Armor, id);
            }
        }

        private void SearchingByAscendingPrice()
        {
            Search.OrderFilter = 0;
            Page = Service.SearchBazaar(Search);
        }

        private void SearchingByDescendingPrice()
        {
            Search.OrderFilter = 1;
            Page = Service.SearchBazaar(Search);
        }

        private void SearchingInGerman()
        {
            Search.Language = RegionType.DE;
            SearchingByAscendingPrice();
        }

        private void SearchingForBootsAndNecklaces()
        {
            Search.ItemVNumFilter = new List<short> { 0, Boots, Necklace };
            SearchingByAscendingPrice();
        }

        private void SearchingTheSecondPage()
        {
            Search.Index = 1;
            SearchingByAscendingPrice();
        }

        private void PageShouldBe_(long[] expected)
        {
            CollectionAssert.AreEqual(expected, Page.Select(s => s.BazaarItem!.BazaarItemId).ToArray());
        }
    }
}
