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
using Serilog;

namespace NosCore.GameObject.Services.BattleService;

// Aggro-driven AI for both monsters and NPCs (guards). Each tick (called from
// MapInstance's life loop every 400ms) does four things in order, matching OpenNos
// MonsterLife:
//   1. Proximity aggro: hostile entities scan NoticeRange for enemies and pick the
//      closest. Two-faction rule — NPCs and players are same-side, monsters the
//      other: NPCs target monsters only, monsters target whichever of player/NPC
//      is closer.
//   2. Skill selection: 20% chance to pick a random cooldown-ready NpcMonsterSkill.
//   3. Attack: if the target is in range for the chosen skill (or BasicRange for
//      the basic attack), enqueue a Hit via IBattleService and stamp the cooldown.
//   4. Pursuit: else step toward the target along a cached JPS path; re-plan if the
//      target moved more than 2 cells from when the path was computed. Stationary
//      entities (CanWalk=false) skip pursuit but still attack in range.
// When the aggro leash expires (aggroService.Current returns HasTarget=false), the
// AI pathfinds back to FirstX/FirstY (for entities that walk), matching OpenNos
// RemoveTarget behaviour.
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
    private readonly ILogger logger;
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
        ILogger logger)
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
        ILogger logger)
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

            var aggro = aggroService.Current(entity);
            if (!aggro.HasTarget && entity.NpcMonster.IsHostile)
            {
                var noticed = DetectNearbyEnemy(entity);
                if (noticed != null)
                {
                    aggroService.AddThreat(entity, noticed, 1);
                    aggro = aggroService.Current(entity);
                }
            }

            if (!aggro.HasTarget)
            {
                return await TryReturnHomeAsync(entity).ConfigureAwait(false);
            }

            var target = ResolveTarget(entity, aggro.TargetVisualId);
            if (target == null || !target.IsAlive)
            {
                aggroService.Clear(entity);
                _pathCache.TryRemove(entity.Handle, out _);
                return false;
            }

            var distance = (int)distanceCalculator.GetDistance(
                (entity.PositionX, entity.PositionY),
                (target.PositionX, target.PositionY));

            var chosenSkill = PickSkill(entity);

            if (chosenSkill != null && distance <= chosenSkill.SkillRange)
            {
                await AttackAsync(entity, target, chosenSkill).ConfigureAwait(false);
                return true;
            }
            if (chosenSkill == null && distance <= Math.Max(1, (int)entity.NpcMonster.BasicRange))
            {
                var basic = new ChosenSkill(
                    SkillVnum: 0,
                    CastId: 0,
                    SkillRange: Math.Max(1, (int)entity.NpcMonster.BasicRange),
                    CooldownMs: Math.Max(200, entity.NpcMonster.BasicCooldown * 100));
                await AttackAsync(entity, target, basic).ConfigureAwait(false);
                return true;
            }

            await StepTowardAsync(entity, target).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "AI tick failed for {VisualId}", entity.VisualId);
            return false;
        }
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
            foreach (var monster in map.Monsters)
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
        foreach (var npc in map.Npcs)
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
        foreach (var monster in map.Monsters)
        {
            if (monster.VisualId == visualId) return monster;
        }
        foreach (var npc in map.Npcs)
        {
            if (npc.VisualId == visualId) return npc;
        }
        return null;
    }

    // 20% chance per tick to roll one of the entity's cooldown-ready skills. When a
    // skill is selected it's only used if in range; otherwise the AI falls through to
    // either basic attack or pursuit. Matches OpenNos RandomNumber(0, 10) > 8 check.
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

        await battleService.Hit(entity, target, new HitArguments { SkillId = skill.CastId }).ConfigureAwait(false);

        if (cooldowns != null)
        {
            cooldowns.Value.NextUsableAt[skill.SkillVnum] = now.Plus(Duration.FromMilliseconds(skill.CooldownMs));
        }
    }

    // Pursuit: cache JPS path, re-plan when the target moved or the path ran out.
    // Consume `Speed / 2` cells per tick — the 400ms life-loop cadence is already
    // the rate limit (matches OpenNos MapMonster.Move which broadcasts the same
    // multi-cell batch per MonsterLife tick).
    private async Task StepTowardAsync(INonPlayableEntity entity, IAliveEntity target)
    {
        if (entity.NpcMonster?.CanWalk != true) return;

        var map = entity.MapInstance;
        if (map == null) return;

        var cache = _pathCache.GetValueOrDefault(entity.Handle);
        var targetMoved = cache == null
            || cache.TargetX != target.PositionX
            || cache.TargetY != target.PositionY;

        if (cache == null || targetMoved || cache.Path.Count == 0)
        {
            var pathfinder = pathfindingService.ForMap(map.Map);
            var path = pathfinder.FindPath(
                    (entity.PositionX, entity.PositionY),
                    (target.PositionX, target.PositionY))
                .Skip(1) // first node is the start cell
                .ToList();
            cache = new CachedPath(target.PositionX, target.PositionY, path, clock.GetCurrentInstant());
            _pathCache[entity.Handle] = cache;
        }

        if (cache.Path.Count == 0) return;

        var speed = (int)Math.Max((byte)1, entity.NpcMonster!.Speed);
        var stepCount = Math.Min(cache.Path.Count, Math.Max(1, speed / 2));
        var dest = cache.Path[stepCount - 1];
        cache.Path.RemoveRange(0, stepCount);
        _pathCache[entity.Handle] = cache with { LastStepAt = clock.GetCurrentInstant() };

        if (!map.Map.IsWalkable((short)dest.Item1, (short)dest.Item2)) return;

        entity.PositionX = (short)dest.Item1;
        entity.PositionY = (short)dest.Item2;
        await map.SendPacketAsync(entity.GenerateMove(entity.PositionX, entity.PositionY)).ConfigureAwait(false);
    }

    private async Task<bool> TryReturnHomeAsync(INonPlayableEntity entity)
    {
        if (!entity.IsMoving) return false;

        var map = entity.MapInstance;
        if (map == null) return false;

        // INonPlayableEntity exposes the spawn cell via its MapX/MapY interface
        // members (see NpcComponentBundle / MonsterComponentBundle partial impls).
        if (entity.PositionX == entity.MapX && entity.PositionY == entity.MapY)
        {
            _pathCache.TryRemove(entity.Handle, out _);
            return false;
        }

        var pathfinder = pathfindingService.ForMap(map.Map);
        var path = pathfinder.FindPath(
                (entity.PositionX, entity.PositionY),
                (entity.MapX, entity.MapY))
            .Skip(1)
            .ToList();
        if (path.Count == 0) return false;

        var step = path[0];
        entity.PositionX = (short)step.Item1;
        entity.PositionY = (short)step.Item2;
        await map.SendPacketAsync(entity.GenerateMove(entity.PositionX, entity.PositionY)).ConfigureAwait(false);
        return true;
    }

    // Cached pathfinding result. Mutable only on replan or step-advance so the record
    // stays cheap to copy in concurrent tick scenarios.
    private sealed record CachedPath(short TargetX, short TargetY, List<(short X, short Y)> Path, Instant LastStepAt);

    private sealed record ChosenSkill(short SkillVnum, long CastId, int SkillRange, int CooldownMs);
}
