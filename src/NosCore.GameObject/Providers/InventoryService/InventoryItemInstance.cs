using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.Providers.InventoryService
{
    public class InventoryItemInstance : InventoryItemInstanceDto
    {
        public IItemInstance ItemInstance { get; set; }

        public static InventoryItemInstance Create(IItemInstance it, long characterId)
        {
            return new InventoryItemInstance
            {
                Id = it.Id,
                CharacterId = characterId,
                ItemInstance = it,
                ItemInstanceId = it.Id,
                Slot = 0,
                Type = it.Item.Type
            };
        }
    }
}