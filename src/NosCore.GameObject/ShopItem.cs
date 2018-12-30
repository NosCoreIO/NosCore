using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemBuilder.Item;

namespace NosCore.GameObject
{
    public class ShopItem : ShopItemDto
    {
        public Item Item { get; set; }
    }
}
