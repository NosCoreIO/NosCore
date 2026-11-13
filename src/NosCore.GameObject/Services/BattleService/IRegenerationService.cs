//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.BattleService;

// HP/MP natural regeneration. Called from MapInstance's life loop so every connected
// player on the map ticks together. Sitting players regen full-rate unconditionally;
// standing players regen half-rate gated on "no damage for 4s".
public interface IRegenerationService
{
    Task TickAsync(MapInstance mapInstance);

    // HitQueue calls this whenever a player takes damage so standing regen stays
    // suppressed for the 4s grace window. Sitting regen is unaffected —
    // resting is an explicit user action and the fall animation already took them
    // out of combat.
    void NotifyDamaged(PlayerComponentBundle character);
}
