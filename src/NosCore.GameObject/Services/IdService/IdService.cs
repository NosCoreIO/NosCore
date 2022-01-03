using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.IdService
{
    public class IdService<T> : IIdService<T>
    {
        private long _lastId;

        public IdService(long firstId)
        {
            _lastId = firstId;
        }
        public ConcurrentDictionary<long, T> Items { get; } = new();

        public long GetNextId()
        {
            return ++_lastId;
        }
    }
}
