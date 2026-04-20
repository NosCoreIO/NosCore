//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Resolves a CombatStats snapshot for any alive entity. Kept behind an interface so the
// damage calculator never needs to know whether the stats came from a Character's
// CombatComponent or a monster's NpcMonsterDto, and tests can swap in canned stats.
public interface IBattleStatsProvider
{
    CombatStats GetStats(IAliveEntity entity);
}
