//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemStorage;
using NosCore.Shared.Enumerations;
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

        bool CanAddItem(short itemVnum);
        bool HasRoomFor(IItemInstance itemInstance);
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
        void RestoreConsumed(InventoryItemInstance source, short amount);

        IReadOnlyList<short> SortPocket(NoscorePocketType pocket, RegionType language);
    }
}
