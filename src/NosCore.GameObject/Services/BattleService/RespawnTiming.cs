//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.BattleService;

// monster.dat PREATT field 5 is in tenths of a second, not milliseconds: 400 is forty
// seconds. Read as milliseconds it is a hundred times too fast, and nothing throws.
public static class RespawnTiming
{
    // Some event monsters carry zero.
    private static readonly Duration Minimum = Duration.FromSeconds(1);

    public static Duration For(NpcMonsterDto mob)
    {
        var delay = Duration.FromMilliseconds(mob.RespawnTime * 100L);
        return delay < Minimum ? Minimum : delay;
    }
}
