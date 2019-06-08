using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject
{
    public class BazaarItem : BazaarItemDto
    {
        public ItemInstance ItemInstance { get; set; }
        public string SellerName { get; set; }
    }
}
