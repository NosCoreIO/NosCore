using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Services.ExchangeInfo
{
    public interface IExchangeInfoService
    {
        ExchangeData ExchangeData { get; set; }

        ConcurrentDictionary<Guid, long> ExchangeRequests { get; set; }
    }
}
