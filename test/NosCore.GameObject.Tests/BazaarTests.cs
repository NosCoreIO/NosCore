//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoFixture;
using Json.More;
using Json.Patch;
using Json.Pointer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Services.BazaarService;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.AutoFixture;
using SpecLight;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class BazaarTests
    {
        private BazaarHub BazaarController = null!;
        private BazaarRegistry BazaarItemsHolder = null!;
        private Guid Guid;
        private Mock<IDao<BazaarItemDto, long>> MockBzDao = null!;
        private Mock<IDao<IItemInstanceDto?, Guid>> MockItemDao = null!;
        private NosCoreFixture Fixture = null!;
        private LanguageKey? LastResult;

        [TestInitialize]
        public void Setup()
        {
            Guid = Guid.NewGuid();
            Fixture = new NosCoreFixture();
            MockBzDao = new Mock<IDao<BazaarItemDto, long>>();
            MockItemDao = new Mock<IDao<IItemInstanceDto?, Guid>>();

            var mockCharacterDao = new Mock<IDao<CharacterDto, long>>();
            BazaarItemsHolder = new BazaarRegistry(MockBzDao.Object, MockItemDao.Object, mockCharacterDao.Object);
            BazaarController = new BazaarHub(new BazaarService(BazaarItemsHolder, MockBzDao.Object, MockItemDao.Object, TestHelpers.Instance.Clock));
            MockItemDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>()))
                .Returns<IItemInstanceDto?>(Task.FromResult);
            MockBzDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<BazaarItemDto>()))
                .Returns<BazaarItemDto>(Task.FromResult);
        }

        [TestMethod]
        public async Task AddingFullStackToBazaarShouldSucceed()
        {
            await new Spec("Adding full stack to bazaar should succeed")
                .Given(AnItemExistsWithQuantity_, 99)
                .WhenAsync(RegisteringItems_, 99)
                .Then(TheItemShouldBeInBazaar)
                .And(TheBazaarAmountShouldBe_, 99)
                .And(TheResultShouldBeSuccess)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingPartialStackShouldCreateNewItemInstance()
        {
            await new Spec("Adding partial stack creates new item instance")
                .Given(AnItemExistsWithQuantity_, 99)
                .WhenAsync(RegisteringItems_, 50)
                .Then(ANewItemInstanceShouldBeCreated)
                .And(TheBazaarAmountShouldBe_, 50)
                .And(TheResultShouldBeSuccess)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingNegativeAmountShouldThrow()
        {
            await new Spec("Adding negative amount should throw")
                .Given(AnItemExistsWithQuantity_, 99)
                .When(AttemptingToRegisterNegativeAmountThrows)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingNegativePriceShouldThrow()
        {
            await new Spec("Adding negative price should throw")
                .Given(AnItemExistsWithQuantity_, 99)
                .When(AttemptingToRegisterNegativePriceThrows)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingMoreThanAvailableShouldThrow()
        {
            await new Spec("Adding more than available should throw")
                .Given(AnItemExistsWithQuantity_, 99)
                .When(AttemptingToRegisterMoreThanAvailableThrows)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingNullItemShouldThrow()
        {
            await new Spec("Adding null item should throw")
                .When(AttemptingToRegisterNonExistentItemThrows)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExceedingBazaarLimitShouldFail()
        {
            var rand = new Random();
            MockBzDao.Setup(m => m.TryInsertOrUpdateAsync(It.IsAny<BazaarItemDto>()))
                .Returns((BazaarItemDto y) =>
                {
                    y.BazaarItemId = rand.Next(0, 9999999);
                    return Task.FromResult(y);
                });

            await new Spec("Exceeding bazaar limit should fail")
                .GivenAsync(APlayerAttemptsToListManyItems)
                .Then(Only_ItemsShouldBeListed, 10)
                .And(TheResultShouldBeLimitExceeded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingNonExistentListingShouldThrow()
        {
            await new Spec("Deleting non-existent listing should throw")
                .When(AttemptingToDeleteNonExistentListingThrows)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OwnerCanRemoveFromBazaar()
        {
            await new Spec("Owner can remove from bazaar")
                .GivenAsync(AnItemIsListedByOwner)
                .WhenAsync(OwnerRemovesAllItems)
                .Then(ListingShouldBeRemoved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ModifyingPriceShouldSucceed()
        {
            await new Spec("Modifying price should succeed")
                .GivenAsync(AnItemIsListedAtPrice_, 80L)
                .WhenAsync(ChangingPriceTo_, 50L)
                .Then(PriceShouldBe_, 50L)
                .ExecuteAsync();
        }

        private void AnItemExistsWithQuantity_(int amount)
        {
            MockItemDao
                .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(new ItemInstanceDto { Id = Guid, Amount = (short)amount });
        }

        private async Task RegisteringItems_(int amount)
        {
            LastResult = await BazaarController.AddBazaarAsync(Fixture.Build<BazaarRequest>()
                .With(r => r.Amount, amount)
                .With(r => r.ItemInstanceId, Guid)
                .Create());
        }

        private void TheItemShouldBeInBazaar()
        {
            Assert.AreEqual(Guid, BazaarItemsHolder.GetById(0)?.BazaarItem?.ItemInstanceId);
        }

        private void TheBazaarAmountShouldBe_(int expected)
        {
            Assert.AreEqual(expected, BazaarItemsHolder.GetById(0)?.BazaarItem?.Amount ?? 0);
        }

        private void TheResultShouldBeSuccess()
        {
            Assert.AreEqual(LanguageKey.OBJECT_IN_BAZAAR, LastResult);
        }

        private void ANewItemInstanceShouldBeCreated()
        {
            Assert.AreNotEqual(Guid, BazaarItemsHolder.GetById(0)?.BazaarItem?.ItemInstanceId);
        }

        private void AttemptingToRegisterNegativeAmountThrows()
        {
            Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
                await BazaarController.AddBazaarAsync(Fixture.Build<BazaarRequest>()
                    .With(r => r.Amount, -50)
                    .With(r => r.ItemInstanceId, Guid)
                    .Create()));
        }

        private void AttemptingToRegisterNegativePriceThrows()
        {
            Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
                await BazaarController.AddBazaarAsync(Fixture.Build<BazaarRequest>()
                    .With(r => r.Amount, 50)
                    .With(r => r.Price, -50)
                    .With(r => r.ItemInstanceId, Guid)
                    .Create()));
        }

        private void AttemptingToRegisterMoreThanAvailableThrows()
        {
            Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
                await BazaarController.AddBazaarAsync(Fixture.Build<BazaarRequest>()
                    .With(r => r.Amount, 100)
                    .With(r => r.ItemInstanceId, Guid)
                    .Create()));
        }

        private void AttemptingToRegisterNonExistentItemThrows()
        {
            Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
                await BazaarController.AddBazaarAsync(Fixture.Build<BazaarRequest>()
                    .With(r => r.ItemInstanceId, Guid)
                    .Create()));
        }

        private async Task APlayerAttemptsToListManyItems()
        {
            for (var i = 0; i < 12; i++)
            {
                var guid = Guid.NewGuid();
                MockItemDao.Reset();
                MockItemDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>()))
                    .Returns<IItemInstanceDto?>(Task.FromResult);
                MockItemDao
                    .Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                    .ReturnsAsync(new ItemInstanceDto { Id = guid, Amount = 99 });
                LastResult = await BazaarController.AddBazaarAsync(Fixture.Build<BazaarRequest>()
                    .With(r => r.Amount, 99)
                    .With(r => r.ItemInstanceId, guid)
                    .With(r => r.CharacterId, 1L)
                    .With(r => r.CharacterName, "test")
                    .With(r => r.HasMedal, false)
                    .Create());
            }
        }

        private void Only_ItemsShouldBeListed(int value)
        {
            Assert.AreEqual(10, BazaarItemsHolder.GetAll().Count());
        }

        private void TheResultShouldBeLimitExceeded()
        {
            Assert.AreEqual(LanguageKey.LIMIT_EXCEEDED, LastResult);
        }

        private void AttemptingToDeleteNonExistentListingThrows()
        {
            Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
                await BazaarController.DeleteBazaarAsync(0, 99, "test"));
        }

        private async Task AnItemIsListedByOwner()
        {
            AnItemExistsWithQuantity_(99);
            await BazaarController.AddBazaarAsync(Fixture.Build<BazaarRequest>()
                .With(r => r.Amount, 99)
                .With(r => r.ItemInstanceId, Guid)
                .With(r => r.CharacterName, "test")
                .Create());
        }

        private async Task OwnerRemovesAllItems()
        {
            var result = await BazaarController.DeleteBazaarAsync(0, 99, "test");
            Assert.IsTrue(result);
        }

        private void ListingShouldBeRemoved()
        {
            Assert.AreEqual(0, BazaarItemsHolder.GetAll().Count());
        }

        private async Task AnItemIsListedAtPrice_(long price)
        {
            AnItemExistsWithQuantity_(99);
            await BazaarController.AddBazaarAsync(Fixture.Build<BazaarRequest>()
                .With(r => r.Amount, 99)
                .With(r => r.ItemInstanceId, Guid)
                .With(r => r.Price, price)
                .Create());
        }

        private async Task ChangingPriceTo_(long newPrice)
        {
            var patch = new JsonPatch(PatchOperation.Replace(
                JsonPointer.Parse("/BazaarItem/Price"), newPrice.AsJsonElement().AsNode()));
            var result = await BazaarController.ModifyBazaarAsync(0, patch);
            Assert.IsNotNull(result);
        }

        private void PriceShouldBe_(long expected)
        {
            Assert.AreEqual(expected, BazaarItemsHolder.GetById(0)?.BazaarItem?.Price);
        }
    }
}
