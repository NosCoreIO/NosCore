//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//


using NosCore.Packets.Enumerations;
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // What the client's upgrade windows list under "Requires" for every item placed in them.
    public static class UpgradeCosts
    {
        private const short GolemCore = 2406;
        private const short OccultBonePowder = 2408;
        private const short LoaRunicPowder = 2411;
        private const short MoritiusObsidian = 2416;
        private const short SpiritForestBranch = 2460;

        public static readonly IReadOnlyList<UpgradeCost> All =
        [
            new(UpgradePacketType.ApplyTattoo, 20_000,
                [(LoaRunicPowder, 15), (MoritiusObsidian, 20), (OccultBonePowder, 20), (SpiritForestBranch, 15), (GolemCore, 10)]),
        ];
    }

    public sealed record UpgradeCost(UpgradePacketType Kind, long Gold, (short ItemVNum, short Amount)[] Materials);
}
