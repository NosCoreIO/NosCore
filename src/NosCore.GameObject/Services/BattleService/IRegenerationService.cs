//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.BattleService;

// HP/MP natural regeneration for one player whose step RegenerationSystem found due.
public interface IRegenerationService
{
    Task RegenerateAsync(PlayerComponentBundle character);

    // HitQueue calls this whenever a player takes damage so standing regen stays
    // suppressed for the 4s grace window. Sitting regen is unaffected —
    // resting is an explicit user action and the fall animation already took them
    // out of combat.
    void NotifyDamaged(PlayerComponentBundle character);
}
