//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.WarehouseService
{
    public class WarehouseService(
        IDao<WarehouseItemDto, Guid> warehouseItemDao,
        IDao<WarehouseDto, Guid> warehouseDao,
        IDao<IItemInstanceDto?, Guid> itemInstanceDao)
        : IWarehouseService
    {
        private const int DefaultWarehouseSize = 68;
        private const int DefaultFamilyWarehouseSize = 49;

        public int GetMaxSlots(WarehouseType warehouseType)
        {
            return warehouseType switch
            {
                WarehouseType.FamilyWareHouse => DefaultFamilyWarehouseSize,
                _ => DefaultWarehouseSize
            };
        }

        public List<WarehouseLink> GetItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot)
        {
            var list = new List<WarehouseLink>();
            if (id == null)
            {
                var warehouses = warehouseDao.Where(s =>
                    s.Type == warehouseType &&
                    s.CharacterId == (warehouseType == WarehouseType.FamilyWareHouse ? null : ownerId) &&
                    s.FamilyId == (warehouseType == WarehouseType.FamilyWareHouse ? ownerId : null))?.ToList();

                if (warehouses == null || warehouses.Count == 0)
                {
                    return list;
                }

                var warehouseIds = warehouses.Select(w => w.Id).ToList();
                var warehouseItems = warehouseItemDao.Where(wi => warehouseIds.Contains(wi.WarehouseId))?.ToList();

                if (warehouseItems == null)
                {
                    return list;
                }

                foreach (var warehouseItem in warehouseItems)
                {
                    if (slot.HasValue && warehouseItem.Slot != slot.Value)
                    {
                        continue;
                    }

                    var itemInstance = itemInstanceDao.FirstOrDefaultAsync(i => i!.Id == warehouseItem.ItemInstanceId).Result;
                    if (itemInstance != null)
                    {
                        list.Add(new WarehouseLink
                        {
                            Slot = warehouseItem.Slot,
                            ItemInstance = itemInstance.Adapt<ItemInstanceDto>()
                        });
                    }
                }
            }
            else
            {
                var warehouseItem = warehouseItemDao.FirstOrDefaultAsync(wi => wi.Id == id.Value).Result;
                if (warehouseItem != null)
                {
                    var itemInstance = itemInstanceDao.FirstOrDefaultAsync(i => i!.Id == warehouseItem.ItemInstanceId).Result;
                    if (itemInstance != null)
                    {
                        list.Add(new WarehouseLink
                        {
                            Slot = warehouseItem.Slot,
                            ItemInstance = itemInstance.Adapt<ItemInstanceDto>()
                        });
                    }
                }
            }

            return list;
        }

        public List<WarehouseItem> GetWarehouseItems(long ownerId, WarehouseType warehouseType)
        {
            var result = new List<WarehouseItem>();

            var warehouses = warehouseDao.Where(s =>
                s.Type == warehouseType &&
                s.CharacterId == (warehouseType == WarehouseType.FamilyWareHouse ? null : ownerId) &&
                s.FamilyId == (warehouseType == WarehouseType.FamilyWareHouse ? ownerId : null))?.ToList();

            if (warehouses == null || warehouses.Count == 0)
            {
                return result;
            }

            var warehouseIds = warehouses.Select(w => w.Id).ToList();
            var warehouseItems = warehouseItemDao.Where(wi => warehouseIds.Contains(wi.WarehouseId))?.ToList();

            if (warehouseItems == null)
            {
                return result;
            }

            foreach (var warehouseItem in warehouseItems)
            {
                var itemInstance = itemInstanceDao.FirstOrDefaultAsync(i => i!.Id == warehouseItem.ItemInstanceId).Result;
                if (itemInstance != null)
                {
                    result.Add(new WarehouseItem
                    {
                        Id = warehouseItem.Id,
                        Slot = warehouseItem.Slot,
                        ItemInstanceId = warehouseItem.ItemInstanceId,
                        WarehouseId = warehouseItem.WarehouseId,
                        WarehouseType = warehouseType,
                        ItemInstance = itemInstance.Adapt<ItemInstance>()
                    });
                }
            }

            return result;
        }

        public WarehouseItem? GetItemBySlot(long ownerId, WarehouseType warehouseType, short slot)
        {
            var items = GetWarehouseItems(ownerId, warehouseType);
            return items.FirstOrDefault(i => i.Slot == slot);
        }

        public short? GetFreeSlot(long ownerId, WarehouseType warehouseType)
        {
            var items = GetWarehouseItems(ownerId, warehouseType);
            var maxSlots = GetMaxSlots(warehouseType);

            return SlotStorageHelper.FindFreeSlot(
                items,
                item => item.Slot,
                maxSlots);
        }

        public async Task<bool> WithdrawItemAsync(Guid id)
        {
            var item = await warehouseItemDao.FirstOrDefaultAsync(s => s.Id == id);
            if (item == null)
            {
                return false;
            }

            await warehouseItemDao.TryDeleteAsync(item.Id);
            await itemInstanceDao.TryDeleteAsync(item.ItemInstanceId);

            var remainingItemsInWarehouse = warehouseItemDao.Where(wi => wi.WarehouseId == item.WarehouseId)?.Any() ?? false;
            if (!remainingItemsInWarehouse)
            {
                await warehouseDao.TryDeleteAsync(item.WarehouseId);
            }

            return true;
        }

        public async Task<WarehouseItem?> WithdrawItemFromSlotAsync(long ownerId, WarehouseType warehouseType, short slot)
        {
            var item = GetItemBySlot(ownerId, warehouseType, slot);
            if (item == null)
            {
                return null;
            }

            var success = await WithdrawItemAsync(item.Id);
            return success ? item : null;
        }

        public async Task<bool> DepositItemAsync(long ownerId, WarehouseType warehouseType, ItemInstanceDto? itemInstance, short slot)
        {
            if (itemInstance == null)
            {
                return false;
            }

            var existingItem = GetItemBySlot(ownerId, warehouseType, slot);
            if (existingItem != null)
            {
                return false;
            }

            var item = itemInstance as IItemInstanceDto;
            item!.Id = Guid.NewGuid();
            item = await itemInstanceDao.TryInsertOrUpdateAsync(item);

            var existingWarehouse = warehouseDao.FirstOrDefaultAsync(s =>
                s.Type == warehouseType &&
                s.CharacterId == (warehouseType == WarehouseType.FamilyWareHouse ? null : ownerId) &&
                s.FamilyId == (warehouseType == WarehouseType.FamilyWareHouse ? ownerId : null)).Result;

            WarehouseDto warehouse;
            if (existingWarehouse != null)
            {
                warehouse = existingWarehouse;
            }
            else
            {
                warehouse = new WarehouseDto
                {
                    CharacterId = warehouseType == WarehouseType.FamilyWareHouse ? null : ownerId,
                    Id = Guid.NewGuid(),
                    FamilyId = warehouseType == WarehouseType.FamilyWareHouse ? ownerId : null,
                    Type = warehouseType,
                };
                warehouse = await warehouseDao.TryInsertOrUpdateAsync(warehouse);
            }

            var warehouseItem = new WarehouseItemDto
            {
                Slot = slot,
                Id = Guid.NewGuid(),
                ItemInstanceId = item!.Id,
                WarehouseId = warehouse.Id
            };
            await warehouseItemDao.TryInsertOrUpdateAsync(warehouseItem);

            return true;
        }

        public async Task<bool> DepositItemAsync(long ownerId, WarehouseType warehouseType, IItemInstance itemInstance)
        {
            var freeSlot = GetFreeSlot(ownerId, warehouseType);
            if (!freeSlot.HasValue)
            {
                return false;
            }

            var dto = itemInstance.Adapt<ItemInstanceDto>();
            return await DepositItemAsync(ownerId, warehouseType, dto, freeSlot.Value);
        }

        public async Task<bool> MoveItemAsync(long ownerId, WarehouseType warehouseType, short sourceSlot, short destinationSlot)
        {
            if (sourceSlot == destinationSlot)
            {
                return false;
            }

            var sourceItem = GetItemBySlot(ownerId, warehouseType, sourceSlot);
            if (sourceItem == null)
            {
                return false;
            }

            var destinationItem = GetItemBySlot(ownerId, warehouseType, destinationSlot);

            if (destinationItem != null)
            {
                var sourceWarehouseItem = await warehouseItemDao.FirstOrDefaultAsync(wi => wi.Id == sourceItem.Id);
                var destWarehouseItem = await warehouseItemDao.FirstOrDefaultAsync(wi => wi.Id == destinationItem.Id);

                if (sourceWarehouseItem != null && destWarehouseItem != null)
                {
                    sourceWarehouseItem.Slot = destinationSlot;
                    destWarehouseItem.Slot = sourceSlot;

                    await warehouseItemDao.TryInsertOrUpdateAsync(sourceWarehouseItem);
                    await warehouseItemDao.TryInsertOrUpdateAsync(destWarehouseItem);
                }
            }
            else
            {
                var warehouseItem = await warehouseItemDao.FirstOrDefaultAsync(wi => wi.Id == sourceItem.Id);
                if (warehouseItem != null)
                {
                    warehouseItem.Slot = destinationSlot;
                    await warehouseItemDao.TryInsertOrUpdateAsync(warehouseItem);
                }
            }

            return true;
        }
    }
}
