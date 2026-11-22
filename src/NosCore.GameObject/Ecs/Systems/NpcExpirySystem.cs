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

public class NpcExpirySystem
{
    private readonly QueryDescription _expiringQuery = new QueryDescription()
        .WithAll<NpcStateComponent, ExpiryComponent>();

    public void TakeExpired(MapWorld world, Instant now, List<Entity> expired)
    {
        world.Exclusive(w => w.Query(in _expiringQuery, (Entity entity, ref ExpiryComponent expiry) =>
        {
            if (expiry.ExpiresAt <= now)
            {
                expired.Add(entity);
            }
        }));
    }
}
