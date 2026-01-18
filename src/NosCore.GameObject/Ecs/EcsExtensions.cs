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

using System;
using Arch.Core;
using NodaTime;
using NosCore.GameObject.Ecs.Components;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs;

public static class EcsExtensions
{
    // Core component getters
    public static PositionComponent? GetPosition(this Entity entity, MapWorld world)
        => world.TryGetComponent<PositionComponent>(entity);

    public static EntityIdentityComponent? GetIdentity(this Entity entity, MapWorld world)
        => world.TryGetComponent<EntityIdentityComponent>(entity);

    public static HealthComponent? GetHealth(this Entity entity, MapWorld world)
        => world.TryGetComponent<HealthComponent>(entity);

    public static ManaComponent? GetMana(this Entity entity, MapWorld world)
        => world.TryGetComponent<ManaComponent>(entity);

    public static VisualComponent? GetVisual(this Entity entity, MapWorld world)
        => world.TryGetComponent<VisualComponent>(entity);

    public static NpcMovementComponent? GetMovement(this Entity entity, MapWorld world)
        => world.TryGetComponent<NpcMovementComponent>(entity);

    public static CombatComponent? GetCombat(this Entity entity, MapWorld world)
        => world.TryGetComponent<CombatComponent>(entity);

    public static NpcDataComponent? GetNpcData(this Entity entity, MapWorld world)
        => world.TryGetComponent<NpcDataComponent>(entity);

    public static MapItemComponent? GetMapItem(this Entity entity, MapWorld world)
        => world.TryGetComponent<MapItemComponent>(entity);

    public static PortalComponent? GetPortal(this Entity entity, MapWorld world)
        => world.TryGetComponent<PortalComponent>(entity);

    // Position helpers
    public static short GetPositionX(this Entity entity, MapWorld world) => entity.GetPosition(world)?.PositionX ?? 0;
    public static short GetPositionY(this Entity entity, MapWorld world) => entity.GetPosition(world)?.PositionY ?? 0;
    public static byte GetDirection(this Entity entity, MapWorld world) => entity.GetPosition(world)?.Direction ?? 0;

    public static void SetPosition(this Entity entity, MapWorld world, short x, short y)
    {
        var pos = entity.GetPosition(world);
        if (pos != null)
        {
            world.SetComponent(entity, pos.Value with { PositionX = x, PositionY = y });
        }
    }

    public static void SetDirection(this Entity entity, MapWorld world, byte direction)
    {
        var pos = entity.GetPosition(world);
        if (pos != null)
        {
            world.SetComponent(entity, pos.Value with { Direction = direction });
        }
    }

    // Identity helpers
    public static VisualType GetVisualType(this Entity entity, MapWorld world) => entity.GetIdentity(world)?.VisualType ?? VisualType.Player;
    public static long GetVisualId(this Entity entity, MapWorld world) => entity.GetIdentity(world)?.VisualId ?? 0;
    public static short GetVNum(this Entity entity, MapWorld world) => entity.GetIdentity(world)?.VNum ?? 0;

    // Health helpers
    public static int GetHp(this Entity entity, MapWorld world) => entity.GetHealth(world)?.Hp ?? 0;
    public static int GetMaxHp(this Entity entity, MapWorld world) => entity.GetHealth(world)?.MaxHp ?? 0;
    public static bool GetIsAlive(this Entity entity, MapWorld world) => entity.GetHealth(world)?.IsAlive ?? false;

    public static void SetHp(this Entity entity, MapWorld world, int hp)
    {
        var health = entity.GetHealth(world);
        if (health != null)
        {
            world.SetComponent(entity, health.Value with { Hp = hp });
        }
    }

    public static void SetIsAlive(this Entity entity, MapWorld world, bool isAlive)
    {
        var health = entity.GetHealth(world);
        if (health != null)
        {
            world.SetComponent(entity, health.Value with { IsAlive = isAlive });
        }
    }

    // Mana helpers
    public static int GetMp(this Entity entity, MapWorld world) => entity.GetMana(world)?.Mp ?? 0;
    public static int GetMaxMp(this Entity entity, MapWorld world) => entity.GetMana(world)?.MaxMp ?? 0;

    public static void SetMp(this Entity entity, MapWorld world, int mp)
    {
        var mana = entity.GetMana(world);
        if (mana != null)
        {
            world.SetComponent(entity, mana.Value with { Mp = mp });
        }
    }

    // Visual helpers
    public static byte GetSize(this Entity entity, MapWorld world) => entity.GetVisual(world)?.Size ?? 10;
    public static short GetMorph(this Entity entity, MapWorld world) => entity.GetVisual(world)?.Morph ?? 0;
    public static byte GetMorphUpgrade(this Entity entity, MapWorld world) => entity.GetVisual(world)?.MorphUpgrade ?? 0;
    public static short GetMorphDesign(this Entity entity, MapWorld world) => entity.GetVisual(world)?.MorphDesign ?? 0;
    public static byte GetMorphBonus(this Entity entity, MapWorld world) => entity.GetVisual(world)?.MorphBonus ?? 0;
    public static short GetEffect(this Entity entity, MapWorld world) => entity.GetVisual(world)?.Effect ?? 0;
    public static short GetEffectDelay(this Entity entity, MapWorld world) => entity.GetVisual(world)?.EffectDelay ?? 0;
    public static bool GetIsSitting(this Entity entity, MapWorld world) => entity.GetVisual(world)?.IsSitting ?? false;
    public static bool GetInvisible(this Entity entity, MapWorld world) => entity.GetVisual(world)?.Invisible ?? false;
    public static bool GetCamouflage(this Entity entity, MapWorld world) => entity.GetVisual(world)?.Camouflage ?? false;
    public static bool GetUseSp(this Entity entity, MapWorld world) => entity.GetVisual(world)?.UseSp ?? false;
    public static bool GetIsVehicled(this Entity entity, MapWorld world) => entity.GetVisual(world)?.IsVehicled ?? false;
    public static byte? GetVehicleSpeed(this Entity entity, MapWorld world) => entity.GetVisual(world)?.VehicleSpeed;

    public static void SetSize(this Entity entity, MapWorld world, byte size)
    {
        var visual = entity.GetVisual(world);
        if (visual != null)
        {
            world.SetComponent(entity, visual.Value with { Size = size });
        }
    }

    public static void SetIsSitting(this Entity entity, MapWorld world, bool isSitting)
    {
        var visual = entity.GetVisual(world);
        if (visual != null)
        {
            world.SetComponent(entity, visual.Value with { IsSitting = isSitting });
        }
    }

    public static void SetInvisible(this Entity entity, MapWorld world, bool invisible)
    {
        var visual = entity.GetVisual(world);
        if (visual != null)
        {
            world.SetComponent(entity, visual.Value with { Invisible = invisible });
        }
    }

    public static void SetCamouflage(this Entity entity, MapWorld world, bool camouflage)
    {
        var visual = entity.GetVisual(world);
        if (visual != null)
        {
            world.SetComponent(entity, visual.Value with { Camouflage = camouflage });
        }
    }

    public static void SetUseSp(this Entity entity, MapWorld world, bool useSp)
    {
        var visual = entity.GetVisual(world);
        if (visual != null)
        {
            world.SetComponent(entity, visual.Value with { UseSp = useSp });
        }
    }

    public static void SetIsVehicled(this Entity entity, MapWorld world, bool isVehicled)
    {
        var visual = entity.GetVisual(world);
        if (visual != null)
        {
            world.SetComponent(entity, visual.Value with { IsVehicled = isVehicled });
        }
    }

    public static void SetVehicleSpeed(this Entity entity, MapWorld world, byte? vehicleSpeed)
    {
        var visual = entity.GetVisual(world);
        if (visual != null)
        {
            world.SetComponent(entity, visual.Value with { VehicleSpeed = vehicleSpeed });
        }
    }

    public static void SetMorph(this Entity entity, MapWorld world, short morph, byte morphUpgrade, short morphDesign, byte morphBonus)
    {
        var visual = entity.GetVisual(world);
        if (visual != null)
        {
            world.SetComponent(entity, visual.Value with
            {
                Morph = morph,
                MorphUpgrade = morphUpgrade,
                MorphDesign = morphDesign,
                MorphBonus = morphBonus
            });
        }
    }

    public static void SetEffect(this Entity entity, MapWorld world, short effect, short effectDelay)
    {
        var visual = entity.GetVisual(world);
        if (visual != null)
        {
            world.SetComponent(entity, visual.Value with { Effect = effect, EffectDelay = effectDelay });
        }
    }

    // Movement helpers
    public static byte GetSpeed(this Entity entity, MapWorld world) => entity.GetMovement(world)?.Speed ?? 0;
    public static bool GetIsMoving(this Entity entity, MapWorld world) => entity.GetMovement(world)?.IsMoving ?? false;
    public static bool GetNoMove(this Entity entity, MapWorld world) => entity.GetMovement(world)?.NoMove ?? false;
    public static Instant GetLastMove(this Entity entity, MapWorld world) => entity.GetMovement(world)?.LastMove ?? default;

    public static void SetLastMove(this Entity entity, MapWorld world, Instant lastMove)
    {
        var movement = entity.GetMovement(world);
        if (movement != null)
        {
            world.SetComponent(entity, movement.Value with { LastMove = lastMove });
        }
    }

    public static void SetNoMove(this Entity entity, MapWorld world, bool noMove)
    {
        var movement = entity.GetMovement(world);
        if (movement != null)
        {
            world.SetComponent(entity, movement.Value with { NoMove = noMove });
        }
    }

    public static void SetSpeed(this Entity entity, MapWorld world, byte speed)
    {
        var movement = entity.GetMovement(world);
        if (movement != null)
        {
            world.SetComponent(entity, movement.Value with { Speed = speed });
        }
    }

    // Combat helpers
    public static bool GetNoAttack(this Entity entity, MapWorld world) => entity.GetCombat(world)?.NoAttack ?? false;
    public static byte GetHeroLevel(this Entity entity, MapWorld world) => entity.GetCombat(world)?.HeroLevel ?? 0;
    public static bool GetCanFight(this Entity entity, MapWorld world) => entity.GetCombat(world)?.CanFight ?? true;

    public static void SetNoAttack(this Entity entity, MapWorld world, bool noAttack)
    {
        var combat = entity.GetCombat(world);
        if (combat != null)
        {
            world.SetComponent(entity, combat.Value with { NoAttack = noAttack });
        }
    }

    public static void SetCanFight(this Entity entity, MapWorld world, bool canFight)
    {
        var combat = entity.GetCombat(world);
        if (combat != null)
        {
            world.SetComponent(entity, combat.Value with { CanFight = canFight });
        }
    }

    public static void SetHeroLevel(this Entity entity, MapWorld world, byte heroLevel)
    {
        var combat = entity.GetCombat(world);
        if (combat != null)
        {
            world.SetComponent(entity, combat.Value with { HeroLevel = heroLevel });
        }
    }

    // NPC data helpers
    public static byte GetLevel(this Entity entity, MapWorld world) => entity.GetNpcData(world)?.Level ?? 0;
    public static short GetRace(this Entity entity, MapWorld world) => entity.GetNpcData(world)?.Race ?? 0;
    public static bool GetIsDisabled(this Entity entity, MapWorld world) => entity.GetNpcData(world)?.IsDisabled ?? false;
    public static short? GetDialog(this Entity entity, MapWorld world) => entity.GetNpcData(world)?.Dialog;
    public static bool GetIsMonster(this Entity entity, MapWorld world) => entity.GetNpcData(world)?.IsMonster ?? false;

    public static void SetLevel(this Entity entity, MapWorld world, byte level)
    {
        var npcData = entity.GetNpcData(world);
        if (npcData != null)
        {
            world.SetComponent(entity, npcData.Value with { Level = level });
        }
    }

    // MapItem helpers
    public static long? GetMapItemOwnerId(this Entity entity, MapWorld world) => entity.GetMapItem(world)?.OwnerId;
    public static Instant GetMapItemDroppedAt(this Entity entity, MapWorld world) => entity.GetMapItem(world)?.DroppedAt ?? default;
    public static short GetMapItemAmount(this Entity entity, MapWorld world) => entity.GetMapItem(world)?.Amount ?? 0;

    public static void SetMapItemOwnerId(this Entity entity, MapWorld world, long? ownerId)
    {
        var mapItem = entity.GetMapItem(world);
        if (mapItem != null)
        {
            world.SetComponent(entity, mapItem.Value with { OwnerId = ownerId });
        }
    }

    public static void SetMapItemDroppedAt(this Entity entity, MapWorld world, Instant droppedAt)
    {
        var mapItem = entity.GetMapItem(world);
        if (mapItem != null)
        {
            world.SetComponent(entity, mapItem.Value with { DroppedAt = droppedAt });
        }
    }

    public static GpPacket GenerateGpPacket(this Entity entity, MapWorld world)
    {
        var portal = entity.GetPortal(world);
        return new GpPacket
        {
            SourceX = portal?.SourceX ?? 0,
            SourceY = portal?.SourceY ?? 0,
            MapId = portal?.DestinationMapId ?? 0,
            PortalType = portal?.Type ?? Packets.Enumerations.PortalType.Open,
            PortalId = portal?.PortalId ?? 0,
            IsDisabled = portal?.IsDisabled ?? false
        };
    }

    // Generic component getters (new names)
    public static NameComponent? GetNameComponent(this Entity entity, MapWorld world)
        => world.TryGetComponent<NameComponent>(entity);

    public static ExperienceComponent? GetExperience(this Entity entity, MapWorld world)
        => world.TryGetComponent<ExperienceComponent>(entity);

    public static AppearanceComponent? GetAppearance(this Entity entity, MapWorld world)
        => world.TryGetComponent<AppearanceComponent>(entity);

    public static GoldComponent? GetGoldComponent(this Entity entity, MapWorld world)
        => world.TryGetComponent<GoldComponent>(entity);

    public static ReputationComponent? GetReputationComponent(this Entity entity, MapWorld world)
        => world.TryGetComponent<ReputationComponent>(entity);

    public static SpComponent? GetSp(this Entity entity, MapWorld world)
        => world.TryGetComponent<SpComponent>(entity);

    public static CombatStateComponent? GetCombatStateComponent(this Entity entity, MapWorld world)
        => world.TryGetComponent<CombatStateComponent>(entity);

    public static CombatState? GetCombatState(this Entity entity, MapWorld world)
        => entity.GetCombatStateComponent(world)?.State;

    // Name helpers
    public static string GetName(this Entity entity, MapWorld world) => entity.GetNameComponent(world)?.Name ?? string.Empty;
    public static string? GetPrefix(this Entity entity, MapWorld world) => entity.GetNameComponent(world)?.Prefix;
    public static long GetAccountId(this Entity entity, MapWorld world) => entity.GetNameComponent(world)?.AccountId ?? 0;
    public static AuthorityType GetAuthority(this Entity entity, MapWorld world) => entity.GetNameComponent(world)?.Authority ?? AuthorityType.User;

    // Experience helpers
    public static byte GetCharacterLevel(this Entity entity, MapWorld world) => entity.GetExperience(world)?.Level ?? 0;
    public static long GetLevelXp(this Entity entity, MapWorld world) => entity.GetExperience(world)?.LevelXp ?? 0;
    public static byte GetJobLevel(this Entity entity, MapWorld world) => entity.GetExperience(world)?.JobLevel ?? 0;
    public static long GetJobLevelXp(this Entity entity, MapWorld world) => entity.GetExperience(world)?.JobLevelXp ?? 0;
    public static long GetHeroXp(this Entity entity, MapWorld world) => entity.GetExperience(world)?.HeroXp ?? 0;

    public static void SetCharacterLevel(this Entity entity, MapWorld world, byte level)
    {
        var stats = entity.GetExperience(world);
        if (stats != null)
        {
            world.SetComponent(entity, stats.Value with { Level = level });
        }
    }

    public static void SetLevelXp(this Entity entity, MapWorld world, long xp)
    {
        var stats = entity.GetExperience(world);
        if (stats != null)
        {
            world.SetComponent(entity, stats.Value with { LevelXp = xp });
        }
    }

    public static void SetJobLevel(this Entity entity, MapWorld world, byte level)
    {
        var stats = entity.GetExperience(world);
        if (stats != null)
        {
            world.SetComponent(entity, stats.Value with { JobLevel = level });
        }
    }

    public static void SetJobLevelXp(this Entity entity, MapWorld world, long xp)
    {
        var stats = entity.GetExperience(world);
        if (stats != null)
        {
            world.SetComponent(entity, stats.Value with { JobLevelXp = xp });
        }
    }

    public static void SetHeroXp(this Entity entity, MapWorld world, long xp)
    {
        var stats = entity.GetExperience(world);
        if (stats != null)
        {
            world.SetComponent(entity, stats.Value with { HeroXp = xp });
        }
    }

    // Appearance helpers
    public static CharacterClassType GetClass(this Entity entity, MapWorld world)
        => entity.GetAppearance(world)?.Class ?? CharacterClassType.Adventurer;
    public static NosCore.Packets.Enumerations.GenderType GetGender(this Entity entity, MapWorld world)
        => entity.GetAppearance(world)?.Gender ?? NosCore.Packets.Enumerations.GenderType.Male;
    public static NosCore.Packets.Enumerations.HairStyleType GetHairStyle(this Entity entity, MapWorld world)
        => entity.GetAppearance(world)?.HairStyle ?? NosCore.Packets.Enumerations.HairStyleType.HairStyleA;
    public static NosCore.Packets.Enumerations.HairColorType GetHairColor(this Entity entity, MapWorld world)
        => entity.GetAppearance(world)?.HairColor ?? NosCore.Packets.Enumerations.HairColorType.DarkPurple;

    public static void SetClass(this Entity entity, MapWorld world, CharacterClassType characterClass)
    {
        var classComponent = entity.GetAppearance(world);
        if (classComponent != null)
        {
            world.SetComponent(entity, classComponent.Value with { Class = characterClass });
        }
    }

    public static void SetGender(this Entity entity, MapWorld world, NosCore.Packets.Enumerations.GenderType gender)
    {
        var appearanceComponent = entity.GetAppearance(world);
        if (appearanceComponent != null)
        {
            world.SetComponent(entity, appearanceComponent.Value with { Gender = gender });
        }
    }

    // Gold helpers
    public static long GetGold(this Entity entity, MapWorld world) => entity.GetGoldComponent(world)?.Gold ?? 0;

    public static void SetGold(this Entity entity, MapWorld world, long gold)
    {
        var goldComp = entity.GetGoldComponent(world);
        if (goldComp != null)
        {
            world.SetComponent(entity, goldComp.Value with { Gold = gold });
        }
    }

    // Reputation helpers
    public static long GetReput(this Entity entity, MapWorld world) => entity.GetReputationComponent(world)?.Reput ?? 0;
    public static short GetDignity(this Entity entity, MapWorld world) => entity.GetReputationComponent(world)?.Dignity ?? 0;

    public static void SetReput(this Entity entity, MapWorld world, long reput)
    {
        var rep = entity.GetReputationComponent(world);
        if (rep != null)
        {
            world.SetComponent(entity, rep.Value with { Reput = reput });
        }
    }

    public static void SetDignity(this Entity entity, MapWorld world, short dignity)
    {
        var rep = entity.GetReputationComponent(world);
        if (rep != null)
        {
            world.SetComponent(entity, rep.Value with { Dignity = dignity });
        }
    }

    // SP component helpers
    public static int GetSpPoint(this Entity entity, MapWorld world) => entity.GetSp(world)?.SpPoint ?? 0;
    public static int GetSpAdditionPoint(this Entity entity, MapWorld world) => entity.GetSp(world)?.SpAdditionPoint ?? 0;
    public static short GetCompliment(this Entity entity, MapWorld world) => entity.GetSp(world)?.Compliment ?? 0;

    public static void SetSpPoint(this Entity entity, MapWorld world, int spPoint)
    {
        var sp = entity.GetSp(world);
        if (sp != null)
        {
            world.SetComponent(entity, sp.Value with { SpPoint = spPoint });
        }
    }

    public static void SetSpAdditionPoint(this Entity entity, MapWorld world, int spAdditionPoint)
    {
        var sp = entity.GetSp(world);
        if (sp != null)
        {
            world.SetComponent(entity, sp.Value with { SpAdditionPoint = spAdditionPoint });
        }
    }

    public static void SetCompliment(this Entity entity, MapWorld world, short compliment)
    {
        var sp = entity.GetSp(world);
        if (sp != null)
        {
            world.SetComponent(entity, sp.Value with { Compliment = compliment });
        }
    }

    // PlayerComponent helpers
    public static PlayerComponent? GetPlayerComponent(this Entity entity, MapWorld world)
        => world.TryGetComponent<PlayerComponent>(entity);

    public static long GetPlayerCharacterId(this Entity entity, MapWorld world)
        => entity.GetPlayerComponent(world)?.CharacterId ?? 0;

    public static short GetSaveMapId(this Entity entity, MapWorld world)
        => entity.GetPlayerComponent(world)?.SaveMapId ?? 0;

    public static short GetSaveMapX(this Entity entity, MapWorld world)
        => entity.GetPlayerComponent(world)?.SaveMapX ?? 0;

    public static short GetSaveMapY(this Entity entity, MapWorld world)
        => entity.GetPlayerComponent(world)?.SaveMapY ?? 0;

    public static void SetSavePosition(this Entity entity, MapWorld world, short mapId, short mapX, short mapY)
    {
        var player = entity.GetPlayerComponent(world);
        if (player != null)
        {
            world.SetComponent(entity, player.Value with { SaveMapId = mapId, SaveMapX = mapX, SaveMapY = mapY });
        }
    }

    // TimingComponent helpers
    public static TimingComponent? GetTimingComponent(this Entity entity, MapWorld world)
        => world.TryGetComponent<TimingComponent>(entity);

    public static Instant GetLastPortal(this Entity entity, MapWorld world)
        => entity.GetTimingComponent(world)?.LastPortal ?? default;

    public static Instant GetLastSp(this Entity entity, MapWorld world)
        => entity.GetTimingComponent(world)?.LastSp ?? default;

    public static short GetSpCooldown(this Entity entity, MapWorld world)
        => entity.GetTimingComponent(world)?.SpCooldown ?? 0;

    public static Instant? GetLastGroupRequest(this Entity entity, MapWorld world)
        => entity.GetTimingComponent(world)?.LastGroupRequest;

    public static void SetLastPortal(this Entity entity, MapWorld world, Instant lastPortal)
    {
        var timing = entity.GetTimingComponent(world);
        if (timing != null)
        {
            world.SetComponent(entity, timing.Value with { LastPortal = lastPortal });
        }
    }

    public static void SetLastSp(this Entity entity, MapWorld world, Instant lastSp)
    {
        var timing = entity.GetTimingComponent(world);
        if (timing != null)
        {
            world.SetComponent(entity, timing.Value with { LastSp = lastSp });
        }
    }

    public static void SetSpCooldown(this Entity entity, MapWorld world, short spCooldown)
    {
        var timing = entity.GetTimingComponent(world);
        if (timing != null)
        {
            world.SetComponent(entity, timing.Value with { SpCooldown = spCooldown });
        }
    }

    public static void SetLastGroupRequest(this Entity entity, MapWorld world, Instant? lastGroupRequest)
    {
        var timing = entity.GetTimingComponent(world);
        if (timing != null)
        {
            world.SetComponent(entity, timing.Value with { LastGroupRequest = lastGroupRequest });
        }
    }

    // PlayerFlagsComponent helpers
    public static PlayerFlagsComponent? GetPlayerFlagsComponent(this Entity entity, MapWorld world)
        => world.TryGetComponent<PlayerFlagsComponent>(entity);

    public static bool GetIsChangingMapInstance(this Entity entity, MapWorld world)
        => entity.GetPlayerFlagsComponent(world)?.IsChangingMapInstance ?? false;

    public static bool GetInShop(this Entity entity, MapWorld world)
        => entity.GetPlayerFlagsComponent(world)?.InShop ?? false;

    public static bool GetIsDisconnecting(this Entity entity, MapWorld world)
        => entity.GetPlayerFlagsComponent(world)?.IsDisconnecting ?? false;

    public static void SetIsChangingMapInstance(this Entity entity, MapWorld world, bool isChangingMapInstance)
    {
        var flags = entity.GetPlayerFlagsComponent(world);
        if (flags != null)
        {
            world.SetComponent(entity, flags.Value with { IsChangingMapInstance = isChangingMapInstance });
        }
    }

    public static void SetInShop(this Entity entity, MapWorld world, bool inShop)
    {
        var flags = entity.GetPlayerFlagsComponent(world);
        if (flags != null)
        {
            world.SetComponent(entity, flags.Value with { InShop = inShop });
        }
    }

    public static void SetIsDisconnecting(this Entity entity, MapWorld world, bool isDisconnecting)
    {
        var flags = entity.GetPlayerFlagsComponent(world);
        if (flags != null)
        {
            world.SetComponent(entity, flags.Value with { IsDisconnecting = isDisconnecting });
        }
    }
}
