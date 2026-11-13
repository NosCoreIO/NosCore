//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NodaTime;
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
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Warehouse;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyWarehouseService(IWarehouseHub warehouseHub, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
        IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IItemGenerationService itemProvider,
        IFamilyLogService familyLogService, IClock clock)
        : IFamilyWarehouseService
    {
        public async Task OpenAsync(ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || family.WarehouseSize == 0 || !CanPut(session, family))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotQualifiedForWarehouse });
                return;
            }

            var items = await GetItemsAsync(family.FamilyId);
            await session.SendPacketAsync(new FStashAllPacket
            {
                WarehouseSize = family.WarehouseSize,
                Items = items.Select(s => s.Item.GenerateWarehouseSubPacket(s.Slot)).ToList()
            });
        }

        public Task OpenHistoryAsync(ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || !family.CanGetHistory(session.Character.CharacterId))
            {
                return session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotAuthorized });
            }

            return session.SendPacketAsync(GenerateFslog(familyLogService.GetWarehouseHistory(family.FamilyId), clock.GetCurrentInstant()));
        }

        public async Task DepositAsync(ClientSession session, PocketType pocket, short slot, short amount, short warehouseSlot)
        {
            var family = session.Character.Family;
            if (family == null || !CanPut(session, family))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotQualifiedForWarehouse });
                return;
            }

            var source = session.Character.InventoryService.LoadBySlotAndType(slot, (NoscorePocketType)pocket);
            if (source?.ItemInstance == null || !source.ItemInstance.Item.IsTradable || source.ItemInstance.BoundCharacterId != null
                || amount <= 0 || amount > source.ItemInstance.Amount
                || warehouseSlot < 0 || warehouseSlot >= family.WarehouseSize
                || (await GetItemsAsync(family.FamilyId)).Any(s => s.Slot == warehouseSlot))
            {
                return;
            }

            var stored = (IItemInstance)source.ItemInstance.Clone();
            stored.Id = Guid.NewGuid();
            stored.Amount = amount;
            await itemInstanceDao.TryInsertOrUpdateAsync(stored);
            if (!await warehouseHub.AddWarehouseItemAsync(new WareHouseDepositRequest
            {
                OwnerId = family.FamilyId,
                WarehouseType = WarehouseType.FamilyWareHouse,
                ItemInstance = stored.Adapt<ItemInstanceDto>(),
                Slot = warehouseSlot
            }))
            {
                await itemInstanceDao.TryDeleteAsync(stored.Id);
                return;
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
            await SendSlotAsync(session, stored, warehouseSlot);
            await familyLogService.AddAsync(family.FamilyId, FamilyLogType.WareHouseAdded, session.Character.Name, stored.ItemVNum, amount);
        }

        public async Task WithdrawAsync(ClientSession session, short warehouseSlot, short amount)
        {
            var family = session.Character.Family;
            if (family == null || !CanTake(session, family))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotQualifiedForWarehouse });
                return;
            }

            var stored = (await GetItemsAsync(family.FamilyId)).FirstOrDefault(s => s.Slot == warehouseSlot);
            if (stored.Item == null || amount <= 0 || amount > stored.Item.Amount)
            {
                return;
            }

            if (!await warehouseHub.TakeWarehouseItemAsync(stored.LinkId, amount))
            {
                return;
            }

            var whole = amount == stored.Item.Amount;
            var taken = (IItemInstance)stored.Item.Clone();
            taken.Id = Guid.NewGuid();
            taken.Amount = amount;
            var added = session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(taken, session.Character.CharacterId));
            if (added == null || added.Count == 0)
            {
                await ReturnAsync(family.FamilyId, whole ? stored.Item : taken, warehouseSlot);
                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.NotEnoughSpace });
                return;
            }

            IItemInstance? left = null;
            if (!whole)
            {
                stored.Item.Amount -= amount;
                left = stored.Item;
            }

            await session.SendPacketAsync(added.GeneratePocketChange());
            await SendSlotAsync(session, left, warehouseSlot);
            await familyLogService.AddAsync(family.FamilyId, FamilyLogType.WareHouseRemoved, session.Character.Name, taken.ItemVNum, amount);
        }

        public async Task MoveAsync(ClientSession session, short sourceSlot, short amount, short destinationSlot)
        {
            var family = session.Character.Family;
            if (family == null || !CanTake(session, family))
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotQualifiedForWarehouse });
                return;
            }

            var items = await GetItemsAsync(family.FamilyId);
            var source = items.FirstOrDefault(s => s.Slot == sourceSlot);
            var destination = items.FirstOrDefault(s => s.Slot == destinationSlot);
            if (source.Item == null || sourceSlot == destinationSlot || amount <= 0 || amount > source.Item.Amount
                || destinationSlot < 0 || destinationSlot >= family.WarehouseSize)
            {
                return;
            }

            if (destination.Item == null && amount < source.Item.Amount)
            {
                var split = (IItemInstance)source.Item.Clone();
                split.Id = Guid.NewGuid();
                split.Amount = amount;
                await itemInstanceDao.TryInsertOrUpdateAsync(split);
                if (!await warehouseHub.SplitWarehouseItemAsync(family.FamilyId, WarehouseType.FamilyWareHouse,
                        sourceSlot, destinationSlot, split.Adapt<ItemInstanceDto>()))
                {
                    await itemInstanceDao.TryDeleteAsync(split.Id);
                    return;
                }

                source.Item.Amount -= amount;
                await SendSlotAsync(session, split, destinationSlot);
                await SendSlotAsync(session, source.Item, sourceSlot);
                return;
            }

            if (!await warehouseHub.MoveWarehouseItemAsync(family.FamilyId, WarehouseType.FamilyWareHouse, sourceSlot, destinationSlot))
            {
                return;
            }

            await SendSlotAsync(session, source.Item, destinationSlot);
            await SendSlotAsync(session, destination.Item, sourceSlot);
        }

        private async Task ReturnAsync(long familyId, IItemInstance item, short slot)
        {
            await itemInstanceDao.TryInsertOrUpdateAsync(item);
            await warehouseHub.ReturnWarehouseItemAsync(familyId, WarehouseType.FamilyWareHouse, slot, item.Adapt<ItemInstanceDto>());
        }

        private static bool CanPut(ClientSession session, Family family) => family.AuthorityOf(session.Character.CharacterId) switch
        {
            FamilyAuthority.Head or FamilyAuthority.Assistant => true,
            FamilyAuthority.Manager => family.ManagerAuthorityType != FamilyAuthorityType.NONE,
            _ => family.MemberAuthorityType != FamilyAuthorityType.NONE
        };

        private static bool CanTake(ClientSession session, Family family) => family.AuthorityOf(session.Character.CharacterId) switch
        {
            FamilyAuthority.Head or FamilyAuthority.Assistant => true,
            FamilyAuthority.Manager => family.ManagerAuthorityType == FamilyAuthorityType.ALL,
            _ => family.MemberAuthorityType == FamilyAuthorityType.ALL
        };

        private async Task<List<(Guid LinkId, short Slot, IItemInstance Item)>> GetItemsAsync(long familyId)
        {
            var items = new List<(Guid, short, IItemInstance)>();
            foreach (var link in await warehouseHub.GetWarehouseItems(null, familyId, WarehouseType.FamilyWareHouse, null))
            {
                var instance = await itemInstanceDao.FirstOrDefaultAsync(s => s.Id == link.ItemInstance.Id);
                if (instance != null)
                {
                    items.Add((link.WarehouseItemId, link.Slot, itemProvider.Convert(instance)));
                }
            }

            return items;
        }

        private static FslogStcPacket GenerateFslog(IEnumerable<FamilyLogDto> history, Instant now) => new()
        {
            Entries = history.Select(s => (FslogStcSubPacket?)new FslogStcSubPacket
            {
                Type = s.FamilyLogType == FamilyLogType.WareHouseAdded ? FamilyWarehouseLogType.Deposit : FamilyWarehouseLogType.Withdrawal,
                CharacterName = s.FirstArgument,
                ItemVNum = ParseShort(s.SecondArgument),
                Amount = ParseShort(s.ThirdArgument),
                HoursAgo = (int)(now - s.Timestamp).TotalHours
            }).ToList()
        };

        private static short ParseShort(string? value) =>
            short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : default;

        private static Task SendSlotAsync(ClientSession session, IItemInstance? item, short slot) =>
            session.SendPacketAsync(new FStashClientPacket { Items = [item.GenerateWarehouseSubPacket(slot)] });
    }
}
