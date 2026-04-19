//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Per-target FIFO serializer. Multiple attackers can call Enqueue concurrently; the
// target sees them applied one at a time in arrival order, so HP and HitList stay
// consistent in PvP without leaking a SemaphoreSlim to callers. Returns a task that
// completes with the final HitOutcome for the caller (damage, crit flag, kill flag).
public interface IHitQueue
{
    Task<HitOutcome> EnqueueAsync(HitRequest request);
}
