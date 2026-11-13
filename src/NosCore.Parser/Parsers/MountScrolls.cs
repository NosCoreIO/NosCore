//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // Scrolls that put their user on a mount drawn at random from a list, every mount as likely.
    // Item.dat has none of this; the English descriptions give the list, the speed and the rules.
    // A Speed of 0 rides at the drawn mount's own speed.
    public static class MountScrolls
    {
        private static readonly short[] NoviceMounts = [1906, 1907, 1965, 5238, 5240, 5232];

        public static readonly IReadOnlyDictionary<short, MountScroll> All = new Dictionary<short, MountScroll>
        {
            // The description only says "one of the various mounts", so the Movement Scrolls draw from
            // the novice list until a fuller one is known.
            [5066] = new(NoviceMounts, ConsumedOnMount: true),
            [5214] = new(NoviceMounts, ConsumedOnMount: true),
            [5833] = new(NoviceMounts, Speed: 17, LevelMaximum: 69),
            [9128] = new(NoviceMounts, Speed: 17, LevelMaximum: 69),
            [5923] = new([5914], ConsumedOnMount: true),
        };
    }

    public sealed record MountScroll(short[] Mounts, byte Speed = 0, bool ConsumedOnMount = false, byte LevelMaximum = 0);
}
