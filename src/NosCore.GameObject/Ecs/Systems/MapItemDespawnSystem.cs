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

public class MapItemDespawnSystem
{
    public static readonly Duration Lifetime = Duration.FromMinutes(3);

    private readonly QueryDescription _itemsQuery = new QueryDescription()
        .WithAll<EntityIdentityComponent, MapItemDataComponent>();

    private readonly List<Entity> _expired = new();

    // Destroys under the same lock as the query, so a pick-up cannot claim an item in between.
    public void TakeExpired(MapWorld world, Instant now, List<long> removed)
    {
        var droppedBefore = now - Lifetime;
        world.Exclusive(w =>
        {
            w.Query(in _itemsQuery, (Entity entity, ref EntityIdentityComponent identity, ref MapItemDataComponent item) =>
            {
                if (item.DroppedAt <= droppedBefore)
                {
                    _expired.Add(entity);
                    removed.Add(identity.VisualId);
                }
            });

            foreach (var entity in _expired)
            {
                w.Destroy(entity);
            }

            _expired.Clear();
        });
    }
}
