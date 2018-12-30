using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject
{
    public class Shop : ShopDto
    {
        public Shop()
        {
            ShopItems = new ConcurrentDictionary<int, ShopItem>();
        }

        public ConcurrentDictionary<int, ShopItem> ShopItems { get; set; }
    }
}
