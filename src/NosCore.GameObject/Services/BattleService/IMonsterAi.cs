//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.BattleService;

// One tick of the monster AI loop for a single map. Split out as an interface so
// MapInstance's life loop can be unit-tested without spinning up the real AI and so
// future behaviour trees can be dropped in without touching MapInstance.
public interface IMonsterAi
{
    // Returns true if the AI drove the monster this tick (chose to attack or step),
    // so the life loop can skip the fallback random-wander logic and avoid stepping
    // the monster twice. Returns false for passive monsters (no aggro).
    Task<bool> TickAsync(MonsterComponentBundle monster);
}
