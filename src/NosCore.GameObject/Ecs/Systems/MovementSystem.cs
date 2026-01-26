//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using Arch.Core;
using NodaTime;
using NosCore.GameObject.Ecs.Components;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Helpers;

namespace NosCore.GameObject.Ecs.Systems;

public class MovementSystem
{
    private readonly IClock _clock;
    private readonly IHeuristic _distanceCalculator;
    private readonly QueryDescription _movableQuery;

    public MovementSystem(IClock clock, IHeuristic distanceCalculator)
    {
        _clock = clock;
        _distanceCalculator = distanceCalculator;
        _movableQuery = new QueryDescription()
            .WithAll<PositionComponent, NpcDataComponent, SpawnComponent, TimingComponent, HealthComponent>();
    }

    public void Update(MapWorld world, Map.Map map, Action<Entity, MoveData> onMove)
    {
        var now = _clock.GetCurrentInstant();

        world.World.Query(in _movableQuery, (Entity entity, ref PositionComponent position, ref NpcDataComponent npcData, ref SpawnComponent spawn, ref TimingComponent timing, ref HealthComponent health) =>
        {
            if (!health.IsAlive || !spawn.IsMoving || npcData.Speed <= 0)
            {
                return;
            }

            var timeSinceLastMove = (now - timing.LastMove).TotalMilliseconds;
            if (timeSinceLastMove <= RandomHelper.Instance.RandomNumber(400, 3200))
            {
                return;
            }

            var mapX = position.PositionX;
            var mapY = position.PositionY;

            if (!map.GetFreePosition(ref mapX, ref mapY,
                (byte)RandomHelper.Instance.RandomNumber(0, 3),
                (byte)RandomHelper.Instance.RandomNumber(0, 3)))
            {
                return;
            }

            var distance = (int)_distanceCalculator.GetDistance(
                (position.PositionX, position.PositionY),
                (mapX, mapY));

            var moveDuration = 1000d * distance / (2 * npcData.Speed);

            timing = timing with { LastMove = now.Plus(Duration.FromMilliseconds(moveDuration)) };
            position = position with { PositionX = mapX, PositionY = mapY };

            onMove(entity, new MoveData(mapX, mapY, npcData.Speed));
        });
    }
}

public readonly record struct MoveData(short MapX, short MapY, byte Speed);
