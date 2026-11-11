//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.SaveService;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Algorithm.ShellService;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using EquipmentOption = NosCore.Database.Entities.EquipmentOption;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD.Steps;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class BazaarTransferTests
    {
        private const short ItemVNum = 1012;

        private BazaarTestMaster _master;
        private InterceptingHub _hub;
        private Mock<ISaveService> _saveService;
        private IDao<EquipmentOptionDto, Guid> _optionDao;
        private ShellOptionService _options;
        private ClientSession _seller;
        private ClientSession _buyer;
        private ClientSession _third;
        private long _listingId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _seller = await TestHelpers.Instance.GenerateSessionAsync();
            _buyer = await TestHelpers.Instance.GenerateSessionAsync();
            _third = await TestHelpers.Instance.GenerateSessionAsync();
            foreach (var session in new[] { _seller, _buyer, _third })
            {
                session.WithWindowOpenedRemotely(NpcDialogWindow.Bazaar);
            }

            _master = new BazaarTestMaster();
            _hub = new InterceptingHub(_master.Hub);
            _saveService = new Mock<ISaveService>();
            _optionDao = new Dao<EquipmentOption, EquipmentOptionDto, Guid>(
                NullLogger<Dao<EquipmentOption, EquipmentOptionDto, Guid>>.Instance, TestHelpers.Instance.DbContextBuilder);
            _options = new ShellOptionService(new ShellService(), new Mock<IRandomNumberSource>().Object, _optionDao);
        }

        [TestMethod]
        public async Task BuyingPartOfAListingDeliversTheBoughtAmount()
        {
            await new Spec("Buying part of a listing delivers the bought amount")
                .GivenAsync(AListingOf_At_, (short)10, 50L)
                .And(TheBuyerHas_Gold, 1000L)
                .WhenAsync(TheBuyerBuys_, (short)3)
                .Then(TheBuyerShouldHold_Items, 3)
                .And(TheBuyerShouldHave_Gold, 850L)
                .And(TheListingShouldHold_, (short)7)
                .And(TheBuyerShouldHaveBeenSaved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingEquipmentKeepsItsStats()
        {
            await new Spec("Buying equipment keeps its stats")
                .GivenAsync(AWeaponListingWithMaximumDamage_, (short)123)
                .And(TheBuyerHas_Gold, 1000L)
                .And(TheListingReachesTheBuyerAsTheBaseItemType)
                .WhenAsync(TheBuyerBuys_, (short)1)
                .Then(TheBuyersWeaponShouldHaveMaximumDamage_, (short)123)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingEquipmentKeepsItsOptions()
        {
            await new Spec("Buying equipment keeps its options")
                .GivenAsync(AWeaponListingWithMaximumDamage_, (short)123)
                .AndAsync(TheListedWeaponHasAnOptionOfValue_, 7)
                .And(TheBuyerHas_Gold, 1000L)
                .WhenAsync(TheBuyerBuys_, (short)1)
                .Then(TheBuyersWeaponShouldHaveAnOptionOfValue_, 7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABuyWhoseOptionsCannotBeCopiedIsCompensated()
        {
            await new Spec("A buy whose options cannot be copied is compensated")
                .GivenAsync(AWeaponListingWithMaximumDamage_, (short)123)
                .And(TheBuyerHas_Gold, 1000L)
                .And(OptionsCannotBeCopied)
                .WhenAsync(TheBuyerBuys_, (short)1)
                .Then(TheBuyerShouldHave_Gold, 1000L)
                .And(TheBuyerShouldHoldNoWeapon)
                .And(TheListingShouldHold_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ListingEquipmentKeepsItsOptions()
        {
            await new Spec("Listing equipment keeps its options")
                .GivenAsync(TheSellerHoldsAWeaponWithAnOptionOfValue_, 7)
                .And(TheSellerHas_Gold, 1000L)
                .WhenAsync(TheSellerListsTheWeapon)
                .Then(TheListedWeaponShouldHaveAnOptionOfValue_, 7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CollectingUnsoldEquipmentKeepsItsOptions()
        {
            await new Spec("Collecting unsold equipment keeps its options")
                .GivenAsync(AWeaponListingWithMaximumDamage_, (short)123)
                .AndAsync(TheListedWeaponHasAnOptionOfValue_, 7)
                .WhenAsync(TheSellerCollects)
                .Then(TheSellersWeaponShouldHaveAnOptionOfValue_, 7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SellerCanCollectASoldOutListing()
        {
            await new Spec("Seller can collect a sold out listing")
                .GivenAsync(AListingOf_At_, (short)10, 50L)
                .And(TheBuyerHas_Gold, 1000L)
                .AndAsync(TheBuyerBuys_, (short)10)
                .WhenAsync(TheSellerCollects)
                .Then(TheSellerShouldHave_Gold, 450L)
                .And(TheListingShouldBeGone)
                .And(TheBuyerShouldHold_Items, 10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CollectingPaysTaxesOnLargeSales()
        {
            await new Spec("Collecting pays taxes on large sales")
                .GivenAsync(AListingOf_At_, (short)2, 500_000L)
                .And(TheBuyerHas_Gold, 500_000L)
                .AndAsync(TheBuyerBuys_, (short)1)
                .WhenAsync(TheSellerCollects)
                .Then(TheSellerShouldHave_Gold, 450_000L)
                .And(TheSellerShouldHold_Items, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARetriedBuyIsChargedAndDeliveredOnce()
        {
            await new Spec("A retried buy is charged and delivered once")
                .GivenAsync(AListingOf_At_, (short)10, 50L)
                .And(TheBuyerHas_Gold, 1000L)
                .And(TheFirstBuyAnswerIsLostAfterTheMasterAppliedIt)
                .WhenAsync(TheBuyerBuys_, (short)3)
                .Then(TheBuyerShouldHold_Items, 3)
                .And(TheBuyerShouldHave_Gold, 850L)
                .And(TheListingShouldHold_, (short)7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABuyTheBuyerCannotReceiveIsCompensated()
        {
            await new Spec("A buy the buyer cannot receive is compensated")
                .GivenAsync(AListingOf_At_, (short)10, 50L)
                .And(TheBuyerHas_Gold, 1000L)
                .And(TheBuyersInventoryFillsWhileTheMasterAnswers)
                .WhenAsync(TheBuyerBuys_, (short)3)
                .Then(TheBuyerShouldHave_Gold, 1000L)
                .And(TheListingShouldHold_, (short)10)
                .AndAsync(TheSellerCollects)
                .And(TheSellerShouldHave_Gold, 0L)
                .And(TheSellerShouldHold_Items, 10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CollectingPaysForASaleThatLandsAfterTheListingWasRead()
        {
            await new Spec("Collecting pays for a sale that lands after the listing was read")
                .GivenAsync(AListingOf_At_, (short)10, 50L)
                .And(TheBuyerHas_Gold, 1000L)
                .And(ASaleLandsBeforeTheSellersClaim)
                .WhenAsync(TheSellerCollects)
                .Then(TheSellerShouldHave_Gold, 45L)
                .And(TheSellerShouldHold_Items, 9)
                .And(TheListingShouldBeGone)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CollectingIsDecidedBySellerIdNotName()
        {
            await new Spec("Collecting is decided by seller id not name")
                .GivenAsync(AListingOf_At_, (short)10, 50L)
                .And(TheListingShowsTheBuyersName)
                .WhenAsync(TheBuyerCollects)
                .Then(TheBuyerShouldHave_Gold, 0L)
                .And(TheBuyerShouldHold_Items, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingOwnListingIsDecidedBySellerIdNotName()
        {
            await new Spec("Buying own listing is decided by seller id not name")
                .GivenAsync(AListingOf_At_, (short)10, 50L)
                .And(TheListingShowsAnotherSellerName)
                .And(TheSellerHas_Gold, 1000L)
                .WhenAsync(TheSellerBuysOwnListing)
                .Then(TheSellerShouldHave_Gold, 1000L)
                .And(TheListingShouldHold_, (short)10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingPriceIsDecidedBySellerIdNotName()
        {
            await new Spec("Changing price is decided by seller id not name")
                .GivenAsync(AListingOf_At_, (short)10, 50L)
                .And(TheListingShowsTheBuyersName)
                .WhenAsync(TheBuyerChangesThePriceTo_, 5L)
                .Then(TheListingPriceShouldBe_, 50L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingAtAPriceChangedSinceItWasReadIsRejected()
        {
            await new Spec("Buying at a price changed since it was read is rejected")
                .GivenAsync(AListingOf_At_, (short)10, 50L)
                .And(TheBuyerHas_Gold, 1000L)
                .AndAsync(TheBuyerReadsTheListingBeforeThePriceBecomes_, 90L)
                .WhenAsync(TheBuyerBuysOneAtTheReadPrice)
                .Then(TheBuyerShouldHave_Gold, 1000L)
                .And(TheBuyerShouldHold_Items, 0)
                .And(TheListingShouldHold_, (short)10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConcurrentBuysAndACollectionPayEachSaleOnce()
        {
            await new Spec("Concurrent buys and a collection pay each sale once")
                .GivenAsync(AListingOf_At_, (short)4, 50L)
                .And(TheBuyerHas_Gold, 1000L)
                .And(TheThirdCharacterHas_Gold, 1000L)
                .WhenAsync(TwoBuyersAndTheSellerActAtOnce)
                .AndAsync(TheSellerCollectsWhatIsLeft)
                .Then(TheGoldAndItemsShouldBalance)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ListingPartOfAStackListsTheListedAmount()
        {
            await new Spec("Listing part of a stack lists the listed amount")
                .Given(TheSellerHolds_Items, (short)99)
                .And(TheSellerHas_Gold, 1000L)
                .WhenAsync(TheSellerLists_, (short)50)
                .Then(TheSellerShouldHold_Items, 49)
                .And(TheSellersListingShouldHold_, (short)50)
                .And(TheListedInstanceShouldNotBeTheSellersStack)
                .And(BuyersShouldSee_Listings, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AListingTheSellerCannotPayForIsCompensated()
        {
            await new Spec("A listing the seller cannot pay for is compensated")
                .Given(TheSellerHolds_Items, (short)99)
                .And(TheSellerHas_Gold, 1000L)
                .And(TheSellersGoldIsSpentWhileTheMasterAnswers)
                .WhenAsync(TheSellerLists_, (short)50)
                .Then(TheSellerShouldHold_Items, 99)
                .And(BuyersShouldSee_Listings, 0)
                .And(TheSellerShouldHave_Listings, 0)
                .ExecuteAsync();
        }

        private IShellOptionService _failingOptions;

        private void OptionsCannotBeCopied()
        {
            var failing = new Mock<IShellOptionService>();
            failing.Setup(s => s.CopyAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);
            _failingOptions = failing.Object;
        }

        private void TheBuyerShouldHoldNoWeapon() =>
            Assert.IsFalse(_buyer.Character.InventoryService.Values.Any(i => i.ItemInstance.ItemVNum == 1));

        private CBuyPacketHandler NewCBuy() =>
            new(_hub, TestHelpers.Instance.GenerateItemProvider(), NullLogger<CBuyPacketHandler>.Instance,
                TestHelpers.Instance.LogLanguageLocalizer, _saveService.Object, _master.ItemInstanceDao,
                TestHelpers.Instance.NpcDialogService, _failingOptions ?? _options);

        private CScalcPacketHandler NewCScalc() =>
            new(TestHelpers.Instance.WorldConfiguration, _hub, TestHelpers.Instance.GenerateItemProvider(),
                NullLogger<CScalcPacketHandler>.Instance, TestHelpers.Instance.LogLanguageLocalizer, _saveService.Object,
                _master.ItemInstanceDao, TestHelpers.Instance.NpcDialogService, _options);

        private async Task AListingOf_At_(short amount, long price) =>
            _listingId = await _master.ListAsync(_seller.Character.CharacterId, _seller.Character.Name, ItemVNum,
                amount, price);

        private async Task AWeaponListingWithMaximumDamage_(short damage) =>
            _listingId = await _master.ListAsync(new WearableInstanceDto
            {
                Id = Guid.NewGuid(), ItemVNum = 1, Amount = 1, Upgrade = 5, DamageMaximum = damage
            }, _seller.Character.CharacterId, _seller.Character.Name, 50);

        private void TheListingReachesTheBuyerAsTheBaseItemType()
        {
            var link = _master.Registry.GetById(_listingId).NotNull();
            var copy = new BazaarLink
            {
                SellerName = link.SellerName,
                BazaarItem = link.BazaarItem,
                ItemInstance = new ItemInstanceDto
                {
                    Id = link.ItemInstance.Id, ItemVNum = link.ItemInstance.ItemVNum, Amount = link.ItemInstance.Amount,
                    Upgrade = link.ItemInstance.Upgrade
                }
            };
            _hub.Get = () => new List<BazaarLink> { copy };
        }

        private async Task TheListedWeaponHasAnOptionOfValue_(int value)
        {
            var weapon = _master.Registry.GetById(_listingId).NotNull().ItemInstance.Id;
            await _optionDao.TryInsertOrUpdateAsync(new EquipmentOptionDto
            {
                Id = Guid.NewGuid(), WearableInstanceId = weapon, Level = 1, Type = 1, Value = value
            });
        }

        private async Task TheSellerHoldsAWeaponWithAnOptionOfValue_(int value)
        {
            var weapon = TestHelpers.Instance.GenerateItemProvider().Create(1, 1);
            weapon.Id = Guid.NewGuid();
            weapon.Item.IsSoldable = true;
            _seller.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(weapon, _seller.Character.CharacterId), NoscorePocketType.Equipment, 0);
            await _optionDao.TryInsertOrUpdateAsync(new EquipmentOptionDto
            {
                Id = Guid.NewGuid(), WearableInstanceId = weapon.Id, Level = 1, Type = 1, Value = value
            });
        }

        private Task TheSellerListsTheWeapon() =>
            new CRegPacketHandler(TestHelpers.Instance.WorldConfiguration, _hub, _master.ItemInstanceDao,
                    _saveService.Object, NullLogger<CRegPacketHandler>.Instance, TestHelpers.Instance.LogLanguageLocalizer,
                    TestHelpers.Instance.NpcDialogService, _options)
                .ExecuteAsync(new CRegPacket
                {
                    Type = 0, Inventory = (byte)PocketType.Equipment, Slot = 0, Amount = 1, Price = 10,
                    Durability = BazaarListingDuration.OneDay, IsPackage = false, Taxe = 0
                }, _seller);

        private void TheListedWeaponShouldHaveAnOptionOfValue_(int value)
        {
            var listed = _master.Registry.GetBySellerId(_seller.Character.CharacterId).Single().ItemInstance.Id;
            Assert.AreEqual(value, _options.Load(listed).Single().Value);
        }

        private void TheBuyersWeaponShouldHaveAnOptionOfValue_(int value) => WeaponShouldHaveAnOptionOfValue(_buyer, value);

        private void TheSellersWeaponShouldHaveAnOptionOfValue_(int value) => WeaponShouldHaveAnOptionOfValue(_seller, value);

        private void WeaponShouldHaveAnOptionOfValue(ClientSession session, int value)
        {
            var weapon = session.Character.InventoryService.Values.Single(i => i.ItemInstance.ItemVNum == 1).ItemInstanceId;
            Assert.AreEqual(value, _options.Load(weapon).Single().Value);
        }

        private void TheBuyersWeaponShouldHaveMaximumDamage_(short damage)
        {
            var weapon = _buyer.Character.InventoryService.Values.Single(i => i.ItemInstance.ItemVNum == 1).ItemInstance;
            Assert.IsInstanceOfType<NosCore.GameObject.Services.ItemGenerationService.Item.WearableInstance>(weapon);
            Assert.AreEqual(damage, ((NosCore.GameObject.Services.ItemGenerationService.Item.WearableInstance)weapon).DamageMaximum);
        }

        private void TheListingShowsAnotherSellerName() => _master.Registry.GetById(_listingId).NotNull().SellerName = "someone else";

        private void TheListingShowsTheBuyersName() => _master.Registry.GetById(_listingId).NotNull().SellerName = _buyer.Character.Name;

        private void TheBuyerHas_Gold(long gold) => _buyer.Character.Gold = gold;

        private void TheSellerHas_Gold(long gold) => _seller.Character.Gold = gold;

        private void TheThirdCharacterHas_Gold(long gold) => _third.Character.Gold = gold;

        private void TheSellerHolds_Items(short amount)
        {
            var item = TestHelpers.Instance.GenerateItemProvider().Create(ItemVNum, amount);
            item.Id = Guid.NewGuid();
            item.Item.IsSoldable = true;
            _seller.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, _seller.Character.CharacterId), NoscorePocketType.Main, 0);
        }

        private Task TheBuyerBuys_(short amount) =>
            BuyAsync(_buyer, amount, _master.Registry.GetById(_listingId).NotNull().BazaarItem.Price);

        private Task BuyAsync(ClientSession session, short amount, long price) =>
            NewCBuy().ExecuteAsync(new CBuyPacket
            {
                BazaarId = _listingId, Amount = amount, Price = price, VNum = ItemVNum
            }, session);

        private Task TheBuyerBuysOneAtTheReadPrice() => BuyAsync(_buyer, 1, 50);

        private Task TheSellerBuysOwnListing() => BuyAsync(_seller, 1, 50);

        private Task TheSellerCollects() =>
            NewCScalc().ExecuteAsync(new CScalcPacket { BazaarId = _listingId, VNum = ItemVNum }, _seller);

        private Task TheBuyerCollects() =>
            NewCScalc().ExecuteAsync(new CScalcPacket { BazaarId = _listingId, VNum = ItemVNum }, _buyer);

        private Task TheBuyerChangesThePriceTo_(long price) =>
            new CModPacketHandler(_hub, NullLogger<CModPacketHandler>.Instance, TestHelpers.Instance.LogLanguageLocalizer,
                    TestHelpers.Instance.NpcDialogService)
                .ExecuteAsync(new CModPacket { BazaarId = _listingId, Amount = 10, NewPrice = price, VNum = ItemVNum },
                    _buyer);

        private Task TheSellerLists_(short amount) =>
            new CRegPacketHandler(TestHelpers.Instance.WorldConfiguration, _hub, _master.ItemInstanceDao,
                    _saveService.Object, NullLogger<CRegPacketHandler>.Instance, TestHelpers.Instance.LogLanguageLocalizer,
                    TestHelpers.Instance.NpcDialogService, _options)
                .ExecuteAsync(new CRegPacket
                {
                    Type = 0, Inventory = (byte)PocketType.Main, Slot = 0, Amount = amount, Price = 10,
                    Durability = BazaarListingDuration.OneDay, IsPackage = false, Taxe = 0
                }, _seller);

        private void TheFirstBuyAnswerIsLostAfterTheMasterAppliedIt()
        {
            var lost = false;
            _hub.Buy = async (operation, id, count, price, buyer) =>
            {
                var result = await _master.Hub.BuyBazaarAsync(operation, id, count, price, buyer);
                if (!lost)
                {
                    lost = true;
                    throw new TimeoutException();
                }

                return result;
            };
        }

        private void TheBuyersInventoryFillsWhileTheMasterAnswers()
        {
            _hub.Buy = async (operation, id, count, price, buyer) =>
            {
                var result = await _master.Hub.BuyBazaarAsync(operation, id, count, price, buyer);
                var provider = TestHelpers.Instance.GenerateItemProvider();
                for (short slot = 0; slot < 2; slot++)
                {
                    var filler = provider.Create(1013, 1);
                    filler.Id = Guid.NewGuid();
                    _buyer.Character.InventoryService.AddItemToPocket(
                        InventoryItemInstance.Create(filler, _buyer.Character.CharacterId), NoscorePocketType.Main, slot);
                }

                return result;
            };
        }

        private void TheSellersGoldIsSpentWhileTheMasterAnswers()
        {
            _hub.Add = async request =>
            {
                var result = await _master.Hub.AddBazaarAsync(request);
                _seller.Character.Gold = 0;
                return result;
            };
        }

        private void ASaleLandsBeforeTheSellersClaim()
        {
            _hub.Claim = async (operation, id, seller) =>
            {
                var buy = Guid.NewGuid();
                await _master.Hub.BuyBazaarAsync(buy, id, 1, 50, _buyer.Character.CharacterId);
                await _master.Hub.SettleBazaarAsync(buy);
                return await _master.Hub.ClaimBazaarAsync(operation, id, seller);
            };
        }

        private async Task TheBuyerReadsTheListingBeforeThePriceBecomes_(long price)
        {
            var stale = (await _master.Hub.GetBazaar(_listingId, null, null, null, null, null, null, null, null)).Single();
            var copy = new BazaarLink
            {
                SellerName = stale.SellerName,
                BazaarItem = new BazaarItemDto
                {
                    BazaarItemId = _listingId, Amount = stale.BazaarItem.Amount, Price = stale.BazaarItem.Price,
                    SellerId = stale.BazaarItem.SellerId
                },
                ItemInstance = new ItemInstanceDto
                {
                    Id = stale.ItemInstance.Id, ItemVNum = ItemVNum, Amount = stale.ItemInstance.Amount
                }
            };
            await _master.Hub.ModifyBazaarAsync(Guid.NewGuid(), _listingId, _seller.Character.CharacterId, price);
            _hub.Get = () => new List<BazaarLink> { copy };
        }

        private async Task TwoBuyersAndTheSellerActAtOnce()
        {
            using var barrier = new Barrier(3);
            Task Run(Func<Task> action) => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                await action();
            });
            await Task.WhenAll(
                Run(() => BuyAsync(_buyer, 1, 50)),
                Run(() => BuyAsync(_third, 2, 50)),
                Run(TheSellerCollects));
        }

        private async Task TheSellerCollectsWhatIsLeft()
        {
            for (var i = 0; (i < 5) && (_master.Registry.GetById(_listingId) != null); i++)
            {
                await TheSellerCollects();
            }
        }

        private void TheGoldAndItemsShouldBalance()
        {
            Assert.IsNull(_master.Registry.GetById(_listingId));
            var bought = _buyer.Character.InventoryService.CountItem(ItemVNum)
                + _third.Character.InventoryService.CountItem(ItemVNum);
            var spent = 2000 - _buyer.Character.Gold - _third.Character.Gold;
            Assert.AreEqual(bought * 50L, spent);
            Assert.AreEqual(bought * 45L, _seller.Character.Gold);
            Assert.AreEqual(4, bought + _seller.Character.InventoryService.CountItem(ItemVNum));
        }

        private void TheBuyerShouldHold_Items(int amount) =>
            Assert.AreEqual(amount, _buyer.Character.InventoryService.CountItem(ItemVNum));

        private void TheSellerShouldHold_Items(int amount) =>
            Assert.AreEqual(amount, _seller.Character.InventoryService.CountItem(ItemVNum));

        private void TheBuyerShouldHave_Gold(long gold) => Assert.AreEqual(gold, _buyer.Character.Gold);

        private void TheSellerShouldHave_Gold(long gold) => Assert.AreEqual(gold, _seller.Character.Gold);

        private void TheListingShouldHold_(short amount) =>
            Assert.AreEqual(amount, _master.Registry.GetById(_listingId)?.ItemInstance?.Amount);

        private void TheListingShouldBeGone() => Assert.IsNull(_master.Registry.GetById(_listingId));

        private void TheListingPriceShouldBe_(long price) =>
            Assert.AreEqual(price, _master.Registry.GetById(_listingId)?.BazaarItem?.Price);

        private void TheBuyerShouldHaveBeenSaved() =>
            _saveService.Verify(s => s.SaveAsync(_buyer), Times.Once);

        private void TheSellersListingShouldHold_(short amount) =>
            Assert.AreEqual(amount, _master.Registry.GetBySellerId(_seller.Character.CharacterId).Single().ItemInstance?.Amount);

        private void TheListedInstanceShouldNotBeTheSellersStack()
        {
            var listed = _master.Registry.GetBySellerId(_seller.Character.CharacterId).Single().ItemInstance.Id;
            Assert.IsTrue(_seller.Character.InventoryService.Values.All(i => i.ItemInstanceId != listed));
        }

        private void BuyersShouldSee_Listings(int count) =>
            Assert.AreEqual(count, _master.Service.GetBazaar(-1, null, null, null, null, null, null, null, null).Count);

        private void TheSellerShouldHave_Listings(int count) =>
            Assert.AreEqual(count, _master.Registry.CountBySellerId(_seller.Character.CharacterId));

        // Forwards to the master, except where a test replaces one call to simulate a lost
        // answer, a stale read or something changing while the master answers.
        private sealed class InterceptingHub(IBazaarHub inner) : IBazaarHub
        {
            public Func<List<BazaarLink>>? Get { get; set; }
            public Func<BazaarRequest, Task<LanguageKey>>? Add { get; set; }
            public Func<Guid, long, short, long, long, Task<bool>>? Buy { get; set; }
            public Func<Guid, long, long, Task<BazaarClaim?>>? Claim { get; set; }

            public Task<List<BazaarLink>> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
                byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter) =>
                Get != null
                    ? Task.FromResult(Get())
                    : inner.GetBazaar(id, index, pageSize, typeFilter, subTypeFilter, levelFilter, rareFilter,
                        upgradeFilter, sellerFilter);

            public Task<List<BazaarLink>> SearchBazaarAsync(BazaarSearch search) => inner.SearchBazaarAsync(search);

            public Task<LanguageKey> AddBazaarAsync(BazaarRequest bazaarRequest) =>
                Add?.Invoke(bazaarRequest) ?? inner.AddBazaarAsync(bazaarRequest);

            public Task<bool> BuyBazaarAsync(Guid operationId, long id, short count, long price, long buyerId) =>
                Buy?.Invoke(operationId, id, count, price, buyerId)
                ?? inner.BuyBazaarAsync(operationId, id, count, price, buyerId);

            public Task<BazaarLink?> ModifyBazaarAsync(Guid operationId, long id, long sellerId, long price) =>
                inner.ModifyBazaarAsync(operationId, id, sellerId, price);

            public Task<BazaarClaim?> ClaimBazaarAsync(Guid operationId, long id, long sellerId) =>
                Claim?.Invoke(operationId, id, sellerId) ?? inner.ClaimBazaarAsync(operationId, id, sellerId);

            public Task<bool> SettleBazaarAsync(Guid operationId) => inner.SettleBazaarAsync(operationId);

            public Task<bool> CompensateBazaarAsync(Guid operationId) => inner.CompensateBazaarAsync(operationId);
        }
    }
}
