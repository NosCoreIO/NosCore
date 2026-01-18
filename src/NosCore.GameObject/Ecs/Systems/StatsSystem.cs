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
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Systems;

public interface IStatsSystem
{
    StPacket GenerateStatPacket(PlayerContext player);
    StPacket GenerateStatPacket(World world, Entity entity);
    StPacket GenerateStatPacket(Entity entity, MapWorld world);
    void SetLevel(PlayerContext player, byte level);
    void SetLevel(World world, Entity entity, byte level);
}

public class StatsSystem : IStatsSystem
{
    public StPacket GenerateStatPacket(PlayerContext player)
    {
        return new StPacket
        {
            Type = VisualType.Player,
            VisualId = player.VisualId,
            Level = player.Level,
            HeroLvl = player.HeroLevel,
            HpPercentage = player.MaxHp > 0 ? (int)(player.Hp / (float)player.MaxHp * 100) : 0,
            MpPercentage = player.MaxMp > 0 ? (int)(player.Mp / (float)player.MaxMp * 100) : 0,
            CurrentHp = player.Hp,
            CurrentMp = player.Mp,
            BuffIds = null
        };
    }

    public void SetLevel(PlayerContext player, byte level)
    {
        player.SetLevel(level);
        player.SetHp(player.MaxHp);
        player.SetMp(player.MaxMp);
    }

    public StPacket GenerateStatPacket(World world, Entity entity)
    {
        ref var identity = ref world.Get<EntityIdentityComponent>(entity);
        ref var health = ref world.Get<HealthComponent>(entity);
        ref var mana = ref world.Get<ManaComponent>(entity);
        ref var npcData = ref world.Get<NpcDataComponent>(entity);
        ref var combat = ref world.Get<CombatComponent>(entity);

        return new StPacket
        {
            Type = identity.VisualType,
            VisualId = identity.VisualId,
            Level = npcData.Level,
            HeroLvl = combat.HeroLevel,
            HpPercentage = health.MaxHp > 0 ? (int)(health.Hp / (float)health.MaxHp * 100) : 0,
            MpPercentage = mana.MaxMp > 0 ? (int)(mana.Mp / (float)mana.MaxMp * 100) : 0,
            CurrentHp = health.Hp,
            CurrentMp = mana.Mp,
            BuffIds = null
        };
    }

    public StPacket GenerateStatPacket(Entity entity, MapWorld world)
    {
        return GenerateStatPacket(world.World, entity);
    }

    public void SetLevel(World world, Entity entity, byte level)
    {
        ref var npcData = ref world.Get<NpcDataComponent>(entity);
        ref var health = ref world.Get<HealthComponent>(entity);
        ref var mana = ref world.Get<ManaComponent>(entity);

        npcData = npcData with { Level = level };
        health = health with { Hp = health.MaxHp };
        mana = mana with { Mp = mana.MaxMp };
    }
}
