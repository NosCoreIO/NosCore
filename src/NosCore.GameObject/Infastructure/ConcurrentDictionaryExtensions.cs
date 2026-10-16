//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NosCore.GameObject.Infastructure;

public static class ConcurrentDictionaryExtensions
{
    // Values without the snapshot list that ConcurrentDictionary.Values allocates.
    public static IEnumerable<TValue> EnumerateValues<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        foreach (var (_, value) in dictionary)
        {
            yield return value;
        }
    }
}
