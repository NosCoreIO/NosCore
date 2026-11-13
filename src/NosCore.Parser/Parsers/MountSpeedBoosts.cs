//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // What a Speed Booster does on each mount, as the mount's English description lists it under "[With Speed Booster]".
    public static class MountSpeedBoosts
    {
        public static readonly IReadOnlyDictionary<short, MountSpeedBoost> All = new Dictionary<short, MountSpeedBoost>
        {
            [1906] = new(Speed: 2, Duration: 3),
            [1907] = new(Speed: 2, Duration: 3),
            [1965] = new(Speed: 2, Duration: 3),
            [5008] = new(Speed: 2, Duration: 3),
            [5117] = new(Speed: 5, Duration: 3),
            [5152] = new(Speed: 3, Duration: 5),
            [5173] = new(Speed: 2, Duration: 8),
            [5196] = new(Speed: 3, Duration: 5, RemovesDebuffsUpToLevel: 4),
            [5226] = new(Speed: 3, Duration: 5),
            [5228] = new(Speed: 3, Duration: 5),
            [5232] = new(Speed: 3, Duration: 5),
            [5234] = new(Speed: 3, Duration: 5, RemovesDebuffsUpToLevel: 4),
            [5236] = new(Speed: 3, Duration: 5, RestoresPerLevel: 15),
            [5238] = new(Speed: 3, Duration: 5),
            [5240] = new(Speed: 3, Duration: 5),
            [5319] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [5321] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [5323] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [5330] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [5332] = new(Speed: 2, Duration: 3),
            [5360] = new(Speed: 3, Duration: 5, RemovesDebuffsUpToLevel: 4),
            [5386] = new(Speed: 2, Duration: 6),
            [5387] = new(Speed: 2, Duration: 6),
            [5388] = new(Speed: 2, Duration: 6),
            [5389] = new(Speed: 2, Duration: 6),
            [5390] = new(Speed: 2, Duration: 6),
            [5391] = new(Speed: 2, Duration: 6),
            [5914] = new(Speed: 2, Duration: 5),
            [5997] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [9054] = new(Speed: 2, Duration: 3),
            [9055] = new(Speed: 2, Duration: 3),
            [9058] = new(Speed: 2, Duration: 3),
            [9065] = new(Speed: 2, Duration: 3),
            [9070] = new(Speed: 5, Duration: 3),
            [9073] = new(Speed: 3, Duration: 5),
            [9078] = new(Speed: 3, Duration: 5),
            [9079] = new(Speed: 3, Duration: 5, RemovesDebuffsUpToLevel: 4),
            [9080] = new(Speed: 3, Duration: 5, RestoresPerLevel: 15),
            [9081] = new(Speed: 3, Duration: 5),
            [9082] = new(Speed: 3, Duration: 5),
            [9083] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [9084] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [9085] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [9086] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [9087] = new(Speed: 2, Duration: 3),
            [9088] = new(Speed: 3, Duration: 5, RemovesDebuffsUpToLevel: 4),
            [9090] = new(Speed: 2, Duration: 6),
            [9091] = new(Speed: 2, Duration: 6),
            [9092] = new(Speed: 2, Duration: 6),
            [9093] = new(Speed: 2, Duration: 6),
            [9094] = new(Speed: 2, Duration: 6),
            [9115] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
        };
    }

    public sealed record MountSpeedBoost(byte Speed, byte Duration, byte RestoresPerLevel = 0, byte RemovesDebuffsUpToLevel = 0,
        bool TeleportsRandomly = false);
}
