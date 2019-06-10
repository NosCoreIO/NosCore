using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Data.WebApi
{
    public class BazaarLink
    {
        public BazaarItemDto BazaarItem { get; set; }
        public ItemInstanceDto ItemInstance { get; set; }
        public string SellerName { get; set; }
    }
}
