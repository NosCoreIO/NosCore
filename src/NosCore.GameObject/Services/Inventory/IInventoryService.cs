using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NosCore.GameObject.Item;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject
{
    public interface IInventoryService : IDictionary<Guid, ItemInstance>
    {
        bool IsExpanded { get; set; }

        List<ItemInstance> AddItemToPocket(ItemInstance newItem, PocketType? type = null, short? slot = null);
        bool CanAddItem(short itemVnum);
        int CountItem(int itemVNum);
        int CountItemInAnPocket(PocketType inv);
        ItemInstance DeleteById(Guid id);
        ItemInstance DeleteFromTypeAndSlot(PocketType type, short slot);
        T LoadByItemInstanceId<T>(Guid id) where T : ItemInstance;
        T LoadBySlotAndType<T>(short slot, PocketType type) where T : ItemInstance;
        ItemInstance MoveInPocket(short sourceSlot, PocketType sourceType, PocketType targetType, short? targetSlot = null, bool wear = true);
        void MoveItem(PocketType sourcetype, short sourceSlot, short amount, short destinationSlot, out ItemInstance sourcePocket, out ItemInstance destinationPocket);
    }
}