//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;

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
                return scriptedInstanceService.GetRun(npcMap.MapInstanceId) is { } npcRun
                    ? NpcFellAsync(npcRun, npcMap.MapInstanceId, npc.VisualId, npc.IsEscort, npc.DeathStep)
                    : Task.CompletedTask;
            }

            if (evt.Victim is not MonsterComponentBundle { MapInstance: { } mapInstance } monster)
            {
                return Task.CompletedTask;
            }

            var run = scriptedInstanceService.GetRun(mapInstance.MapInstanceId);
            return run == null
                ? Task.CompletedTask
                : run.MonsterKilledAsync(mapInstance.MapInstanceId, monster.VNum, monster.IsBonus, monster.DeathStep,
                    evt.Killer is MonsterComponentBundle ? null : evt.KillerLevel, monster.VisualId);
        }

        // Every NPC that falls counts as lost. An escort's fall fails the run, which takes the place of its
        // death step; any other NPC runs its own.
        private static async Task NpcFellAsync(ScriptedInstanceRun run, Guid mapInstanceId, long npcId, bool isEscort,
            Action<IInstanceRun>? deathStep)
        {
            await run.NpcFellAsync(mapInstanceId, npcId);
            if (isEscort)
            {
                await run.EscortFellAsync(mapInstanceId);
            }
            else if (deathStep != null)
            {
                await run.NpcFellAsync(mapInstanceId, deathStep);
            }
        }

        // A raid counts each player's own hits, the killing blow included; a partner's or a pet's do not count.
        // The thresholds a hit crossed are taken from the HP that hit left, so a later hit that has
        // already landed, or killed the monster, cannot hide them.
        [UsedImplicitly]
        public Task Handle(EntityDamagedEvent evt)
        {
            if (evt.Target is not MonsterComponentBundle { MapInstance: { } mapInstance } monster)
            {
                return Task.CompletedTask;
            }

            if (evt.Attacker is PlayerComponentBundle player && mapInstance.MapInstanceType == MapInstanceType.RaidInstance
                && scriptedInstanceService.GetRun(mapInstance.MapInstanceId) is { } raid)
            {
                raid.MonsterDamaged(mapInstance.MapInstanceId, monster.VisualId, monster.MaxHp, player.CharacterId, evt.Damage);
            }

            if (evt.Killed || (monster.HitStep == null && monster.HpThresholds.Count == 0)
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
        public Task Handle(ItemsReceivedEvent evt)
        {
            if (evt.PlayerHasLeft || evt.Character.MapInstance is not { } mapInstance)
            {
                return Task.CompletedTask;
            }

            return scriptedInstanceService.GetRun(mapInstance.MapInstanceId)?.ItemCollectedAsync(mapInstance.MapInstanceId, evt.VNum, evt.Amount)
                ?? Task.CompletedTask;
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
