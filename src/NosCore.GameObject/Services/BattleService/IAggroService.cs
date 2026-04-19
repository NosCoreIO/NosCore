//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

// Tracks a monster's current aggro target. The move/attack AI (run from the map's life
// loop) reads Current() to decide whether to pursue a player; combat pushes threat via
// AddThreat() every time the monster takes damage. Leash duration is the grace window
// after which the monster forgets its last target if nothing else happened.
public interface IAggroService
{
    AggroSnapshot Current(IAliveEntity entity);

    void AddThreat(IAliveEntity mob, IAliveEntity attacker, int damage);

    void Clear(IAliveEntity mob);
}

public readonly record struct AggroSnapshot(long TargetVisualId, int ThreatScore, bool HasTarget);
