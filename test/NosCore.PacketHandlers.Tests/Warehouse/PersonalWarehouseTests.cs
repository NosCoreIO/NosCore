//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.RarityPointService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.PacketHandlers.Warehouse;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Warehouse;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Warehouse
{
    [TestClass]
    public class PersonalWarehouseTests
    {
        private readonly Mock<NosCore.GameObject.Services.SaveService.ISaveService> _saves = new();
        private TestWorld World;
        private ClientSession _session;
        private IItemGenerationService _items;
        private readonly Dictionary<Guid, IItemInstanceDto> _instances = new();
        private readonly List<(Guid Id, WarehouseType Type, short Slot, Guid ItemInstanceId)> _stored = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _items = new ItemGenerationService(new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Main, VNum = 1012 },
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon },
                new Item { Type = NoscorePocketType.Miniland, VNum = 3001, ItemType = ItemType.Minigame, MinilandObjectPoint = 21, IsWarehouse = true }
            }, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());
        }

        private IPersonalWarehouseService Service()
        {
            var hub = new Mock<IWarehouseHub>();
            hub.Setup(s => s.GetWarehouseItems(null, It.IsAny<long?>(), It.IsAny<WarehouseType>(), null))
                .ReturnsAsync((Guid? _, long? _, WarehouseType type, byte? _) => _stored.Where(s => s.Type == type)
                    .Select(s => new WarehouseLink
                    {
                        WarehouseItemId = s.Id,
                        Slot = s.Slot,
                        ItemInstance = new ItemInstanceDto { Id = s.ItemInstanceId }
                    }).ToList());
            hub.Setup(s => s.AddWarehouseItemAsync(It.IsAny<WareHouseDepositRequest>()))
                .ReturnsAsync((WareHouseDepositRequest r) =>
                {
                    _stored.Add((Guid.NewGuid(), r.WarehouseType, r.Slot, r.ItemInstance.NotNull().Id));
                    return true;
                });
            hub.Setup(s => s.TakeWarehouseItemAsync(It.IsAny<Guid>(), It.IsAny<short>()))
                .ReturnsAsync((Guid id, short amount) =>
                {
                    var index = _stored.FindIndex(s => s.Id == id);
                    var instance = _instances[_stored[index].ItemInstanceId];
                    if (amount == instance.Amount)
                    {
                        _stored.RemoveAt(index);
                    }
                    else
                    {
                        instance.Amount -= amount;
                    }

                    return true;
                });
            hub.Setup(s => s.MoveWarehouseItemAsync(It.IsAny<long>(), It.IsAny<WarehouseType>(), It.IsAny<short>(), It.IsAny<short>()))
                .ReturnsAsync((long _, WarehouseType type, short from, short to) =>
                {
                    var source = _stored.Single(s => s.Type == type && s.Slot == from);
                    var destination = _stored.FindIndex(s => s.Type == type && s.Slot == to);
                    if (destination >= 0)
                    {
                        _stored[destination] = _stored[destination] with { Slot = from };
                    }

                    _stored[_stored.IndexOf(source)] = source with { Slot = to };
                    return true;
                });

            var instances = new Mock<IDao<IItemInstanceDto?, Guid>>();
            instances.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>()))
                .ReturnsAsync((IItemInstanceDto? i) => _instances[i.NotNull().Id] = i.NotNull());
            instances.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync((Expression<Func<IItemInstanceDto?, bool>> p) => _instances.Values.FirstOrDefault(p.Compile()));

            return new PersonalWarehouseService(new WarehouseTransferService(hub.Object, instances.Object,
                new Mock<IDao<InventoryItemInstanceDto, Guid>>().Object, _items, _saves.Object));
        }

        private void OwnsAMinilandWarehouse()
        {
            var warehouse = InventoryItemInstance.Create(_items.Create(3001, 1), _session.Character.CharacterId);
            warehouse.Type = NoscorePocketType.Miniland;
            _session.Character.InventoryService[warehouse.Id] = warehouse;
        }

        private void HasThePartnerBackpack() =>
            _session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = _session.Character.CharacterId,
                StaticBonusType = StaticBonusType.PetBackPack
            });

        private void Holds(short vnum, short amount) =>
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(vnum, amount), _session.Character.CharacterId));

        private void Store(WarehouseType type, short vnum, short amount, short slot)
        {
            var item = _items.Create(vnum, amount);
            _instances[item.Id] = item;
            _stored.Add((Guid.NewGuid(), type, slot, item.Id));
        }

        private IItemInstanceDto At(WarehouseType type, short slot) =>
            _instances[_stored.Single(s => s.Type == type && s.Slot == slot).ItemInstanceId];

        private Task DepositAsync(short amount, short newSlot, bool partnerBackpack = false) =>
            new DepositPacketHandler(Service()).ExecuteAsync(new DepositPacket
            {
                Inventory = PocketType.Main, Slot = 0, Amount = amount, NewSlot = (byte)newSlot, PartnerBackpack = partnerBackpack
            }, _session);

        [TestMethod]
        public async Task TheMinilandWarehouseOpensWithTheSizeOfTheWarehouseOwned()
        {
            OwnsAMinilandWarehouse();
            Store(WarehouseType.Warehouse, 1012, 5, 3);

            await Service().OpenAsync(_session, WarehouseType.Warehouse);

            var stash = _session.LastPackets.OfType<StashAllPacket>().Single();
            Assert.AreEqual(21, stash.WarehouseSize);
            Assert.AreEqual(3, stash.Items.NotNull().Single().Slot);
        }

        [TestMethod]
        public async Task DepositingPartOfAStackKeepsTheRestInTheBag()
        {
            OwnsAMinilandWarehouse();
            Holds(1012, 10);

            await DepositAsync(4, 2);

            Assert.AreEqual(4, At(WarehouseType.Warehouse, 2).Amount);
            Assert.AreEqual(6, _session.Character.InventoryService.CountItem(1012));
            Assert.AreEqual(2, _session.LastPackets.OfType<StashClientPacket>().Single().Items.NotNull().Single().Slot);
            _saves.Verify(s => s.SaveAsync(_session), Times.Once, "the bag is stored with the deposit");
        }

        [TestMethod]
        public async Task WithoutAMinilandWarehouseNothingCanBeStored()
        {
            Holds(1012, 10);

            await DepositAsync(10, 0);

            Assert.AreEqual(0, _stored.Count);
            Assert.AreEqual(10, _session.Character.InventoryService.CountItem(1012));
        }

        [TestMethod]
        public async Task DepositingBeyondTheWarehouseSizeDoesNothing()
        {
            OwnsAMinilandWarehouse();
            Holds(1012, 10);

            await DepositAsync(10, 21);

            Assert.AreEqual(0, _stored.Count);
        }

        [TestMethod]
        public async Task ABoundItemCanBeKeptInTheOwnWarehouse()
        {
            OwnsAMinilandWarehouse();
            var weapon = _items.Create(1, 1);
            weapon.BoundCharacterId = _session.Character.CharacterId;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(weapon, _session.Character.CharacterId));

            await new DepositPacketHandler(Service()).ExecuteAsync(
                new DepositPacket { Inventory = PocketType.Equipment, Slot = 0, Amount = 1, NewSlot = 0 }, _session);

            Assert.AreEqual(1, At(WarehouseType.Warehouse, 0).ItemVNum);
        }

        [TestMethod]
        public async Task WithdrawingPartOfAStackLeavesTheRest()
        {
            OwnsAMinilandWarehouse();
            Store(WarehouseType.Warehouse, 1012, 5, 3);

            await new WithdrawPacketHandler(Service()).ExecuteAsync(new WithdrawPacket { Slot = 3, Amount = 2 }, _session);

            Assert.AreEqual(3, At(WarehouseType.Warehouse, 3).Amount);
            Assert.AreEqual(2, _session.Character.InventoryService.CountItem(1012));
            _saves.Verify(s => s.SaveAsync(_session), Times.Once, "the bag is stored with the withdrawal");
        }

        [TestMethod]
        public async Task MovingOntoAnotherItemSwapsThem()
        {
            OwnsAMinilandWarehouse();
            Store(WarehouseType.Warehouse, 1012, 5, 3);
            Store(WarehouseType.Warehouse, 1, 1, 4);

            await new ReposPacketHandler(Service()).ExecuteAsync(new ReposPacket { OldSlot = 3, Amount = 5, NewSlot = 4 }, _session);

            Assert.AreEqual(1012, At(WarehouseType.Warehouse, 4).ItemVNum);
            Assert.AreEqual(1, At(WarehouseType.Warehouse, 3).ItemVNum);
        }

        [TestMethod]
        public async Task ThePartnerBackpackHoldsFiftyItemsOnceBought()
        {
            HasThePartnerBackpack();
            Store(WarehouseType.PetWarehouse, 1012, 5, 49);

            await Service().OpenAsync(_session, WarehouseType.PetWarehouse);

            var stash = _session.LastPackets.OfType<PStashAllPacket>().Single();
            Assert.AreEqual(50, stash.WarehouseSize);
            Assert.AreEqual(49, stash.Items.NotNull().Single().Slot);
        }

        [TestMethod]
        public async Task DepositingIntoThePartnerBackpackKeepsItApartFromTheWarehouse()
        {
            OwnsAMinilandWarehouse();
            HasThePartnerBackpack();
            Holds(1012, 10);

            await DepositAsync(10, 30, partnerBackpack: true);

            Assert.AreEqual(10, At(WarehouseType.PetWarehouse, 30).Amount);
            Assert.IsFalse(_stored.Any(s => s.Type == WarehouseType.Warehouse));
            Assert.AreEqual(30, _session.LastPackets.OfType<PStashClientPacket>().Single().Items.NotNull().Single().Slot);
        }

        [TestMethod]
        public async Task WithoutThePartnerBackpackNothingGoesIntoIt()
        {
            OwnsAMinilandWarehouse();
            Holds(1012, 10);

            await DepositAsync(10, 0, partnerBackpack: true);

            Assert.AreEqual(0, _stored.Count);
        }

        [TestMethod]
        public async Task WithdrawingFromThePartnerBackpackTakesFromIt()
        {
            HasThePartnerBackpack();
            Store(WarehouseType.PetWarehouse, 1012, 5, 3);
            Store(WarehouseType.Warehouse, 1012, 7, 3);

            await new WithdrawPacketHandler(Service()).ExecuteAsync(
                new WithdrawPacket { Slot = 3, Amount = 5, PetBackpack = true }, _session);

            Assert.AreEqual(5, _session.Character.InventoryService.CountItem(1012));
            Assert.AreEqual(7, At(WarehouseType.Warehouse, 3).Amount);
            Assert.IsFalse(_stored.Any(s => s.Type == WarehouseType.PetWarehouse));
        }
    }
}
