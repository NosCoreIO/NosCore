//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.PathFinder.Interfaces;

namespace NosCore.GameObject.Services.MateService
{
    public sealed class MateAttackService(IBattleService battleService, IHeuristic distanceCalculator, IClock clock)
        : IMateAttackService
    {
        // Keyed like the monster AI's basic attack, so both paths share one cooldown.
        private const short BasicAttackKey = 0;
        private const int MinimumCooldownMs = 200;

        public async Task<bool> BasicAttackAsync(INonPlayableEntity attacker, IAliveEntity target)
        {
            var npcMonster = attacker.NpcMonster;
            if (npcMonster == null || attacker.MapInstance is not { } map || target.MapInstance != map
                || map.EcsWorld.TryGetComponent<SkillCooldownComponent>(attacker.Handle) is not { } cooldowns)
            {
                return false;
            }

            var now = clock.GetCurrentInstant();
            if (cooldowns.NextUsableAt.TryGetValue(BasicAttackKey, out var readyAt) && readyAt > now)
            {
                return false;
            }

            var distance = (int)distanceCalculator.GetDistance((attacker.PositionX, attacker.PositionY),
                (target.PositionX, target.PositionY));
            var targetArea = target is INonPlayableEntity { NpcMonster: { } targetMonster } ? targetMonster.BasicArea : (byte)0;
            if (!SkillReach.IsInReach(SkillReach.TargetedSkill, npcMonster.BasicRange, 0, distance, targetArea))
            {
                return false;
            }

            if (!await battleService.Hit(attacker, target, new HitArguments { BasicAttack = true }))
            {
                return false;
            }

            cooldowns.NextUsableAt[BasicAttackKey] =
                now.Plus(Duration.FromMilliseconds(Math.Max(MinimumCooldownMs, npcMonster.BasicCooldown * 100)));
            return true;
        }
    }
}
