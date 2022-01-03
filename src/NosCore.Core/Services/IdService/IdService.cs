using System.Collections.Concurrent;

namespace NosCore.Core.Services.IdService
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
