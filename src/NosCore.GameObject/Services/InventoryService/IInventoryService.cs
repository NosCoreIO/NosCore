//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemStorage;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.InventoryService
{
    public interface IInventoryService : IDictionary<Guid, InventoryItemInstance>,
        IMutableSlotBasedStorage<InventoryItemInstance, NoscorePocketType>
    {
        Dictionary<NoscorePocketType, byte> Expensions { get; set; }

        List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem);
        List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem, NoscorePocketType? type);

        List<InventoryItemInstance>?
            AddItemToPocket(InventoryItemInstance newItem, NoscorePocketType? type, short? slot);

        // Places every item or none of them: null when they do not all fit.
        List<InventoryItemInstance>? AddItemsToPocket(IReadOnlyCollection<InventoryItemInstance> newItems);

        bool CanAddItem(short itemVnum);
        bool HasRoomFor(IItemInstance itemInstance);
        bool HasRoomFor(IReadOnlyCollection<IItemInstance> itemInstances);
        int CountItemInAnPocket(NoscorePocketType inv);
        InventoryItemInstance? DeleteById(Guid id);
        InventoryItemInstance? DeleteFromTypeAndSlot(NoscorePocketType type, short slot);
        InventoryItemInstance? LoadByItemInstanceId(Guid id);
        InventoryItemInstance? LoadBySlotAndType(short slot, NoscorePocketType type);

        InventoryItemInstance? MoveInPocket(short sourceSlot, NoscorePocketType sourceType,
            NoscorePocketType targetType);

        InventoryItemInstance? MoveInPocket(short sourceSlot, NoscorePocketType sourceType, NoscorePocketType targetType,
            short? targetSlot, bool swap);

        InventoryItemInstance? RemoveItemAmountFromInventory(short amount, Guid id);
        bool TryConsume(InventoryItemInstance source, short amount);
        // Returns the entries that now hold it, which may not be its old slot; empty when it fits nowhere.
        List<InventoryItemInstance> RestoreConsumed(InventoryItemInstance source, short amount);

        // Takes the amount from the source and places every item, or does neither: null when the
        // source no longer holds the amount, empty when the items do not all fit.
        List<InventoryItemInstance>? ConsumeAndAdd(InventoryItemInstance source, short amount,
            IReadOnlyCollection<InventoryItemInstance> newItems);

        void SortPocket(NoscorePocketType pocket);
    }
}
