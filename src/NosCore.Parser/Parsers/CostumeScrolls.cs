//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // The monsters each costume scroll can turn its user into, by monster.dat vnum, from the
    // scroll's English description. monster.dat has no Whitewitch; the Strong Whitewitch stands in.
    public static class CostumeScrolls
    {
        private static readonly short[] BookOfHalloween =
        [
            446, // Yak Warrior
            324, // Princess Catrisha
            404, // Pinching Torturer
            405, // Wheel Torturer
            406, // Iron Torturer
        ];

        private static readonly short[] Halloween =
        [
            .. BookOfHalloween,
            441, // Death Warrior
            276, // Dust King
            248, // Darkwitch
            249, // Bloodwitch
            438, // Strong Whitewitch
        ];

        public static readonly IReadOnlyDictionary<short, short[]> All = new Dictionary<short, short[]>
        {
            [1913] = Halloween, // Halloween Costume Scroll
            [1914] = BookOfHalloween, // Book of Halloween Transformation
            [1915] = Halloween, // Halloween Costume Scroll (Event)
        };
    }
}
