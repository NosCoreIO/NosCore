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

using NosCore.GameObject.Services.ItemGenerationService.Item;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.ItemStorage
{
    public static class SlotStorageHelper
    {
        public static short? FindFreeSlot<TItem>(
            IEnumerable<TItem> items,
            Func<TItem, short> slotSelector,
            int maxSlots)
            where TItem : class
        {
            if (maxSlots <= 0 || maxSlots > short.MaxValue)
            {
                return null;
            }

            var occupiedSlots = new HashSet<short>(items.Select(slotSelector));

            for (short slot = 0; slot < maxSlots; slot++)
            {
                if (!occupiedSlots.Contains(slot))
                {
                    return slot;
                }
            }

            return null;
        }

        public static List<TItem> StackItems<TItem>(
            IEnumerable<TItem> existingItems,
            TItem newItem,
            Func<TItem, IItemInstance?> itemInstanceSelector,
            Action<TItem, short> setAmount,
            short maxItemAmount,
            int maxSlots,
            int currentCount)
            where TItem : class
        {
            var result = new List<TItem>();
            var newItemInstance = itemInstanceSelector(newItem);

            if (newItemInstance == null)
            {
                return result;
            }

            var slotNotFull = existingItems
                .Where(i =>
                {
                    var inst = itemInstanceSelector(i);
                    return inst != null &&
                           inst.ItemVNum == newItemInstance.ItemVNum &&
                           inst.Amount < maxItemAmount;
                })
                .ToList();

            var freeSlotCount = maxSlots - currentCount;
            var totalCapacity = freeSlotCount * maxItemAmount +
                                slotNotFull.Sum(s => maxItemAmount - (itemInstanceSelector(s)?.Amount ?? 0));

            if (newItemInstance.Amount > totalCapacity)
            {
                return result;
            }

            foreach (var existingSlot in slotNotFull)
            {
                var existingInstance = itemInstanceSelector(existingSlot);
                if (existingInstance == null || newItemInstance.Amount <= 0)
                {
                    continue;
                }

                var combinedAmount = existingInstance.Amount + newItemInstance.Amount;
                var maxAmount = combinedAmount > maxItemAmount ? maxItemAmount : combinedAmount;
                var remaining = (short)(existingInstance.Amount + newItemInstance.Amount - maxAmount);

                existingInstance.Amount = (short)maxAmount;
                newItemInstance.Amount = remaining;

                result.Add(existingSlot);
            }

            return result;
        }

        public static bool CanStack(IItemInstance? itemInstance)
        {
            if (itemInstance == null)
            {
                return false;
            }

            var itemType = itemInstance.Item?.Type;
            return itemType == Data.Enumerations.NoscorePocketType.Etc ||
                   itemType == Data.Enumerations.NoscorePocketType.Main;
        }

        public static bool ValidateEnoughPlace<TItem>(
            IEnumerable<TItem> existingItems,
            List<IItemInstance> itemsToAdd,
            Func<TItem, IItemInstance?> itemInstanceSelector,
            short maxItemAmount,
            int maxSlots,
            bool isStackable)
            where TItem : class
        {
            var existingList = existingItems.ToList();
            var currentCount = existingList.Count;
            var availableSlots = maxSlots - currentCount;

            foreach (var itemGroup in itemsToAdd.GroupBy(s => s.ItemVNum))
            {
                var amount = itemGroup.Sum(s => s.Amount);

                if (isStackable)
                {
                    var existingCapacity = existingList
                        .Where(s => itemInstanceSelector(s)?.ItemVNum == itemGroup.Key)
                        .Sum(s => maxItemAmount - (itemInstanceSelector(s)?.Amount ?? 0));

                    var needsNewSlots = amount - existingCapacity;
                    if (needsNewSlots > 0)
                    {
                        var slotsNeeded = (int)Math.Ceiling((double)needsNewSlots / maxItemAmount);
                        availableSlots -= slotsNeeded;
                    }
                }
                else
                {
                    availableSlots -= itemGroup.Count();
                }

                if (availableSlots < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static (TItem? Source, TItem? Destination) SwapItems<TItem>(
            TItem source,
            TItem destination,
            Action<TItem, short> setSlot)
            where TItem : class, ISlotItem
        {
            var sourceSlot = source.Slot;
            var destSlot = destination.Slot;

            setSlot(source, destSlot);
            setSlot(destination, sourceSlot);

            return (source, destination);
        }

        public static TItem? SplitItem<TItem>(
            TItem source,
            short amount,
            short destinationSlot,
            Func<IItemInstance, Guid, short, TItem> createNewItem)
            where TItem : class, ISlotItem
        {
            var sourceInstance = source.ItemInstance;
            if (sourceInstance == null || sourceInstance.Amount < amount)
            {
                return null;
            }

            var clonedInstance = (IItemInstance)sourceInstance.Clone();
            sourceInstance.Amount -= amount;
            clonedInstance.Amount = amount;
            clonedInstance.Id = Guid.NewGuid();

            return createNewItem(clonedInstance, Guid.NewGuid(), destinationSlot);
        }

        public static bool TryStackOntoExisting<TItem>(
            TItem source,
            TItem destination,
            short amount,
            short maxItemAmount,
            Action<TItem>? removeSource = null)
            where TItem : class, ISlotItem
        {
            var sourceInstance = source.ItemInstance;
            var destInstance = destination.ItemInstance;

            if (sourceInstance == null || destInstance == null)
            {
                return false;
            }

            if (sourceInstance.ItemVNum != destInstance.ItemVNum)
            {
                return false;
            }

            var totalAmount = destInstance.Amount + amount;
            if (totalAmount > maxItemAmount)
            {
                var overflow = totalAmount - maxItemAmount;
                destInstance.Amount = maxItemAmount;
                sourceInstance.Amount = (short)overflow;
            }
            else
            {
                destInstance.Amount = (short)totalAmount;
                sourceInstance.Amount -= amount;

                if (sourceInstance.Amount == 0)
                {
                    removeSource?.Invoke(source);
                }
            }

            return true;
        }
    }
}
