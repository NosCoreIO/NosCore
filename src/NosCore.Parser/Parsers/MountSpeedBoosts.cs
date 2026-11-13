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
            [5152] = new(Speed: 3, Duration: 5, DashCells: 5),
            [5173] = new(Speed: 2, Duration: 8),
            [5196] = new(Speed: 3, Duration: 5, RemovesDebuffsUpToLevel: 4),
            [5226] = new(Speed: 3, Duration: 5),
            [5228] = new(Speed: 3, Duration: 5),
            [5232] = new(Speed: 3, Duration: 5, DashCells: 5),
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
            [5712] = new(Speed: 3, Duration: 2),
            [5714] = new(Speed: 4, Duration: 2),
            [5743] = new(Speed: 3, Duration: 8, RemovesDebuffsUpToLevel: 4),
            [5806] = new(Speed: 2, Duration: 4),
            [5807] = new(Speed: 3, Duration: 5, RemovesDebuffsUpToLevel: 4),
            [5834] = new(Speed: 4, Duration: 2, RemovesDebuffsUpToLevel: 4),
            [5914] = new(Speed: 2, Duration: 5),
            [5997] = new(Speed: 0, Duration: 0, TeleportsRandomly: true),
            [9054] = new(Speed: 2, Duration: 3),
            [9055] = new(Speed: 2, Duration: 3),
            [9058] = new(Speed: 2, Duration: 3),
            [9065] = new(Speed: 2, Duration: 3),
            [9070] = new(Speed: 5, Duration: 3),
            [9073] = new(Speed: 3, Duration: 5, DashCells: 5),
            [9076] = new(Speed: 3, Duration: 5, RemovesDebuffsUpToLevel: 4),
            [9078] = new(Speed: 3, Duration: 5, DashCells: 5),
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
            [9121] = new(Speed: 4, Duration: 2, RemovesDebuffsUpToLevel: 4),
            [9138] = new(Speed: 3, Duration: 2),
            [9140] = new(Speed: 4, Duration: 2),
            [9141] = new(Speed: 3, Duration: 8, RemovesDebuffsUpToLevel: 4),
            [9145] = new(Speed: 2, Duration: 4),
            [9146] = new(Speed: 3, Duration: 5, RemovesDebuffsUpToLevel: 4),
            [9149] = new(Speed: 3, Duration: 9, RemovesDebuffsUpToLevel: 4),
            [9150] = new(Speed: 3, Duration: 9, RemovesDebuffsUpToLevel: 4),
            [9220] = new(Speed: 3, Duration: 8, RemovesDebuffsUpToLevel: 4),
            [9221] = new(Speed: 3, Duration: 8, RemovesDebuffsUpToLevel: 4),
            [9274] = new(Speed: 2, Duration: 4),
            [9276] = new(Speed: 3, Duration: 8, RemovesDebuffsUpToLevel: 4),
            [9278] = new(Speed: 2, Duration: 4),
            [9286] = new(Speed: 3, Duration: 8, RemovesDebuffsUpToLevel: 4),
            [9443] = new(Speed: 2, Duration: 10, RemovesDebuffsUpToLevel: 4),
            [9589] = new(Speed: 2, Duration: 10, RemovesDebuffsUpToLevel: 4),
            [13414] = new(Speed: 2, Duration: 4),
            [13415] = new(Speed: 3, Duration: 8, RemovesDebuffsUpToLevel: 4, DashCells: 5),
            [13468] = new(Speed: 5, Duration: 8, RemovesDebuffsUpToLevel: 4),
            [13752] = new(Speed: 4, Duration: 12, DashCells: 5),
        };
    }

    public sealed record MountSpeedBoost(byte Speed, byte Duration, byte RestoresPerLevel = 0, byte RemovesDebuffsUpToLevel = 0,
        bool TeleportsRandomly = false, byte DashCells = 0);
}
