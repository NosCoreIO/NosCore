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

public class RespawnSystem
{
    private readonly QueryDescription _pendingQuery = new QueryDescription().WithAll<RespawnComponent>();

    // Claims every entity whose time has come, so a respawn is only ever handed out once.
    public void TakeDue(MapWorld world, Instant now, List<Entity> due)
    {
        world.Exclusive(w => w.Query(in _pendingQuery, (Entity entity, ref RespawnComponent respawn) =>
        {
            if (respawn.RespawnAt is { } at && at <= now)
            {
                respawn = new RespawnComponent(null);
                due.Add(entity);
            }
        }));
    }
}
