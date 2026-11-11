//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Database;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.WarehouseService
{
    [TestClass]
    public class WarehouseServiceTests
    {
        private IWarehouseService Service = null!;
        private IDao<WarehouseItemDto, Guid> WarehouseItemDao = null!;
        private IDao<WarehouseDto, Guid> WarehouseDao = null!;
        private IDao<IItemInstanceDto?, Guid> ItemInstanceDao = null!;
        private long OwnerId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();

            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                Guid.NewGuid().ToString());
            NosCoreContext ContextBuilder() => new NosCoreContext(optionsBuilder.Options);

            WarehouseItemDao = new Dao<Database.Entities.WarehouseItem, WarehouseItemDto, Guid>(NullLogger<Dao<Database.Entities.WarehouseItem, WarehouseItemDto, Guid>>.Instance, ContextBuilder);
            WarehouseDao = new Dao<Database.Entities.Warehouse, WarehouseDto, Guid>(NullLogger<Dao<Database.Entities.Warehouse, WarehouseDto, Guid>>.Instance, ContextBuilder);
            ItemInstanceDao = new Dao<Database.Entities.ItemInstance, IItemInstanceDto?, Guid>(NullLogger<Dao<Database.Entities.ItemInstance, IItemInstanceDto?, Guid>>.Instance, ContextBuilder);

            Service = new GameObject.Services.WarehouseService.WarehouseService(
                WarehouseItemDao,
                WarehouseDao,
                ItemInstanceDao);

            var session = await TestHelpers.Instance.GenerateSessionAsync();
            OwnerId = session.Character.CharacterId;
        }

        [TestMethod]
        public void GetMaxSlotsShouldReturnCorrectValue()
        {
            new Spec("Get max slots should return correct value")
                .Then(PersonalWarehouseShouldHave68Slots)
                .And(FamilyWarehouseShouldHave49Slots)
                .Execute();
        }

        [TestMethod]
        public async Task DepositingItemShouldSucceed()
        {
            await new Spec("Depositing item should succeed")
                .WhenAsync(DepositingItem)
                .ThenAsync(ItemShouldBeInWarehouse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DepositingToOccupiedSlotShouldFail()
        {
            await new Spec("Depositing to occupied slot should fail")
                .GivenAsync(SlotIsOccupied)
                .WhenAsync(DepositingToSameSlot)
                .Then(DepositShouldFail)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithdrawingItemShouldSucceed()
        {
            await new Spec("Withdrawing item should succeed")
                .GivenAsync(ItemIsInWarehouse)
                .WhenAsync(WithdrawingItem)
                .Then(WithdrawShouldSucceed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithdrawingNonExistentItemShouldFail()
        {
            await new Spec("Withdrawing non-existent item should fail")
                .WhenAsync(WithdrawingNonExistentItem)
                .Then(WithdrawShouldFail)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingItemShouldSucceed()
        {
            await new Spec("Moving item should succeed")
                .GivenAsync(ItemIsInWarehouse)
                .WhenAsync(MovingItemToNewSlot)
                .Then(MoveShouldSucceed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingItemToSameSlotShouldFail()
        {
            await new Spec("Moving item to same slot should fail")
                .GivenAsync(ItemIsInWarehouse)
                .WhenAsync(MovingItemToSameSlot)
                .Then(MoveShouldFail)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetFreeSlotShouldReturnAvailableSlot()
        {
            await new Spec("Get free slot should return available slot")
                .ThenAsync(FreeSlotShouldBeAvailable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemInstanceThatIsAlreadySavedIsStoredAsItIs()
        {
            var saved = await ItemInstanceDao.TryInsertOrUpdateAsync(
                new ItemInstanceDto { Id = Guid.NewGuid(), ItemVNum = 1012, Amount = 3 });

            Assert.IsTrue(await Service.DepositItemAsync(1, WarehouseType.FamilyWareHouse,
                new ItemInstanceDto { Id = saved!.Id, ItemVNum = 1012, Amount = 3 }, 4));

            var link = (await Service.GetItemsAsync(null, 1, WarehouseType.FamilyWareHouse, null)).Single();
            Assert.AreEqual(saved.Id, link.ItemInstance!.Id);
            Assert.AreEqual((await Service.GetWarehouseItemsAsync(1, WarehouseType.FamilyWareHouse)).Single().Id,
                link.WarehouseItemId);
        }

        [TestMethod]
        public async Task TakingPartOfAStackLeavesTheRest()
        {
            var id = await StoreStackAsync(10, 3);
            Assert.IsTrue(await Service.TakeItemAsync(id, 6));
            Assert.AreEqual<short?>(4, await AmountAtAsync(3));
        }

        [TestMethod]
        public async Task TakingAWholeStackFreesTheSlot()
        {
            var id = await StoreStackAsync(10, 3);
            Assert.IsTrue(await Service.TakeItemAsync(id, 10));
            Assert.IsNull(await Service.GetItemBySlotAsync(OwnerId, WarehouseType.FamilyWareHouse, 3));
        }

        [TestMethod]
        public async Task TakingMoreThanIsStoredChangesNothing()
        {
            var id = await StoreStackAsync(10, 3);
            Assert.IsFalse(await Service.TakeItemAsync(id, 11));
            Assert.AreEqual<short?>(10, await AmountAtAsync(3));
        }

        [TestMethod]
        public async Task ConcurrentTakesNeverHandOutMoreThanIsStored()
        {
            for (var run = 0; run < 25; run++)
            {
                var slot = (short)(run % 40);
                var id = await StoreStackAsync(10, slot);
                var results = await Task.WhenAll(
                    Task.Run(() => Service.TakeItemAsync(id, 6)),
                    Task.Run(() => Service.TakeItemAsync(id, 6)));
                Assert.AreEqual(1, results.Count(r => r), $"run {run}");
                Assert.AreEqual<short?>(4, await AmountAtAsync(slot), $"run {run}");
                Assert.IsTrue(await Service.TakeItemAsync(id, 4));
            }
        }

        [TestMethod]
        public async Task ConcurrentWithdrawalsOfOneStackSucceedOnce()
        {
            for (var run = 0; run < 25; run++)
            {
                var id = await StoreStackAsync(5, (short)(run % 40));
                var results = await Task.WhenAll(
                    Task.Run(() => Service.WithdrawItemAsync(id)),
                    Task.Run(() => Service.WithdrawItemAsync(id)));
                Assert.AreEqual(1, results.Count(r => r), $"run {run}");
            }
        }

        [TestMethod]
        public async Task ReturningAnAmountMergesIntoTheSameItem()
        {
            await StoreStackAsync(4, 3);
            Assert.IsTrue(await Service.ReturnItemAsync(OwnerId, WarehouseType.FamilyWareHouse, 3,
                new ItemInstanceDto { Id = Guid.NewGuid(), ItemVNum = 1012, Amount = 6 }));
            Assert.AreEqual<short?>(10, await AmountAtAsync(3));
        }

        [TestMethod]
        public async Task SplittingMovesPartOfAStackToAnEmptySlot()
        {
            await StoreStackAsync(10, 3);
            var split = await SavedInstanceAsync(4);
            Assert.IsTrue(await Service.SplitItemAsync(OwnerId, WarehouseType.FamilyWareHouse, 3, 7, split));
            Assert.AreEqual<short?>(6, await AmountAtAsync(3));
            Assert.AreEqual<short?>(4, await AmountAtAsync(7));
        }

        [TestMethod]
        public async Task SplittingOntoAnOccupiedSlotChangesNothing()
        {
            await StoreStackAsync(10, 3);
            await StoreStackAsync(1, 7);
            Assert.IsFalse(await Service.SplitItemAsync(OwnerId, WarehouseType.FamilyWareHouse, 3, 7, await SavedInstanceAsync(4)));
            Assert.AreEqual<short?>(10, await AmountAtAsync(3));
        }

        private async Task<ItemInstanceDto> SavedInstanceAsync(short amount)
        {
            var instance = new ItemInstanceDto { Id = Guid.NewGuid(), ItemVNum = 1012, Amount = amount };
            await ItemInstanceDao.TryInsertOrUpdateAsync(instance);
            return instance;
        }

        private async Task<Guid> StoreStackAsync(short amount, short slot)
        {
            Assert.IsTrue(await Service.DepositItemAsync(OwnerId, WarehouseType.FamilyWareHouse,
                new ItemInstanceDto { Id = Guid.NewGuid(), ItemVNum = 1012, Amount = amount }, slot));
            return (await Service.GetItemBySlotAsync(OwnerId, WarehouseType.FamilyWareHouse, slot))!.Id;
        }

        private async Task<short?> AmountAtAsync(short slot)
        {
            var item = await Service.GetItemBySlotAsync(OwnerId, WarehouseType.FamilyWareHouse, slot);
            if (item == null)
            {
                return null;
            }

            return (await ItemInstanceDao.FirstOrDefaultAsync(i => i.Id == item.ItemInstanceId))?.Amount;
        }

        private bool DepositResult;
        private bool WithdrawResult;
        private bool MoveResult;
        private Guid WarehouseItemId;

        private void PersonalWarehouseShouldHave68Slots()
        {
            Assert.AreEqual(68, Service.GetMaxSlots(WarehouseType.Warehouse));
        }

        private void FamilyWarehouseShouldHave49Slots()
        {
            Assert.AreEqual(49, Service.GetMaxSlots(WarehouseType.FamilyWareHouse));
        }

        private async Task DepositingItem()
        {
            var itemInstance = new ItemInstanceDto { Id = Guid.NewGuid(), ItemVNum = 1012, Amount = 1 };
            DepositResult = await Service.DepositItemAsync(OwnerId, WarehouseType.Warehouse, itemInstance, 0);
        }

        private async Task SlotIsOccupied()
        {
            var itemInstance = new ItemInstanceDto { Id = Guid.NewGuid(), ItemVNum = 1012, Amount = 1 };
            await Service.DepositItemAsync(OwnerId, WarehouseType.Warehouse, itemInstance, 0);
        }

        private async Task DepositingToSameSlot()
        {
            var itemInstance = new ItemInstanceDto { Id = Guid.NewGuid(), ItemVNum = 1013, Amount = 1 };
            DepositResult = await Service.DepositItemAsync(OwnerId, WarehouseType.Warehouse, itemInstance, 0);
        }

        private async Task ItemIsInWarehouse()
        {
            var itemInstance = new ItemInstanceDto { Id = Guid.NewGuid(), ItemVNum = 1012, Amount = 1 };
            await Service.DepositItemAsync(OwnerId, WarehouseType.Warehouse, itemInstance, 5);
            var items = await Service.GetWarehouseItemsAsync(OwnerId, WarehouseType.Warehouse);
            WarehouseItemId = items.First().Id;
        }

        private async Task WithdrawingItem()
        {
            WithdrawResult = await Service.WithdrawItemAsync(WarehouseItemId);
        }

        private async Task WithdrawingNonExistentItem()
        {
            WithdrawResult = await Service.WithdrawItemAsync(Guid.NewGuid());
        }

        private async Task MovingItemToNewSlot()
        {
            MoveResult = await Service.MoveItemAsync(OwnerId, WarehouseType.Warehouse, 5, 10);
        }

        private async Task MovingItemToSameSlot()
        {
            MoveResult = await Service.MoveItemAsync(OwnerId, WarehouseType.Warehouse, 5, 5);
        }

        private async Task ItemShouldBeInWarehouse()
        {
            Assert.IsTrue(DepositResult);
            var items = await Service.GetWarehouseItemsAsync(OwnerId, WarehouseType.Warehouse);
            Assert.AreEqual(1, items.Count);
        }

        private void DepositShouldFail()
        {
            Assert.IsFalse(DepositResult);
        }

        private void WithdrawShouldSucceed()
        {
            Assert.IsTrue(WithdrawResult);
        }

        private void WithdrawShouldFail()
        {
            Assert.IsFalse(WithdrawResult);
        }

        private void MoveShouldSucceed()
        {
            Assert.IsTrue(MoveResult);
        }

        private void MoveShouldFail()
        {
            Assert.IsFalse(MoveResult);
        }

        private async Task FreeSlotShouldBeAvailable()
        {
            var freeSlot = await Service.GetFreeSlotAsync(OwnerId, WarehouseType.Warehouse);
            Assert.IsNotNull(freeSlot);
            Assert.AreEqual((short)0, freeSlot.Value);
        }
    }
}
