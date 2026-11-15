//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Parser.Parsers
{
    // The monsters each summoning item releases, one picked at random per use.
    public static class ItemSummons
    {
        // Sealed Pink Jelly to Sealed Slade Golem.
        private static readonly short[] SealedMonsters = [.. Enumerable.Range(1386, 20).Select(v => (short)v)];

        public static readonly IReadOnlyDictionary<short, short[]> All = new Dictionary<short, short[]>
        {
            [1945] = SealedMonsters, // Sealed Vessel
            [1946] = SealedMonsters, // Sealed Vessel (Event)
            [9057] = SealedMonsters, // Sealed Vessel
        };
    }
}
