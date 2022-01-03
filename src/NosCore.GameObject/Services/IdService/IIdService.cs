using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.IdService
{
    public interface IIdService<T>
    {
        ConcurrentDictionary<long, T> Items { get; }

        long GetNextId();
    }
}
