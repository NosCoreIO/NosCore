//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
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
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Providers.ItemProvider.Item;
using Serilog;

namespace NosCore.GameObject.Providers.InventoryService
{
    public class InventoryService : ConcurrentDictionary<Guid, IItemInstance>, IInventoryService
    {
        private readonly List<ItemDto> _items;
        private readonly ILogger _logger;
        private readonly WorldConfiguration _configuration;

        public InventoryService(List<ItemDto> items, WorldConfiguration configuration, ILogger logger)
        {
            _items = items;
            _configuration = configuration;
            _logger = logger;
        }


        public bool IsExpanded { get; set; }

        public T LoadBySlotAndType<T>(short slot, PocketType type) where T : IItemInstance
        {
            T retItem = default;
            try
            {
                retItem = (T) this.Select(s => s.Value)
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
                        retItem = (T) item;
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

        public List<IItemInstance> AddItemToPocket(IItemInstance newItem) => AddItemToPocket(newItem, null, null);

        public List<IItemInstance> AddItemToPocket(IItemInstance newItem, PocketType? type) =>
            AddItemToPocket(newItem, type, null);

        public List<IItemInstance> AddItemToPocket(IItemInstance newItem, PocketType? type, short? slot)
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
                    && i.ItemVNum.Equals(newItem.ItemVNum) && i.Amount < _configuration.MaxItemAmount);
                var freeslot = _configuration.BackpackSize + ((IsExpanded ? 1 : 0) * 12)
                    - this.Count(s => s.Value.Type == newItem.Type);
                IEnumerable<IItemInstance> itemInstances = slotNotFull as IList<IItemInstance> ?? slotNotFull.ToList();
                if (newItem.Amount <= (freeslot * _configuration.MaxItemAmount)
                    + itemInstances.Sum(s => _configuration.MaxItemAmount - s.Amount))
                {
                    foreach (var slotToAdd in itemInstances)
                    {
                        var max = slotToAdd.Amount + newItem.Amount;
                        max = max > _configuration.MaxItemAmount ? _configuration.MaxItemAmount : max;
                        newItem.Amount = (short) (slotToAdd.Amount + newItem.Amount - max);
                        slotToAdd.Amount = (short) max;
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
                ? (LoadBySlotAndType<IItemInstance>((short) newItem.Item.EquipmentSlot, PocketType.Wear) == null
                    ? (short?) newItem.Item.EquipmentSlot
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
                || newItem.Slot >= _configuration.BackpackSize + ((IsExpanded ? 1 : 0) * 12))
            {
                return null;
            }

            if (newItem.Type == PocketType.Specialist && !(newItem is SpecialistInstance))
            {
                var e = new InvalidOperationException(
                    "Cannot add an item of type Specialist without beeing a SpecialistInstance.");
                _logger.Error(e.Message, e);
                return null;
            }

            if ((newItem.Type == PocketType.Equipment || newItem.Type == PocketType.Wear)
                && !(newItem is WearableInstance) && !(newItem is SpecialistInstance))
            {
                var e = new InvalidOperationException(
                    "Cannot add an item of type Equipment or Wear without beeing a WearableInstance or a SpecialistInstance.");
                _logger.Error(e.Message, e);
                return null;
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
            return (T) this[id];
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

        public IItemInstance MoveInPocket(short sourceSlot, PocketType sourceType, PocketType targetType)
            => MoveInPocket(sourceSlot, sourceType, targetType, null, false);

        public IItemInstance MoveInPocket(short sourceSlot, PocketType sourceType, PocketType targetType,
            short? targetSlot, bool swap)
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

            if (sourceInstance is WearableInstance && targetType != PocketType.Equipment &&
                targetType != PocketType.Costume && targetType != PocketType.Wear)
            {
                var e = new InvalidOperationException("WearableInstance can't be move to this inventory");
                _logger.Error(e.Message, e);
                return null;
            }

            if (sourceInstance is SpecialistInstance && targetType != PocketType.Equipment &&
                targetType != PocketType.Specialist && targetType != PocketType.Wear)
            {
                var e = new InvalidOperationException("SpecialistInstance can't be move to this inventory");
                _logger.Error(e.Message, e);
                return null;
            }

            if (targetSlot.HasValue)
            {
                var targetInstance = LoadBySlotAndType<IItemInstance>(targetSlot.Value, targetType);

                if (swap && targetInstance != null)
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
                    var e = new InvalidOperationException("Source can not be swapped");
                    _logger.Error(e.Message, e);
                    return null;
                }

                return sourceInstance;
            }

            // check for free target slot
            short? nextFreeSlot;
            if (targetType == PocketType.Wear)
            {
                nextFreeSlot =
                    LoadBySlotAndType<IItemInstance>((short) sourceInstance.Item.EquipmentSlot, targetType) == null
                        ? (short) sourceInstance.Item.EquipmentSlot
                        : (short) -1;
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

        public bool TryMoveItem(PocketType sourcetype, short sourceSlot, short amount, short destinationSlot,
            out IItemInstance sourcePocket, out IItemInstance destinationPocket)
        {
            // load source and destination slots
            sourcePocket = LoadBySlotAndType<IItemInstance>(sourceSlot, sourcetype);
            destinationPocket = LoadBySlotAndType<IItemInstance>(destinationSlot, sourcetype);

            if (sourceSlot == destinationSlot || amount == 0
                || destinationSlot > _configuration.BackpackSize + ((IsExpanded ? 1 : 0) * 12))
            {
                return false;
            }

            if (sourcePocket != null && amount <= sourcePocket.Amount)
            {
                switch (destinationPocket)
                {
                    case null when sourcePocket.Amount == amount:
                        sourcePocket.Slot = destinationSlot;
                        break;
                    case null:
                        IItemInstance itemDest = (IItemInstance) sourcePocket.Clone();
                        sourcePocket.Amount -= amount;
                        itemDest.Amount = amount;
                        itemDest.Id = Guid.NewGuid();
                        AddItemToPocket(itemDest, sourcetype, destinationSlot);
                        break;
                    default:
                        if (destinationPocket.ItemVNum == sourcePocket.ItemVNum
                            && (sourcePocket.Item.Type == PocketType.Main || sourcePocket.Item.Type == PocketType.Etc))
                        {
                            if (destinationPocket.Amount + amount > _configuration.MaxItemAmount)
                            {
                                var saveItemCount = destinationPocket.Amount;
                                destinationPocket.Amount = _configuration.MaxItemAmount;
                                sourcePocket.Amount =
                                    (short) (saveItemCount + sourcePocket.Amount - _configuration.MaxItemAmount);
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
                            this[destinationPocket.Id] = destinationPocket;
                            this[sourcePocket.Id] = sourcePocket;
                        }

                        break;
                }
            }

            sourcePocket = LoadBySlotAndType<IItemInstance>(sourceSlot, sourcetype);
            destinationPocket = LoadBySlotAndType<IItemInstance>(destinationSlot, sourcetype);
            return true;
        }

        public bool EnoughPlace(List<IItemInstance> itemInstances)
        {
            var place = new Dictionary<PocketType, int>();
            foreach (var itemGroup in itemInstances.GroupBy(s => s.ItemVNum))
            {
                var type = itemGroup.First().Type;
                var itemList = this.Select(s => s.Value).Where(i => i.Type == type).ToList();
                if (!place.ContainsKey(type))
                {
                    place.Add(type,
                        (type != PocketType.Miniland ? _configuration.BackpackSize + Convert.ToInt16(IsExpanded) * 12
                            : 50) - itemList.Count);
                }

                var amount = itemGroup.Sum(s => s.Amount);
                var rest = amount % (type == PocketType.Equipment ? 1 : _configuration.MaxItemAmount);
                var newSlotNeeded = itemList.Where(s => s.ItemVNum == itemGroup.Key)
                    .Sum(s => _configuration.MaxItemAmount - s.Amount) <= rest;
                place[type] -= (amount / (type == PocketType.Equipment ? 1 : _configuration.MaxItemAmount)) +
                    (newSlotNeeded ? 1 : 0);

                if (place[type] < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public IItemInstance RemoveItemAmountFromInventory(short amount, Guid id)
        {
            var inv = this[id];
            if (inv != null)
            {
                inv.Amount -= amount;
                if (inv.Amount <= 0)
                {
                    return TryRemove(inv.Id, out _) ? null : inv;
                }

                return inv;
            }

            var e = new InvalidOperationException("Expected item wasn't deleted, Type or Slot did not match!");
            _logger.Error(e.Message, e);
            return null;
        }

        private short? GetFreeSlot(PocketType type)
        {
            var backPack = IsExpanded ? 1 : 0;
            var itemInstanceSlotsByType = this.Select(s => s.Value).Where(i => i.Type == type).OrderBy(i => i.Slot)
                .Select(i => (int) i.Slot);
            IEnumerable<int> instanceSlotsByType =
                itemInstanceSlotsByType as int[] ?? itemInstanceSlotsByType.ToArray();
            var nextFreeSlot = instanceSlotsByType.Any()
                ? Enumerable
                    .Range(0, (type != PocketType.Miniland ? _configuration.BackpackSize + (backPack * 12) : 50) + 1)
                    .Except(instanceSlotsByType).FirstOrDefault()
                : 0;
            return (short?) nextFreeSlot
                < (type != PocketType.Miniland ? _configuration.BackpackSize + (backPack * 12) : 50)
                    ? (short?) nextFreeSlot : null;
        }

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