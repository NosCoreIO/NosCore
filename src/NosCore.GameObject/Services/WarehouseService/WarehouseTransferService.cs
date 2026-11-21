//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.Extensions.Logging;
using NosCore.Core.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Warehouse;
using System;
using System.Collections.Generic;
using NosCore.GameObject.Services.SaveService;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.WarehouseService
{
    public class WarehouseTransferService(IWarehouseHub warehouseHub, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
        IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IItemGenerationService itemProvider,
        ISaveService saveService, IItemDeliveryService itemDelivery, ILogger<WarehouseTransferService> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
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

        public async Task<IItemInstance?> GetItemAsync(long ownerId, WarehouseType warehouseType, short warehouseSlot)
        {
            if (warehouseSlot is < byte.MinValue or > byte.MaxValue)
            {
                return null;
            }

            var link = (await warehouseHub.GetWarehouseItems(null, ownerId, warehouseType, (byte)warehouseSlot)).FirstOrDefault();
            var instance = link == null ? null : await itemInstanceDao.FirstOrDefaultAsync(s => s.Id == link.ItemInstance.Id);
            return instance == null ? null : itemProvider.Convert(instance);
        }

        public async Task<IItemInstance?> DepositAsync(ClientSession session, WarehouseSlots warehouse, PocketType pocket,
            short slot, short amount, short warehouseSlot, Func<IItemInstance, bool> canStore)
        {
            var inventory = session.Character.InventoryService;
            var source = inventory.LoadBySlotAndType(slot, (NoscorePocketType)pocket);
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
            // Taken from the bag before the warehouse holds it, so nothing written meanwhile can take it twice.
            if (!inventory.TryConsume(source, amount))
            {
                return null;
            }

            IItemInstanceDto? copy;
            try
            {
                copy = await itemInstanceDao.TryInsertOrUpdateAsync(stored);
            }
            catch
            {
                await itemDelivery.ReturnConsumedAsync(session.Character, source, amount);
                throw;
            }

            if (copy == null)
            {
                await itemDelivery.ReturnConsumedAsync(session.Character, source, amount);
                return null;
            }

            bool deposited;
            try
            {
                deposited = await warehouseHub.AddWarehouseItemAsync(new WareHouseDepositRequest
                {
                    OwnerId = warehouse.OwnerId,
                    WarehouseType = warehouse.Type,
                    ItemInstance = stored.Adapt<ItemInstanceDto>(),
                    Slot = warehouseSlot
                });
            }
            catch (Exception e)
            {
                // The warehouse may have stored it before failing, so only what it reads back decides.
                switch (await IsStoredAsync(warehouse, warehouseSlot, stored.Id))
                {
                    case true:
                        deposited = true;
                        break;
                    case false:
                        _ = await itemInstanceDao.TryDeleteAsync(stored.Id);
                        await itemDelivery.ReturnConsumedAsync(session.Character, source, amount);
                        throw;
                    default:
                        // Giving it back could leave it in both places; the copy is kept and named instead.
                        logger.LogError(e, logLanguage[LogLanguageKey.DEPOSIT_OUTCOME_UNKNOWN], stored.ItemVNum, amount,
                            session.Character.CharacterId, warehouseSlot, stored.Id);
                        await saveService.SaveAsync(session);
                        throw;
                }
            }

            if (!deposited)
            {
                // Nothing points at the copy, so one left behind is unreachable.
                _ = await itemInstanceDao.TryDeleteAsync(stored.Id);
                await itemDelivery.ReturnConsumedAsync(session.Character, source, amount);
                return null;
            }

            if (!inventory.ContainsKey(source.ItemInstanceId))
            {
                // The save below drops a bag row the inventory no longer holds, should this miss it.
                _ = await inventoryItemInstanceDao.TryDeleteAsync(source.Id);
            }

            // The warehouse side is already stored; the bag must not wait for the next save.
            await saveService.SaveAsync(session);
            await session.SendPacketAsync(inventory.LoadBySlotAndType(slot, (NoscorePocketType)pocket)
                .GeneratePocketChange(pocket, slot));
            await SendSlotAsync(session, warehouse.Type, stored, warehouseSlot);
            return stored;
        }

        private async Task<bool?> IsStoredAsync(WarehouseSlots warehouse, short warehouseSlot, Guid itemInstanceId)
        {
            try
            {
                return (await warehouseHub.GetWarehouseItems(null, warehouse.OwnerId, warehouse.Type, (byte)warehouseSlot))
                    .Any(link => link.ItemInstance?.Id == itemInstanceId);
            }
            catch (Exception)
            {
                return null;
            }
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
                // A partial amount merges back into the stack still stored, so it needs no row of its own.
                if (await itemInstanceDao.TryInsertOrUpdateAsync(back) != null || !whole)
                {
                    // A failed return leaves the taken amount in its own row rather than in the warehouse.
                    await warehouseHub.ReturnWarehouseItemAsync(warehouse.OwnerId, warehouse.Type, warehouseSlot,
                        back.Adapt<ItemInstanceDto>()).OrThrowAsync();
                }

                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.NotEnoughSpace });
                return null;
            }

            IItemInstance? left = null;
            if (!whole)
            {
                stored.Item.Amount -= amount;
                left = stored.Item;
            }

            await saveService.SaveAsync(session);
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
                if (await itemInstanceDao.TryInsertOrUpdateAsync(split) == null)
                {
                    return;
                }

                if (!await warehouseHub.SplitWarehouseItemAsync(warehouse.OwnerId, warehouse.Type,
                        sourceSlot, destinationSlot, split.Adapt<ItemInstanceDto>()))
                {
                    _ = await itemInstanceDao.TryDeleteAsync(split.Id);
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
