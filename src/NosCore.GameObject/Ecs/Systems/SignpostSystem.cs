//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using Arch.Core;
using NodaTime;
using NosCore.GameObject.Ecs.Components;

namespace NosCore.GameObject.Ecs.Systems;

// A signpost's health is the share of its time it has left, which is what its in line shows.
public class SignpostSystem
{
    public static readonly Duration Lifetime = Duration.FromMinutes(60);

    private readonly QueryDescription _signpostsQuery = new QueryDescription()
        .WithAll<SignpostComponent, HealthComponent>();

    public void TakeExpired(MapWorld world, Instant now, List<Entity> expired)
    {
        world.Exclusive(w => w.Query(in _signpostsQuery, (Entity entity, ref SignpostComponent signpost, ref HealthComponent health) =>
        {
            if (signpost.SignpostOwnerId == 0)
            {
                return;
            }

            var left = signpost.SignpostExpiresAt - now;
            if (left <= Duration.Zero)
            {
                expired.Add(entity);
                return;
            }

            health = health with { Hp = (int)Math.Ceiling(health.MaxHp * ((double)left.TotalTicks / Lifetime.TotalTicks)) };
        }));
    }
}
