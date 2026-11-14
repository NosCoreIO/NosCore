//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Arch.Core;
using NodaTime;
using NosCore.GameObject.Ecs.Components;

namespace NosCore.GameObject.Ecs.Systems;

public class SkillResetSystem
{
    private readonly QueryDescription _pendingQuery =
        new QueryDescription().WithAll<SkillResetComponent>().WithNone<MateStateComponent>();

    private readonly QueryDescription _pendingPetQuery =
        new QueryDescription().WithAll<SkillResetComponent, MateStateComponent>();

    // Claims every skill whose cooldown has run out, so each reset is handed out once.
    public void TakeReady(MapWorld world, Instant now, List<(Entity Entity, long CastId)> ready)
    {
        world.Exclusive(w => w.Query(in _pendingQuery, (Entity entity, ref SkillResetComponent resets) =>
        {
            if (resets.PendingResets.IsEmpty)
            {
                return;
            }

            foreach (var castId in Claim(resets, now))
            {
                ready.Add((entity, castId));
            }
        }));
    }

    // The same for the slots of pets' skill bars, each with the owner to tell.
    public void TakeReadyPetSlots(MapWorld world, Instant now, List<(long OwnerId, short Slot)> ready)
    {
        world.Exclusive(w => w.Query(in _pendingPetQuery, (ref SkillResetComponent resets, ref MateStateComponent mate) =>
        {
            if (resets.PendingResets.IsEmpty)
            {
                return;
            }

            foreach (var slot in Claim(resets, now))
            {
                ready.Add((mate.OwnerId, (short)slot));
            }
        }));
    }

    private static IEnumerable<long> Claim(SkillResetComponent resets, Instant now)
    {
        foreach (var (key, readyAt) in resets.PendingResets)
        {
            if (readyAt <= now && resets.PendingResets.TryRemove(key, out _))
            {
                yield return key;
            }
        }
    }
}
