//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class BazaarTests
    {
        private const long Seller = 1;
        private const long Buyer = 2;

        private BazaarTestMaster Master = null!;
        private long ListingId;
        private Guid InstanceId;
        private Guid OperationId;
        private LanguageKey? LastResult;
        private bool LastBuy;
        private BazaarClaim? LastClaim;
        private readonly ConcurrentBag<BazaarClaim> Claims = new();
        private int SettledBuys;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Master = new BazaarTestMaster();
        }

        [TestMethod]
        public async Task ARegisteredListingStaysHiddenUntilSettled()
        {
            await new Spec("A registered listing stays hidden until settled")
                .GivenAsync(AnInstanceOf_Exists, (short)10)
                .WhenAsync(RegisteringIt_, (short)10)
                .Then(TheResultShouldBe_, LanguageKey.OBJECT_IN_BAZAAR)
                .And(BuyersShouldSee_Listings, 0)
                .AndAsync(SettlingTheOperation)
                .And(BuyersShouldSee_Listings, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARetriedRegistrationListsOnce()
        {
            await new Spec("A retried registration lists once")
                .GivenAsync(AnInstanceOf_Exists, (short)10)
                .AndAsync(RegisteringIt_, (short)10)
                .WhenAsync(RetryingTheRegistration)
                .Then(TheResultShouldBe_, LanguageKey.OBJECT_IN_BAZAAR)
                .And(TheSellerShouldHave_Listings, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACompensatedRegistrationRemovesTheListing()
        {
            await new Spec("A compensated registration removes the listing")
                .GivenAsync(AnInstanceOf_Exists, (short)10)
                .AndAsync(RegisteringIt_, (short)10)
                .WhenAsync(CompensatingTheOperation)
                .Then(TheSellerShouldHave_Listings, 0)
                .AndAsync(TheListedInstanceShouldBeGone)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisteringAnAmountTheInstanceDoesNotHoldThrows()
        {
            await new Spec("Registering an amount the instance does not hold throws")
                .GivenAsync(AnInstanceOf_Exists, (short)10)
                .Then(RegisteringIt_Throws, (short)5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExceedingTheListingLimitIsRefused()
        {
            await new Spec("Exceeding the listing limit is refused")
                .GivenAsync(TheSellerHas_Listings, 10)
                .AndAsync(AnInstanceOf_Exists, (short)10)
                .WhenAsync(RegisteringIt_, (short)10)
                .Then(TheResultShouldBe_, LanguageKey.LIMIT_EXCEEDED)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARetriedBuyAppliesOnce()
        {
            await new Spec("A retried buy applies once")
                .GivenAsync(AListingOf_, (short)10)
                .AndAsync(TheBuyerBuys_, (short)3)
                .WhenAsync(RetryingTheBuy_, (short)3)
                .Then(TheBuyShouldBeAccepted)
                .And(TheListingShouldHold_, (short)7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABuyArrivingAfterItsCompensationIsRefused()
        {
            await new Spec("A buy arriving after its compensation is refused")
                .GivenAsync(AListingOf_, (short)10)
                .AndAsync(TheBuyIsCompensatedBeforeItArrives)
                .WhenAsync(RetryingTheBuy_, (short)3)
                .Then(TheBuyShouldBeRefused)
                .And(TheListingShouldHold_, (short)10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABuyRacingItsCompensationNeverKeepsTheUnits()
        {
            await new Spec("A buy racing its compensation never keeps the units")
                .GivenAsync(AListingOf_, (short)20)
                .WhenAsync(TwentyBuysRaceTheirOwnCompensation)
                .ThenAsync(EveryUnitShouldBeBackOnTheListing)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACompensatedBuyRestoresTheListing()
        {
            await new Spec("A compensated buy restores the listing")
                .GivenAsync(AListingOf_, (short)10)
                .AndAsync(TheBuyerBuys_, (short)3)
                .WhenAsync(CompensatingTheOperation)
                .Then(TheListingShouldHold_, (short)10)
                .AndAsync(CompensatingTheOperation)
                .And(TheListingShouldHold_, (short)10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CollectingPaysOnlySettledSales()
        {
            await new Spec("Collecting pays only settled sales")
                .GivenAsync(AListingOf_, (short)10)
                .AndAsync(TheBuyerBuysAndSettles_, (short)2)
                .AndAsync(TheBuyerBuys_, (short)3)
                .WhenAsync(TheSellerCollects)
                .Then(TheClaimShouldPayFor_AndReturn_, (short)2, (short)5)
                .AndAsync(SettlingTheOperation)
                .And(TheListingShouldStillHoldThePending_, (short)3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABuyCompensatedAfterACollectionComesBackToTheSeller()
        {
            await new Spec("A buy compensated after a collection comes back to the seller")
                .GivenAsync(AListingOf_, (short)10)
                .AndAsync(TheBuyerBuys_, (short)3)
                .AndAsync(TheSellerCollectsAndSettles)
                .WhenAsync(CompensatingTheFirstBuy)
                .AndAsync(TheSellerCollects)
                .Then(TheClaimShouldPayFor_AndReturn_, (short)0, (short)3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACompensatedCollectionLeavesTheListingForSale()
        {
            await new Spec("A compensated collection leaves the listing for sale")
                .GivenAsync(AListingOf_, (short)10)
                .AndAsync(TheSellerCollects)
                .WhenAsync(CompensatingTheOperation)
                .Then(BuyersShouldSee_Listings, 1)
                .And(TheListingShouldHold_, (short)10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyTheSellerCanCollect()
        {
            await new Spec("Only the seller can collect")
                .GivenAsync(AListingOf_, (short)10)
                .WhenAsync(TheBuyerTriesToCollect)
                .Then(TheClaimShouldBeRefused)
                .And(BuyersShouldSee_Listings, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingThePriceWithAPendingBuyIsRefused()
        {
            await new Spec("Changing the price with a pending buy is refused")
                .GivenAsync(AListingOf_, (short)10)
                .AndAsync(TheBuyerBuys_, (short)1)
                .WhenAsync(TheSellerChangesThePriceTo_, 80L)
                .Then(ThePriceShouldBe_, 50L)
                .AndAsync(CompensatingTheOperation)
                .AndAsync(TheSellerChangesThePriceTo_, 80L)
                .And(ThePriceShouldBe_, 80L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConcurrentBuysAndCollectionsPayEachSaleOnce()
        {
            await new Spec("Concurrent buys and collections pay each sale once")
                .GivenAsync(AListingOf_, (short)20)
                .WhenAsync(TenBuyersAndTheSellerRunAtOnce)
                .AndAsync(TheSellerCollectsWhatIsLeft)
                .Then(EverySettledSaleShouldBePaidOnceAndEveryUnitAccountedFor)
                .ExecuteAsync();
        }

        private async Task AnInstanceOf_Exists(short amount)
        {
            InstanceId = Guid.NewGuid();
            await Master.ItemInstanceDao.TryInsertOrUpdateAsync(new ItemInstanceDto
            {
                Id = InstanceId, ItemVNum = 1012, Amount = amount
            });
        }

        private async Task RegisteringIt_(short amount)
        {
            OperationId = Guid.NewGuid();
            LastResult = await Master.Hub.AddBazaarAsync(Request(amount));
        }

        private BazaarRequest Request(short amount) => new()
        {
            OperationId = OperationId,
            ItemInstanceId = InstanceId,
            CharacterId = Seller,
            CharacterName = "seller",
            Price = 50,
            Duration = 24,
            Amount = amount
        };

        private async Task RetryingTheRegistration() => LastResult = await Master.Hub.AddBazaarAsync(Request(10));

        private void RegisteringIt_Throws(short amount) =>
            Assert.ThrowsExactly<ArgumentException>(() =>
                Master.Hub.AddBazaarAsync(Request(amount)).GetAwaiter().GetResult());

        private async Task TheSellerHas_Listings(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await Master.ListAsync(Seller, "seller", 1012, 1, 50);
            }
        }

        private async Task AListingOf_(short amount) =>
            ListingId = await Master.ListAsync(Seller, "seller", 1012, amount, 50);

        private Guid FirstBuy;

        private async Task TheBuyerBuys_(short amount)
        {
            OperationId = Guid.NewGuid();
            if (FirstBuy == Guid.Empty)
            {
                FirstBuy = OperationId;
            }

            LastBuy = await Master.Hub.BuyBazaarAsync(OperationId, ListingId, amount, 50, Buyer);
        }

        private async Task TheBuyerBuysAndSettles_(short amount)
        {
            await TheBuyerBuys_(amount);
            await Master.Hub.SettleBazaarAsync(OperationId);
            FirstBuy = Guid.Empty;
        }

        private async Task RetryingTheBuy_(short amount) =>
            LastBuy = await Master.Hub.BuyBazaarAsync(OperationId, ListingId, amount, 50, Buyer);

        private async Task TheSellerCollects()
        {
            OperationId = Guid.NewGuid();
            LastClaim = await Master.Hub.ClaimBazaarAsync(OperationId, ListingId, Seller);
        }

        private async Task TheSellerCollectsAndSettles()
        {
            await TheSellerCollects();
            await Master.Hub.SettleBazaarAsync(OperationId);
        }

        private async Task TheBuyerTriesToCollect() =>
            LastClaim = await Master.Hub.ClaimBazaarAsync(Guid.NewGuid(), ListingId, Buyer);

        private async Task TheSellerChangesThePriceTo_(long price) =>
            await Master.Hub.ModifyBazaarAsync(Guid.NewGuid(), ListingId, Seller, price);

        private Task SettlingTheOperation() => Master.Hub.SettleBazaarAsync(OperationId);

        private Task CompensatingTheOperation() => Master.Hub.CompensateBazaarAsync(OperationId);

        private Task CompensatingTheFirstBuy() => Master.Hub.CompensateBazaarAsync(FirstBuy);

        private readonly ConcurrentBag<Guid> RacedOperations = new();

        private async Task TwentyBuysRaceTheirOwnCompensation()
        {
            for (var i = 0; i < 20; i++)
            {
                var operation = Guid.NewGuid();
                RacedOperations.Add(operation);
                using var barrier = new Barrier(2);
                var buy = Task.Run(async () =>
                {
                    barrier.SignalAndWait();
                    await Master.Hub.BuyBazaarAsync(operation, ListingId, 1, 50, Buyer);
                });
                var compensate = Task.Run(async () =>
                {
                    barrier.SignalAndWait();
                    // A compensation that loses the race on the record retries, as the channel does.
                    for (var attempt = 0; attempt < 5; attempt++)
                    {
                        try
                        {
                            await Master.Hub.CompensateBazaarAsync(operation);
                            return;
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                });
                await Task.WhenAll(buy, compensate);
            }
        }

        private async Task EveryUnitShouldBeBackOnTheListing()
        {
            Assert.AreEqual((short)20, Master.Registry.GetById(ListingId)?.ItemInstance?.Amount);
            foreach (var id in RacedOperations)
            {
                var operation = await Master.OperationDao.FirstOrDefaultAsync(s => s.BazaarOperationId == id);
                Assert.AreEqual(BazaarOperationState.Compensated, operation?.State);
            }
        }

        private async Task TenBuyersAndTheSellerRunAtOnce()
        {
            using var barrier = new Barrier(11);
            var buyers = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                var operation = Guid.NewGuid();
                if (await Master.Hub.BuyBazaarAsync(operation, ListingId, 2, 50, 100 + i))
                {
                    // Every third buyer cannot apply its side and gives the units back.
                    if (i % 3 == 0)
                    {
                        await Master.Hub.CompensateBazaarAsync(operation);
                    }
                    else
                    {
                        await Master.Hub.SettleBazaarAsync(operation);
                        Interlocked.Add(ref SettledBuys, 2);
                    }
                }
            }));
            var seller = Task.Run(async () =>
            {
                barrier.SignalAndWait();
                for (var i = 0; i < 5; i++)
                {
                    var operation = Guid.NewGuid();
                    var claim = await Master.Hub.ClaimBazaarAsync(operation, ListingId, Seller);
                    if (claim != null)
                    {
                        Claims.Add(claim);
                        await Master.Hub.SettleBazaarAsync(operation);
                    }

                    await Task.Yield();
                }
            });
            await Task.WhenAll(buyers.Append(seller));
        }

        private async Task TheSellerCollectsWhatIsLeft()
        {
            while (Master.Registry.GetById(ListingId) != null)
            {
                var operation = Guid.NewGuid();
                var claim = await Master.Hub.ClaimBazaarAsync(operation, ListingId, Seller);
                Assert.IsNotNull(claim);
                Claims.Add(claim);
                await Master.Hub.SettleBazaarAsync(operation);
            }
        }

        private void EverySettledSaleShouldBePaidOnceAndEveryUnitAccountedFor()
        {
            Assert.AreEqual(SettledBuys, Claims.Sum(c => c.SoldAmount));
            Assert.AreEqual(SettledBuys * 50L, Claims.Sum(c => c.Gold + c.Taxes));
            Assert.AreEqual(20, Claims.Sum(c => c.SoldAmount + c.ReturnedAmount));
        }

        private void TheResultShouldBe_(LanguageKey key) => Assert.AreEqual(key, LastResult);

        private void BuyersShouldSee_Listings(int count) =>
            Assert.AreEqual(count, Master.Service.GetBazaar(-1, null, null, null, null, null, null, null, null).Count);

        private void TheSellerShouldHave_Listings(int count) =>
            Assert.AreEqual(count, Master.Registry.CountBySellerId(Seller));

        private async Task TheListedInstanceShouldBeGone()
        {
            var id = InstanceId;
            Assert.IsNull(await Master.ItemInstanceDao.FirstOrDefaultAsync(s => s.Id == id));
        }

        private void TheBuyShouldBeAccepted() => Assert.IsTrue(LastBuy);

        private void TheBuyShouldBeRefused() => Assert.IsFalse(LastBuy);

        private async Task TheBuyIsCompensatedBeforeItArrives()
        {
            OperationId = Guid.NewGuid();
            Assert.IsTrue(await Master.Hub.CompensateBazaarAsync(OperationId));
        }

        private void TheListingShouldHold_(short amount) =>
            Assert.AreEqual(amount, Master.Registry.GetById(ListingId)?.ItemInstance?.Amount);

        private void TheClaimShouldPayFor_AndReturn_(short sold, short returned)
        {
            Assert.IsNotNull(LastClaim);
            Assert.AreEqual(sold, LastClaim.SoldAmount);
            Assert.AreEqual(returned, LastClaim.ReturnedAmount);
            Assert.AreEqual(sold * 50 - (long)(sold * 50 * 0.10), LastClaim.Gold);
        }

        private void TheListingShouldStillHoldThePending_(short pending)
        {
            var link = Master.Registry.GetById(ListingId);
            Assert.AreEqual(pending, link?.BazaarItem?.Amount);
            Assert.AreEqual((short)0, link?.ItemInstance?.Amount);
        }

        private void TheClaimShouldBeRefused() => Assert.IsNull(LastClaim);

        private void ThePriceShouldBe_(long price) =>
            Assert.AreEqual(price, Master.Registry.GetById(ListingId)?.BazaarItem?.Price);
    }
}
