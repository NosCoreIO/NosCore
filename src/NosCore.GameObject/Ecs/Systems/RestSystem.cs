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
using NosCore.Packets.ServerPackets.Entities;
using System.Threading.Tasks;
using NosCore.Networking;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Systems;

public interface IRestSystem
{
    Task ToggleRestAsync(PlayerContext player);
    Task SetRestAsync(PlayerContext player, bool isSitting);
    Task ToggleRestAsync(World world, Entity entity, MapInstance mapInstance);
    Task SetRestAsync(World world, Entity entity, MapInstance mapInstance, bool isSitting);
    Task ToggleRestAsync(Entity entity, MapWorld world, MapInstance mapInstance);
    Task SetRestAsync(Entity entity, MapWorld world, MapInstance mapInstance, bool isSitting);
}

public class RestSystem : IRestSystem
{
    public Task ToggleRestAsync(PlayerContext player)
    {
        var newIsSitting = !player.IsSitting;
        player.SetIsSitting(newIsSitting);

        return player.MapInstance.SendPacketAsync(new RestPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            IsSitting = newIsSitting
        });
    }

    public Task SetRestAsync(PlayerContext player, bool isSitting)
    {
        player.SetIsSitting(isSitting);

        return player.MapInstance.SendPacketAsync(new RestPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            IsSitting = isSitting
        });
    }

    public Task ToggleRestAsync(World world, Entity entity, MapInstance mapInstance)
    {
        ref var visual = ref world.Get<VisualComponent>(entity);
        ref var identity = ref world.Get<EntityIdentityComponent>(entity);

        visual = visual with { IsSitting = !visual.IsSitting };

        return mapInstance.SendPacketAsync(new RestPacket
        {
            VisualType = identity.VisualType,
            VisualId = identity.VisualId,
            IsSitting = visual.IsSitting
        });
    }

    public Task SetRestAsync(World world, Entity entity, MapInstance mapInstance, bool isSitting)
    {
        ref var visual = ref world.Get<VisualComponent>(entity);
        ref var identity = ref world.Get<EntityIdentityComponent>(entity);

        visual = visual with { IsSitting = isSitting };

        return mapInstance.SendPacketAsync(new RestPacket
        {
            VisualType = identity.VisualType,
            VisualId = identity.VisualId,
            IsSitting = isSitting
        });
    }

    public Task ToggleRestAsync(Entity entity, MapWorld world, MapInstance mapInstance)
    {
        return ToggleRestAsync(world.World, entity, mapInstance);
    }

    public Task SetRestAsync(Entity entity, MapWorld world, MapInstance mapInstance, bool isSitting)
    {
        return SetRestAsync(world.World, entity, mapInstance, isSitting);
    }
}
