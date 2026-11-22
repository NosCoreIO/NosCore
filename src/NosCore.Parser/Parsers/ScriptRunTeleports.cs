//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // The destination of each tutorial "run" step that asks the player to teleport, as the traced question
    // and the arrival that follows it show. Runs 203 and 204 have no trace yet, so they stay unanswered.
    public static class ScriptRunTeleports
    {
        public static readonly IReadOnlyDictionary<short, (short MapId, short X, short Y)> All =
            new Dictionary<short, (short MapId, short X, short Y)>
            {
                [200] = (1, 81, 33),
                [201] = (2, 72, 15),
                [202] = (3, 173, 186),
            };
    }
}
