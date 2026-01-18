//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Arch.Core;
using NodaTime;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Helpers;

namespace NosCore.GameObject.Ecs.Systems;

public class MovementSystem(IHeuristic distanceCalculator, IClock clock)
{
    private readonly QueryDescription _movableQuery = new QueryDescription()
        .WithAll<EntityIdentityComponent, PositionComponent, SpawnComponent, NpcMovementComponent, HealthComponent>();

    public void Update(World world, MapInstance mapInstance)
    {
        var now = clock.GetCurrentInstant();

        world.Query(in _movableQuery, (Entity entity, ref EntityIdentityComponent identity, ref PositionComponent position, ref SpawnComponent spawn, ref NpcMovementComponent movement, ref HealthComponent health) =>
        {
            if (!health.IsAlive || !movement.IsMoving || movement.NoMove || movement.Speed <= 0)
            {
                return;
            }

            if (now < movement.NextMoveTime)
            {
                return;
            }

            var mapX = spawn.SpawnX;
            var mapY = spawn.SpawnY;

            if (!mapInstance.Map.GetFreePosition(ref mapX, ref mapY,
                (byte)RandomHelper.Instance.RandomNumber(0, 3),
                (byte)RandomHelper.Instance.RandomNumber(0, 3)))
            {
                return;
            }

            var distance = (int)distanceCalculator.GetDistance(
                (position.PositionX, position.PositionY),
                (mapX, mapY));

            var moveDurationMs = 1000d * distance / (2 * movement.Speed);
            var nextMoveDelay = RandomHelper.Instance.RandomNumber(400, 3200);

            position = position with
            {
                PositionX = mapX,
                PositionY = mapY
            };

            movement = movement with
            {
                LastMove = now,
                NextMoveTime = now.Plus(Duration.FromMilliseconds(moveDurationMs + nextMoveDelay))
            };

            var movePacket = new MovePacket
            {
                VisualType = identity.VisualType,
                VisualEntityId = identity.VisualId,
                MapX = mapX,
                MapY = mapY,
                Speed = movement.Speed
            };

            _ = mapInstance.SendPacketAsync(movePacket);
        });
    }
}
