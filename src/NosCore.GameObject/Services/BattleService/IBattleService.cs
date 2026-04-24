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
        Task Hit(IAliveEntity origin, IAliveEntity target, HitArguments arguments);

        // Drain the pending skill-cooldown-reset table for characters on this map
        // and emit SkillResetPacket for any whose ReadyAt has elapsed. Called once
        // per map per 400ms life tick from MapInstance.
        Task TickCooldownResetsAsync(MapInstance mapInstance);
    }
}
