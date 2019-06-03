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
    }
}
