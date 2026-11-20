//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // Items that restore their EffectValue HP to the NPCs around their user, by the number of
    // cells they reach. Item.dat carries the amount but not the reach; the item text gives it.
    public static class NpcHealingItems
    {
        public static readonly IReadOnlyDictionary<short, byte> Reach = new Dictionary<short, byte>
        {
            [2127] = 6,
        };
    }
}
