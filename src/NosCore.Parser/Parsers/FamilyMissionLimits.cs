//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;

namespace NosCore.Parser.Parsers
{
    // Family missions that count each member once a day, as their descriptions say ("once per day per player").
    // The Giant Black Spider raid mission's description leaves the limit out, unlike every other raid's.
    public static class FamilyMissionLimits
    {
        public static readonly IReadOnlySet<short> OncePerMemberPerDay = new HashSet<short>(
            new short[] { 9017 }
                .Concat(Range(9007, 9016))
                .Concat(Range(9079, 9112))
                .Concat(Range(9115, 9135))
                .Concat(Range(9189, 9191)));

        private static IEnumerable<short> Range(short first, short last) =>
            Enumerable.Range(first, last - first + 1).Select(s => (short)s);
    }
}
