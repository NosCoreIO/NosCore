using System.Collections.Concurrent;

namespace NosCore.Core.Services.IdService
{
    public interface IIdService<T>
    {
        ConcurrentDictionary<long, T> Items { get; }

        long GetNextId();
    }
}
