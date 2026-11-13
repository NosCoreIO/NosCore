//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.WarehouseService
{
    public class WarehouseTransferService(IWarehouseHub warehouseHub, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
        IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IItemGenerationService itemProvider)
        : IWarehouseTransferService
    {
        public async Task<List<StoredItem>> GetItemsAsync(long ownerId, WarehouseType warehouseType)
        {
            var items = new List<StoredItem>();
            foreach (var link in await warehouseHub.GetWarehouseItems(null, ownerId, warehouseType, null))
            {
                var instance = await itemInstanceDao.FirstOrDefaultAsync(s => s.Id == link.ItemInstance.Id);
                if (instance != null)
                {
                    items.Add(new StoredItem(link.WarehouseItemId, link.Slot, itemProvider.Convert(instance)));
                }
            }

            return items;
        }

        public async Task<IItemInstance?> DepositAsync(ClientSession session, WarehouseSlots warehouse, PocketType pocket,
            short slot, short amount, short warehouseSlot, Func<IItemInstance, bool> canStore)
        {
            var source = session.Character.InventoryService.LoadBySlotAndType(slot, (NoscorePocketType)pocket);
            if (source?.ItemInstance == null || !canStore(source.ItemInstance)
                || amount <= 0 || amount > source.ItemInstance.Amount
                || warehouseSlot < 0 || warehouseSlot >= warehouse.Size
                || (await GetItemsAsync(warehouse.OwnerId, warehouse.Type)).Any(s => s.Slot == warehouseSlot))
            {
                return null;
            }

            var stored = (IItemInstance)source.ItemInstance.Clone();
            stored.Id = Guid.NewGuid();
            stored.Amount = amount;
            await itemInstanceDao.TryInsertOrUpdateAsync(stored);
            if (!await warehouseHub.AddWarehouseItemAsync(new WareHouseDepositRequest
            {
                OwnerId = warehouse.OwnerId,
                WarehouseType = warehouse.Type,
                ItemInstance = stored.Adapt<ItemInstanceDto>(),
                Slot = warehouseSlot
            }))
            {
                await itemInstanceDao.TryDeleteAsync(stored.Id);
                return null;
            }

            InventoryItemInstance? remaining;
            if (amount == source.ItemInstance.Amount)
            {
                await inventoryItemInstanceDao.TryDeleteAsync(source.Id);
                session.Character.InventoryService.DeleteById(source.ItemInstanceId);
                remaining = null;
            }
            else
            {
                remaining = session.Character.InventoryService.RemoveItemAmountFromInventory(amount, source.ItemInstanceId);
            }

            await session.SendPacketAsync(remaining.GeneratePocketChange(pocket, slot));
            await SendSlotAsync(session, warehouse.Type, stored, warehouseSlot);
            return stored;
        }

        public async Task<IItemInstance?> WithdrawAsync(ClientSession session, WarehouseSlots warehouse, short warehouseSlot,
            short amount)
        {
            var stored = (await GetItemsAsync(warehouse.OwnerId, warehouse.Type)).FirstOrDefault(s => s.Slot == warehouseSlot);
            if (stored.Item == null || amount <= 0 || amount > stored.Item.Amount)
            {
                return null;
            }

            if (!await warehouseHub.TakeWarehouseItemAsync(stored.LinkId, amount))
            {
                return null;
            }

            var whole = amount == stored.Item.Amount;
            var taken = (IItemInstance)stored.Item.Clone();
            taken.Id = Guid.NewGuid();
            taken.Amount = amount;
            var added = session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(taken, session.Character.CharacterId));
            if (added == null || added.Count == 0)
            {
                var back = whole ? stored.Item : taken;
                await itemInstanceDao.TryInsertOrUpdateAsync(back);
                await warehouseHub.ReturnWarehouseItemAsync(warehouse.OwnerId, warehouse.Type, warehouseSlot,
                    back.Adapt<ItemInstanceDto>());
                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.NotEnoughSpace });
                return null;
            }

            IItemInstance? left = null;
            if (!whole)
            {
                stored.Item.Amount -= amount;
                left = stored.Item;
            }

            await session.SendPacketAsync(added.GeneratePocketChange());
            await SendSlotAsync(session, warehouse.Type, left, warehouseSlot);
            return taken;
        }

        public async Task MoveAsync(ClientSession session, WarehouseSlots warehouse, short sourceSlot, short amount,
            short destinationSlot)
        {
            var items = await GetItemsAsync(warehouse.OwnerId, warehouse.Type);
            var source = items.FirstOrDefault(s => s.Slot == sourceSlot);
            var destination = items.FirstOrDefault(s => s.Slot == destinationSlot);
            if (source.Item == null || sourceSlot == destinationSlot || amount <= 0 || amount > source.Item.Amount
                || destinationSlot < 0 || destinationSlot >= warehouse.Size)
            {
                return;
            }

            if (destination.Item == null && amount < source.Item.Amount)
            {
                var split = (IItemInstance)source.Item.Clone();
                split.Id = Guid.NewGuid();
                split.Amount = amount;
                await itemInstanceDao.TryInsertOrUpdateAsync(split);
                if (!await warehouseHub.SplitWarehouseItemAsync(warehouse.OwnerId, warehouse.Type,
                        sourceSlot, destinationSlot, split.Adapt<ItemInstanceDto>()))
                {
                    await itemInstanceDao.TryDeleteAsync(split.Id);
                    return;
                }

                source.Item.Amount -= amount;
                await SendSlotAsync(session, warehouse.Type, split, destinationSlot);
                await SendSlotAsync(session, warehouse.Type, source.Item, sourceSlot);
                return;
            }

            if (!await warehouseHub.MoveWarehouseItemAsync(warehouse.OwnerId, warehouse.Type, sourceSlot, destinationSlot))
            {
                return;
            }

            await SendSlotAsync(session, warehouse.Type, source.Item, destinationSlot);
            await SendSlotAsync(session, warehouse.Type, destination.Item, sourceSlot);
        }

        public Task SendSlotAsync(ClientSession session, WarehouseType warehouseType, IItemInstance? item, short slot)
        {
            List<WarehouseSubPacket> items = [item.GenerateWarehouseSubPacket(slot)];
            IPacket packet = warehouseType switch
            {
                WarehouseType.FamilyWareHouse => new FStashClientPacket { Items = items },
                WarehouseType.PetWarehouse => new PStashClientPacket { Items = items },
                _ => new StashClientPacket { Items = items }
            };
            return session.SendPacketAsync(packet);
        }
    }
}
