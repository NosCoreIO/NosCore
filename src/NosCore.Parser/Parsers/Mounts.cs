//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // The riding speed and the look of each mount. Item.dat has neither: the speed is the one the
    // mount's English description gives, and the look is 1000 + the vnum of the mount's male rider
    // in monster.dat, the female rider being the next vnum. A riding card is the buff the
    // description says the rider receives.
    public static class Mounts
    {
        public static readonly IReadOnlyDictionary<short, Mount> All = new Dictionary<short, Mount>
        {
            [1618] = new(Speed: 23, Morph: 3763, RidingCard: 4248),
            [1906] = new(Speed: 20, Morph: 2368),
            [1907] = new(Speed: 20, Morph: 2370),
            [1965] = new(Speed: 20, Morph: 2406),
            [5008] = new(Speed: 20, Morph: 2411),
            [5117] = new(Speed: 21, Morph: 2429),
            [5152] = new(Speed: 21, Morph: 2432),
            [5173] = new(Speed: 16, Morph: 2511),
            [5196] = new(Speed: 21, Morph: 2517),
            [5226] = new(Speed: 20, Morph: 1817),
            [5228] = new(Speed: 20, Morph: 1819),
            [5232] = new(Speed: 21, Morph: 2520),
            [5234] = new(Speed: 21, Morph: 2522),
            [5236] = new(Speed: 21, Morph: 2524),
            [5238] = new(Speed: 20, Morph: 1817),
            [5240] = new(Speed: 20, Morph: 1819),
            [5319] = new(Speed: 22, Morph: 2526),
            [5321] = new(Speed: 22, Morph: 2528),
            [5323] = new(Speed: 22, Morph: 2530),
            [5330] = new(Speed: 22, Morph: 2928),
            [5332] = new(Speed: 14, Morph: 2930),
            [5360] = new(Speed: 22, Morph: 2932),
            [5386] = new(Speed: 21, Morph: 2934),
            [5387] = new(Speed: 21, Morph: 2936),
            [5388] = new(Speed: 21, Morph: 2938),
            [5389] = new(Speed: 21, Morph: 2940),
            [5390] = new(Speed: 21, Morph: 2942),
            [5391] = new(Speed: 21, Morph: 2944),
            [5712] = new(Speed: 20, Morph: 2440),
            [5714] = new(Speed: 22, Morph: 2442),
            [5743] = new(Speed: 21, Morph: 2444),
            [5806] = new(Speed: 15, Morph: 2450),
            [5807] = new(Speed: 20, Morph: 2448, RidingCard: 764),
            [5834] = new(Speed: 20, Morph: 3693),
            [5914] = new(Speed: 14, Morph: 2513),
            [5997] = new(Speed: 21, Morph: 3679),
            [9054] = new(Speed: 20, Morph: 2368),
            [9055] = new(Speed: 20, Morph: 2370),
            [9058] = new(Speed: 20, Morph: 2406),
            [9065] = new(Speed: 20, Morph: 2411),
            [9070] = new(Speed: 21, Morph: 2429),
            [9073] = new(Speed: 21, Morph: 2432),
            [9076] = new(Speed: 21, Morph: 2517),
            [9078] = new(Speed: 21, Morph: 2520),
            [9079] = new(Speed: 21, Morph: 2522),
            [9080] = new(Speed: 21, Morph: 2524),
            [9081] = new(Speed: 20, Morph: 1817),
            [9082] = new(Speed: 20, Morph: 1819),
            [9083] = new(Speed: 22, Morph: 2526),
            [9084] = new(Speed: 22, Morph: 2528),
            [9085] = new(Speed: 22, Morph: 2530),
            [9086] = new(Speed: 22, Morph: 2928),
            [9087] = new(Speed: 14, Morph: 2930),
            [9088] = new(Speed: 22, Morph: 2932),
            [9090] = new(Speed: 21, Morph: 2936),
            [9091] = new(Speed: 21, Morph: 2938),
            [9092] = new(Speed: 21, Morph: 2940),
            [9093] = new(Speed: 21, Morph: 2942),
            [9094] = new(Speed: 21, Morph: 2944),
            [9115] = new(Speed: 21, Morph: 3679),
            [9121] = new(Speed: 20, Morph: 3693),
            [9138] = new(Speed: 20, Morph: 2440),
            [9140] = new(Speed: 22, Morph: 2442),
            [9141] = new(Speed: 21, Morph: 2444),
            [9145] = new(Speed: 15, Morph: 2450),
            [9146] = new(Speed: 20, Morph: 2448, RidingCard: 764),
            [9149] = new(Speed: 20, Morph: 3711),
            [9150] = new(Speed: 20, Morph: 3711),
            [9220] = new(Speed: 21, Morph: 3717),
            [9221] = new(Speed: 21, Morph: 3717),
            [9274] = new(Speed: 18, Morph: 3724),
            [9276] = new(Speed: 21, Morph: 3726),
            [9278] = new(Speed: 18, Morph: 3724),
            [9286] = new(Speed: 21, Morph: 3726),
            [9443] = new(Speed: 23, Morph: 3728),
            [9589] = new(Speed: 23, Morph: 3728),
            [13414] = new(Speed: 16, Morph: 3756),
            [13415] = new(Speed: 24, Morph: 3758),
            [13468] = new(Speed: 24, Morph: 3760, RidingCard: 4237),
            [13752] = new(Speed: 24, Morph: 3770, RidingCard: 4328),
        };
    }

    public sealed record Mount(byte Speed, short Morph, short RidingCard = 0);
}
