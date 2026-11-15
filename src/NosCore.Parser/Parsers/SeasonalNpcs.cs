//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Event;
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // NPCs that only stand in the world for one time of year, as their names in monster.dat say.
    // A capture taken outside that time does not show them, so it must not retire them.
    public static class SeasonalNpcs
    {
        public static readonly IReadOnlyDictionary<short, SeasonType> All = new Dictionary<short, SeasonType>
        {
            [985] = SeasonType.Easter, // Easter Sack
            [1515] = SeasonType.Halloween, // Eric
            [1516] = SeasonType.Halloween, // Mary
        };
    }
}
