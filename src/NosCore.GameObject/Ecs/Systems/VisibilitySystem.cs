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
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Systems;

public interface IVisibilitySystem
{
    InPacket GenerateInPacket(PlayerContext player);
    InPacket GenerateInPacket(World world, Entity entity);
    InPacket GenerateInPacket(Entity entity, MapWorld world);
    InPacket GenerateMonsterInPacket(Entity entity, MapWorld world);
    InPacket GenerateNpcInPacket(Entity entity, MapWorld world);
    InPacket GenerateNpcInPacket(World world, Entity entity, short? dialog);
    InPacket GenerateNpcInPacket(Entity entity, MapWorld world, short? dialog);
    InPacket GenerateCountableEntityInPacket(MapItemRef entity);
    SpeakPacket GenerateSpk(PlayerContext player, SpeakPacket packet);
}

public class VisibilitySystem : IVisibilitySystem
{
    public InPacket GenerateInPacket(PlayerContext player)
    {
        return new InPacket
        {
            VisualType = VisualType.Player,
            Name = null,
            VisualId = player.VisualId,
            VNum = player.VNum == 0 ? string.Empty : player.VNum.ToString(),
            PositionX = player.PositionX,
            PositionY = player.PositionY,
            Direction = player.Direction,
            InNonPlayerSubPacket = new InNonPlayerSubPacket
            {
                Dialog = 0,
                InAliveSubPacket = new InAliveSubPacket
                {
                    Mp = player.MaxMp > 0 ? (int)(player.Mp / (float)player.MaxMp * 100) : 0,
                    Hp = player.MaxHp > 0 ? (int)(player.Hp / (float)player.MaxHp * 100) : 0
                },
                IsSitting = player.IsSitting,
                SpawnEffect = SpawnEffectType.NoEffect,
                Unknow1 = 2
            }
        };
    }

    public InPacket GenerateInPacket(World world, Entity entity)
    {
        ref var identity = ref world.Get<EntityIdentityComponent>(entity);
        ref var position = ref world.Get<PositionComponent>(entity);
        ref var health = ref world.Get<HealthComponent>(entity);
        ref var mana = ref world.Get<ManaComponent>(entity);
        ref var visual = ref world.Get<VisualComponent>(entity);

        return new InPacket
        {
            VisualType = identity.VisualType,
            Name = null,
            VisualId = identity.VisualId,
            VNum = identity.VNum == 0 ? string.Empty : identity.VNum.ToString(),
            PositionX = position.PositionX,
            PositionY = position.PositionY,
            Direction = position.Direction,
            InNonPlayerSubPacket = new InNonPlayerSubPacket
            {
                Dialog = 0,
                InAliveSubPacket = new InAliveSubPacket
                {
                    Mp = mana.MaxMp > 0 ? (int)(mana.Mp / (float)mana.MaxMp * 100) : 0,
                    Hp = health.MaxHp > 0 ? (int)(health.Hp / (float)health.MaxHp * 100) : 0
                },
                IsSitting = visual.IsSitting,
                SpawnEffect = SpawnEffectType.NoEffect,
                Unknow1 = 2
            }
        };
    }

    public InPacket GenerateInPacket(Entity entity, MapWorld world)
    {
        return GenerateInPacket(world.World, entity);
    }

    public InPacket GenerateNpcInPacket(World world, Entity entity, short? dialog)
    {
        var packet = GenerateInPacket(world, entity);
        if (packet.InNonPlayerSubPacket != null)
        {
            packet.InNonPlayerSubPacket.Dialog = dialog ?? 0;
        }
        return packet;
    }

    public InPacket GenerateNpcInPacket(Entity entity, MapWorld world, short? dialog)
    {
        return GenerateNpcInPacket(world.World, entity, dialog);
    }

    public InPacket GenerateMonsterInPacket(Entity entity, MapWorld world)
    {
        return GenerateInPacket(world.World, entity);
    }

    public InPacket GenerateNpcInPacket(Entity entity, MapWorld world)
    {
        var dialog = entity.GetDialog(world);
        return GenerateNpcInPacket(world.World, entity, dialog);
    }

    public InPacket GenerateCountableEntityInPacket(MapItemRef entity)
    {
        return new InPacket
        {
            VisualType = entity.VisualType,
            VisualId = entity.VisualId,
            VNum = entity.VNum == 0 ? string.Empty : entity.VNum.ToString(),
            PositionX = entity.PositionX,
            PositionY = entity.PositionY,
            InItemSubPacket = new InItemSubPacket
            {
                Amount = entity.Amount,
                IsQuestRelative = false,
                Owner = 0
            }
        };
    }

    public SpeakPacket GenerateSpk(PlayerContext player, SpeakPacket packet)
    {
        return new SpeakPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            SpeakType = packet.SpeakType,
            EntityName = player.Name,
            Message = packet.Message
        };
    }
}
