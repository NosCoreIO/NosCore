//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.InventoryService
{
    // The order the inventory sort leaves the equipment, main and etc pockets in.
    public static class BagSortOrder
    {
        // Only these slots have been seen in a sorted equipment pocket; the others come after them.
        private static readonly EquipmentType[] EquipmentSlots =
        [
            EquipmentType.MainWeapon, EquipmentType.SecondaryWeapon, EquipmentType.Hat, EquipmentType.Armor,
            EquipmentType.Mask, EquipmentType.Gloves, EquipmentType.Boots, EquipmentType.Necklace,
            EquipmentType.Bracelet, EquipmentType.Fairy, EquipmentType.Amulet
        ];

        public static IOrderedEnumerable<InventoryItemInstance> Sort(NoscorePocketType pocket,
            IEnumerable<InventoryItemInstance> items) =>
            (pocket == NoscorePocketType.Equipment
                ? items.OrderBy(i => EquipmentRank(i.ItemInstance.Item))
                    .ThenBy(i => i.ItemInstance.Item.ItemType)
                    .ThenByDescending(i => i.ItemInstance.Item.LevelMinimum)
                : items.OrderBy(i => i.ItemInstance.Item.ItemType))
            .ThenByDescending(i => i.ItemInstance.ItemVNum)
            .ThenBy(i => i.Slot);

        private static int EquipmentRank(Item item)
        {
            if (item.ItemType is not (ItemType.Weapon or ItemType.Armor or ItemType.Fashion or ItemType.Jewelery
                or ItemType.Specialist))
            {
                return int.MaxValue;
            }

            var rank = Array.IndexOf(EquipmentSlots, item.EquipmentSlot);
            return rank >= 0 ? rank : EquipmentSlots.Length + (int)item.EquipmentSlot;
        }
    }
}
