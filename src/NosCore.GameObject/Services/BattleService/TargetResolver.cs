//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

// Given a primary target, walks the map instance for everyone else the skill should also
// damage. Single-target skills return exactly one entity; AOE skills walk monsters/NPCs
// via MapInstance and players via ISessionRegistry (since players aren't in the ECS
// query path yet). Allies are filtered by comparing VisualType between attacker and
// candidate — cheap and avoids a factions/party lookup for v1.
public sealed class TargetResolver(ISessionRegistry sessionRegistry) : ITargetResolver
{
    public IReadOnlyList<IAliveEntity> Resolve(IAliveEntity attacker, IAliveEntity primaryTarget, SkillInfo skill)
    {
        if (!skill.IsAoe)
        {
            return new[] { primaryTarget };
        }

        var results = new List<IAliveEntity>(capacity: 8) { primaryTarget };
        var mapInstance = primaryTarget.MapInstance;
        if (mapInstance == null)
        {
            return results;
        }

        var pattern = SkillCells.Parse(skill.CellPattern);
        var cells = pattern == null
            ? null
            : SkillCells.Resolve(pattern, attacker.PositionX, attacker.PositionY,
                primaryTarget.PositionX, primaryTarget.PositionY);

        var range = skill.TargetRange;
        var cx = primaryTarget.PositionX;
        var cy = primaryTarget.PositionY;

        foreach (var monster in mapInstance.Monsters)
        {
            if (monster.VisualId == primaryTarget.VisualId && monster.VisualType == primaryTarget.VisualType) continue;
            if (!monster.IsAlive) continue;
            if (!IsEnemy(attacker, monster)) continue;
            if (IsHit(cells, cx, cy, monster.PositionX, monster.PositionY, range))
            {
                results.Add(monster);
            }
        }

        // Only monsters strike players in an area, and only players or NPCs strike monsters,
        // the same sides BattleService.CanAttack enforces for the primary target.
        foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(mapInstance.MapInstanceId))
        {
            if (!session.HasPlayerEntity) continue;
            var player = session.Character;
            if (player.VisualId == primaryTarget.VisualId && primaryTarget.VisualType == VisualType.Player) continue;
            if (player.VisualId == attacker.VisualId && attacker.VisualType == VisualType.Player) continue;
            if (!player.IsAlive) continue;
            if (!IsEnemy(attacker, player)) continue;
            if (IsHit(cells, cx, cy, player.PositionX, player.PositionY, range))
            {
                results.Add(player);
            }
        }

        return results;
    }

    // Players and NPCs are one side, monsters the other; nobody in an area hits their own side.
    private static bool IsEnemy(IAliveEntity attacker, IAliveEntity candidate)
    {
        return (attacker.VisualType, candidate.VisualType) switch
        {
            (VisualType.Player, VisualType.Monster) => true,
            (VisualType.Monster, VisualType.Player) => true,
            (VisualType.Npc, VisualType.Monster) => true,
            _ => false,
        };
    }

    private static bool IsHit(HashSet<(short X, short Y)>? cells, short cx, short cy, short x,
        short y, int range)
    {
        return cells != null ? cells.Contains((x, y)) : WithinRange(cx, cy, x, y, range);
    }

    private static bool WithinRange(short cx, short cy, short x, short y, int range)
    {
        return Math.Abs(cx - x) <= range && Math.Abs(cy - y) <= range;
    }
}
