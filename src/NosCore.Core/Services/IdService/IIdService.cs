//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;

namespace NosCore.Core.Services.IdService
{
    public interface IIdService<T>
    {
        ConcurrentDictionary<long, T> Items { get; }

        long GetNextId();
    }
}
