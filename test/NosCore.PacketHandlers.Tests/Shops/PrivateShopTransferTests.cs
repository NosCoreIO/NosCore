//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using Moq;
using NosCore.Algorithm.ShellService;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.GameObject.Services.UpgradeService;
using EquipmentOption = NosCore.Database.Entities.EquipmentOption;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class PrivateShopTransferTests
    {
        private TestWorld World;
        private const short ItemVNum = 1;
        private const long UnitPrice = 100;
        private const short WeaponVNum = 2;

        private ClientSession _owner;
        private ClientSession _buyer;
        private ClientSession _secondBuyer;
        private ClientSession _visitor;
        private ItemGenerationService _items;
        private MShopPacketHandler _mShop;
        private readonly Dictionary<short, Guid> _ownerInstances = new();
        private Exception _buyFailure;
        private IDao<EquipmentOptionDto, Guid> _optionDao;
        private IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private ShellOptionService _options;
        private Guid _weaponId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _owner = await World.CreateSessionAsync();
            _buyer = await World.CreateSessionAsync();
            _secondBuyer = await World.CreateSessionAsync();
            _items = new ItemGenerationService(new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = ItemVNum, IsTradable = true },
                new Item { Type = NoscorePocketType.Equipment, VNum = WeaponVNum, ItemType = ItemType.Weapon, IsTradable = true }
            }, NullLoggerFactory.Instance, World.LogLanguageLocalizer);
            _mShop = World.Create<MShopPacketHandler>();

            _owner.Character.MapInstance = World.GetBaseMap(1);
            _owner.Character.PositionX = 7;
            _owner.Character.PositionY = 7;
            foreach (var (slot, amount) in new (short, short)[] { (0, 1), (1, 3) })
            {
                var item = _items.Create(ItemVNum, amount);
                item.Id = Guid.NewGuid();
                _owner.Character.InventoryService.AddItemToPocket(
                    InventoryItemInstance.Create(item, _owner.Character.CharacterId), NoscorePocketType.Etc, slot);
                _ownerInstances[slot] = item.Id;
            }

            _optionDao = new Dao<EquipmentOption, EquipmentOptionDto, Guid>(
                NullLogger<Dao<EquipmentOption, EquipmentOptionDto, Guid>>.Instance, World.DbContextBuilder);
            _itemInstanceDao = new Dao<NosCore.Database.Entities.ItemInstance, IItemInstanceDto?, Guid>(
                NullLogger<Dao<NosCore.Database.Entities.ItemInstance, IItemInstanceDto?, Guid>>.Instance,
                World.DbContextBuilder);
            _options = new ShellOptionService(new ShellService(), new Mock<IRandomNumberSource>().Object, _optionDao);

            _buyer.Character.Gold = 10_000;
            _secondBuyer.Character.Gold = 10_000;
        }

        [TestMethod]
        public async Task OpeningAShopWithTheSameInventorySlotTwiceIsRejected()
        {
            await new Spec("Opening a shop with the same inventory slot twice is rejected")
                .WhenAsync(TheOwnerOpensAShopListingSlot_Twice, (short)1)
                .Then(TheOwnerShouldHaveNoShop)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingAWholeStackGivesTheBuyerItsOwnInstance()
        {
            await new Spec("Buying a whole stack gives the buyer its own instance")
                .GivenAsync(TheOwnerOpensTheShop)
                .WhenAsync(TheBuyerBuys_FromShopSlot_, (short)3, (short)1)
                .Then(TheBuyerShouldHold_, 3)
                .And(TheOwnerShouldHold_, 1)
                .And(TheBuyerShouldHave_Gold, 9_700L)
                .And(TheOwnerShouldHave_Gold, 300L)
                .And(NoInstanceShouldBeSharedBetweenOwnerAndBuyer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingEquipmentKeepsItsOptions()
        {
            await new Spec("Buying equipment keeps its options")
                .GivenAsync(TheOwnerOpensAShopWithAWeaponWithAnOptionOfValue_, 7)
                .WhenAsync(TheBuyerBuysTheWeapon)
                .Then(TheBuyersWeaponShouldHaveAnOptionOfValue_, 7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingEquipmentWhoseOptionsCannotBeCopiedIsRefused()
        {
            await new Spec("Buying equipment whose options cannot be copied is refused")
                .GivenAsync(TheOwnerOpensAShopWithAWeaponWithAnOptionOfValue_, 7)
                .WhenAsync(TheBuyerBuysTheWeaponButItsOptionsCannotBeCopied)
                .Then(TheBuyerShouldHave_Gold, 10_000L)
                .And(TheOwnerShouldHave_Gold, 0L)
                .And(TheOwnerShouldStillHoldTheWeapon)
                .And(TheBuyerShouldHoldNoWeapon)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingPartOfAStackLeavesTheRestWithTheOwner()
        {
            await new Spec("Buying part of a stack leaves the rest with the owner")
                .GivenAsync(TheOwnerOpensTheShop)
                .WhenAsync(TheBuyerBuys_FromShopSlot_, (short)2, (short)1)
                .Then(TheBuyerShouldHold_, 2)
                .And(TheOwnerShouldHold_, 2)
                .And(TheOwnerSlot_ShouldHold_, (short)1, (short)1)
                .And(NoInstanceShouldBeSharedBetweenOwnerAndBuyer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingAnItemTheOwnerNoLongerHoldsIsRejected()
        {
            await new Spec("Buying an item the owner no longer holds is rejected")
                .GivenAsync(TheOwnerOpensTheShop)
                .And(TheOwnerNoLongerHoldsSlot_, (short)1)
                .WhenAsync(TheBuyerBuys_FromShopSlot_, (short)3, (short)1)
                .Then(TheBuyerShouldHold_, 0)
                .And(TheBuyerShouldHave_Gold, 10_000L)
                .And(TheOwnerShouldHave_Gold, 0L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingMoreThanTheOwnerStillHoldsIsRejected()
        {
            await new Spec("Buying more than the owner still holds is rejected")
                .GivenAsync(TheOwnerOpensTheShop)
                .And(TheOwnersSlot_NowHolds_, (short)1, (short)1)
                .WhenAsync(TheBuyerBuys_FromShopSlot_, (short)2, (short)1)
                .Then(TheBuyerShouldHold_, 0)
                .And(TheBuyerShouldHave_Gold, 10_000L)
                .And(TheOwnerSlot_ShouldHold_, (short)1, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFromAShopClosedSinceItWasOpenedIsRejected()
        {
            await new Spec("Buying from a shop closed since it was opened is rejected")
                .GivenAsync(TheOwnerOpensTheShop)
                .AndAsync(TheBuyerKeepsTheShopAndTheOwnerClosesIt)
                .WhenAsync(TheBuyerBuysFromTheKeptShop)
                .Then(TheBuyerShouldHold_, 0)
                .And(TheBuyerShouldHave_Gold, 10_000L)
                .And(TheOwnerShouldHold_, 4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClosingTheShopStopsAnsweringClicks()
        {
            await new Spec("Closing the shop stops answering clicks")
                .GivenAsync(TheOwnerOpensTheShop)
                .AndAsync(TheOwnerClosesTheShop)
                .AndAsync(TheOwnerOpensTheShop)
                .WhenAsync(AVisitorClicksTheOwner)
                .Then(TheVisitorShouldReceive_ItemLists, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingMapClosesTheShop()
        {
            await new Spec("Changing map closes the shop")
                .GivenAsync(TheOwnerOpensTheShop)
                .WhenAsync(TheOwnerChangesMap)
                .Then(TheOwnerShouldHaveNoShop)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TwoBuyersRacingForTheSameStackOnlyOneGetsIt()
        {
            await new Spec("Two buyers racing for the same stack only one gets it")
                .GivenAsync(TheOwnerOpensTheShop)
                .WhenAsync(TwoBuyersBuy_FromShopSlot_AtOnce, (short)3, (short)1)
                .Then(ExactlyOneBuyerShouldHold_, 3)
                .And(TheOwnerShouldHold_, 1)
                .And(TheOwnerShouldHave_Gold, 300L)
                .And(TheBuyersShouldHaveSpent_, 300L)
                .ExecuteAsync();
        }

        private MShopPacket ShopPacket(params (short Slot, short Amount)[] items) => new()
        {
            Type = CreateShopPacketType.Open,
            ItemList = items.Select(i => (MShopItemSubPacket?)new MShopItemSubPacket
            {
                Type = PocketType.Etc, Slot = i.Slot, Amount = i.Amount, Price = UnitPrice
            }).ToList(),
            Name = "shop"
        };

        private async Task TheOwnerOpensAShopWithAWeaponWithAnOptionOfValue_(int value)
        {
            var weapon = _items.Create(WeaponVNum, 1);
            weapon.Id = Guid.NewGuid();
            _weaponId = weapon.Id;
            _owner.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(weapon, _owner.Character.CharacterId), NoscorePocketType.Equipment, 0);
            await _optionDao.TryInsertOrUpdateAsync(new EquipmentOptionDto
            {
                Id = Guid.NewGuid(), WearableInstanceId = weapon.Id, Level = 1, Type = 1, Value = value
            });
            await _mShop.ExecuteAsync(new MShopPacket
            {
                Type = CreateShopPacketType.Open,
                ItemList = new List<MShopItemSubPacket?>
                {
                    new() { Type = PocketType.Equipment, Slot = 0, Amount = 1, Price = UnitPrice }
                },
                Name = "shop"
            }, _owner);
        }

        private Task TheBuyerBuysTheWeapon() =>
            _buyer.Character.BuyAsync(_owner.Character.Shop.NotNull(), 0, 1, World.WorldConfiguration, _items,
                _options, _itemInstanceDao);

        private Task TheBuyerBuysTheWeaponButItsOptionsCannotBeCopied()
        {
            var options = new Mock<IShellOptionService>();
            options.Setup(o => o.CopyAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);
            return _buyer.Character.BuyAsync(_owner.Character.Shop.NotNull(), 0, 1, World.WorldConfiguration,
                _items, options.Object, _itemInstanceDao);
        }

        private void TheOwnerShouldStillHoldTheWeapon() =>
            Assert.IsTrue(_owner.Character.InventoryService.ContainsKey(_weaponId));

        private void TheBuyerShouldHoldNoWeapon() =>
            Assert.IsFalse(_buyer.Character.InventoryService.Values.Any(i => i.ItemInstance.ItemVNum == WeaponVNum));

        private void TheBuyersWeaponShouldHaveAnOptionOfValue_(int value)
        {
            var weapon = _buyer.Character.InventoryService.Values.Single(i => i.ItemInstance.ItemVNum == WeaponVNum);
            Assert.AreNotEqual(_weaponId, weapon.ItemInstanceId);
            Assert.AreEqual(value, _options.Load(weapon.ItemInstanceId).Single().Value);
        }

        private Task TheOwnerOpensTheShop() =>
            _mShop.ExecuteAsync(ShopPacket((0, 1), (1, 3)), _owner);

        private Task TheOwnerOpensAShopListingSlot_Twice(short slot) =>
            _mShop.ExecuteAsync(ShopPacket((slot, 1), (slot, 2)), _owner);

        private Task TheOwnerClosesTheShop() =>
            _mShop.ExecuteAsync(new MShopPacket { Type = CreateShopPacketType.Close }, _owner);

        private async Task TheBuyerBuys_FromShopSlot_(short amount, short slot)
        {
            try
            {
                await _buyer.Character.BuyAsync(_owner.Character.Shop.NotNull(), slot, amount,
                    World.WorldConfiguration, _items);
            }
            catch (Exception e)
            {
                _buyFailure = e;
            }
        }

        private NosCore.GameObject.Services.ShopService.Shop? _keptShop;

        private async Task TheBuyerKeepsTheShopAndTheOwnerClosesIt()
        {
            _keptShop = _owner.Character.Shop;
            await TheOwnerClosesTheShop();
        }

        private async Task TheBuyerBuysFromTheKeptShop()
        {
            try
            {
                await _buyer.Character.BuyAsync(_keptShop.NotNull(), 1, 3, World.WorldConfiguration, _items);
            }
            catch (Exception e)
            {
                _buyFailure = e;
            }
        }

        private void TheOwnerNoLongerHoldsSlot_(short slot) =>
            _owner.Character.InventoryService.Remove(_ownerInstances[slot]);

        private void TheOwnersSlot_NowHolds_(short slot, short amount) =>
            _owner.Character.InventoryService[_ownerInstances[slot]].ItemInstance.Amount = amount;

        private Task AVisitorClicksTheOwner()
        {
            _visitor = _secondBuyer;
            _visitor.LastPackets.Clear();
            _owner.Character.Requests[typeof(NpcDialogRequestSubject)].OnNext(new RequestData(_visitor));
            return Task.CompletedTask;
        }

        private Task TheOwnerChangesMap() =>
            World.MapChangeService.ChangeMapInstanceAsync(_owner,
                World.GetBaseMap(0).MapInstanceId);

        private async Task TwoBuyersBuy_FromShopSlot_AtOnce(short amount, short slot)
        {
            var shop = _owner.Character.Shop.NotNull();
            using var barrier = new Barrier(2);
            Task Buy(ClientSession session) => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                try
                {
                    await session.Character.BuyAsync(shop, slot, amount, World.WorldConfiguration, _items);
                }
                catch (Exception e)
                {
                    _buyFailure = e;
                }
            });
            await Task.WhenAll(Buy(_buyer), Buy(_secondBuyer));
        }

        private static int Held(ClientSession session) => session.Character.InventoryService.CountItem(ItemVNum);

        private void TheOwnerShouldHaveNoShop() => Assert.IsNull(_owner.Character.Shop);

        private void TheBuyerShouldHold_(int amount)
        {
            Assert.IsNull(_buyFailure, _buyFailure?.ToString());
            Assert.AreEqual(amount, Held(_buyer));
        }

        private void TheOwnerShouldHold_(int amount) => Assert.AreEqual(amount, Held(_owner));

        private void TheOwnerSlot_ShouldHold_(short slot, short amount) =>
            Assert.AreEqual(amount, _owner.Character.InventoryService.LoadBySlotAndType(slot, NoscorePocketType.Etc)?.ItemInstance.Amount);

        private void TheBuyerShouldHave_Gold(long gold) => Assert.AreEqual(gold, _buyer.Character.Gold);

        private void TheOwnerShouldHave_Gold(long gold) => Assert.AreEqual(gold, _owner.Character.Gold);

        private void NoInstanceShouldBeSharedBetweenOwnerAndBuyer()
        {
            var ownerIds = _owner.Character.InventoryService.Values.Select(i => i.ItemInstanceId).ToHashSet();
            var ownerObjects = _owner.Character.InventoryService.Values.Select(i => i.ItemInstance).ToList();
            Assert.IsTrue(_buyer.Character.InventoryService.Values.All(i =>
                !ownerIds.Contains(i.ItemInstanceId) && ownerObjects.All(o => !ReferenceEquals(o, i.ItemInstance))));
            Assert.IsTrue(_buyer.Character.InventoryService.Values.All(i => !_ownerInstances.ContainsValue(i.ItemInstanceId)));
        }

        private void TheVisitorShouldReceive_ItemLists(int count) =>
            Assert.AreEqual(count, _visitor.LastPackets.OfType<NInvPacket>().Count());

        private void ExactlyOneBuyerShouldHold_(int amount)
        {
            Assert.IsNull(_buyFailure, _buyFailure?.ToString());
            var held = new[] { Held(_buyer), Held(_secondBuyer) }.OrderBy(h => h).ToArray();
            CollectionAssert.AreEqual(new[] { 0, amount }, held);
        }

        private void TheBuyersShouldHaveSpent_(long gold) =>
            Assert.AreEqual(gold, 20_000 - _buyer.Character.Gold - _secondBuyer.Character.Gold);
    }
}
