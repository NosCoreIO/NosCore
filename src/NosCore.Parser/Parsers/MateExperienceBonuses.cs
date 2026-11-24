//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // Cards that raise the experience of the bearer's NosMates. Card.dat leaves their effect rows
    // empty and BCard.dat has no NosMate experience type, so the percent comes from the card text.
    public static class MateExperienceBonuses
    {
        public static readonly IReadOnlyDictionary<short, short> All = new Dictionary<short, short>
        {
            [122] = 100,
        };
    }
}
