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

using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using System.Threading.Tasks;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Movement;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Systems;

public interface IDirectionSystem
{
    Task ChangeDirectionAsync(PlayerContext player, byte direction);
    Task ChangeDirectionAsync(Entity entity, MapWorld world, MapInstance mapInstance, byte direction);
}

public class DirectionSystem : IDirectionSystem
{
    public Task ChangeDirectionAsync(PlayerContext player, byte direction)
    {
        player.SetDirection(direction);

        return player.MapInstance.SendPacketAsync(new DirPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Direction = direction
        });
    }

    public Task ChangeDirectionAsync(Entity entity, MapWorld world, MapInstance mapInstance, byte direction)
    {
        ref var position = ref world.World.Get<PositionComponent>(entity);
        ref var identity = ref world.World.Get<EntityIdentityComponent>(entity);

        position = position with { Direction = direction };

        return mapInstance.SendPacketAsync(new DirPacket
        {
            VisualType = identity.VisualType,
            VisualId = identity.VisualId,
            Direction = direction
        });
    }
}
