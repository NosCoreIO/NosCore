//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;
using NosCore.Packets.Enumerations;

namespace NosCore.Parser.Parsers
{
    // Item.dat lists only the dyed looks (DATA 14 and 12 on the Savage Dyes); the description names
    // the hairstyles they apply to, in the same order.
    public static class HairStyleDyes
    {
        public static readonly IReadOnlyDictionary<short, (HairStyleType HairStyle, HairStyleType Dyed)[]> All =
            new Dictionary<short, (HairStyleType, HairStyleType)[]>
            {
                [2533] =
                [
                    (HairStyleType.FauxHawkUndercut, HairStyleType.SavageFauxHawkUndercut),
                    (HairStyleType.ChoppyBangs, HairStyleType.SavageChoppyBangs),
                ],
            };
    }
}
