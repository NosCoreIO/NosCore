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
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Systems;

public interface ICondSystem
{
    CondPacket GenerateCondPacket(PlayerContext player);
    CondPacket GenerateCondPacket(Entity entity, MapWorld world);
}

public class CondSystem : ICondSystem
{
    public CondPacket GenerateCondPacket(PlayerContext player)
    {
        return new CondPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            NoAttack = player.NoAttack,
            NoMove = player.NoMove,
            Speed = player.Speed
        };
    }

    public CondPacket GenerateCondPacket(Entity entity, MapWorld world)
    {
        ref var identity = ref world.World.Get<EntityIdentityComponent>(entity);
        ref var combat = ref world.World.Get<CombatComponent>(entity);
        ref var movement = ref world.World.Get<NpcMovementComponent>(entity);

        return new CondPacket
        {
            VisualType = identity.VisualType,
            VisualId = identity.VisualId,
            NoAttack = combat.NoAttack,
            NoMove = movement.NoMove,
            Speed = movement.Speed
        };
    }
}
