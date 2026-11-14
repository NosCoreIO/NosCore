//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Parser.Parsers
{
    // The regions in which a mount rides faster, as its English description lists them under
    // [Properties]. No client file assigns a map to a region, so a region is the maps of the act it
    // names; Mount Krem is the whole of Act 2. Moritius and Undercity are the maps whose MapIDData
    // names belong to them, and are the least certain. Left out, for want of a map list: "the
    // Celestial Lair and the Land of Life" (Royal Rabbit Carriage) and "other dimensions" (Chi Tu).
    public static class MountMapSpeeds
    {
        private static IEnumerable<short> Maps(int first, int last) =>
            Enumerable.Range(first, last - first + 1).Select(m => (short)m);

        private static readonly short[] Act1 = [.. Maps(1, 8), .. Maps(10, 17), .. Maps(49, 84), 103, 104, 129, 2104, 2520];

        private static readonly short[] NosVille = [1];

        private static readonly short[] PortAlveus = [129, 145, 146, 147];

        private static readonly short[] MountKrem = [.. Maps(20, 33), .. Maps(85, 102), .. Maps(119, 121), 2500, 2510, 2530];

        private static readonly short[] Glacernon = [.. Maps(130, 142), .. Maps(151, 154)];

        private static readonly short[] Desert = [.. Maps(170, 204), 2587, .. Maps(2590, 2593)];

        private static readonly short[] BurningSword = [.. Maps(205, 220), .. Maps(2536, 2539), .. Maps(2553, 2558)];

        // From Olorun Village to the Polluted Forest.
        private static readonly short[] Moritius = [.. Maps(2628, 2641)];

        // From the Underground Battlefield to Undercity, with the paths to the polluted water and the secret hut.
        private static readonly short[] Undercity = [.. Maps(267, 280), 282, 283];

        private static readonly short[] DesertAndBurningSword = [.. Desert, .. BurningSword];
        private static readonly short[] NosVilleAndPortAlveus = [.. NosVille, .. PortAlveus];

        public static readonly IReadOnlyDictionary<short, (short[] Maps, byte Speed)[]> All =
            new Dictionary<short, (short[] Maps, byte Speed)[]>
            {
                [1618] = [(Undercity, 3), (Glacernon, 3)],
                [1907] = [(Desert, 2)],
                [1965] = [(Glacernon, 2)],
                [5008] = [(NosVilleAndPortAlveus, 4)],
                [5152] = [(Glacernon, 2)],
                [5196] = [(Glacernon, 2)],
                [5226] = [(Glacernon, 3)],
                [5228] = [(Glacernon, 3)],
                [5232] = [(DesertAndBurningSword, 2)],
                [5234] = [(DesertAndBurningSword, 2)],
                [5236] = [(DesertAndBurningSword, 2)],
                [5238] = [(Glacernon, 3)],
                [5240] = [(Glacernon, 3)],
                [5319] = [(Glacernon, 1)],
                [5321] = [(NosVilleAndPortAlveus, 3)],
                [5323] = [(DesertAndBurningSword, 2)],
                [5330] = [(Glacernon, 1)],
                [5332] = [(Act1, 6)],
                [5360] = [(DesertAndBurningSword, 2)],
                [5386] = [(DesertAndBurningSword, 2)],
                [5387] = [(DesertAndBurningSword, 2)],
                [5388] = [(DesertAndBurningSword, 2)],
                [5389] = [(DesertAndBurningSword, 2)],
                [5390] = [(DesertAndBurningSword, 2)],
                [5391] = [(DesertAndBurningSword, 2)],
                [5712] = [(Glacernon, 1)],
                [5714] = [(Glacernon, 3)],
                [5743] = [(NosVilleAndPortAlveus, 2)],
                [5806] = [(MountKrem, 2)],
                [5807] = [(MountKrem, 3), (Glacernon, 2)],
                [5834] = [(DesertAndBurningSword, 2), (Glacernon, 3)],
                [5914] = [(DesertAndBurningSword, 6)],
                [5997] = [(NosVilleAndPortAlveus, 1), (DesertAndBurningSword, 2)],
                [9055] = [(Desert, 2)],
                [9058] = [(Glacernon, 2)],
                [9065] = [(NosVilleAndPortAlveus, 4)],
                [9073] = [(Glacernon, 2)],
                [9076] = [(Glacernon, 2)],
                [9078] = [(DesertAndBurningSword, 2)],
                [9079] = [(DesertAndBurningSword, 2)],
                [9080] = [(DesertAndBurningSword, 2)],
                [9081] = [(Glacernon, 3)],
                [9082] = [(Glacernon, 3)],
                [9083] = [(Glacernon, 1)],
                [9084] = [(NosVilleAndPortAlveus, 3)],
                [9085] = [(DesertAndBurningSword, 2)],
                [9086] = [(Glacernon, 1)],
                [9087] = [(Act1, 6)],
                [9088] = [(DesertAndBurningSword, 2)],
                [9090] = [(DesertAndBurningSword, 2)],
                [9091] = [(DesertAndBurningSword, 2)],
                [9092] = [(DesertAndBurningSword, 2)],
                [9093] = [(DesertAndBurningSword, 2)],
                [9094] = [(DesertAndBurningSword, 2)],
                [9115] = [(NosVilleAndPortAlveus, 1), (DesertAndBurningSword, 2)],
                [9121] = [(DesertAndBurningSword, 2), (Glacernon, 3)],
                [9138] = [(Glacernon, 1)],
                [9140] = [(Glacernon, 3)],
                [9141] = [(NosVilleAndPortAlveus, 2)],
                [9145] = [(MountKrem, 2)],
                [9146] = [(MountKrem, 3), (Glacernon, 2)],
                [9149] = [(PortAlveus, 3), (Moritius, 2)],
                [9150] = [(PortAlveus, 3), (Moritius, 2)],
                [9220] = [(NosVille, 2), (Moritius, 2)],
                [9221] = [(NosVille, 2), (Moritius, 2)],
                [9274] = [(NosVille, 2), (PortAlveus, 2)],
                [9276] = [(Glacernon, 2), (Moritius, 2)],
                [9278] = [(NosVille, 2), (PortAlveus, 2)],
                [9286] = [(Glacernon, 2), (Moritius, 2)],
                [9443] = [(Moritius, 2)],
                [9589] = [(Moritius, 2)],
                [13414] = [(PortAlveus, 2)],
                [13415] = [(PortAlveus, 2), (Undercity, 2)],
                [13468] = [(Glacernon, 2)],
            };
    }
}
