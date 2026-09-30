//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.PathfindingService;
using NosCore.Networking;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Services.BattleService;

// Aggro-driven AI for both monsters and NPCs (guards), called from MapInstance's life
// loop every 400ms. What an entity does is decided by MonsterBehaviourRules.Decide from
// a snapshot of its situation; this class gathers the snapshot and performs the result:
//   AcquireTarget: hostile entities scan NoticeRange for the closest enemy. Two-faction
//      rule — NPCs and players are same-side, monsters the other: NPCs target monsters
//      only, monsters target whichever of player/NPC is closer.
//   Attack: 20% chance per tick to pick a random cooldown-ready NpcMonsterSkill, else
//      the basic attack; if the target is within its range, enqueue a Hit via
//      IBattleService and stamp the cooldown once the hit was accepted.
//   Chase: otherwise step toward the target along a cached JPS path; re-plan when the
//      target moved. Stationary entities (CanWalk=false) never chase but still attack.
//   Return: once the aggro leash expires (aggroService.Current returns HasTarget=false)
//      the entity paths back to FirstX/FirstY, then idles or wanders from there.
public sealed class MonsterAi : IMonsterAi, ISingletonService
{
    private readonly IBattleService battleService;
    private readonly IAggroService aggroService;
    private readonly IPathfindingService pathfindingService;
    private readonly ISessionRegistry sessionRegistry;
    private readonly IHeuristic distanceCalculator;
    private readonly INpcCombatCatalog catalog;
    private readonly IRandomProvider random;
    private readonly IClock clock;
    private readonly ILogger<MonsterAi> logger;
    private readonly IReadOnlyDictionary<short, SkillDto> skillsByVnum;

    public MonsterAi(
        IBattleService battleService,
        IAggroService aggroService,
        IPathfindingService pathfindingService,
        ISessionRegistry sessionRegistry,
        IHeuristic distanceCalculator,
        INpcCombatCatalog catalog,
        IRandomProvider random,
        IClock clock,
        List<SkillDto> skills,
        ILogger<MonsterAi> logger)
        : this(battleService, aggroService, pathfindingService, sessionRegistry, distanceCalculator,
            catalog, random, clock, skills.ToDictionary(s => s.SkillVNum, s => s), logger)
    {
    }

    public MonsterAi(
        IBattleService battleService,
        IAggroService aggroService,
        IPathfindingService pathfindingService,
        ISessionRegistry sessionRegistry,
        IHeuristic distanceCalculator,
        INpcCombatCatalog catalog,
        IRandomProvider random,
        IClock clock,
        IReadOnlyDictionary<short, SkillDto> skillsByVnum,
        ILogger<MonsterAi> logger)
    {
        this.battleService = battleService;
        this.aggroService = aggroService;
        this.pathfindingService = pathfindingService;
        this.sessionRegistry = sessionRegistry;
        this.distanceCalculator = distanceCalculator;
        this.catalog = catalog;
        this.random = random;
        this.clock = clock;
        this.skillsByVnum = skillsByVnum;
        this.logger = logger;
    }

    // Cached path per entity — invalidated when the target moves far enough that
    // JPS's result is no longer useful.
    private readonly ConcurrentDictionary<Entity, CachedPath> _pathCache = new();

    public async Task<bool> TickAsync(INonPlayableEntity entity)
    {
        try
        {
            if (!entity.IsAlive || entity.NpcMonster == null) return false;

            var target = CurrentTarget(entity);
            var chosenSkill = target == null ? null : PickSkill(entity);
            var behaviour = MonsterBehaviourRules.Decide(
                Snapshot(entity, target, chosenSkill, hasScanned: false));

            if (behaviour == MonsterBehaviour.AcquireTarget)
            {
                var noticed = DetectNearbyEnemy(entity);
                if (noticed != null)
                {
                    aggroService.AddThreat(entity, noticed, 1);
                }

                target = CurrentTarget(entity);
                chosenSkill = target == null ? null : PickSkill(entity);
                behaviour = MonsterBehaviourRules.Decide(
                    Snapshot(entity, target, chosenSkill, hasScanned: true));
            }

            switch (behaviour)
            {
                case MonsterBehaviour.Attack:
                    await AttackAsync(entity, target!, chosenSkill ?? BasicAttack(entity));
                    return true;
                case MonsterBehaviour.Chase:
                    await StepAlongPathAsync(entity, target!.PositionX, target.PositionY, cellsToLeave: 1);
                    return true;
                case MonsterBehaviour.Return:
                    return await StepAlongPathAsync(entity, entity.MapX, entity.MapY, cellsToLeave: 0);
                case MonsterBehaviour.Wander:
                    _pathCache.TryRemove(entity.Handle, out _);
                    return false;
                default:
                    return target != null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI tick failed for {VisualId}", entity.VisualId);
            return false;
        }
    }

    // The aggro target, or null once it is gone or dead (which also drops the aggro).
    private IAliveEntity? CurrentTarget(INonPlayableEntity entity)
    {
        var aggro = aggroService.Current(entity);
        if (!aggro.HasTarget) return null;

        var target = ResolveTarget(entity, aggro.TargetVisualId);
        if (target is { IsAlive: true }) return target;

        aggroService.Clear(entity);
        _pathCache.TryRemove(entity.Handle, out _);
        return null;
    }

    private MonsterSituation Snapshot(INonPlayableEntity entity, IAliveEntity? target, ChosenSkill? chosenSkill, bool hasScanned)
    {
        var npcMonster = entity.NpcMonster!;
        var distance = target == null
            ? 0
            : (int)distanceCalculator.GetDistance(
                (entity.PositionX, entity.PositionY),
                (target.PositionX, target.PositionY));

        return new MonsterSituation(
            IsAlive: entity.IsAlive,
            HasTarget: target != null,
            IsHostile: npcMonster.IsHostile,
            HasScanned: hasScanned,
            CanWalk: npcMonster.CanWalk,
            IsMoving: entity.IsMoving,
            AtHome: entity.PositionX == entity.MapX && entity.PositionY == entity.MapY,
            Distance: distance,
            AttackRange: chosenSkill?.SkillRange ?? Math.Max(1, (int)npcMonster.BasicRange));
    }

    private static ChosenSkill BasicAttack(INonPlayableEntity entity)
    {
        var npcMonster = entity.NpcMonster!;
        return new ChosenSkill(
            SkillVnum: 0,
            CastId: 0,
            SkillRange: Math.Max(1, (int)npcMonster.BasicRange),
            CooldownMs: Math.Max(200, npcMonster.BasicCooldown * 100));
    }

    // Returns the closest qualifying enemy within NoticeRange. Two-faction rule:
    // NPCs and players are on one side, monsters on the other. NPCs scan the
    // monster pool; monsters scan both players and NPCs and just take the closest,
    // no player preference.
    private IAliveEntity? DetectNearbyEnemy(INonPlayableEntity entity)
    {
        var map = entity.MapInstance;
        if (map == null) return null;

        var range = (int)Math.Max(entity.NpcMonster.NoticeRange, (byte)1);
        IAliveEntity? best = null;
        var bestDistance = double.MaxValue;

        if (entity.VisualType == VisualType.Npc)
        {
            foreach (var monster in map.EnumerateMonsters())
            {
                if (!monster.IsAlive || monster.NpcMonster == null) continue;
                var d = distanceCalculator.GetDistance(
                    (entity.PositionX, entity.PositionY),
                    (monster.PositionX, monster.PositionY));
                if (d > range) continue;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = monster;
                }
            }
            return best;
        }

        foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(entity.MapInstanceId))
        {
            if (!session.HasPlayerEntity) continue;
            var player = session.Character;
            if (!player.IsAlive) continue;
            var d = distanceCalculator.GetDistance(
                (entity.PositionX, entity.PositionY),
                (player.PositionX, player.PositionY));
            if (d > range) continue;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = player;
            }
        }
        foreach (var npc in map.EnumerateNpcs())
        {
            if (!npc.IsAlive || npc.NpcMonster == null) continue;
            var d = distanceCalculator.GetDistance(
                (entity.PositionX, entity.PositionY),
                (npc.PositionX, npc.PositionY));
            if (d > range) continue;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = npc;
            }
        }
        return best;
    }

    // Resolves the cached aggro target by visual id. Walks all three pools
    // (players, monsters, NPCs) because any of them can be an aggro target.
    private IAliveEntity? ResolveTarget(INonPlayableEntity entity, long visualId)
    {
        var map = entity.MapInstance;
        if (map == null) return null;

        foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(entity.MapInstanceId))
        {
            if (session.HasPlayerEntity && session.Character.VisualId == visualId)
            {
                return session.Character;
            }
        }
        if (map.FindMonster(m => m.VisualId == visualId) is { } monster) return monster;
        if (map.FindNpc(n => n.VisualId == visualId) is { } npc) return npc;
        return null;
    }

    // 20% chance per tick to roll one of the entity's cooldown-ready skills. When a
    // skill is selected it's only used if in range; otherwise the AI falls through to
    // either basic attack or pursuit.
    private ChosenSkill? PickSkill(INonPlayableEntity entity)
    {
        if (random.Next(0, 10) < 8) return null;
        var map = entity.MapInstance;
        if (map == null) return null;

        var skills = catalog.GetSkills(entity.NpcMonster!.NpcMonsterVNum);
        if (skills.Count == 0) return null;

        var cooldowns = map.EcsWorld.TryGetComponent<SkillCooldownComponent>(entity.Handle);
        if (cooldowns == null) return null;

        var now = clock.GetCurrentInstant();
        foreach (var sk in skills.OrderBy(_ => random.Next(0, 100)))
        {
            if (cooldowns.Value.NextUsableAt.TryGetValue(sk.SkillVNum, out var readyAt) && readyAt > now)
            {
                continue;
            }
            if (!skillsByVnum.TryGetValue(sk.SkillVNum, out var dto))
            {
                continue;
            }
            return new ChosenSkill(
                SkillVnum: dto.SkillVNum,
                CastId: dto.CastId,
                SkillRange: Math.Max(1, (int)dto.Range),
                CooldownMs: Math.Max(200, dto.Cooldown * 100));
        }
        return null;
    }

    private async Task AttackAsync(INonPlayableEntity entity, IAliveEntity target, ChosenSkill skill)
    {
        var map = entity.MapInstance;
        if (map == null) return;

        var cooldowns = map.EcsWorld.TryGetComponent<SkillCooldownComponent>(entity.Handle);
        var now = clock.GetCurrentInstant();
        if (cooldowns != null &&
            cooldowns.Value.NextUsableAt.TryGetValue(skill.SkillVnum, out var readyAt) &&
            readyAt > now)
        {
            return;
        }

        var accepted = await battleService.Hit(entity, target, new HitArguments { SkillId = skill.CastId });

        if (accepted && cooldowns != null)
        {
            cooldowns.Value.NextUsableAt[skill.SkillVnum] = now.Plus(Duration.FromMilliseconds(skill.CooldownMs));
        }
    }

    // Walks a cached JPS path toward the goal (a target's cell, or the spawn cell to go
    // home), re-planning when the goal changed or the path ran out. Consumes `Speed / 2`
    // cells per tick: the 400ms life-loop cadence is already the rate limit. The path
    // starts at the first step, not at the cell the entity stands on, and the last
    // `cellsToLeave` cells of it are never entered (a chaser stops beside its target).
    // Returns whether the entity moved; no path or a blocked cell just means it stays put.
    private async Task<bool> StepAlongPathAsync(INonPlayableEntity entity, short goalX, short goalY, int cellsToLeave)
    {
        var map = entity.MapInstance;
        if (map == null) return false;

        var cache = _pathCache.GetValueOrDefault(entity.Handle);
        var goalMoved = cache == null || cache.TargetX != goalX || cache.TargetY != goalY;

        if (cache == null || goalMoved || cache.Path.Count == 0)
        {
            var pathfinder = pathfindingService.ForMap(map.Map);
            var path = pathfinder.FindPath(
                    (entity.PositionX, entity.PositionY),
                    (goalX, goalY))
                .ToList();
            cache = new CachedPath(goalX, goalY, path, clock.GetCurrentInstant());
            _pathCache[entity.Handle] = cache;
        }

        var speed = (int)Math.Max((byte)1, entity.NpcMonster!.Speed);
        var stepCount = Math.Min(cache.Path.Count - cellsToLeave, Math.Max(1, speed / 2));
        if (stepCount <= 0) return false;

        var dest = cache.Path[stepCount - 1];
        cache.Path.RemoveRange(0, stepCount);
        _pathCache[entity.Handle] = cache with { LastStepAt = clock.GetCurrentInstant() };

        if (!map.Map.IsWalkable((short)dest.Item1, (short)dest.Item2)) return false;

        entity.PositionX = (short)dest.Item1;
        entity.PositionY = (short)dest.Item2;
        await map.SendPacketAsync(entity.GenerateMove(entity.PositionX, entity.PositionY));
        return true;
    }

    // Cached pathfinding result. Mutable only on replan or step-advance so the record
    // stays cheap to copy in concurrent tick scenarios.
    private sealed record CachedPath(short TargetX, short TargetY, List<(short X, short Y)> Path, Instant LastStepAt);

    private sealed record ChosenSkill(short SkillVnum, long CastId, int SkillRange, int CooldownMs);
}
