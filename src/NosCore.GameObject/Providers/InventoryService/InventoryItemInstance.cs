using System;
using NosCore.Data.Dto;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.Providers.InventoryService
{
    public class InventoryItemInstance : InventoryItemInstanceDto
    {
        public IItemInstance ItemInstance { get; set; }

        public static InventoryItemInstance Create(IItemInstance it, long characterId)
        {
            return Create(it, characterId, null);
        }

        public static InventoryItemInstance Create(IItemInstance it, long characterId,
            InventoryItemInstanceDto inventoryItemInstance)
        {
            return new InventoryItemInstance
            {
                Id = inventoryItemInstance?.Id ?? Guid.NewGuid(),
                CharacterId = characterId,
                ItemInstance = it,
                ItemInstanceId = it.Id,
                Slot = inventoryItemInstance?.Slot ?? 0,
                Type = inventoryItemInstance?.Type ?? it.Item.Type
            };
        }
    }
}