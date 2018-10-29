//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NosCore.Configuration;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Services.Inventory
{
    public class InventoryService : ConcurrentDictionary<Guid, IItemInstance>, IInventoryService
    {
        private readonly List<Item> _items;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        public InventoryService(List<Item> items, WorldConfiguration configuration)
        {
            _items = items;
            Configuration = configuration;
        }

        private WorldConfiguration Configuration { get; }

        public bool IsExpanded { get; set; }

        public T LoadBySlotAndType<T>(short slot, PocketType type) where T : IItemInstance
        {
            T retItem = default(T);
            try
            {
                retItem = (T)this.Select(s => s.Value)
                    .SingleOrDefault(i => i is T && i.Slot == slot && i.Type == type);
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.Error(ioEx.Message, ioEx);
                var isFirstItem = true;
                foreach (var item in this.Select(s => s.Value).Where(i => i is T && i.Slot == slot && i.Type == type))
                {
                    if (isFirstItem)
                    {
                        retItem = (T)item;
                        isFirstItem = false;
                        continue;
                    }

                    var iteminstance = this.Select(s => s.Value).FirstOrDefault(i =>
                        i != null && i.GetType() == typeof(T) && i.Slot == slot && i.Type == type);
                    if (iteminstance != null)
                    {
                        TryRemove(iteminstance.Id, out var value);
                    }
                }
            }

            return retItem;
        }

        public bool CanAddItem(short itemVnum)
        {
            var type = _items.Find(item => item.VNum == itemVnum).Type;
            return GetFreeSlot(type).HasValue;
        }

        public int CountItem(int itemVNum)
        {
            return this.Select(s => s.Value).Where(s => s.ItemVNum == itemVNum).Sum(i => i.Amount);
        }

        public int CountItemInAnPocket(PocketType inv)
        {
            return this.Count(s => s.Value.Type == inv);
        }

        public List<IItemInstance> AddItemToPocket(IItemInstance newItem, PocketType? type = null, short? slot = null)
        {
            var invlist = new List<IItemInstance>();

            // override type if necessary
            if (type.HasValue)
            {
                newItem.Type = type.Value;
            }

            // check if item can be stapled
            if (slot == null && newItem.Type != PocketType.Bazaar
                && (newItem.Item.Type == PocketType.Etc || newItem.Item.Type == PocketType.Main))
            {
                var slotNotFull = this.ToList().Select(s => s.Value).Where(i =>
                    i.Type != PocketType.Bazaar && i.Type != PocketType.PetWarehouse
                    && i.Type != PocketType.Warehouse && i.Type != PocketType.FamilyWareHouse
                    && i.ItemVNum.Equals(newItem.ItemVNum) && i.Amount < Configuration.MaxItemAmount);
                var freeslot = Configuration.BackpackSize + ((IsExpanded ? 1 : 0) * 12)
                    - this.Count(s => s.Value.Type == newItem.Type);
                IEnumerable<IItemInstance> itemInstances = slotNotFull as IList<IItemInstance> ?? slotNotFull.ToList();
                if (newItem.Amount <= (freeslot * Configuration.MaxItemAmount)
                    + itemInstances.Sum(s => Configuration.MaxItemAmount - s.Amount))
                {
                    foreach (var slotToAdd in itemInstances)
                    {
                        var max = slotToAdd.Amount + newItem.Amount;
                        max = max > Configuration.MaxItemAmount ? Configuration.MaxItemAmount : max;
                        newItem.Amount = (short)(slotToAdd.Amount + newItem.Amount - max);
                        slotToAdd.Amount = (short)max;
                        invlist.Add(slotToAdd);
                    }
                }
            }

            if (newItem.Amount <= 0)
            {
                return invlist;
            }

            // create new item
            var freeSlot = newItem.Type == PocketType.Wear
                ? (LoadBySlotAndType<IItemInstance>((short)newItem.Item.EquipmentSlot, PocketType.Wear) == null
                    ? (short?)newItem.Item.EquipmentSlot
                    : null)
                : GetFreeSlot(newItem.Type);
            if (!slot.HasValue && !freeSlot.HasValue)
            {
                return invlist;
            }

            newItem.Slot = slot ?? freeSlot.Value;

            if (ContainsKey(newItem.Id))
            {
                var e = new InvalidOperationException("Cannot add the same ItemInstance twice to pocket.");
                _logger.Error(e.Message, e);
                return null;
            }

            if (this.Any(s => s.Value.Slot == newItem.Slot && s.Value.Type == newItem.Type)
                || newItem.Slot >= Configuration.BackpackSize + ((IsExpanded ? 1 : 0) * 12))
            {
                return null;
            }

            if (newItem.Type == PocketType.Specialist && !(newItem is SpecialistInstance))
            {
                var e = new InvalidOperationException(
                    "Cannot add an item of type Specialist without beeing a SpecialistInstance.");
                _logger.Error(e.Message, e);
            }

            if ((newItem.Type == PocketType.Equipment || newItem.Type == PocketType.Wear)
                && !(newItem is WearableInstance))
            {
                var e = new InvalidOperationException(
                    "Cannot add an item of type Equipment or Wear without beeing a WearableInstance.");
                _logger.Error(e.Message, e);
            }

            this[newItem.Id] = newItem;
            invlist.Add(newItem);

            return invlist;
        }

        public IItemInstance DeleteById(Guid id)
        {
            var inv = this[id];
            if (inv != null)
            {
                if (TryRemove(inv.Id, out var value))
                {
                    return null;
                }
                return value;
            }

            var e = new InvalidOperationException("Expected item wasn't deleted, Type or Slot did not match!");
            _logger.Error(e.Message, e);
            return null;
        }

        public T LoadByItemInstanceId<T>(Guid id) where T : IItemInstance
        {
            return (T)this[id];
        }

        public IItemInstance DeleteFromTypeAndSlot(PocketType type, short slot)
        {
            var inv = this.Select(s => s.Value).FirstOrDefault(i => i.Slot.Equals(slot) && i.Type.Equals(type));

            if (inv != null && type != PocketType.Bazaar)
            {
                if (TryRemove(inv.Id, out var value))
                {
                    return null;
                }
                return value;
            }

            var e = new InvalidOperationException("Expected item wasn't deleted, Type or Slot did not match!");
            _logger.Error(e.Message, e);
            return null;
        }

        public IItemInstance MoveInPocket(short sourceSlot, PocketType sourceType, PocketType targetType,
            short? targetSlot = null, bool wear = true)
        {
            if (sourceSlot == targetSlot && sourceType == targetType)
            {
                var e = new InvalidOperationException("SourceInstance can't be moved on the same spot");
                _logger.Error(e.Message, e);
                return null;
            }

            var sourceInstance = LoadBySlotAndType<IItemInstance>(sourceSlot, sourceType);
            if (!(sourceInstance is WearableInstance || sourceInstance is SpecialistInstance))
            {
                var e = new InvalidOperationException("SourceInstance can't be moved between pockets");
                _logger.Error(e.Message, e);
                return null;
            }

            switch (sourceInstance?.Item.ItemType)
            {
                case ItemType.Fashion when targetType != PocketType.Main && targetType != PocketType.Costume:
                case ItemType.Specialist when targetType != PocketType.Main && targetType != PocketType.Specialist:
                    var e = new InvalidOperationException("SourceInstance can't be moved to this Pocket");
                    _logger.Error(e.Message, e);
                    return null;
                default:
                    if (targetSlot.HasValue)
                    {
                        if (wear)
                        {
                            // swap
                            var targetInstance = LoadBySlotAndType<IItemInstance>(targetSlot.Value, targetType);

                            sourceInstance.Slot = targetSlot.Value;
                            sourceInstance.Type = targetType;

                            targetInstance.Slot = sourceSlot;
                            targetInstance.Type = sourceType;
                        }
                        else
                        {
                            // move source to target
                            var freeTargetSlot = GetFreeSlot(targetType);
                            if (!freeTargetSlot.HasValue)
                            {
                                return sourceInstance;
                            }

                            sourceInstance.Slot = freeTargetSlot.Value;
                            sourceInstance.Type = targetType;
                        }

                        return sourceInstance;
                    }

                    // check for free target slot
                    short? nextFreeSlot;
                    if (targetType == PocketType.Wear)
                    {
                        nextFreeSlot =
                            LoadBySlotAndType<IItemInstance>((short)sourceInstance.Item.EquipmentSlot, targetType) == null
                                ? (short)sourceInstance.Item.EquipmentSlot
                                : (short)-1;
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

        public void TryMoveItem(PocketType sourcetype, short sourceSlot, short amount, short destinationSlot,
            out IItemInstance sourcePocket, out IItemInstance destinationPocket)
        {
            // load source and destination slots
            sourcePocket = LoadBySlotAndType<IItemInstance>(sourceSlot, sourcetype);
            destinationPocket = LoadBySlotAndType<IItemInstance>(destinationSlot, sourcetype);

            if (sourceSlot == destinationSlot || amount == 0
                || destinationSlot > Configuration.BackpackSize + ((IsExpanded ? 1 : 0) * 12))
            {
                return;
            }

            if (sourcePocket != null && amount <= sourcePocket.Amount)
            {
                switch (destinationPocket)
                {
                    case null when sourcePocket.Amount == amount:
                        sourcePocket.Slot = destinationSlot;
                        break;
                    case null:
                        IItemInstance itemDest = (IItemInstance)sourcePocket.Clone();
                        sourcePocket.Amount -= amount;
                        itemDest.Amount = amount;
                        itemDest.Id = Guid.NewGuid();
                        AddItemToPocket(itemDest, sourcetype, destinationSlot);
                        break;
                    default:
                        if (destinationPocket.ItemVNum == sourcePocket.ItemVNum
                            && (sourcePocket.Item.Type == PocketType.Main || sourcePocket.Item.Type == PocketType.Etc))
                        {
                            if (destinationPocket.Amount + amount > Configuration.MaxItemAmount)
                            {
                                var saveItemCount = destinationPocket.Amount;
                                destinationPocket.Amount = Configuration.MaxItemAmount;
                                sourcePocket.Amount =
                                    (short)(saveItemCount + sourcePocket.Amount - Configuration.MaxItemAmount);
                            }
                            else
                            {
                                destinationPocket.Amount += amount;
                                sourcePocket.Amount -= amount;

                                // item with amount of 0 should be removed
                                if (sourcePocket.Amount == 0)
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
                                return;
                            }

                            destinationPocket.Slot = sourceSlot;
                            destinationPocket.Type = sourcetype;
                            sourcePocket = TakeItem(sourcePocket.Slot, sourcePocket.Type);
                            if (sourcePocket == null)
                            {
                                return;
                            }

                            sourcePocket.Slot = destinationSlot;
                            this[destinationPocket.Id] = destinationPocket;
                            this[sourcePocket.Id] = sourcePocket;
                        }

                        break;
                }
            }

            sourcePocket = LoadBySlotAndType<IItemInstance>(sourceSlot, sourcetype);
            destinationPocket = LoadBySlotAndType<IItemInstance>(destinationSlot, sourcetype);
        }

        private short? GetFreeSlot(PocketType type)
        {
            var backPack = IsExpanded ? 1 : 0;
            var itemInstanceSlotsByType = this.Select(s => s.Value).Where(i => i.Type == type).OrderBy(i => i.Slot)
                .Select(i => (int)i.Slot);
            IEnumerable<int> instanceSlotsByType =
                itemInstanceSlotsByType as int[] ?? itemInstanceSlotsByType.ToArray();
            var nextFreeSlot = instanceSlotsByType.Any()
                ? Enumerable
                    .Range(0, (type != PocketType.Miniland ? Configuration.BackpackSize + (backPack * 12) : 50) + 1)
                    .Except(instanceSlotsByType).FirstOrDefault()
                : 0;
            return (short?)nextFreeSlot
                < (type != PocketType.Miniland ? Configuration.BackpackSize + (backPack * 12) : 50)
                    ? (short?)nextFreeSlot : null;
        }

        //    public bool EnoughPlace(List<ItemInstance> itemInstances, int backPack)
        //    {
        //        var place = new Dictionary<PocketType, int>();
        //        foreach (var itemgroup in itemInstances.GroupBy(s => s.ItemVNum))
        //        {
        //            var type = itemgroup.First().Type;
        //            var listitem = this.Select(s => s.Value).Where(i => i.Type == type).ToList();
        //            if (!place.ContainsKey(type))
        //            {
        //                place.Add(type, (type != PocketType.Miniland ? Configuration.BackpackSize + backPack * 12 : 50) - listitem.Count);
        //            }

        //            var amount = itemgroup.Sum(s => s.Amount);
        //            var rest = amount % (type == PocketType.Equipment ? 1 : 99);
        //            var needanotherslot = listitem.Where(s => s.ItemVNum == itemgroup.Key).Sum(s => Configuration.MaxItemAmount - s.Amount) <= rest;
        //            place[type] -= (amount / (type == PocketType.Equipment ? 1 : 99)) + (needanotherslot ? 1 : 0);

        //            if (place[type] < 0)
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    }

        //    public IEnumerable<ItemInstance> RemoveItemAmount(int vnum, int amount = 1)
        //    {
        //        var remainingAmount = amount;

        //        foreach (var pocket in this.Select(s => s.Value).Where(s => s.ItemVNum == vnum && s.Type != PocketType.Wear && s.Type != PocketType.Bazaar && s.Type != PocketType.Warehouse && s.Type != PocketType.PetWarehouse && s.Type != PocketType.FamilyWareHouse).OrderBy(i => i.Slot))
        //        {
        //            if (remainingAmount > 0)
        //            {
        //                if (pocket.Amount > remainingAmount)
        //                {
        //                    // amount completely removed
        //                    pocket.Amount -= (short)remainingAmount;
        //                    remainingAmount = 0;
        //                }
        //                else
        //                {
        //                    // amount partly removed
        //                    remainingAmount -= pocket.Amount;
        //                    DeleteById(pocket.Id);
        //                }

        //                yield return pocket;
        //            }
        //            else
        //            {
        //                // amount to remove reached
        //                break;
        //            }
        //        }
        //    }

        //    public ItemInstance RemoveItemAmountFromPocket(short amount, Guid id)
        //    {

        //        var inv = this.Select(s => s.Value).FirstOrDefault(i => i.Id.Equals(id));

        //        if (inv == null)
        //        {
        //            return null;
        //        }
        //        inv.Amount -= amount;
        //        if (inv.Amount <= 0)
        //        {
        //            TryRemove(inv.Id, out _);
        //        }
        //        return inv;
        //    }

        //    public IEnumerable<ItemInstance> Reorder(ClientSession session, PocketType pocketType)
        //    {
        //        var itemsByPocketType = new List<ItemInstance>();
        //        switch (pocketType)
        //        {
        //            case PocketType.Costume:
        //                itemsByPocketType = this.Select(s => s.Value).Where(s => s.Type == PocketType.Costume).OrderBy(s => s.ItemVNum).ToList();
        //                break;

        //            case PocketType.Specialist:
        //                itemsByPocketType = this.Select(s => s.Value).Where(s => s.Type == PocketType.Specialist).OrderBy(s => s.Item.LevelJobMinimum).ToList();
        //                break;
        //        }

        //        short i = 0;

        //        foreach (var item in itemsByPocketType)
        //        {
        //            // remove item from pocket
        //            TryRemove(item.Id, out var value);
        //            yield return new ItemInstance(){Slot = item.Slot, Type = item.Type};
        //            // readd item to pocket
        //            item.Slot = i;
        //            yield return item;
        //            this[item.Id] = item;

        //            // increment slot
        //            i++;
        //        }
        //    }

        //private ItemInstance GetFreeSlot(IEnumerable<ItemInstance> slotfree)
        //{
        //    var pocketitemids = slotfree.Select(itemfree => itemfree.Id).ToList();
        //    return this.Select(s => s.Value).Where(i => pocketitemids.Contains(i.Id) && i.Type != PocketType.Wear && i.Type != PocketType.PetWarehouse && i.Type != PocketType.FamilyWareHouse && i.Type != PocketType.Warehouse && i.Type != PocketType.Bazaar).OrderBy(i => i.Slot).FirstOrDefault();
        //}

        private IItemInstance TakeItem(short slot, PocketType type)
        {
            var itemInstance = this.Select(s => s.Value).SingleOrDefault(i => i.Slot == slot && i.Type == type);
            if (itemInstance == null)
            {
                return null;
            }

            TryRemove(itemInstance.Id, out _);
            return itemInstance;
        }
    }
}