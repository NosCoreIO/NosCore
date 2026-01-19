//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Database;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.WarehouseService
{
    [TestClass]
    public class WarehouseServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
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

            WarehouseItemDao = new Dao<Database.Entities.WarehouseItem, WarehouseItemDto, Guid>(Logger, ContextBuilder);
            WarehouseDao = new Dao<Database.Entities.Warehouse, WarehouseDto, Guid>(Logger, ContextBuilder);
            ItemInstanceDao = new Dao<Database.Entities.ItemInstance, IItemInstanceDto?, Guid>(Logger, ContextBuilder);

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
                .Then(FreeSlotShouldBeAvailable)
                .ExecuteAsync();
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
            var items = Service.GetWarehouseItems(OwnerId, WarehouseType.Warehouse);
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
            var items = Service.GetWarehouseItems(OwnerId, WarehouseType.Warehouse);
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

        private void FreeSlotShouldBeAvailable()
        {
            var freeSlot = Service.GetFreeSlot(OwnerId, WarehouseType.Warehouse);
            Assert.IsNotNull(freeSlot);
            Assert.AreEqual((short)0, freeSlot.Value);
        }
    }
}
