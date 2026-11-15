//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // The instance script each raid gate runs, by the map and cell the gate stands on.
    public static class RaidGateScripts
    {
        public static readonly IReadOnlyDictionary<(short MapId, short X, short Y), string> All =
            new Dictionary<(short MapId, short X, short Y), string>
            {
                [(2510, 20, 5)] = "raid-mother-cuby",
                [(2520, 25, 1)] = "raid-ginseng",
                [(2536, 26, 25)] = "raid-grenigas",
                [(2587, 12, 43)] = "raid-glacerus",
            };
    }
}
