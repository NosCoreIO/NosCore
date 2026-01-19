//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;

namespace NosCore.Core.Services.IdService
{
    public class IdService<T>(long firstId) : IIdService<T>
    {
        public ConcurrentDictionary<long, T> Items { get; } = new();

        public long GetNextId()
        {
            return ++firstId;
        }
    }
}
