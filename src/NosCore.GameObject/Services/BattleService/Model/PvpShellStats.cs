//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.BattleService.Model;

// The weapon shell options that act only when a player hits a player. The resistance
// reductions are negative, like the type 14 effects they add to.
public readonly record struct PvpShellStats(
    int EnemyDefenceReduction = 0,
    int EnemyFireResistance = 0,
    int EnemyWaterResistance = 0,
    int EnemyLightResistance = 0,
    int EnemyDarkResistance = 0,
    bool AlwaysHits = false,
    int MpDrain = 0,
    bool IgnoresFireResistance = false,
    bool IgnoresWaterResistance = false,
    bool IgnoresLightResistance = false,
    bool IgnoresDarkResistance = false);
