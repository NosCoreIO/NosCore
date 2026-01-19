//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemStorage;
using System;

namespace NosCore.GameObject.Services.InventoryService
{
    public class InventoryItemInstance : InventoryItemInstanceDto, ISlotItem<NoscorePocketType>
    {
        public InventoryItemInstance(IItemInstance itemInstance)
        {
            ItemInstance = itemInstance;
            ItemInstanceId = itemInstance.Id;
        }

        public IItemInstance ItemInstance { get; set; }

        IItemInstance? ISlotItem.ItemInstance => ItemInstance;

        NoscorePocketType ISlotItem<NoscorePocketType>.SlotType
        {
            get => Type;
            set => Type = value;
        }

        public static InventoryItemInstance Create(IItemInstance it, long characterId)
        {
            return Create(it, characterId, null);
        }

        public static InventoryItemInstance Create(IItemInstance it, long characterId,
            InventoryItemInstanceDto? inventoryItemInstance)
        {
            return new InventoryItemInstance(it)
            {
                Id = inventoryItemInstance?.Id ?? Guid.NewGuid(),
                CharacterId = characterId,
                Slot = inventoryItemInstance?.Slot ?? 0,
                Type = inventoryItemInstance?.Type ?? it.Item.Type
            };
        }
    }
}
