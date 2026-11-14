//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // A raid box rolls one line, every line as likely. A weapon or armour comes out at the box's
    // rarity. Raids hand these boxes out with design 50, which is what the rows match on.
    public static class RaidBoxes
    {
        public const short Design = 50;

        private static readonly (short VNum, byte Amount)[] Shells =
        [
            (565, 1), (568, 1), (571, 1), (574, 1), (577, 1), (580, 1), (583, 1), (586, 1), (1430, 1), (1429, 5),
        ];

        public static readonly IReadOnlyDictionary<short, (short VNum, byte Amount)[]> All =
            new Dictionary<short, (short, byte)[]>
            {
                [882] = [.. Shells, (350, 1), (354, 1), (357, 1), (264, 1), (267, 1), (270, 1), (2514, 1), (2436, 1), (4984, 1), (9384, 1)],
                [185] = [.. Shells, (199, 1), (940, 1), (941, 1), (2516, 1), (2439, 1), (4979, 1), (9385, 1)],
                [942] = [.. Shells, (944, 1), (945, 1), (946, 1), (947, 1), (948, 1), (949, 1), (2515, 1), (2437, 1), (4986, 1), (4762, 1), (9386, 1)],
                [999] = [.. Shells, (994, 1), (993, 1), (989, 1), (2395, 3), (2396, 5), (2397, 10), (2517, 1), (2438, 1), (4978, 1), (9387, 1)],
            };
    }
}
