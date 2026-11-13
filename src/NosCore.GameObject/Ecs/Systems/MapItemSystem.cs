//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Arch.Core;
using NosCore.GameObject.Ecs.Components;

namespace NosCore.GameObject.Ecs.Systems;

public class MapItemSystem
{
    private readonly QueryDescription _itemsQuery = new QueryDescription()
        .WithAll<EntityIdentityComponent, PositionComponent, MapItemDataComponent>();

    public List<Entity> GetAll(MapWorld world)
    {
        var items = new List<Entity>();
        world.Exclusive(w => w.Query(in _itemsQuery, (Entity entity) => items.Add(entity)));
        return items;
    }

    public int Count(MapWorld world) => world.Exclusive(w => w.CountEntities(in _itemsQuery));

    public Entity? Find(MapWorld world, long visualId)
    {
        Entity? found = null;
        world.Exclusive(w => w.Query(in _itemsQuery, (Entity entity, ref EntityIdentityComponent identity) =>
        {
            if (identity.VisualId == visualId)
            {
                found = entity;
            }
        }));
        return found;
    }
}
