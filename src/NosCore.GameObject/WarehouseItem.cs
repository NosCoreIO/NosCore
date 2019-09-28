using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.Dto;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject
{
    public class WarehouseItem : WarehouseItemDto
    {
        public IItemInstance ItemInstance { get; set; }
    }
}
