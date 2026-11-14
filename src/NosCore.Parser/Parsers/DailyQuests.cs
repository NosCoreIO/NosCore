//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Linq;

namespace NosCore.Parser.Parsers
{
    // The quests whose English title ends in "(Daily)": quest.dat has no column for it.
    public static class DailyQuests
    {
        private static readonly (short First, short Last)[] Ranges =
        [
            (2250, 2255), (5514, 5519), (5898, 5921), (5932, 5953), (7500, 7532), (7534, 7540),
            (7600, 7634), (7636, 7648), (7652, 7654), (7658, 7660), (7664, 7689), (7709, 7733),
        ];

        public static bool Contains(short questId) => Ranges.Any(r => questId >= r.First && questId <= r.Last);
    }
}
