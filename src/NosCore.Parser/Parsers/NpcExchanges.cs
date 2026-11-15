//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//


using NosCore.Packets.Enumerations;
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // What the NPC dialog exchanges take and give, as their option texts, dialog lines and the
    // materials' descriptions state it. A choice listed several times gives the first item that fits
    // the player's class.
    public static class NpcExchanges
    {
        private const short SeedOfPower = 1012;
        private const short GillionStone = 1013;
        private const short DonaRiverSand = 1027;
        private const short IceCubes = 2307;
        private const short DracosClaw = 2522;
        private const short GlacerusMane = 2523;
        private const short AkamurCoupon = 5910;
        private const short IceFlower = 5911;

        public static readonly IReadOnlyList<NpcExchange> All =
        [
            new(NrunRunnerType.ExchangeForGrenigasRaidSeal, 5922, 1, [(5917, 1), (5918, 1)]),
            new(NrunRunnerType.ExchangeFor10xIceFlowerOil, 5929, 10, [(AkamurCoupon, 5)]),
            new(NrunRunnerType.ExchangeForMagicCamel, 5914, 1, [(AkamurCoupon, 90)]),
            new(NrunRunnerType.ExchangeForMagicCamelBox, 5915, 1, [(AkamurCoupon, 300)]),
            new(NrunRunnerType.ExchangeForLordDracosRaidSeal, 5500, 1, [(SeedOfPower, 20), (DonaRiverSand, 20), (GillionStone, 20)]),
            new(NrunRunnerType.ExchangeForGlacerusRaidSeal, 5512, 1, [(SeedOfPower, 20), (IceCubes, 20), (IceFlower, 20)]),
            new(NrunRunnerType.ExchangeForSP5, 4500, 1, [(DracosClaw, 50)]),
            new(NrunRunnerType.ExchangeForSP5, 4501, 1, [(DracosClaw, 50)]),
            new(NrunRunnerType.ExchangeForSP5, 4502, 1, [(DracosClaw, 50)]),
            new(NrunRunnerType.ExchangeForRubyOfCompletion, 2518, 1, [(DracosClaw, 5)]),
            new(NrunRunnerType.ExchangeForSP6, 4497, 1, [(GlacerusMane, 50)]),
            new(NrunRunnerType.ExchangeForSP6, 4498, 1, [(GlacerusMane, 50)]),
            new(NrunRunnerType.ExchangeForSP6, 4499, 1, [(GlacerusMane, 50)]),
            new(NrunRunnerType.ExchangeForSapphireOfCompletion, 2519, 1, [(GlacerusMane, 5)]),
            new(NrunRunnerType.ExchangeForCleansingPowder, 5984, 3, [(5986, 3)]),
            new(NrunRunnerType.ExchangeFor2xWitchLaurenaSeal, 5977, 2, [(5987, 5)]),
        ];
    }

    public sealed record NpcExchange(NrunRunnerType Runner, short ItemVNum, short Amount, (short ItemVNum, short Amount)[] Materials);
}
