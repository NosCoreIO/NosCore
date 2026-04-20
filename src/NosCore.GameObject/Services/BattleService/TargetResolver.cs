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

        var range = skill.TargetRange;
        var cx = primaryTarget.PositionX;
        var cy = primaryTarget.PositionY;

        foreach (var monster in mapInstance.Monsters)
        {
            if (monster.VisualId == primaryTarget.VisualId && monster.VisualType == primaryTarget.VisualType) continue;
            if (!monster.IsAlive) continue;
            if (!IsEnemy(attacker, monster)) continue;
            if (WithinRange(cx, cy, monster.PositionX, monster.PositionY, range))
            {
                results.Add(monster);
            }
        }

        // Player targets are only relevant when the attacker or allies are non-player
        // (e.g. a monster AOE hits several players), or in PvP contexts. In both cases
        // we enumerate players on the same map and filter out allies.
        foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(mapInstance.MapInstanceId))
        {
            if (!session.HasPlayerEntity) continue;
            var player = session.Character;
            if (player.VisualId == primaryTarget.VisualId && primaryTarget.VisualType == VisualType.Player) continue;
            if (player.VisualId == attacker.VisualId && attacker.VisualType == VisualType.Player) continue;
            if (!player.IsAlive) continue;
            if (!IsEnemy(attacker, player)) continue;
            if (WithinRange(cx, cy, player.PositionX, player.PositionY, range))
            {
                results.Add(player);
            }
        }

        return results;
    }

    // "Enemy" for v1 is "different visual type" — a character AOE hurts monsters and
    // other characters (PvP), but a monster AOE only hurts characters and not other
    // monsters. Groups/party aliasing is a follow-up task.
    private static bool IsEnemy(IAliveEntity attacker, IAliveEntity candidate)
    {
        return (attacker.VisualType, candidate.VisualType) switch
        {
            (VisualType.Player, VisualType.Monster) => true,
            (VisualType.Player, VisualType.Player) => true,
            (VisualType.Monster, VisualType.Player) => true,
            (VisualType.Npc, VisualType.Player) => true,
            (VisualType.Npc, VisualType.Monster) => true,
            _ => false,
        };
    }

    private static bool WithinRange(short cx, short cy, short x, short y, int range)
    {
        return Math.Abs(cx - x) <= range && Math.Abs(cy - y) <= range;
    }
}
