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
    private readonly QueryDescription _pendingQuery = new QueryDescription().WithAll<SkillResetComponent>();

    // Claims every skill whose cooldown has run out, so each reset is handed out once. A recast
    // that moved the deadline in the meantime keeps its entry.
    public void TakeReady(MapWorld world, Instant now, List<(Entity Entity, long CastId)> ready)
    {
        world.Exclusive(w => w.Query(in _pendingQuery, (Entity entity, ref SkillResetComponent resets) =>
        {
            if (resets.PendingResets.IsEmpty)
            {
                return;
            }

            foreach (var (castId, readyAt) in resets.PendingResets)
            {
                if (readyAt <= now && resets.PendingResets.TryRemove(new KeyValuePair<long, Instant>(castId, readyAt)))
                {
                    ready.Add((entity, castId));
                }
            }
        }));
    }
}
