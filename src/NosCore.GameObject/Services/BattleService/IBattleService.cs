//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.BattleService
{
    // Entry point for anything that wants to inflict damage. Same signature for
    // character → monster, character → character (PvP), monster → character and
    // pet → monster — the implementation routes to the per-target hit queue so
    // concurrent attackers don't race on HP/HitList state.
    public interface IBattleService
    {
        // False when a gate refused the cast (state, faction, unknown skill, out of reach) and
        // nothing was dispatched, so callers only spend mana and cooldown on true.
        Task<bool> Hit(IAliveEntity origin, IAliveEntity target, HitArguments arguments);

        // A skill cast at a map cell: hits everyone the origin may attack within the skill's
        // target range of that cell. A cast that lands on nobody is still a cast.
        Task<bool> HitArea(IAliveEntity origin, short mapX, short mapY, HitArguments arguments);
    }
}
