//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemStorage;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NosCore.GameObject.Services.ItemGenerationService;

namespace NosCore.GameObject.Services.InventoryService
{
    public class InventoryService(IItemCatalog itemCatalog, IOptions<WorldConfiguration> configuration, ILogger<InventoryService> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : ConcurrentDictionary<Guid, InventoryItemInstance>, IInventoryService
    {
        // Every write, and every read that spans several entries, goes through this lock, so a
        // writer on another character's behalf cannot interleave with the owner's own.
        private readonly Lock _gate = new();

        public int GetMaxSlots(NoscorePocketType pocket)
        {
            //TODO make this configurable
            return (byte)(pocket switch
            {
                NoscorePocketType.Miniland => 50 + Expensions[pocket],
                NoscorePocketType.Specialist => 45 + Expensions[pocket],
                NoscorePocketType.Costume => 60 + Expensions[pocket],
                NoscorePocketType.Wear or NoscorePocketType.FirstPartnerInventory
                    or NoscorePocketType.SecondPartnerInventory or NoscorePocketType.ThirdPartnerInventory => 17,
                NoscorePocketType.Mount => 20 + Expensions[pocket],
                NoscorePocketType.Raid => 63 + Expensions[pocket],
                _ => configuration.Value.BackpackSize + Expensions[pocket]
            });
        }

        public Dictionary<NoscorePocketType, byte> Expensions { get; set; } = new()
        {
            { NoscorePocketType.Costume, 0 },
            { NoscorePocketType.Equipment, 0 },
            { NoscorePocketType.Etc, 0 },
            { NoscorePocketType.Miniland, 0 },
            { NoscorePocketType.Main, 0 },
            { NoscorePocketType.Specialist, 0 },
            { NoscorePocketType.Wear, 0 },
            { NoscorePocketType.Mount, 0 },
            { NoscorePocketType.Raid, 0 },
            { NoscorePocketType.FirstPartnerInventory, 0 },
            { NoscorePocketType.SecondPartnerInventory, 0 },
            { NoscorePocketType.ThirdPartnerInventory, 0 },
        };

        public InventoryItemInstance? LoadBySlotAndType(short slot, NoscorePocketType type)
        {
            lock (_gate)
            {
                InventoryItemInstance? retItem = default;
                try
                {
                    retItem = this.Select(s => s.Value)
                        .SingleOrDefault(i => (i.Slot == slot) && (i.Type == type));
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, logLanguage[LogLanguageKey.INVENTORY_SLOT_HOLDS_SEVERAL_ITEMS], slot, type);
                }

                return retItem;
            }
        }

        public bool CanAddItem(short itemVnum)
        {
            var type = itemCatalog.GetItem(itemVnum)?.Type;
            return type != null && GetFreeSlot(type.Value).HasValue;
        }

        public int CountItem(int itemVNum)
        {
            lock (_gate)
            {
                return this.Select(s => s.Value).Where(s => s.Type != NoscorePocketType.Wear && s.ItemInstance?.ItemVNum == itemVNum)
                    .Sum(i => i.ItemInstance?.Amount ?? 0);
            }
        }

        public int CountItemInAnPocket(NoscorePocketType inv)
        {
            lock (_gate)
            {
                return this.Count(s => s.Value.Type == inv);
            }
        }

        public List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem)
        {
            return AddItemToPocket(newItem, null, null);
        }

        public List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem, NoscorePocketType? type)
        {
            return AddItemToPocket(newItem, type, null);
        }

        public List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem, NoscorePocketType? type,
            short? slot)
        {
            lock (_gate)
            {
                var placement = Plan(newItem, type, slot);
                return placement == null ? null : placement.Fits ? Commit(placement) : [];
            }
        }

        public List<InventoryItemInstance>? AddItemsToPocket(IReadOnlyCollection<InventoryItemInstance> newItems)
        {
            lock (_gate)
            {
                var committed = new List<Placement>();
                var changed = new List<InventoryItemInstance>();
                foreach (var newItem in newItems)
                {
                    var placement = Plan(newItem, null, null);
                    if (placement is not { Fits: true })
                    {
                        committed.Reverse();
                        committed.ForEach(Undo);
                        return null;
                    }

                    changed.AddRange(Commit(placement));
                    committed.Add(placement);
                }

                return changed.Distinct().ToList();
            }
        }

        public List<InventoryItemInstance>? ConsumeAndAdd(InventoryItemInstance source, short amount,
            IReadOnlyCollection<InventoryItemInstance> newItems)
        {
            lock (_gate)
            {
                if (!TryConsume(source, amount))
                {
                    return null;
                }

                if (AddItemsToPocket(newItems) is { Count: > 0 } added)
                {
                    return added;
                }

                // Nothing else could write while the lock was held, so the source's slot is still free.
                if (TryGetValue(source.ItemInstanceId, out var left))
                {
                    left.ItemInstance.Amount += amount;
                }
                else
                {
                    source.ItemInstance.Amount = amount;
                    this[source.ItemInstanceId] = source;
                }

                return [];
            }
        }

        private sealed record Merge(InventoryItemInstance Stack, short Was, short Becomes);

        private sealed record NewStack(InventoryItemInstance Entry, short Slot, short Amount);

        private sealed record Placement(InventoryItemInstance Item, NoscorePocketType Pocket, IReadOnlyList<Merge> Merges,
            IReadOnlyList<NewStack> NewStacks, bool Fits)
        {
            public short Amount { get; } = Item.ItemInstance.Amount;
            public NoscorePocketType PocketBefore { get; } = Item.Type;
            public short SlotBefore { get; } = Item.Slot;
        }

        // Decides where every unit of the item goes without changing anything, so a placement
        // that cannot be completed leaves the held stacks and the item as they were.
        private Placement? Plan(InventoryItemInstance newItem, NoscorePocketType? type, short? slot)
        {
            if (newItem?.ItemInstance == null || newItem.ItemInstance.Amount <= 0)
            {
                return null;
            }

            if (ContainsKey(newItem.ItemInstanceId))
            {
                logger.LogError(logLanguage[LogLanguageKey.ITEM_ALREADY_IN_INVENTORY], newItem.ItemInstanceId);
                return null;
            }

            var pocket = type ?? newItem.Type;
            var amount = newItem.ItemInstance.Amount;
            var stackable = pocket is NoscorePocketType.Etc or NoscorePocketType.Main;
            var maxAmount = configuration.Value.MaxItemAmount;
            var merges = new List<Merge>();
            if (slot == null && stackable)
            {
                var stacks = Values.Where(i => i.ItemInstance.ItemVNum.Equals(newItem.ItemInstance.ItemVNum)
                    && (i.ItemInstance.Amount < maxAmount)).ToList();
                foreach (var stack in stacks)
                {
                    if (amount <= 0)
                    {
                        break;
                    }

                    var total = stack.ItemInstance.Amount + amount;
                    var becomes = (short)Math.Min(total, maxAmount);
                    amount = (short)(total - becomes);
                    merges.Add(new Merge(stack, stack.ItemInstance.Amount, becomes));
                }

                if (amount <= 0)
                {
                    return new Placement(newItem, pocket, merges, [], true);
                }

                var chunks = (amount + maxAmount - 1) / maxAmount;
                var freeSlots = FreeSlots(pocket).Take(chunks).ToList();
                if (freeSlots.Count < chunks)
                {
                    return new Placement(newItem, pocket, [], [], false);
                }

                var newStacks = new List<NewStack>();
                foreach (var freeSlot in freeSlots)
                {
                    var chunk = (short)Math.Min(amount, maxAmount);
                    amount -= chunk;
                    newStacks.Add(new NewStack(newStacks.Count == 0 ? newItem : SplitOff(newItem), freeSlot, chunk));
                }

                return new Placement(newItem, pocket, merges, newStacks, true);
            }

            var target = slot ?? (pocket == NoscorePocketType.Wear
                ? this.LoadBySlotAndType(newItem.ItemInstance.Item.EquipmentSlot, NoscorePocketType.Wear) == null
                    ? (short?)newItem.ItemInstance.Item.EquipmentSlot
                    : null
                : GetFreeSlot(pocket));
            if (!target.HasValue)
            {
                return new Placement(newItem, pocket, [], [], false);
            }

            if (target < 0 || target >= GetMaxSlots(pocket) || (stackable && amount > maxAmount)
                || this.Any(s => (s.Value.Slot == target) && (s.Value.Type == pocket)))
            {
                return null;
            }

            if ((pocket == NoscorePocketType.Specialist) && !(newItem.ItemInstance is SpecialistInstance))
            {
                logger.LogError(logLanguage[LogLanguageKey.SPECIALIST_POCKET_REFUSED_ITEM], newItem.ItemInstance.ItemVNum);
                return null;
            }

            if (((pocket == NoscorePocketType.Equipment) || (pocket == NoscorePocketType.Wear))
                && newItem.ItemInstance is not (WearableInstance or SpecialistInstance or BoxInstance))
            {
                logger.LogError(logLanguage[LogLanguageKey.EQUIPMENT_POCKET_REFUSED_ITEM], newItem.ItemInstance.ItemVNum, pocket);
                return null;
            }

            return new Placement(newItem, pocket, merges, [new NewStack(newItem, target.Value, amount)], true);
        }

        private static InventoryItemInstance SplitOff(InventoryItemInstance item)
        {
            var copy = (IItemInstance)item.ItemInstance.Clone();
            copy.Id = Guid.NewGuid();
            return new InventoryItemInstance(copy) { Id = Guid.NewGuid(), CharacterId = item.CharacterId };
        }

        private List<InventoryItemInstance> Commit(Placement placement)
        {
            var changed = new List<InventoryItemInstance>();
            foreach (var merge in placement.Merges)
            {
                merge.Stack.ItemInstance.Amount = merge.Becomes;
                changed.Add(merge.Stack);
            }

            var item = placement.Item;
            item.Type = placement.Pocket;
            item.ItemInstance.Amount = 0;
            foreach (var stack in placement.NewStacks)
            {
                stack.Entry.Type = placement.Pocket;
                stack.Entry.Slot = stack.Slot;
                stack.Entry.ItemInstance.Amount = stack.Amount;
                this[stack.Entry.ItemInstanceId] = stack.Entry;
                changed.Add(stack.Entry);
            }

            return changed;
        }

        private void Undo(Placement placement)
        {
            foreach (var merge in placement.Merges)
            {
                merge.Stack.ItemInstance.Amount = merge.Was;
            }

            foreach (var stack in placement.NewStacks)
            {
                TryRemove(stack.Entry.ItemInstanceId, out _);
            }

            var item = placement.Item;
            item.ItemInstance.Amount = placement.Amount;
            item.Type = placement.PocketBefore;
            item.Slot = placement.SlotBefore;
        }

        public InventoryItemInstance? DeleteById(Guid id)
        {
            lock (_gate)
            {
                return TryGetValue(id, out var inv) ? Delete(inv) : null;
            }
        }

        public InventoryItemInstance LoadByItemInstanceId(Guid id)
        {
            return this[id];
        }

        public InventoryItemInstance? DeleteFromTypeAndSlot(NoscorePocketType type, short slot)
        {
            lock (_gate)
            {
                var inv = this.Select(s => s.Value).FirstOrDefault(i => i.Slot.Equals(slot) && i.Type.Equals(type));
                if (inv != null)
                {
                    return Delete(inv);
                }

                logger.LogError(logLanguage[LogLanguageKey.INVENTORY_SLOT_EMPTY], slot, type);
                return null;
            }
        }

        // Returns what is left in the slot: null once the item is gone.
        private InventoryItemInstance? Delete(InventoryItemInstance inv) =>
            TryRemove(inv.ItemInstanceId, out var value) ? null : value;

        public InventoryItemInstance? MoveInPocket(short sourceSlot, NoscorePocketType sourceType,
            NoscorePocketType targetType)
        {
            return MoveInPocket(sourceSlot, sourceType, targetType, null, false);
        }

        public InventoryItemInstance? MoveInPocket(short sourceSlot, NoscorePocketType sourceType,
            NoscorePocketType targetType,
            short? targetSlot, bool swap)
        {
            lock (_gate)
            {
                if ((sourceSlot == targetSlot) && (sourceType == targetType))
                {
                    logger.LogError(logLanguage[LogLanguageKey.ITEM_MOVED_ONTO_ITSELF], sourceSlot, sourceType);
                    return null;
                }

                var sourceInstance = LoadBySlotAndType(sourceSlot, sourceType);
                if (sourceInstance == null
                    || (sourceInstance.ItemInstance is not (WearableInstance or SpecialistInstance)
                        && !IsMountBetweenBags(sourceInstance, targetType)))
                {
                    logger.LogError(logLanguage[LogLanguageKey.ITEM_CANNOT_CHANGE_POCKET], sourceSlot, sourceType);
                    return null;
                }

                if (sourceInstance.ItemInstance is WearableInstance && (targetType != NoscorePocketType.Equipment) &&
                    (targetType != NoscorePocketType.Costume) && !IsWorn(targetType))
                {
                    logger.LogError(logLanguage[LogLanguageKey.ITEM_REFUSED_BY_POCKET], sourceInstance.ItemInstance.ItemVNum, targetType);
                    return null;
                }

                if (sourceInstance.ItemInstance is SpecialistInstance && (targetType != NoscorePocketType.Equipment) &&
                    (targetType != NoscorePocketType.Specialist) && !IsWorn(targetType))
                {
                    logger.LogError(logLanguage[LogLanguageKey.ITEM_REFUSED_BY_POCKET], sourceInstance.ItemInstance.ItemVNum, targetType);
                    return null;
                }

                if (targetSlot.HasValue)
                {
                    if (targetSlot.Value < 0 || targetSlot.Value >= GetMaxSlots(targetType))
                    {
                        logger.LogError(logLanguage[LogLanguageKey.ITEM_REFUSED_BY_POCKET], sourceInstance.ItemInstance.ItemVNum, targetType);
                        return null;
                    }

                    var targetInstance = LoadBySlotAndType(targetSlot.Value, targetType);

                    if (swap && (targetInstance != null))
                    {
                        // swap

                        sourceInstance.Slot = targetSlot.Value;
                        sourceInstance.Type = targetType;

                        targetInstance.Slot = sourceSlot;
                        targetInstance.Type = sourceType;
                    }
                    else if (targetInstance == null)
                    {
                        sourceInstance.Slot = targetSlot.Value;
                        sourceInstance.Type = targetType;
                    }
                    else
                    {
                        logger.LogError(logLanguage[LogLanguageKey.ITEM_SLOT_TAKEN], targetSlot.Value, targetType);
                        return null;
                    }

                    return sourceInstance;
                }

                // check for free target slot
                short? nextFreeSlot;
                if (IsWorn(targetType))
                {
                    nextFreeSlot =
                        this.LoadBySlotAndType(sourceInstance.ItemInstance.Item.EquipmentSlot, targetType) == null
                            ? (short)sourceInstance.ItemInstance.Item.EquipmentSlot
                            : null;
                }
                else
                {
                    nextFreeSlot = GetFreeSlot(targetType);
                }

                if (nextFreeSlot.HasValue)
                {
                    sourceInstance.Type = targetType;
                    sourceInstance.Slot = nextFreeSlot.Value;
                }
                else
                {
                    return null;
                }

                return sourceInstance;
            }
        }

        private static bool IsMountBetweenBags(InventoryItemInstance source, NoscorePocketType targetType) =>
            source.ItemInstance.Item.Type == NoscorePocketType.Mount
            && source.Type is NoscorePocketType.Mount or NoscorePocketType.Main
            && targetType is NoscorePocketType.Mount or NoscorePocketType.Main;

        private static bool IsWorn(NoscorePocketType type) => type is NoscorePocketType.Wear
            or NoscorePocketType.FirstPartnerInventory or NoscorePocketType.SecondPartnerInventory
            or NoscorePocketType.ThirdPartnerInventory;

        public bool TryMoveItem(NoscorePocketType sourcetype, short sourceSlot, short amount, short destinationSlot,
            out InventoryItemInstance? sourcePocket, out InventoryItemInstance? destinationPocket)
        {
            lock (_gate)
            {
                // load source and destination slots
                sourcePocket = LoadBySlotAndType(sourceSlot, sourcetype);
                destinationPocket = LoadBySlotAndType(destinationSlot, sourcetype);

                if ((sourceSlot == destinationSlot) || (amount <= 0)
                    || (destinationSlot < 0) || (destinationSlot >= GetMaxSlots(sourcetype)))
                {
                    return false;
                }

                // check if the item is a palced MinilandObject
                if (sourcetype == NoscorePocketType.Miniland)
                {
                    //todo test this (looks weird)
                    if (sourcePocket?.ItemInstance is MinilandObjectDto || destinationPocket?.ItemInstance is MinilandObjectDto)
                    {
                        return false;
                    }
                }

                if ((sourcePocket != null) && (amount <= sourcePocket.ItemInstance?.Amount))
                {
                    switch (destinationPocket)
                    {
                        case null when sourcePocket.ItemInstance.Amount == amount:
                            sourcePocket.Slot = destinationSlot;
                            break;
                        case null:
                            var itemDest = (IItemInstance)sourcePocket.ItemInstance.Clone();
                            itemDest.Amount = amount;
                            itemDest.Id = Guid.NewGuid();
                            if (AddItemToPocket(new InventoryItemInstance(itemDest)
                                {
                                    Id = Guid.NewGuid(),
                                    CharacterId = sourcePocket.CharacterId,
                                    Slot = sourcePocket.Slot,
                                    Type = sourcePocket.Type,
                                }, sourcetype, destinationSlot) is { Count: > 0 })
                            {
                                sourcePocket.ItemInstance.Amount -= amount;
                            }

                            break;
                        default:
                            if ((destinationPocket.ItemInstance?.ItemVNum == sourcePocket.ItemInstance.ItemVNum)
                                && ((sourcePocket.ItemInstance.Item.Type == NoscorePocketType.Main) ||
                                    (sourcePocket.ItemInstance.Item.Type == NoscorePocketType.Etc)))
                            {
                                if (destinationPocket.ItemInstance.Amount + amount > configuration.Value.MaxItemAmount)
                                {
                                    var saveItemCount = destinationPocket.ItemInstance.Amount;
                                    destinationPocket.ItemInstance.Amount = configuration.Value.MaxItemAmount;
                                    sourcePocket.ItemInstance.Amount =
                                        (short)(saveItemCount + sourcePocket.ItemInstance.Amount -
                                            configuration.Value.MaxItemAmount);
                                }
                                else
                                {
                                    destinationPocket.ItemInstance.Amount += amount;
                                    sourcePocket.ItemInstance.Amount -= amount;

                                    // item with amount of 0 should be removed
                                    if (sourcePocket.ItemInstance.Amount == 0)
                                    {
                                        DeleteFromTypeAndSlot(sourcePocket.Type, sourcePocket.Slot);
                                    }
                                }
                            }
                            else
                            {
                                // add and remove save pocket
                                destinationPocket = TakeItem(destinationPocket.Slot, destinationPocket.Type);
                                if (destinationPocket == null)
                                {
                                    return true;
                                }

                                destinationPocket.Slot = sourceSlot;
                                destinationPocket.Type = sourcetype;
                                sourcePocket = TakeItem(sourcePocket.Slot, sourcePocket.Type);
                                if (sourcePocket == null)
                                {
                                    return true;
                                }

                                sourcePocket.Slot = destinationSlot;
                                this[destinationPocket.ItemInstanceId] = destinationPocket;
                                this[sourcePocket.ItemInstanceId] = sourcePocket;
                            }

                            break;
                    }
                }

                sourcePocket = LoadBySlotAndType(sourceSlot, sourcetype);
                destinationPocket = LoadBySlotAndType(destinationSlot, sourcetype);
                return true;
            }
        }

        public bool EnoughPlace(List<IItemInstance> itemInstances, NoscorePocketType type)
        {
            lock (_gate)
            {
                var place = new Dictionary<NoscorePocketType, int>();
                foreach (var itemGroup in itemInstances.GroupBy(s => s.ItemVNum))
                {
                    var itemList = this.Select(s => s.Value).Where(i => i.Type == type).ToList();
                    if (!place.ContainsKey(type))
                    {
                        place.Add(type, GetMaxSlots(type) - itemList.Count);
                    }

                    var amount = itemGroup.Sum(s => s.Amount);
                    var rest = amount % (type == NoscorePocketType.Equipment ? 1 : configuration.Value.MaxItemAmount);
                    var newSlotNeeded = itemList.Where(s => s.ItemInstance?.ItemVNum == itemGroup.Key)
                        .Sum(s => configuration.Value.MaxItemAmount - s.ItemInstance?.Amount) <= rest;
                    place[type] -= amount / (type == NoscorePocketType.Equipment ? 1 : configuration.Value.MaxItemAmount) +
                        (newSlotNeeded ? 1 : 0);

                    if (place[type] < 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public InventoryItemInstance? RemoveItemAmountFromInventory(short amount, Guid id)
        {
            lock (_gate)
            {
                if (amount <= 0)
                {
                    return null;
                }

                if (TryGetValue(id, out var inv) && inv.ItemInstance != null)
                {
                    inv.ItemInstance.Amount -= amount;
                    if (inv.ItemInstance.Amount <= 0)
                    {
                        return TryRemove(inv.ItemInstanceId, out _) ? null : inv;
                    }

                    return inv;
                }

                logger.LogError(logLanguage[LogLanguageKey.INVENTORY_ITEM_NOT_FOUND], id);
                return null;
            }
        }

        public bool HasRoomFor(IItemInstance itemInstance) => HasRoomFor([itemInstance]);

        public bool HasRoomFor(IReadOnlyCollection<IItemInstance> itemInstances)
        {
            lock (_gate)
            {
                return itemInstances.GroupBy(i => i.Item.Type).All(pocket =>
                {
                    var slotsNeeded = pocket.Key is NoscorePocketType.Etc or NoscorePocketType.Main
                        ? pocket.GroupBy(i => i.ItemVNum).Sum(g => SlotsToStack(g.Key, g.Sum(i => i.Amount)))
                        : pocket.Count();
                    return slotsNeeded <= GetMaxSlots(pocket.Key) - CountItemInAnPocket(pocket.Key);
                });
            }
        }

        private int SlotsToStack(short vnum, int amount)
        {
            var maxAmount = configuration.Value.MaxItemAmount;
            var stackRoom = this.Select(s => s.Value)
                .Where(i => i.ItemInstance?.ItemVNum == vnum && i.ItemInstance.Amount < maxAmount)
                .Sum(i => maxAmount - i.ItemInstance.Amount);
            var overflow = amount - stackRoom;
            return overflow <= 0 ? 0 : (overflow + maxAmount - 1) / maxAmount;
        }

        public bool TryConsume(InventoryItemInstance source, short amount)
        {
            lock (_gate)
            {
                if (amount <= 0
                    || !TryGetValue(source.ItemInstanceId, out var inv)
                    || inv.ItemInstance == null
                    || inv.ItemInstance.Amount < amount)
                {
                    return false;
                }

                inv.ItemInstance.Amount -= amount;
                if (inv.ItemInstance.Amount == 0)
                {
                    TryRemove(inv.ItemInstanceId, out _);
                }

                return true;
            }
        }

        public (List<InventoryItemInstance> Restored, short Left) RestoreConsumed(InventoryItemInstance source, short amount)
        {
            lock (_gate)
            {
                if (TryGetValue(source.ItemInstanceId, out var inv))
                {
                    var room = Math.Max(0, configuration.Value.MaxItemAmount - inv.ItemInstance.Amount);
                    var taken = (short)Math.Min(amount, room);
                    inv.ItemInstance.Amount += taken;
                    List<InventoryItemInstance> restored = taken > 0 ? [inv] : [];
                    var rest = (short)(amount - taken);
                    if (rest == 0)
                    {
                        return (restored, 0);
                    }

                    var split = SplitOff(inv);
                    split.Type = inv.Type;
                    split.ItemInstance.Amount = rest;
                    var placed = AddItemToPocket(split, inv.Type) ?? [];
                    return ([.. restored, .. placed], placed.Count > 0 ? (short)0 : rest);
                }

                source.ItemInstance.Amount = amount;
                var back = AddItemToPocket(source, source.Type, source.Slot) is { Count: > 0 } own
                    ? own
                    : AddItemToPocket(source, source.Type) ?? [];
                return (back, back.Count > 0 ? (short)0 : amount);
            }
        }

        public short? GetFreeSlot(NoscorePocketType type)
        {
            lock (_gate)
            {
                return FreeSlots(type).Select(slot => (short?)slot).FirstOrDefault();
            }
        }

        private IEnumerable<short> FreeSlots(NoscorePocketType type)
        {
            var maxSlots = GetMaxSlots(type);
            var occupiedSlots = new HashSet<short>(
                this.Select(s => s.Value).Where(i => i.Type == type).Select(i => i.Slot));
            for (short slot = 0; slot < maxSlots; slot++)
            {
                if (!occupiedSlots.Contains(slot))
                {
                    yield return slot;
                }
            }
        }

        public bool HasFreeSlot(NoscorePocketType slotType) => GetFreeSlot(slotType).HasValue;

        public IEnumerable<InventoryItemInstance> GetAllBySlotType(NoscorePocketType slotType) =>
            this.Select(s => s.Value).Where(i => i.Type == slotType);

        public IEnumerable<InventoryItemInstance> GetAll() => this.Select(s => s.Value);

        InventoryItemInstance? ISlotBasedStorage<InventoryItemInstance, NoscorePocketType>.GetBySlot(short slot, NoscorePocketType slotType) =>
            LoadBySlotAndType(slot, slotType);

        InventoryItemInstance? ISlotBasedStorage<InventoryItemInstance, NoscorePocketType>.GetById(Guid id) =>
            LoadByItemInstanceId(id);

        int ISlotBasedStorage<InventoryItemInstance, NoscorePocketType>.CountBySlotType(NoscorePocketType slotType) =>
            CountItemInAnPocket(slotType);

        List<InventoryItemInstance>? IMutableSlotBasedStorage<InventoryItemInstance, NoscorePocketType>.AddItem(
            InventoryItemInstance item, NoscorePocketType? slotType, short? slot) =>
            AddItemToPocket(item, slotType, slot);

        InventoryItemInstance? IMutableSlotBasedStorage<InventoryItemInstance, NoscorePocketType>.RemoveBySlot(
            short slot, NoscorePocketType slotType) =>
            DeleteFromTypeAndSlot(slotType, slot);

        InventoryItemInstance? IMutableSlotBasedStorage<InventoryItemInstance, NoscorePocketType>.RemoveById(Guid id) =>
            DeleteById(id);

        InventoryItemInstance? IMutableSlotBasedStorage<InventoryItemInstance, NoscorePocketType>.RemoveItemAmount(
            short amount, Guid id) =>
            RemoveItemAmountFromInventory(amount, id);

        InventoryItemInstance? IMutableSlotBasedStorage<InventoryItemInstance, NoscorePocketType>.MoveItem(
            short sourceSlot, NoscorePocketType sourceType, NoscorePocketType targetType, short? targetSlot, bool swap) =>
            MoveInPocket(sourceSlot, sourceType, targetType, targetSlot, swap);

        InventoryItemInstance IDictionary<Guid, InventoryItemInstance>.this[Guid key]
        {
            get => this[key];
            set
            {
                lock (_gate)
                {
                    this[key] = value;
                }
            }
        }

        void IDictionary<Guid, InventoryItemInstance>.Add(Guid key, InventoryItemInstance value)
        {
            lock (_gate)
            {
                if (!TryAdd(key, value))
                {
                    throw new ArgumentException(null, nameof(key));
                }
            }
        }

        bool IDictionary<Guid, InventoryItemInstance>.Remove(Guid key)
        {
            lock (_gate)
            {
                return TryRemove(key, out _);
            }
        }

        void ICollection<KeyValuePair<Guid, InventoryItemInstance>>.Add(KeyValuePair<Guid, InventoryItemInstance> item) =>
            ((IDictionary<Guid, InventoryItemInstance>)this).Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<Guid, InventoryItemInstance>>.Remove(KeyValuePair<Guid, InventoryItemInstance> item)
        {
            lock (_gate)
            {
                return TryRemove(item);
            }
        }

        void ICollection<KeyValuePair<Guid, InventoryItemInstance>>.Clear()
        {
            lock (_gate)
            {
                Clear();
            }
        }

        private InventoryItemInstance? TakeItem(short slot, NoscorePocketType type)
        {
            var itemInstance = this.Select(s => s.Value).SingleOrDefault(i => (i.Slot == slot) && (i.Type == type));
            if (itemInstance == null)
            {
                return null;
            }

            TryRemove(itemInstance.ItemInstanceId, out _);
            return itemInstance;
        }

        public void SortPocket(NoscorePocketType pocket)
        {
            lock (_gate)
            {
                var pocketItems = this.Select(s => s.Value).Where(i => i.Type == pocket).ToList();
                var sorted = (pocket switch
                {
                    NoscorePocketType.Specialist => pocketItems
                        .OrderBy(i => i.ItemInstance.Item.LevelJobMinimum)
                        .ThenBy(i => i.ItemInstance.ItemVNum),
                    NoscorePocketType.Costume => pocketItems.OrderBy(i => i.ItemInstance.ItemVNum),
                    _ => BagSortOrder.Sort(pocket, pocketItems)
                }).ToList();
                for (short slot = 0; slot < sorted.Count; slot++)
                {
                    sorted[slot].Slot = slot;
                }
            }
        }
    }
}
