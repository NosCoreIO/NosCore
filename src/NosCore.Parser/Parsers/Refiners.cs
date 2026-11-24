//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;
using NosCore.Data.Enumerations.Items;

namespace NosCore.Parser.Parsers
{
    // Item.dat only says which kind a refiner is. Each refine gives Cella Powder, every amount as
    // likely, and at most one of the kind's extra items; the chances are out of ChanceScale and the
    // rest of the scale gives no extra item.
    public static class Refiners
    {
        public const short CellaPowder = 1014;

        public const short ChanceScale = 10_000;

        public static readonly IReadOnlyList<short> CellaAmounts = [5, 6, 7, 8, 9, 10];

        public static readonly IReadOnlyDictionary<RefinerType, (short VNum, short Chance)[]> Extras =
            new Dictionary<RefinerType, (short VNum, short Chance)[]>
            {
                // No count of soul gem refines is at hand: these assume the three-in-ten extra item
                // the other two refiners show, and the split between the two gems is a guess.
                [RefinerType.SoulGem] = [(1015, 2700), (1016, 300)],
                [RefinerType.Crystal] = [(1028, 2393), (1029, 203), (1030, 32), (1031, 92), (1032, 84), (1033, 86), (1034, 94)],
                // Counted over a thousand refines, so level 5 coming out rarer than level 6 is noise, and
                // levels 7 to 10, never seen, are left out though they may be possible.
                [RefinerType.Cellon] = [(1017, 1430), (1018, 800), (1019, 470), (1020, 250), (1021, 30), (1022, 40)],
            };
    }
}
