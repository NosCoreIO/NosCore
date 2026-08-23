//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.BattleService;

// How long a monster takes to come back.
//
// THE VALUE IN THE FILE IS IN TENTHS OF A SECOND, not milliseconds. monster.dat, PREATT, fifth
// field: an ordinary wolf carries 400, and four hundred tenths are forty seconds.
//
// Read as milliseconds it becomes four tenths of a second - a hundred times too fast. There is no
// exception and no log, just maps that never empty and a monster reappearing on top of whoever
// killed it. The kind of mistake only noticed by counting.
//
// Two independent confirmations of the unit: the atlagaming API exposes the same field as
// respTimeSek and gives 40.0 for that 400, and the older emulator divides it by ten.
public static class RespawnTiming
{
    // A second at least: some event monsters carry zero, and respawning them the instant they
    // die would mean never letting them die.
    private static readonly Duration Minimum = Duration.FromSeconds(1);

    public static Duration For(NpcMonsterDto mob)
    {
        var delay = Duration.FromMilliseconds(mob.RespawnTime * 100L);
        return delay < Minimum ? Minimum : delay;
    }
}
