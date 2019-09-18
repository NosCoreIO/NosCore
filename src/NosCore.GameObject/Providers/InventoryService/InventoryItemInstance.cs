using NosCore.GameObject.Providers.ItemProvider.Item;
using System;
using NosCore.Data.Dto;

namespace NosCore.GameObject.Providers.InventoryService
{
    public class InventoryItemInstance : InventoryItemInstanceDto
    {
        public IItemInstance ItemInstance { get; set; }
        public static InventoryItemInstance Create(IItemInstance it, long characterId) => Create(it, characterId, null);
        public static InventoryItemInstance Create(IItemInstance it, long characterId, InventoryItemInstanceDto inventoryItemInstance)
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