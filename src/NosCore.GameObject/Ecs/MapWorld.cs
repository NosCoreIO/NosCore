//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using Arch.Core;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs;

public class MapWorld : IDisposable
{
    public World World { get; }

    public MapWorld()
    {
        World = World.Create();
    }

    public T? TryGetComponent<T>(Entity entity) where T : struct
    {
        if (World.Has<T>(entity))
        {
            return World.Get<T>(entity);
        }
        return null;
    }

    public void SetComponent<T>(Entity entity, T component) where T : struct
    {
        World.Set(entity, component);
    }

    public bool HasComponent<T>(Entity entity) where T : struct
    {
        return World.Has<T>(entity);
    }

    public void AddComponent<T>(Entity entity, T component) where T : struct
    {
        World.Add(entity, component);
    }

    public void RemoveComponent<T>(Entity entity) where T : struct
    {
        World.Remove<T>(entity);
    }

    public Entity CreateMonster(
        int monsterId,
        NpcMonsterDto npcMonster,
        MapInstance mapInstance,
        short positionX,
        short positionY,
        byte direction,
        short firstX,
        short firstY,
        bool isMoving,
        bool isHostile,
        bool isDisabled)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var entity = World.Create(
            new EntityIdentityComponent(monsterId, VisualType.Monster, 0),
            new HealthComponent(npcMonster.MaxHp, npcMonster.MaxHp, true),
            new ManaComponent(npcMonster.MaxMp, npcMonster.MaxMp),
            new PositionComponent(positionX, positionY, direction, mapInstance.MapInstanceId),
            new VisualComponent(0, 0, 0, 0, false, false, false),
            new NpcDataComponent(npcMonster.NpcMonsterVNum, npcMonster.Race, npcMonster.Level, npcMonster.HeroLevel, npcMonster.Speed, 10),
            new SpawnComponent(firstX, firstY, isMoving, isHostile),
            new EffectComponent(0, 0),
            new TimingComponent(now, now),
            new NpcStateComponent(npcMonster, mapInstance, new SemaphoreSlim(1, 1), new ConcurrentDictionary<Entity, int>(), null, null, new Dictionary<Type, Subject<RequestData>>(), null, isDisabled),
            new BuffStateComponent(new ConcurrentDictionary<short, BuffInstance>()),
            new AggroComponent(VisualType.Object, 0, 0, Instant.MinValue),
            new SkillCooldownComponent(new ConcurrentDictionary<short, Instant>())
        );
        return entity;
    }

    public Entity CreateNpc(
        int npcId,
        NpcMonsterDto npcMonster,
        MapInstance mapInstance,
        short positionX,
        short positionY,
        byte direction,
        short firstX,
        short firstY,
        bool isMoving,
        bool isDisabled,
        short? dialog,
        short effect,
        short effectDelay,
        Shop? shop)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var entity = World.Create(
            new EntityIdentityComponent(npcId, VisualType.Npc, 0),
            new HealthComponent(npcMonster.MaxHp, npcMonster.MaxHp, true),
            new ManaComponent(npcMonster.MaxMp, npcMonster.MaxMp),
            new PositionComponent(positionX, positionY, direction, mapInstance.MapInstanceId),
            // NPCs are dialogue/shop targets, not combat targets — NoAttack=true (parameter 5).
            // Without this an upgrade NPC like Smith Malcolm can be killed via UseSkill packets,
            // breaking the n_run flow and corrupting save state.
            new VisualComponent(0, 0, 0, 0, true, false, false),
            new NpcDataComponent(npcMonster.NpcMonsterVNum, npcMonster.Race, npcMonster.Level, 0, npcMonster.Speed, 10),
            new SpawnComponent(firstX, firstY, isMoving, false),
            new EffectComponent(effect, effectDelay),
            new TimingComponent(now, now),
            new NpcStateComponent(npcMonster, mapInstance, new SemaphoreSlim(1, 1), new ConcurrentDictionary<Entity, int>(), shop, null, new Dictionary<Type, Subject<RequestData>> { [typeof(NpcDialogRequestSubject)] = new() }, dialog, isDisabled),
            new BuffStateComponent(new ConcurrentDictionary<short, BuffInstance>()),
            new AggroComponent(VisualType.Object, 0, 0, Instant.MinValue),
            new SkillCooldownComponent(new ConcurrentDictionary<short, Instant>())
        );
        return entity;
    }

    public Entity CreateMapItem(
        long visualId,
        short vNum,
        short amount,
        Guid mapInstanceId,
        short positionX,
        short positionY,
        long? ownerId,
        Instant droppedAt,
        Guid itemInstanceId,
        IItemInstance? itemInstance)
    {
        var entity = World.Create(
            new EntityIdentityComponent(visualId, VisualType.Object, 0),
            new PositionComponent(positionX, positionY, 0, mapInstanceId),
            new MapItemDataComponent(vNum, amount, ownerId, droppedAt, itemInstanceId, itemInstance)
        );
        return entity;
    }

    public Entity CreatePlayer(
        int visaulId,
        long characterId,
        long accountId,
        string name,
        Guid mapInstanceId,
        short positionX,
        short positionY,
        byte direction,
        int hp,
        int maxHp,
        int mp,
        int maxMp,
        byte level,
        long levelXp,
        byte jobLevel,
        long jobLevelXp,
        byte heroLevel,
        long heroLevelXp,
        long gold,
        long reputation,
        short dignity,
        short compliment,
        GenderType gender,
        HairStyleType hairStyle,
        HairColorType hairColor,
        CharacterClassType characterClass,
        byte face,
        byte speed,
        AuthorityType authority,
        bool isGm,
        int serverId)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var entity = World.Create(
            new EntityIdentityComponent(visaulId, VisualType.Player, characterId),
            new HealthComponent(hp, maxHp, true),
            new ManaComponent(mp, maxMp),
            new PositionComponent(positionX, positionY, direction, mapInstanceId),
            new VisualComponent(0, 0, 0, 0, false, false, false),
            new AppearanceComponent(gender, hairStyle, hairColor, characterClass, face, 100),
            new ExperienceComponent(level, levelXp, jobLevel, jobLevelXp, heroLevel, heroLevelXp),
            new GoldComponent(gold),
            new ReputationComponent(reputation, dignity, compliment),
            new SpComponent(0, 0, 0),
            new NameComponent(name),
            new CombatComponent(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new SemaphoreSlim(1, 1), new ConcurrentDictionary<Entity, int>()),
            new BuffStateComponent(new ConcurrentDictionary<short, BuffInstance>()),
            new PlayerComponent(accountId, characterId, isGm, serverId),
            new PlayerFlagsComponent(false, false, false, false, false, false, false, false, false, false, false, authority, false, false, false, false),
            new TimingComponent(now, now),
            new SpeedComponent(speed)
        );
        return entity;
    }

    public Entity ClonePlayer(
        EntityIdentityComponent identity,
        HealthComponent health,
        ManaComponent mana,
        PositionComponent position,
        VisualComponent visual,
        AppearanceComponent appearance,
        ExperienceComponent experience,
        GoldComponent gold,
        ReputationComponent reputation,
        SpComponent sp,
        NameComponent name,
        CombatComponent combat,
        BuffStateComponent buffs,
        PlayerComponent player,
        PlayerFlagsComponent playerFlags,
        TimingComponent timing,
        SpeedComponent speed,
        PlayerStateComponent state,
        PlayerNetworkComponent network,
        PlayerContextComponent context,
        PlayerInventoryComponent inventory,
        PlayerSocialComponent social,
        PlayerRequestsComponent requests)
    {
        return World.Create(identity, health, mana, position, visual, appearance, experience, gold,
            reputation, sp, name, combat, buffs, player, playerFlags, timing, speed, state, network,
            context, inventory, social, requests);
    }

    public void DestroyEntity(Entity entity)
    {
        World.Destroy(entity);
    }

    public void Dispose()
    {
        World.Dispose();
        GC.SuppressFinalize(this);
    }
}
