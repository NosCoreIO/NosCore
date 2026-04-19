//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using Arch.Core;
using NosCore.GameObject.Ecs.Components;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Systems;

public class VisibilitySystem
{
    private readonly QueryDescription _npcDataQuery;
    private readonly QueryDescription _itemsQuery;
    private readonly QueryDescription _positionQuery;

    public VisibilitySystem()
    {
        _npcDataQuery = new QueryDescription()
            .WithAll<EntityIdentityComponent, PositionComponent, HealthComponent, NpcDataComponent, VisualComponent, SpawnComponent>();

        _itemsQuery = new QueryDescription()
            .WithAll<EntityIdentityComponent, PositionComponent, MapItemDataComponent>();

        _positionQuery = new QueryDescription().WithAll<PositionComponent>();
    }

    public IEnumerable<Entity> GetMonsterEntities(MapWorld world)
    {
        var monsters = new List<Entity>();
        world.World.Query(in _npcDataQuery, (Entity entity) =>
        {
            var identity = world.World.Get<EntityIdentityComponent>(entity);
            if (identity.VisualType == VisualType.Monster)
            {
                monsters.Add(entity);
            }
        });
        return monsters;
    }

    public IEnumerable<Entity> GetNpcEntities(MapWorld world)
    {
        var npcs = new List<Entity>();
        world.World.Query(in _npcDataQuery, (Entity entity) =>
        {
            var identity = world.World.Get<EntityIdentityComponent>(entity);
            if (identity.VisualType == VisualType.Npc)
            {
                npcs.Add(entity);
            }
        });
        return npcs;
    }

    public IEnumerable<Entity> GetMapItemEntities(MapWorld world)
    {
        var items = new List<Entity>();
        world.World.Query(in _itemsQuery, (Entity entity) =>
        {
            items.Add(entity);
        });
        return items;
    }

    public IEnumerable<Entity> GetEntitiesInRange(MapWorld world, short centerX, short centerY, int range)
    {
        var entities = new List<Entity>();

        world.World.Query(in _positionQuery, (Entity entity, ref PositionComponent position) =>
        {
            var dx = Math.Abs(position.PositionX - centerX);
            var dy = Math.Abs(position.PositionY - centerY);
            if (dx <= range && dy <= range)
            {
                entities.Add(entity);
            }
        });

        return entities;
    }
}
