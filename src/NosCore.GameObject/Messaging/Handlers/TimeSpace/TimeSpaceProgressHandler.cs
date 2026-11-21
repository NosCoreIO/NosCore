//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ScriptedInstanceService;

namespace NosCore.GameObject.Messaging.Handlers.TimeSpace
{
    [UsedImplicitly]
    public sealed class TimeSpaceProgressHandler(IScriptedInstanceService scriptedInstanceService)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is NpcComponentBundle { MapInstance: { } npcMap } npc)
            {
                var npcRun = scriptedInstanceService.GetRun(npcMap.MapInstanceId);
                if (npcRun == null)
                {
                    return Task.CompletedTask;
                }

                return npc.IsEscort
                    ? npcRun.EscortFellAsync(npcMap.MapInstanceId)
                    : npcRun.NpcFellAsync(npcMap.MapInstanceId, npc.VisualId);
            }

            if (evt.Victim is not MonsterComponentBundle { MapInstance: { } mapInstance } monster)
            {
                return Task.CompletedTask;
            }

            var run = scriptedInstanceService.GetRun(mapInstance.MapInstanceId);
            return run == null
                ? Task.CompletedTask
                : run.MonsterKilledAsync(mapInstance.MapInstanceId, monster.VNum, monster.IsBonus, monster.DeathStep);
        }

        // The thresholds a hit crossed are taken from the HP that hit left, so a later hit that has
        // already landed, or killed the monster, cannot hide them.
        [UsedImplicitly]
        public Task Handle(EntityDamagedEvent evt)
        {
            if (evt.Killed || evt.Target is not MonsterComponentBundle { MapInstance: { } mapInstance } monster
                || (monster.HitStep == null && monster.HpThresholds.Count == 0)
                || scriptedInstanceService.GetRun(mapInstance.MapInstanceId) is not { } run)
            {
                return Task.CompletedTask;
            }

            var thresholds = monster.HpThresholds;
            var crossed = thresholds.Where(t => (long)evt.HpLeft * 100 < (long)t.Percent * monster.MaxHp).ToList();
            if (crossed.Count > 0)
            {
                monster.HpThresholds = thresholds.Except(crossed).ToList();
            }

            return run.MonsterHitAsync(mapInstance.MapInstanceId, monster.HitStep, crossed.Select(t => t.Step).ToList());
        }

        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt)
        {
            var run = scriptedInstanceService.GetRun(evt.MapInstance.MapInstanceId);
            return run == null
                ? Task.CompletedTask
                : run.EnteredAsync(evt.ClientSession, evt.MapInstance.MapInstanceId);
        }
    }
}
