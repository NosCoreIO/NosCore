//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

// Updates the AggroComponent on the mob's ECS entity. We don't recompute threat from the
// whole HitList every tick — instead each damage event bumps threat for the attacker
// and replaces the target only when the new threat exceeds the current (with a small
// sticky bonus so a single big hit doesn't thrash targeting). The leash is refreshed
// on every damage event, giving the AI ~20s grace before forgetting the player.
public sealed class AggroService(IClock clock) : IAggroService
{
    private const int StickyBonus = 100;
    private static readonly Duration LeashDuration = Duration.FromSeconds(20);

    public AggroSnapshot Current(IAliveEntity entity)
    {
        if (!TryReadAggro(entity, out var aggro, out _, out _))
        {
            return new AggroSnapshot(0, 0, false);
        }

        var now = clock.GetCurrentInstant();
        if (aggro.UntilLeash <= now || aggro.TargetVisualId == 0)
        {
            return new AggroSnapshot(0, 0, false);
        }
        return new AggroSnapshot(aggro.TargetVisualId, aggro.ThreatScore, true);
    }

    public void AddThreat(IAliveEntity mob, IAliveEntity attacker, int damage)
    {
        if (damage <= 0) return;
        if (!TryReadAggro(mob, out var aggro, out var world, out var handle)) return;

        var now = clock.GetCurrentInstant();
        var baseThreat = damage;
        var isSameTarget = aggro.TargetVisualId == attacker.VisualId && aggro.TargetVisualType == attacker.VisualType;

        int newThreat;
        long newTargetId;
        Shared.Enumerations.VisualType newTargetType;

        if (isSameTarget)
        {
            newThreat = aggro.ThreatScore + baseThreat;
            newTargetId = aggro.TargetVisualId;
            newTargetType = aggro.TargetVisualType;
        }
        else if (baseThreat > aggro.ThreatScore + StickyBonus || aggro.TargetVisualId == 0)
        {
            newThreat = baseThreat;
            newTargetId = attacker.VisualId;
            newTargetType = attacker.VisualType;
        }
        else
        {
            // Existing target stays but ages down slightly so the mob eventually
            // re-targets if the incumbent stops hitting.
            newThreat = System.Math.Max(1, aggro.ThreatScore - 1);
            newTargetId = aggro.TargetVisualId;
            newTargetType = aggro.TargetVisualType;
        }

        world.SetComponent(handle, new AggroComponent(newTargetType, newTargetId, newThreat, now.Plus(LeashDuration)));
    }

    public void Clear(IAliveEntity mob)
    {
        if (!TryReadAggro(mob, out _, out var world, out var handle)) return;
        world.SetComponent(handle, new AggroComponent(Shared.Enumerations.VisualType.Object, 0, 0, Instant.MinValue));
    }

    private static bool TryReadAggro(IAliveEntity entity, out AggroComponent aggro, out MapWorld world, out Arch.Core.Entity handle)
    {
        switch (entity)
        {
            case MonsterComponentBundle m:
                {
                    var c = m.World.TryGetComponent<AggroComponent>(m.Entity);
                    if (c.HasValue) { aggro = c.Value; world = m.World; handle = m.Entity; return true; }
                    break;
                }
            case NpcComponentBundle n:
                {
                    var c = n.World.TryGetComponent<AggroComponent>(n.Entity);
                    if (c.HasValue) { aggro = c.Value; world = n.World; handle = n.Entity; return true; }
                    break;
                }
        }
        aggro = default;
        world = null!;
        handle = default;
        return false;
    }
}
