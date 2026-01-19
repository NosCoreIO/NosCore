//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.BazaarService;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.BazaarService
{
    [TestClass]
    public class BazaarRegistryTests
    {
        private IBazaarRegistry Registry = null!;
        private Mock<IDao<BazaarItemDto, long>> MockBazaarItemDao = null!;
        private Mock<IDao<IItemInstanceDto?, Guid>> MockItemInstanceDao = null!;
        private Mock<IDao<CharacterDto, long>> MockCharacterDao = null!;

        private BazaarLink? ResultLink;
        private IEnumerable<BazaarLink>? ResultLinks;
        private bool UnregisterResult;
        private int CountResult;

        private const long TestBazaarItemId = 1;
        private const long TestBazaarItemId2 = 2;
        private const long TestSellerId = 100;
        private const long TestSellerId2 = 200;
        private const string TestSellerName = "TestSeller";
        private const string TestSellerName2 = "TestSeller2";

        [TestInitialize]
        public void Setup()
        {
            MockBazaarItemDao = new Mock<IDao<BazaarItemDto, long>>();
            MockItemInstanceDao = new Mock<IDao<IItemInstanceDto?, Guid>>();
            MockCharacterDao = new Mock<IDao<CharacterDto, long>>();

            MockBazaarItemDao.Setup(d => d.LoadAll()).Returns(new List<BazaarItemDto>());
            MockItemInstanceDao.Setup(d => d.Where(It.IsAny<System.Linq.Expressions.Expression<Func<IItemInstanceDto?, bool>>>()))
                .Returns(new List<IItemInstanceDto?>());
            MockCharacterDao.Setup(d => d.Where(It.IsAny<System.Linq.Expressions.Expression<Func<CharacterDto, bool>>>()))
                .Returns(new List<CharacterDto>());

            Registry = new BazaarRegistry(MockBazaarItemDao.Object, MockItemInstanceDao.Object, MockCharacterDao.Object);
        }

        [TestMethod]
        public async Task RegisterShouldAddBazaarLink()
        {
            await new Spec("Register should add bazaar link")
                .Given(ABazaarLinkIsCreated)
                .When(RegisteringTheBazaarLink)
                .Then(BazaarLinkShouldBeRetrievable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetByIdShouldReturnNullForUnknownId()
        {
            await new Spec("GetById should return null for unknown id")
                .When(GettingBazaarLinkByUnknownId)
                .Then(ResultShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnregisterShouldRemoveBazaarLink()
        {
            await new Spec("Unregister should remove bazaar link")
                .Given(ABazaarLinkIsRegistered)
                .When(UnregisteringTheBazaarLink)
                .Then(UnregisterShouldSucceed)
                .And(BazaarLinkShouldNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetBySellerIdShouldFilterCorrectly()
        {
            await new Spec("GetBySellerId should filter correctly")
                .Given(MultipleBazaarLinksAreRegistered)
                .When(GettingBazaarLinksBySellerId)
                .Then(OnlyLinksForSellerShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CountBySellerIdShouldReturnCorrectCount()
        {
            await new Spec("CountBySellerId should return correct count")
                .Given(MultipleBazaarLinksAreRegistered)
                .When(CountingBazaarLinksBySellerId)
                .Then(CountShouldBeCorrect)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpdateShouldReplaceExistingLink()
        {
            await new Spec("Update should replace existing link")
                .Given(ABazaarLinkIsRegistered)
                .When(UpdatingTheBazaarLink)
                .Then(BazaarLinkShouldBeUpdated)
                .ExecuteAsync();
        }

        private BazaarLink? TestBazaarLink;
        private BazaarLink? UpdatedBazaarLink;

        private void ABazaarLinkIsCreated()
        {
            TestBazaarLink = CreateBazaarLink(TestBazaarItemId, TestSellerId, TestSellerName);
        }

        private void ABazaarLinkIsRegistered()
        {
            ABazaarLinkIsCreated();
            Registry.Register(TestBazaarItemId, TestBazaarLink!);
        }

        private void MultipleBazaarLinksAreRegistered()
        {
            var link1 = CreateBazaarLink(TestBazaarItemId, TestSellerId, TestSellerName);
            var link2 = CreateBazaarLink(TestBazaarItemId2, TestSellerId, TestSellerName);
            var link3 = CreateBazaarLink(3, TestSellerId2, TestSellerName2);

            Registry.Register(TestBazaarItemId, link1);
            Registry.Register(TestBazaarItemId2, link2);
            Registry.Register(3, link3);
        }

        private void RegisteringTheBazaarLink()
        {
            Registry.Register(TestBazaarItemId, TestBazaarLink!);
        }

        private void GettingBazaarLinkByUnknownId()
        {
            ResultLink = Registry.GetById(9999);
        }

        private void UnregisteringTheBazaarLink()
        {
            UnregisterResult = Registry.Unregister(TestBazaarItemId);
        }

        private void GettingBazaarLinksBySellerId()
        {
            ResultLinks = Registry.GetBySellerId(TestSellerId);
        }

        private void CountingBazaarLinksBySellerId()
        {
            CountResult = Registry.CountBySellerId(TestSellerId);
        }

        private void UpdatingTheBazaarLink()
        {
            UpdatedBazaarLink = CreateBazaarLink(TestBazaarItemId, TestSellerId, "UpdatedSellerName");
            Registry.Update(TestBazaarItemId, UpdatedBazaarLink);
        }

        private void BazaarLinkShouldBeRetrievable()
        {
            var result = Registry.GetById(TestBazaarItemId);
            Assert.IsNotNull(result);
            Assert.AreEqual(TestSellerName, result.SellerName);
        }

        private void ResultShouldBeNull()
        {
            Assert.IsNull(ResultLink);
        }

        private void UnregisterShouldSucceed()
        {
            Assert.IsTrue(UnregisterResult);
        }

        private void BazaarLinkShouldNotExist()
        {
            var result = Registry.GetById(TestBazaarItemId);
            Assert.IsNull(result);
        }

        private void OnlyLinksForSellerShouldBeReturned()
        {
            Assert.IsNotNull(ResultLinks);
            var linksList = ResultLinks.ToList();
            Assert.AreEqual(2, linksList.Count);
            Assert.IsTrue(linksList.All(l => l.BazaarItem?.SellerId == TestSellerId));
        }

        private void CountShouldBeCorrect()
        {
            Assert.AreEqual(2, CountResult);
        }

        private void BazaarLinkShouldBeUpdated()
        {
            var result = Registry.GetById(TestBazaarItemId);
            Assert.IsNotNull(result);
            Assert.AreEqual("UpdatedSellerName", result.SellerName);
        }

        private static BazaarLink CreateBazaarLink(long bazaarItemId, long sellerId, string sellerName)
        {
            return new BazaarLink
            {
                BazaarItem = new BazaarItemDto
                {
                    BazaarItemId = bazaarItemId,
                    SellerId = sellerId,
                    ItemInstanceId = Guid.NewGuid(),
                    Amount = 1,
                    Price = 100
                },
                ItemInstance = new ItemInstanceDto
                {
                    Id = Guid.NewGuid(),
                    Amount = 1
                },
                SellerName = sellerName
            };
        }
    }
}
