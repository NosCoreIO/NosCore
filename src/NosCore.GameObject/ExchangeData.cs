using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using NosCore.GameObject.Services.ItemBuilder.Item;

namespace NosCore.GameObject
{
    public class ExchangeData : ConcurrentDictionary<long, ItemInstance>
    {
        public long TargetVisualId { get; set; }

        public long Gold { get; set; }

        public long BankGold { get; set; }
    }
}
