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
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Systems;

public interface IMorphSystem
{
    CModePacket GenerateCModePacket(PlayerContext player);
    CModePacket GenerateCModePacket(World world, Entity entity);
    CModePacket GenerateCModePacket(Entity entity, MapWorld world);
    CharScPacket GenerateCharScPacket(PlayerContext player);
    CharScPacket GenerateCharScPacket(World world, Entity entity);
    CharScPacket GenerateCharScPacket(Entity entity, MapWorld world);
    void SetSize(PlayerContext player, byte size);
    void SetSize(World world, Entity entity, byte size);
    void SetMorph(PlayerContext player, short morph, byte morphUpgrade, short morphDesign, byte morphBonus);
    void SetMorph(World world, Entity entity, short morph, byte morphUpgrade, short morphDesign, byte morphBonus);
}

public class MorphSystem : IMorphSystem
{
    public CModePacket GenerateCModePacket(PlayerContext player)
    {
        return new CModePacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Morph = player.Morph,
            MorphUpgrade = player.MorphUpgrade,
            MorphDesign = player.MorphDesign,
            MorphBonus = player.MorphBonus,
            Size = player.Size
        };
    }

    public CharScPacket GenerateCharScPacket(PlayerContext player)
    {
        return new CharScPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            Size = player.Size
        };
    }

    public void SetSize(PlayerContext player, byte size)
    {
        player.SetSize(size);
    }

    public void SetMorph(PlayerContext player, short morph, byte morphUpgrade, short morphDesign, byte morphBonus)
    {
        player.SetMorph(morph, morphUpgrade, morphDesign, morphBonus);
    }

    public CModePacket GenerateCModePacket(World world, Entity entity)
    {
        ref var identity = ref world.Get<EntityIdentityComponent>(entity);
        ref var visual = ref world.Get<VisualComponent>(entity);

        return new CModePacket
        {
            VisualType = identity.VisualType,
            VisualId = identity.VisualId,
            Morph = visual.Morph,
            MorphUpgrade = visual.MorphUpgrade,
            MorphDesign = visual.MorphDesign,
            MorphBonus = visual.MorphBonus,
            Size = visual.Size
        };
    }

    public CModePacket GenerateCModePacket(Entity entity, MapWorld world)
    {
        return GenerateCModePacket(world.World, entity);
    }

    public CharScPacket GenerateCharScPacket(World world, Entity entity)
    {
        ref var identity = ref world.Get<EntityIdentityComponent>(entity);
        ref var visual = ref world.Get<VisualComponent>(entity);

        return new CharScPacket
        {
            VisualType = identity.VisualType,
            VisualId = identity.VisualId,
            Size = visual.Size
        };
    }

    public CharScPacket GenerateCharScPacket(Entity entity, MapWorld world)
    {
        return GenerateCharScPacket(world.World, entity);
    }

    public void SetSize(World world, Entity entity, byte size)
    {
        ref var visual = ref world.Get<VisualComponent>(entity);
        visual = visual with { Size = size };
    }

    public void SetMorph(World world, Entity entity, short morph, byte morphUpgrade, short morphDesign, byte morphBonus)
    {
        ref var visual = ref world.Get<VisualComponent>(entity);
        visual = visual with
        {
            Morph = morph,
            MorphUpgrade = morphUpgrade,
            MorphDesign = morphDesign,
            MorphBonus = morphBonus
        };
    }
}
