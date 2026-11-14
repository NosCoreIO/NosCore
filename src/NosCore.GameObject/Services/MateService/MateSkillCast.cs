//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;
using NosCore.PathFinder.Interfaces;

namespace NosCore.GameObject.Services.MateService
{
    // What a skill cast from a NosMate's skill bar has to pass and what it costs, for a partner and a pet alike.
    internal static class MateSkillCast
    {
        private const int MinimumCooldownMs = 200;

        public static async Task<bool> CastAsync(PlayerComponentBundle owner, Mate mate, SkillDto skill, IAliveEntity target,
            IBattleService battleService, IHeuristic distanceCalculator, Instant now)
        {
            if (!mate.IsTeamMember
                || mate.Entity is not { } entity
                || target.MapInstance != entity.MapInstance
                || entity.Mp < skill.MpCost
                || (entity.NextUsableAt.TryGetValue(skill.SkillVNum, out var readyAt) && readyAt > now))
            {
                return false;
            }

            var distance = (int)distanceCalculator.GetDistance((entity.PositionX, entity.PositionY),
                (target.PositionX, target.PositionY));
            var targetArea = target is INonPlayableEntity { NpcMonster: { } npcMonster } ? npcMonster.BasicArea : (byte)0;
            if (!SkillReach.IsInReach(skill.TargetType, skill.Range, skill.TargetRange, distance, targetArea)
                || !await battleService.Hit(entity, target, new HitArguments { SkillId = skill.CastId, Skill = skill }))
            {
                return false;
            }

            entity.NextUsableAt[skill.SkillVNum] =
                now.Plus(Duration.FromMilliseconds(Math.Max(MinimumCooldownMs, skill.Cooldown * 100)));
            entity.Mp -= skill.MpCost;
            mate.Mp = entity.Mp;
            await owner.SendPacketAsync(mate.GeneratePst());
            return true;
        }
    }
}
