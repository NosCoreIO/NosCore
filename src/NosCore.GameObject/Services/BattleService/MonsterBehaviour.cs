//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.BattleService;

// Respawn is not a state here: a dead monster is skipped by the tick and brought back by
// RespawnService.
public enum MonsterBehaviour
{
    Dead,
    Idle,
    Wander,
    AcquireTarget,
    Chase,
    Attack,
    Return,
}

// AttackRange is the reach of what the monster would swing this tick: the rolled skill's
// range, or the basic attack's when no skill was rolled.
public readonly record struct MonsterSituation(
    bool IsAlive,
    bool HasTarget,
    bool IsHostile,
    bool HasScanned,
    bool CanWalk,
    bool IsMoving,
    bool AtHome,
    int Distance,
    int AttackRange);

public static class MonsterBehaviourRules
{
    public static MonsterBehaviour Decide(in MonsterSituation situation)
    {
        if (!situation.IsAlive)
        {
            return MonsterBehaviour.Dead;
        }

        if (situation.HasTarget)
        {
            if (situation.Distance <= situation.AttackRange)
            {
                return MonsterBehaviour.Attack;
            }

            return situation.CanWalk ? MonsterBehaviour.Chase : MonsterBehaviour.Idle;
        }

        if (situation.IsHostile && !situation.HasScanned)
        {
            return MonsterBehaviour.AcquireTarget;
        }

        if (!situation.IsMoving)
        {
            return MonsterBehaviour.Idle;
        }

        return situation.AtHome ? MonsterBehaviour.Wander : MonsterBehaviour.Return;
    }
}
