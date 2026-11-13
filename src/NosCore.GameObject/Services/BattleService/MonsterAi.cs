//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Enumerations.Map;
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
using NosCore.GameObject.Services.MovementService;

namespace NosCore.GameObject.Services.BattleService;

// Aggro-driven AI for both monsters and NPCs (guards), called from MapInstance's life
// loop every 400ms. Each tick:
//   1. Hostile entities without a target scan NoticeRange for the closest enemy; a
//      group member (GroupAttack) without one takes the target of a fighting member.
//      Two-faction rule — NPCs and players are same-side, monsters the other: NPCs
//      target monsters only, monsters target whichever of player/NPC is closer.
//   2. 20% chance to pick a random cooldown-ready NpcMonsterSkill, else the basic
//      attack; if the target is within its range, enqueue a Hit via IBattleService and
//      stamp the cooldown once the hit was accepted.
//   3. Otherwise step toward the target along a cached JPS path, stopping beside it.
//      Stationary entities (CanWalk=false) never chase but still attack.
// Once the aggro leash expires (aggroService.Current returns HasTarget=false) the entity
// paths back to FirstX/FirstY.
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

    private static readonly Duration UnreachableRetry = Duration.FromSeconds(2);

    public async Task<bool> TickAsync(INonPlayableEntity entity)
    {
        try
        {
            if (!entity.IsAlive || entity.NpcMonster == null) return false;

            if (entity.CastingUntil > clock.GetCurrentInstant()) return true;

            var target = CurrentTarget(entity);
            if (target == null && entity.NpcMonster.IsHostile)
            {
                var noticed = DetectNearbyEnemy(entity);
                if (noticed != null)
                {
                    aggroService.AddThreat(entity, noticed, 1);
                    target = CurrentTarget(entity);
                }
            }

            if (target == null && entity.NpcMonster.GroupAttack != (byte)GroupAttackType.None)
            {
                var groupTarget = TargetOfGroup(entity);
                if (groupTarget != null)
                {
                    aggroService.AddThreat(entity, groupTarget, 1);
                    target = CurrentTarget(entity);
                }
            }

            if (target == null)
            {
                return await TryReturnHomeAsync(entity);
            }

            var skill = PickSkill(entity) ?? BasicAttack(entity);
            var distance = (int)distanceCalculator.GetDistance(
                (entity.PositionX, entity.PositionY),
                (target.PositionX, target.PositionY));

            if (distance <= skill.SkillRange)
            {
                await AttackAsync(entity, target, skill);
            }
            else if (entity.NpcMonster.CanWalk)
            {
                await StepAlongPathAsync(entity, target.PositionX, target.PositionY, cellsToLeave: 1);
            }

            return true;
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
        entity.ChasePath = null;
        return null;
    }

    private async Task<bool> TryReturnHomeAsync(INonPlayableEntity entity)
    {
        if (!entity.IsMoving) return false;

        if (entity.PositionX == entity.MapX && entity.PositionY == entity.MapY)
        {
            entity.ChasePath = null;
            return false;
        }

        return await StepAlongPathAsync(entity, entity.MapX, entity.MapY, cellsToLeave: 0);
    }

    private static ChosenSkill BasicAttack(INonPlayableEntity entity)
    {
        var npcMonster = entity.NpcMonster;
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

    // A group member that is not fighting joins whatever another member of its group is
    // fighting, once that target comes within its own notice range.
    private IAliveEntity? TargetOfGroup(INonPlayableEntity entity)
    {
        var map = entity.MapInstance;
        if (map == null) return null;

        var range = (int)Math.Max(entity.NpcMonster.NoticeRange, (byte)1);
        if (entity.VisualType == VisualType.Npc)
        {
            foreach (var npc in map.EnumerateNpcs())
            {
                if (TargetOfMember(entity, npc, range) is { } target) return target;
            }
            return null;
        }

        foreach (var monster in map.EnumerateMonsters())
        {
            if (TargetOfMember(entity, monster, range) is { } target) return target;
        }
        return null;
    }

    private IAliveEntity? TargetOfMember<TMember>(INonPlayableEntity entity, TMember member, int range)
        where TMember : INonPlayableEntity
    {
        if (member.VisualId == entity.VisualId || !member.IsAlive || !SameGroup(entity.NpcMonster, member.NpcMonster))
        {
            return null;
        }

        var aggro = aggroService.Current(member);
        if (!aggro.HasTarget) return null;

        var target = ResolveTarget(entity, aggro.TargetVisualId);
        if (target is not { IsAlive: true } || target.VisualType == entity.VisualType) return null;

        var d = distanceCalculator.GetDistance(
            (entity.PositionX, entity.PositionY),
            (target.PositionX, target.PositionY));
        return d <= range ? target : null;
    }

    private static bool SameGroup(NpcMonsterDto entity, NpcMonsterDto? other)
    {
        if (other == null || other.GroupAttack != entity.GroupAttack) return false;

        return (GroupAttackType)entity.GroupAttack switch
        {
            GroupAttackType.SameRace => other.Race == entity.Race,
            GroupAttackType.SameIcon => other.IconId == entity.IconId,
            _ => false
        };
    }

    // Resolves the cached aggro target by visual id. Walks every pool (players and
    // their mates, monsters, NPCs) because any of them can be an aggro target.
    private IAliveEntity? ResolveTarget(INonPlayableEntity entity, long visualId)
    {
        var map = entity.MapInstance;
        if (map == null) return null;

        foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(entity.MapInstanceId))
        {
            if (!session.HasPlayerEntity)
            {
                continue;
            }

            if (session.Character.VisualId == visualId)
            {
                return session.Character;
            }

            foreach (var mate in session.Character.Mates.Values)
            {
                if (mate.Entity is { } mateEntity && mateEntity.VisualId == visualId)
                {
                    return mateEntity;
                }
            }
        }
        foreach (var monster in map.EnumerateMonsters())
        {
            if (monster.VisualId == visualId) return monster;
        }
        foreach (var npc in map.EnumerateNpcs())
        {
            if (npc.VisualId == visualId) return npc;
        }
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

        var skills = catalog.GetSkills(entity.NpcMonster.NpcMonsterVNum);
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
                CooldownMs: Math.Max(200, dto.Cooldown * 100),
                CastTimeMs: dto.CastTime * 100);
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

        if (skill.CastTimeMs > 0)
        {
            // The life loop must not wait out the cast: the monster stands still until the hit
            // has resolved on its own.
            if (cooldowns != null)
            {
                cooldowns.Value.NextUsableAt[skill.SkillVnum] = now.Plus(Duration.FromMilliseconds(skill.CooldownMs));
            }

            entity.CastingUntil = Instant.MaxValue;
            _ = HitAfterCastAsync(entity, target, skill);
            return;
        }

        var accepted = await battleService.Hit(entity, target, new HitArguments { SkillId = skill.CastId });

        if (accepted && cooldowns != null)
        {
            cooldowns.Value.NextUsableAt[skill.SkillVnum] = now.Plus(Duration.FromMilliseconds(skill.CooldownMs));
        }
    }

    private async Task HitAfterCastAsync(INonPlayableEntity entity, IAliveEntity target, ChosenSkill skill)
    {
        try
        {
            await battleService.Hit(entity, target, new HitArguments { SkillId = skill.CastId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cast of skill {Skill} by {VisualId} failed", skill.SkillVnum, entity.VisualId);
        }
        finally
        {
            if (entity.MapInstance?.EcsWorld.IsAlive(entity.Handle) == true)
            {
                entity.CastingUntil = Instant.MinValue;
            }
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
        if (map == null || entity.Speed == 0) return false;

        var cache = entity.ChasePath;
        var now = clock.GetCurrentInstant();

        // A goal the last search found no way to is searched for again only after a pause; a
        // target out of reach would otherwise cost a full search on every tick.
        if (cache is { Unreachable: true } && now - cache.LastStepAt < UnreachableRetry)
        {
            return false;
        }

        var goalMoved = cache == null || cache.TargetX != goalX || cache.TargetY != goalY;
        // A path cached before the entity was moved elsewhere (a respawn) no longer starts next to it.
        var leftPath = cache is { Path.Count: > 0 }
            && WalkPath.Steps(cache.Path[0].Item1, cache.Path[0].Item2, entity.PositionX, entity.PositionY) > 1;

        if (cache == null || goalMoved || leftPath || cache.Path.Count == 0)
        {
            var pathfinder = pathfindingService.ForMap(map.Map);
            var path = pathfinder.FindPath(
                    (entity.PositionX, entity.PositionY),
                    (goalX, goalY))
                .ToList();
            cache = new CachedPath(goalX, goalY, path, now, path.Count == 0);
            entity.ChasePath = cache;
        }

        var stepCount = Math.Min(cache.Path.Count - cellsToLeave, Math.Max(1, entity.Speed / 2));
        if (stepCount <= 0) return false;

        var dest = cache.Path[stepCount - 1];
        cache.Path.RemoveRange(0, stepCount);
        entity.ChasePath = cache with { LastStepAt = clock.GetCurrentInstant() };

        if (!map.Map.IsWalkable((short)dest.Item1, (short)dest.Item2)) return false;

        entity.PositionX = (short)dest.Item1;
        entity.PositionY = (short)dest.Item2;
        await map.SendPacketAsync(entity.GenerateMove(entity.PositionX, entity.PositionY));
        return true;
    }

    private sealed record ChosenSkill(short SkillVnum, long CastId, int SkillRange, int CooldownMs, int CastTimeMs = 0);
}
