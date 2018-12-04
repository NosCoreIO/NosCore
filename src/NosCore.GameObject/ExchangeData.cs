using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using NosCore.GameObject.Services.ItemBuilder.Item;

namespace NosCore.GameObject
{
    public class ExchangeData
    {
        public ExchangeData()
        {
            ExchangeItems = new ConcurrentDictionary<long, IItemInstance>();
        }

        public ConcurrentDictionary<long, IItemInstance> ExchangeItems { get; set; }

        public long TargetVisualId { get; set; }

        public long Gold { get; set; }

        public long BankGold { get; set; }

        public bool ExchangeListIsValid { get; set; }

        public bool ExchangeConfirmed { get; set; }
    }
}
