//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // Family missions that count each member once a day, as their descriptions say ("once per day per player").
    public static class FamilyMissionLimits
    {
        public static readonly IReadOnlySet<short> OncePerMemberPerDay = new HashSet<short>
        {
            9017
        };
    }
}
