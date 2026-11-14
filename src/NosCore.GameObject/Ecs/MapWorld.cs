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
    // Arch registers every world in a static table, so worlds must not be created or
    // disposed concurrently.
    private static readonly Lock WorldTableLock = new();
    private static long _lastOrder;
    private static readonly long ForwardRetention = (long)TimeSpan.FromMinutes(5).TotalMilliseconds;

    // Arch does not tolerate a structural change overlapping any other access, and a
    // component is rewritten whole on every property set. Every access to the world goes
    // through this lock; it is re-entrant and is never held across an await.
    private readonly Lock _sync = new();
    private readonly World _world;
    private readonly long _order = Interlocked.Increment(ref _lastOrder);
    private readonly ConcurrentDictionary<Entity, (ClientSession Session, long At)> _forwards = new();
    private bool _disposed;

    public MapWorld()
    {
        lock (WorldTableLock)
        {
            _world = World.Create();
        }
    }

    // Runs one synchronous block against the raw world while nothing else touches it. For
    // queries and for keeping several reads and writes consistent with each other.
    public void Exclusive(Action<World> action)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            action(_world);
        }
    }

    public TResult Exclusive<TResult>(Func<World, TResult> func)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            return func(_world);
        }
    }

    // Holds two worlds at once, always in the same order, for a step that has to see both
    // consistent - a player leaving one for the other.
    public static void Exclusive(MapWorld first, MapWorld second, Action action)
    {
        var (outer, inner) = first._order <= second._order ? (first, second) : (second, first);
        lock (outer._sync)
        {
            lock (inner._sync)
            {
                action();
            }
        }
    }

    // A player's entity is replaced when they change map, but events and handlers may still
    // hold the old handle. The world it left remembers whose it was for a while.
    public void Forward(Entity from, ClientSession to)
    {
        var now = Environment.TickCount64;
        _forwards[from] = (to, now);
        foreach (var (entity, forward) in _forwards)
        {
            if (now - forward.At > ForwardRetention)
            {
                _forwards.TryRemove(entity, out _);
            }
        }
    }

    public ClientSession? ForwardedTo(Entity from) =>
        _forwards.TryGetValue(from, out var forward) ? forward.Session : null;

    public bool IsAlive(Entity entity)
    {
        lock (_sync)
        {
            return !_disposed && _world.IsAlive(entity);
        }
    }

    // Reads and writes through the handle of a player who has since changed map reach the
    // entity they have now - again if they moved once more in between. The lookup happens
    // outside this world's lock, so two worlds are never locked here in an order the map
    // change could contradict.
    public T? TryGetComponent<T>(Entity entity) where T : struct
    {
        lock (_sync)
        {
            if (!_disposed && _world.IsAlive(entity))
            {
                return _world.Has<T>(entity) ? _world.Get<T>(entity) : null;
            }
        }

        while (MovedTo(entity) is { } moved)
        {
            if (moved.World.TryGetComponentHere<T>(moved.Entity, out var component))
            {
                return component;
            }
        }

        return null;
    }

    // False only when the entity is gone; a missing component reads as null.
    private bool TryGetComponentHere<T>(Entity entity, out T? component) where T : struct
    {
        lock (_sync)
        {
            component = null;
            if (_disposed || !_world.IsAlive(entity))
            {
                return false;
            }

            if (_world.Has<T>(entity))
            {
                component = _world.Get<T>(entity);
            }

            return true;
        }
    }

    public void SetComponent<T>(Entity entity, T component) where T : struct
    {
        Update<T, T>(entity, component, static (_, value) => value);
    }

    // Read-modify-write of one component as a single step, so two writers of different
    // fields of the same component do not undo each other. Returns false, writing nothing,
    // when the entity is gone or lacks the component.
    public bool Update<T, TState>(Entity entity, TState state, Func<T, TState, T> update) where T : struct
    {
        var outcome = UpdateHere(entity, state, update);
        while (outcome == UpdateOutcome.Gone && MovedTo(entity) is { } moved)
        {
            outcome = moved.World.UpdateHere(moved.Entity, state, update);
        }

        return outcome == UpdateOutcome.Done;
    }

    private UpdateOutcome UpdateHere<T, TState>(Entity entity, TState state, Func<T, TState, T> update) where T : struct
    {
        lock (_sync)
        {
            if (_disposed || !_world.IsAlive(entity))
            {
                return UpdateOutcome.Gone;
            }

            if (!_world.Has<T>(entity))
            {
                return UpdateOutcome.Missing;
            }

            ref var component = ref _world.Get<T>(entity);
            component = update(component, state);
            return UpdateOutcome.Done;
        }
    }

    private enum UpdateOutcome
    {
        Done,
        Missing,
        Gone,
    }

    private PlayerComponentBundle? MovedTo(Entity entity)
    {
        return ForwardedTo(entity) is { } session && session.TryGetCharacter(out var character) ? character : null;
    }

    public bool HasComponent<T>(Entity entity) where T : struct
    {
        lock (_sync)
        {
            return !_disposed && _world.IsAlive(entity) && _world.Has<T>(entity);
        }
    }

    public void AddComponent<T>(Entity entity, T component) where T : struct
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            _world.Add(entity, component);
        }
    }

    public void RemoveComponent<T>(Entity entity) where T : struct
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            _world.Remove<T>(entity);
        }
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
        bool isDisabled,
        bool respawns = true)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        using var scope = _sync.EnterScope();
        ThrowIfDisposed();
        var entity = _world.Create(
            new EntityIdentityComponent(monsterId, VisualType.Monster, 0),
            new HealthComponent(npcMonster.MaxHp, npcMonster.MaxHp, true),
            new ManaComponent(npcMonster.MaxMp, npcMonster.MaxMp),
            new PositionComponent(positionX, positionY, direction, mapInstance.MapInstanceId),
            new VisualComponent(0, 0, 0, 0, false, false, false),
            new NpcDataComponent(npcMonster.NpcMonsterVNum, npcMonster.Race, npcMonster.Level, npcMonster.HeroLevel, npcMonster.Speed, 10),
            new SpawnComponent(firstX, firstY, isMoving, isHostile),
            new EffectComponent(0, 0),
            new TimingComponent(now, now, Instant.MinValue, Instant.MinValue, Instant.MinValue),
            new NpcStateComponent(npcMonster, mapInstance, null, isDisabled),
            new RequestsComponent(new Dictionary<Type, Subject<RequestData>>()),
            new NpcShopComponent(null),
            new BuffStateComponent(new ConcurrentDictionary<short, BuffInstance>()),
            new AggroComponent(VisualType.Object, 0, 0, Instant.MinValue),
            new ChasePathComponent(null),
            new SkillCooldownComponent(new ConcurrentDictionary<short, Instant>(), Instant.MinValue),
            new HitListComponent(new ConcurrentDictionary<Entity, int>()),
            new RespawnComponent(null, respawns)
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
        using var scope = _sync.EnterScope();
        ThrowIfDisposed();
        var entity = _world.Create(
            new EntityIdentityComponent(npcId, VisualType.Npc, 0),
            new HealthComponent(npcMonster.MaxHp, npcMonster.MaxHp, true),
            new ManaComponent(npcMonster.MaxMp, npcMonster.MaxMp),
            new PositionComponent(positionX, positionY, direction, mapInstance.MapInstanceId),
            // NoAttack is strictly a "locked/untargetable" state flag here (the faction
            // rule in BattleService.CanAttack is what actually protects NPCs from
            // players — NPCs and players are allies). NPCs default to unlocked so
            // monsters can land hits on them.
            new VisualComponent(0, 0, 0, 0, false, false, false),
            new NpcDataComponent(npcMonster.NpcMonsterVNum, npcMonster.Race, npcMonster.Level, 0, npcMonster.Speed, 10),
            new SpawnComponent(firstX, firstY, isMoving, false),
            new EffectComponent(effect, effectDelay),
            new TimingComponent(now, now, Instant.MinValue, Instant.MinValue, Instant.MinValue),
            new NpcStateComponent(npcMonster, mapInstance, dialog, isDisabled),
            new RequestsComponent(new Dictionary<Type, Subject<RequestData>> { [typeof(NpcDialogRequestSubject)] = new() }),
            new NpcShopComponent(shop),
            new RecipesComponent(null),
            new BuffStateComponent(new ConcurrentDictionary<short, BuffInstance>()),
            new AggroComponent(VisualType.Object, 0, 0, Instant.MinValue),
            new ChasePathComponent(null),
            new SkillCooldownComponent(new ConcurrentDictionary<short, Instant>(), Instant.MinValue),
            new HitListComponent(new ConcurrentDictionary<Entity, int>()),
            new RespawnComponent(null, true)
        );
        return entity;
    }

    public Entity CreateMate(
        int visualId,
        Services.MateService.Mate mate,
        MapInstance mapInstance,
        short positionX,
        short positionY,
        byte direction)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        using var scope = _sync.EnterScope();
        ThrowIfDisposed();
        return _world.Create(
            new EntityIdentityComponent(visualId, VisualType.Npc, mate.CharacterId),
            new HealthComponent(mate.Hp, mate.MaxHp, true),
            new ManaComponent(mate.Mp, mate.MaxMp),
            new PositionComponent(positionX, positionY, direction, mapInstance.MapInstanceId),
            new VisualComponent(0, 0, 0, 0, false, false, false),
            new NpcDataComponent(mate.VNum, mate.NpcMonster.Race, mate.Level, 0, mate.NpcMonster.Speed, 10),
            new SpawnComponent(positionX, positionY, false, false),
            new EffectComponent(0, 0),
            new TimingComponent(now, now, Instant.MinValue, Instant.MinValue, Instant.MinValue),
            new NpcStateComponent(mate.NpcMonster, mapInstance, null, false),
            new RequestsComponent(new Dictionary<Type, Subject<RequestData>>()),
            new NpcShopComponent(null),
            new BuffStateComponent(new ConcurrentDictionary<short, BuffInstance>()),
            new AggroComponent(VisualType.Object, 0, 0, Instant.MinValue),
            new ChasePathComponent(null),
            new SkillCooldownComponent(new ConcurrentDictionary<short, Instant>(), Instant.MinValue),
            new MateStateComponent(mate, mate.CharacterId),
            new HitListComponent(new ConcurrentDictionary<Entity, int>())
        );
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
        using var scope = _sync.EnterScope();
        ThrowIfDisposed();
        var entity = _world.Create(
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
        short mapId,
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
        long bankGold,
        long reputation,
        int spPoint,
        int spAdditionPoint,
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
        int serverId,
        FactionType faction)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        using var scope = _sync.EnterScope();
        ThrowIfDisposed();
        var entity = _world.Create(
            new EntityIdentityComponent(visaulId, VisualType.Player, characterId),
            new HealthComponent(hp, maxHp, true),
            new ManaComponent(mp, maxMp),
            new PositionComponent(positionX, positionY, direction, mapInstanceId),
            new VisualComponent(0, 0, 0, 0, false, false, false),
            new AppearanceComponent(gender, hairStyle, hairColor, characterClass, face, 100),
            new ExperienceComponent(level, levelXp, jobLevel, jobLevelXp, heroLevel, heroLevelXp),
            new GoldComponent(gold, bankGold),
            new ReputationComponent(reputation, dignity, compliment),
            new SpComponent(0, spPoint, spAdditionPoint),
            new NameComponent(name),
            new CombatComponent(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            new BuffStateComponent(new ConcurrentDictionary<short, BuffInstance>()),
            new PlayerComponent(accountId, characterId, isGm, serverId),
            new PlayerFlagsComponent(false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, authority, false, false, false, false),
            new TimingComponent(now, Instant.MinValue, Instant.MinValue, Instant.MinValue, Instant.MinValue),
            new SpeedComponent(speed),
            new DigestionComponent(new DigestionState()),
            new PendingDelayComponent(null, Instant.MinValue, null),
            new WalkAllowanceComponent(0),
            new NpcDialogComponent(null),
            new ListedTargetsComponent(null),
            new MinigameSessionComponent(null, Instant.MinValue, null),
            new HitListComponent(new ConcurrentDictionary<Entity, int>())
        );
        _world.Add(entity, new BaseMapPositionComponent(mapId, positionX, positionY));
        _world.Add(entity, new SkillResetComponent(new ConcurrentDictionary<long, Instant>()));
        _world.Add(entity, new FactionComponent(faction));
        _world.Add(entity, new WalkTrailComponent(0, 0));
        _world.Add(entity, new SkillComboComponent(0, 0));
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
        RequestsComponent requests,
        PlayerMatesComponent mates,
        DigestionComponent digestion,
        HitListComponent hitList,
        BaseMapPositionComponent baseMapPosition,
        SkillResetComponent skillResets,
        FactionComponent faction)
    {
        // World.Create stops short of 25 generic components, so the last ones are added afterwards.
        using var scope = _sync.EnterScope();
        ThrowIfDisposed();
        var entity = _world.Create(identity, health, mana, position, visual, appearance, experience, gold,
            reputation, sp, name, combat, buffs, player, playerFlags, timing, speed, state, network,
            context, inventory, social, requests, mates);
        _world.Add(entity, digestion);
        _world.Add(entity, hitList);
        _world.Add(entity, baseMapPosition);
        _world.Add(entity, new PendingDelayComponent(null, Instant.MinValue, null));
        _world.Add(entity, new WalkAllowanceComponent(0));
        _world.Add(entity, new WalkTrailComponent(0, 0));
        _world.Add(entity, new NpcDialogComponent(null));
        _world.Add(entity, new ListedTargetsComponent(null));
        _world.Add(entity, new MinigameSessionComponent(null, Instant.MinValue, null));
        _world.Add(entity, skillResets);
        _world.Add(entity, faction);
        _world.Add(entity, new SkillComboComponent(0, 0));
        return entity;
    }

    // Returns false when the entity was already gone, so callers racing to remove the same
    // entity can tell which of them did it.
    public bool DestroyEntity(Entity entity)
    {
        lock (_sync)
        {
            if (_disposed || !_world.IsAlive(entity))
            {
                return false;
            }

            _world.Destroy(entity);
            return true;
        }
    }

    public void Dispose()
    {
        lock (WorldTableLock)
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _world.Dispose();
            }
        }

        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
