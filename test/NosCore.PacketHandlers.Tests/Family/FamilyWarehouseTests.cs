//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Warehouse;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Warehouse;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.PacketHandlers.Tests.Family
{
    [TestClass]
    public class FamilyWarehouseTests
    {
        private const long FamilyId = 9;
        private ClientSession _session = null!;
        private IItemGenerationService _items = null!;
        private readonly Dictionary<Guid, IItemInstanceDto> _instances = new();
        private readonly List<(Guid Id, short Slot, Guid ItemInstanceId)> _stored = new();
        private bool _takenElsewhere;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _items = new ItemGenerationService(new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Main, VNum = 1012, IsTradable = true },
                new Item { Type = NoscorePocketType.Main, VNum = 1013 },
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon, IsTradable = true }
            }, NullLoggerFactory.Instance, TestHelpers.Instance.LogLanguageLocalizer);
            JoinFamily(FamilyAuthority.Head, 21);
        }

        private void JoinFamily(FamilyAuthority authority, byte warehouseSize,
            FamilyAuthorityType memberRights = FamilyAuthorityType.NONE)
        {
            _session.Character.Family = new GameObject.Services.FamilyService.Family
            {
                FamilyId = FamilyId,
                FamilyLevel = 7,
                WarehouseSize = warehouseSize,
                MemberAuthorityType = memberRights,
                Members = new List<FamilyCharacterDto>
                {
                    new() { FamilyId = FamilyId, CharacterId = _session.Character.CharacterId, Authority = authority }
                }
            };
        }

        private FamilyWarehouseService Service()
        {
            var hub = new Mock<IWarehouseHub>();
            hub.Setup(s => s.GetWarehouseItems(null, FamilyId, WarehouseType.FamilyWareHouse, null))
                .ReturnsAsync(() => _stored.Select(s => new WarehouseLink
                {
                    WarehouseItemId = s.Id,
                    Slot = s.Slot,
                    ItemInstance = new ItemInstanceDto { Id = s.ItemInstanceId }
                }).ToList());
            hub.Setup(s => s.AddWarehouseItemAsync(It.IsAny<WareHouseDepositRequest>()))
                .ReturnsAsync((WareHouseDepositRequest r) =>
                {
                    _stored.Add((Guid.NewGuid(), r.Slot, r.ItemInstance!.Id));
                    return true;
                });
            hub.Setup(s => s.DeleteWarehouseItemAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => !_takenElsewhere && _stored.RemoveAll(s => s.Id == id) == 1);
            hub.Setup(s => s.MoveWarehouseItemAsync(FamilyId, WarehouseType.FamilyWareHouse, It.IsAny<short>(), It.IsAny<short>()))
                .ReturnsAsync((long _, WarehouseType _, short from, short to) =>
                {
                    var source = _stored.Single(s => s.Slot == from);
                    var destination = _stored.FindIndex(s => s.Slot == to);
                    if (destination >= 0)
                    {
                        _stored[destination] = (_stored[destination].Id, from, _stored[destination].ItemInstanceId);
                    }

                    _stored[_stored.IndexOf(source)] = (source.Id, to, source.ItemInstanceId);
                    return true;
                });

            hub.Setup(s => s.TakeWarehouseItemAsync(It.IsAny<Guid>(), It.IsAny<short>()))
                .ReturnsAsync((Guid id, short amount) =>
                {
                    var index = _stored.FindIndex(s => s.Id == id);
                    if (_takenElsewhere || index < 0 || amount <= 0)
                    {
                        return false;
                    }

                    var instance = _instances[_stored[index].ItemInstanceId]!;
                    if (amount > instance.Amount)
                    {
                        return false;
                    }

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
            hub.Setup(s => s.ReturnWarehouseItemAsync(FamilyId, WarehouseType.FamilyWareHouse, It.IsAny<short>(), It.IsAny<ItemInstanceDto>()))
                .ReturnsAsync((long _, WarehouseType _, short slot, ItemInstanceDto item) =>
                {
                    var existing = _stored.FindIndex(s => s.Slot == slot);
                    if (existing >= 0 && _instances[_stored[existing].ItemInstanceId]!.ItemVNum == item.ItemVNum)
                    {
                        _instances[_stored[existing].ItemInstanceId]!.Amount += item.Amount;
                        return true;
                    }

                    _stored.Add((Guid.NewGuid(), slot, item.Id));
                    return true;
                });
            hub.Setup(s => s.SplitWarehouseItemAsync(FamilyId, WarehouseType.FamilyWareHouse, It.IsAny<short>(), It.IsAny<short>(), It.IsAny<ItemInstanceDto>()))
                .ReturnsAsync((long _, WarehouseType _, short from, short to, ItemInstanceDto split) =>
                {
                    if (_stored.Any(s => s.Slot == to))
                    {
                        return false;
                    }

                    _instances[_stored.Single(s => s.Slot == from).ItemInstanceId]!.Amount -= split.Amount;
                    _stored.Add((Guid.NewGuid(), to, split.Id));
                    return true;
                });

            var instances = new Mock<IDao<IItemInstanceDto?, Guid>>();
            instances.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IItemInstanceDto?>()))
                .ReturnsAsync((IItemInstanceDto? i) => _instances[i!.Id] = i);
            instances.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync((Expression<Func<IItemInstanceDto?, bool>> p) => _instances.Values.FirstOrDefault(p.Compile()));

            return new FamilyWarehouseService(hub.Object, instances.Object,
                new Mock<IDao<InventoryItemInstanceDto, Guid>>().Object, _items);
        }

        private void Store(short vnum, short amount, short slot)
        {
            var item = _items.Create(vnum, amount);
            _instances[item.Id] = item;
            _stored.Add((Guid.NewGuid(), slot, item.Id));
        }

        private IItemInstanceDto At(short slot) => _instances[_stored.Single(s => s.Slot == slot).ItemInstanceId];

        private static Game18NConstString? LastInfo(ClientSession session) =>
            session.LastPackets.OfType<InfoiPacket>().LastOrDefault()?.Message;

        [TestMethod]
        public async Task OpeningShowsTheSizeAndTheStoredItems()
        {
            Store(1012, 5, 3);

            await Service().OpenAsync(_session);

            var stash = _session.LastPackets.OfType<FStashAllPacket>().Single();
            Assert.AreEqual(21, stash.WarehouseSize);
            Assert.AreEqual(3, stash.Items!.Single().Slot);
        }

        [TestMethod]
        public async Task AFamilyWithoutAWarehouseCannotOpenOne()
        {
            JoinFamily(FamilyAuthority.Head, 0);

            await Service().OpenAsync(_session);

            Assert.AreEqual(Game18NConstString.NotQualifiedForWarehouse, LastInfo(_session));
            Assert.IsFalse(_session.LastPackets.OfType<FStashAllPacket>().Any());
        }

        [TestMethod]
        public async Task DepositingMovesPartOfAStackIntoTheWarehouse()
        {
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(1012, 10), _session.Character.CharacterId));

            await new FDepositPacketHandler(Service()).ExecuteAsync(
                new FDepositPacket { Inventory = PocketType.Main, Slot = 0, Amount = 4, NewSlot = 2 }, _session);

            Assert.AreEqual(4, At(2).Amount);
            Assert.AreEqual(6, _session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Main)!.ItemInstance.Amount);
        }

        [TestMethod]
        public async Task DepositingAWholeStackEmptiesTheInventorySlot()
        {
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(1012, 10), _session.Character.CharacterId));

            await new FDepositPacketHandler(Service()).ExecuteAsync(
                new FDepositPacket { Inventory = PocketType.Main, Slot = 0, Amount = 10, NewSlot = 0 }, _session);

            Assert.AreEqual(10, At(0).Amount);
            Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Main));
        }

        [TestMethod]
        public async Task DepositingBeyondTheWarehouseSizeDoesNothing()
        {
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(1012, 10), _session.Character.CharacterId));

            await new FDepositPacketHandler(Service()).ExecuteAsync(
                new FDepositPacket { Inventory = PocketType.Main, Slot = 0, Amount = 10, NewSlot = 21 }, _session);

            Assert.AreEqual(0, _stored.Count);
        }

        [TestMethod]
        public async Task AMemberWithoutRightsCannotDeposit()
        {
            JoinFamily(FamilyAuthority.Member, 21);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(1012, 10), _session.Character.CharacterId));

            await new FDepositPacketHandler(Service()).ExecuteAsync(
                new FDepositPacket { Inventory = PocketType.Main, Slot = 0, Amount = 10, NewSlot = 0 }, _session);

            Assert.AreEqual(Game18NConstString.NotQualifiedForWarehouse, LastInfo(_session));
            Assert.AreEqual(0, _stored.Count);
        }

        [TestMethod]
        public async Task AMemberAllowedToPutCannotTakeOut()
        {
            JoinFamily(FamilyAuthority.Member, 21, FamilyAuthorityType.PUT);
            Store(1012, 5, 3);

            await new FWithdrawPacketHandler(Service()).ExecuteAsync(new FWithdrawPacket { Slot = 3, Amount = 5 }, _session);

            Assert.AreEqual(Game18NConstString.NotQualifiedForWarehouse, LastInfo(_session));
            Assert.AreEqual(1, _stored.Count);
        }

        [TestMethod]
        public async Task WithdrawingPartOfAStackLeavesTheRest()
        {
            Store(1012, 5, 3);

            await new FWithdrawPacketHandler(Service()).ExecuteAsync(new FWithdrawPacket { Slot = 3, Amount = 2 }, _session);

            Assert.AreEqual(3, At(3).Amount);
            Assert.AreEqual(2, _session.Character.InventoryService.CountItem(1012));
        }

        [TestMethod]
        public async Task WithdrawingAWholeStackFreesTheSlot()
        {
            Store(1012, 5, 3);

            await new FWithdrawPacketHandler(Service()).ExecuteAsync(new FWithdrawPacket { Slot = 3, Amount = 5 }, _session);

            Assert.AreEqual(0, _stored.Count);
            Assert.AreEqual(5, _session.Character.InventoryService.CountItem(1012));
        }

        [TestMethod]
        public async Task AnUntradableItemCannotBeDeposited()
        {
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(1013, 10), _session.Character.CharacterId));

            await new FDepositPacketHandler(Service()).ExecuteAsync(
                new FDepositPacket { Inventory = PocketType.Main, Slot = 0, Amount = 10, NewSlot = 0 }, _session);

            Assert.AreEqual(0, _stored.Count);
            Assert.AreEqual(10, _session.Character.InventoryService.CountItem(1013));
        }

        [TestMethod]
        public async Task ABoundItemCannotBeDeposited()
        {
            var weapon = _items.Create(1, 1);
            weapon.BoundCharacterId = _session.Character.CharacterId;
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(weapon, _session.Character.CharacterId));

            await new FDepositPacketHandler(Service()).ExecuteAsync(
                new FDepositPacket { Inventory = PocketType.Equipment, Slot = 0, Amount = 1, NewSlot = 0 }, _session);

            Assert.AreEqual(0, _stored.Count);
        }

        [TestMethod]
        public async Task AStackAlreadyTakenFromAnotherSessionIsNotGivenTwice()
        {
            Store(1012, 5, 3);
            _takenElsewhere = true;

            await new FWithdrawPacketHandler(Service()).ExecuteAsync(new FWithdrawPacket { Slot = 3, Amount = 5 }, _session);

            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(1012));
        }

        [TestMethod]
        public async Task WithdrawingIntoAFullInventoryKeepsTheItemInTheWarehouse()
        {
            while (_session.Character.InventoryService.AddItemToPocket(
                       InventoryItemInstance.Create(_items.Create(1013, 999), _session.Character.CharacterId)) is { Count: > 0 })
            {
            }

            Store(1012, 5, 3);

            await new FWithdrawPacketHandler(Service()).ExecuteAsync(new FWithdrawPacket { Slot = 3, Amount = 5 }, _session);

            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(1012));
            Assert.AreEqual(5, At(3).Amount);
            Assert.AreEqual(Game18NConstString.NotEnoughSpace, _session.LastPackets.OfType<MsgiPacket>().Last().Message);
        }

        [TestMethod]
        public async Task MovingOntoAnotherItemSwapsThem()
        {
            Store(1012, 5, 3);
            Store(1, 1, 4);

            await new FReposPacketHandler(Service()).ExecuteAsync(new FReposPacket { OldSlot = 3, Amount = 5, NewSlot = 4 }, _session);

            Assert.AreEqual(1012, At(4).ItemVNum);
            Assert.AreEqual(1, At(3).ItemVNum);
        }

        [TestMethod]
        public async Task MovingPartOfAStackToAnEmptySlotSplitsIt()
        {
            Store(1012, 5, 3);

            await new FReposPacketHandler(Service()).ExecuteAsync(new FReposPacket { OldSlot = 3, Amount = 2, NewSlot = 6 }, _session);

            Assert.AreEqual(3, At(3).Amount);
            Assert.AreEqual(2, At(6).Amount);
        }
    }
}
