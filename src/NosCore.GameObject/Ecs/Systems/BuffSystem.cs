//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Arch.Core;
using NodaTime;
using NosCore.GameObject.Ecs.Components;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Systems;

public class BuffSystem
{
    // Mates carry buff state but take no buffs yet, so they are never due.
    private readonly QueryDescription _buffedQuery = new QueryDescription()
        .WithAll<EntityIdentityComponent, BuffStateComponent>()
        .WithNone<MateStateComponent>();

    // Lists the entities with a buff to expire or a periodic effect to fire; the rest of the
    // map, most of it buffless, costs one check each.
    public void FindDue(MapWorld world, Instant now, List<(Entity Entity, VisualType VisualType)> due)
    {
        world.Exclusive(w => w.Query(in _buffedQuery, (Entity entity, ref EntityIdentityComponent identity, ref BuffStateComponent state) =>
        {
            if (state.ActiveBuffs.IsEmpty)
            {
                return;
            }

            foreach (var (_, buff) in state.ActiveBuffs)
            {
                if (buff.IsDue(now))
                {
                    due.Add((entity, identity.VisualType));
                    return;
                }
            }
        }));
    }
}
