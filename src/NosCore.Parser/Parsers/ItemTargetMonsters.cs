//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // The monsters an item works on, as its description names them.
    public static class ItemTargetMonsters
    {
        // "Reduce a Kenko soldier's HP into the red": the Rookie, plain and Elite Swordsman, Spearman
        // and Raider, the Flailmen and the Giant Raider. The Spy and the sealed Storm Soldier are left out.
        private static readonly short[] KenkoSoldiers = [145, 146, 147, 148, 149, 150, 151, 152, 153, 269, 316, 979];

        public static readonly IReadOnlyDictionary<short, short[]> All = new Dictionary<short, short[]>
        {
            [1173] = KenkoSoldiers, // Suction Funnel
        };
    }
}
