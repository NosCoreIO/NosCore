//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // Items whose text limits them to equipment bound to the user's character; Item.dat has no column for it.
    public static class BoundItemsOnly
    {
        public static readonly IReadOnlySet<short> VNums = new HashSet<short>
        {
            8541, 8542, 8543, 8544,
        };
    }
}
