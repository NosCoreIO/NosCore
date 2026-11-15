//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // SP recharging items that do more than add points. Item.dat only carries the points; the
    // rest is in their item text.
    public static class SpecialPotions
    {
        public static readonly IReadOnlyDictionary<short, SpecialPotion> All = new Dictionary<short, SpecialPotion>
        {
            [1120] = new(RestoresPercent: 0, RemovesDebuffsUpToLevel: byte.MaxValue),
            [9016] = new(RestoresPercent: 0, RemovesDebuffsUpToLevel: byte.MaxValue),
            [5904] = new(RestoresPercent: 20, RemovesDebuffsUpToLevel: 3),
        };
    }

    public sealed record SpecialPotion(byte RestoresPercent, byte RemovesDebuffsUpToLevel);
}
