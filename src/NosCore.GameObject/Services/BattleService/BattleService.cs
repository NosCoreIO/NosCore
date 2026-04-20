//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using Serilog;
using Wolverine;

namespace NosCore.GameObject.Services.BattleService
{
    // Orchestrator: validate → resolve skill → resolve targets → enqueue hits → publish
    // packets + Wolverine events. Almost nothing happens here directly; the heavy lifting
    // lives in IDamageCalculator / IHitQueue / IBuffService / IRewardService. This class
    // stays testable because every collaborator is an interface.
    public sealed class BattleService(
        ISkillResolver skillResolver,
        ITargetResolver targetResolver,
        IHitQueue hitQueue,
        IMessageBus messageBus,
        ILogger logger) : IBattleService
    {
        public async Task Hit(IAliveEntity origin, IAliveEntity target, HitArguments arguments)
        {
            if (!CanAttack(origin, target))
            {
                await CancelAsync(origin, target).ConfigureAwait(false);
                return;
            }

            UpdateAttackerPosition(origin, arguments);

            var skill = skillResolver.Resolve(origin, arguments.SkillId);
            if (skill == null)
            {
                await CancelAsync(origin, target).ConfigureAwait(false);
                return;
            }

            var targets = targetResolver.Resolve(origin, target, skill);

            // Primary target is always first; we pass IsPrimaryTarget so the queue /
            // packet layer can treat the main hit specially (big animation, SkillEffect).
            for (var i = 0; i < targets.Count; i++)
            {
                var currentTarget = targets[i];
                var isPrimary = i == 0;
                try
                {
                    await ProcessHitAsync(origin, currentTarget, skill, isPrimary).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Hit processing failed: {Attacker} -> {Target}", origin.VisualId, currentTarget.VisualId);
                }
            }

            ScheduleCooldownReset(origin, skill);
        }

        private async Task ProcessHitAsync(IAliveEntity origin, IAliveEntity target, SkillInfo skill, bool isPrimary)
        {
            var request = new HitRequest(
                Origin: origin,
                Target: target,
                Skill: skill,
                IsPrimaryTarget: isPrimary,
                Completion: new TaskCompletionSource<HitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously),
                Cancellation: CancellationToken.None);

            var outcome = await hitQueue.EnqueueAsync(request).ConfigureAwait(false);
            if (outcome.Status == HitStatus.Cancelled)
            {
                return;
            }

            await BroadcastHitAsync(origin, target, skill, outcome).ConfigureAwait(false);
            await BroadcastStatusAsync(target).ConfigureAwait(false);
            await messageBus.PublishAsync(new EntityDamagedEvent(origin, target, outcome.Damage, outcome.Killed)).ConfigureAwait(false);

            if (outcome.Killed)
            {
                // Death broadcast: DiePacket tells the client who killed whom so it
                // plays the death animation + records the kill. Published before the
                // Wolverine event so handlers can assume the client already saw it.
                if (target.MapInstance != null)
                {
                    await target.MapInstance.SendPacketAsync(new DiePacket
                    {
                        VisualType = target.VisualType,
                        VisualId = target.VisualId,
                        TargetVisualType = origin.VisualType,
                        TargetId = origin.VisualId,
                    }).ConfigureAwait(false);
                }
                await messageBus.PublishAsync(new EntityDiedEvent(target, origin)).ConfigureAwait(false);
            }
        }

        // Two packets refresh the UI after a hit:
        //   * StPacket broadcast — the selected-target portrait at the top of every
        //     client's screen reads off `st`. SuPacket's HpPercentage animates the
        //     damage float but doesn't reliably update the portrait for monster
        //     targets (empirically the bar stays frozen at 100/100 until the player
        //     re-selects the mob). Broadcasting st to the map keeps every spectator
        //     who has this target selected in sync.
        //   * StatPacket unicast — the attacked character's own top-left HP/MP HUD
        //     is driven by `stat`, matching OpenNos TargetHit's
        //     `Broadcast(null, GenerateStat(), OnlySomeone, Target)`.
        private static async Task BroadcastStatusAsync(IAliveEntity target)
        {
            if (target.MapInstance == null) return;
            await target.MapInstance.SendPacketAsync(target.GenerateStatInfo()).ConfigureAwait(false);
            if (target is PlayerComponentBundle player)
            {
                await player.SendPacketAsync(player.GenerateStat()).ConfigureAwait(false);
            }
        }

        private static Task BroadcastHitAsync(IAliveEntity origin, IAliveEntity target, SkillInfo skill, HitOutcome outcome)
        {
            if (target.MapInstance == null)
            {
                return Task.CompletedTask;
            }

            var packet = new SuPacket
            {
                VisualType = origin.VisualType,
                VisualId = origin.VisualId,
                TargetVisualType = target.VisualType,
                TargetId = target.VisualId,
                SkillVnum = skill.SkillVnum,
                SkillCooldown = skill.Cooldown,
                AttackAnimation = skill.AttackAnimation,
                SkillEffect = skill.CastEffect,
                PositionX = origin.PositionX,
                PositionY = origin.PositionY,
                TargetIsAlive = target.IsAlive,
                HpPercentage = target.MaxHp > 0 ? (byte)(target.Hp * 100 / target.MaxHp) : (byte)0,
                Damage = (uint)outcome.Damage,
                HitMode = outcome.HitMode,
                SkillTypeMinusOne = (int)skill.Type - 1,
            };
            return target.MapInstance.SendPacketAsync(packet);
        }

        private void ScheduleCooldownReset(IAliveEntity origin, SkillInfo skill)
        {
            if (origin is not ICharacterEntity character) return;

            // Fire-and-forget: cooldown ends server-side at the same moment the client
            // re-enables the skill. We catch exceptions to avoid tearing down the orchestrator
            // on a disconnected character.
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(skill.Cooldown * 100).ConfigureAwait(false);
                    await character.SendPacketAsync(new SkillResetPacket { CastId = skill.CastId }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Failed to reset cooldown for skill {CastId}", skill.CastId);
                }
            });
        }

        private static void UpdateAttackerPosition(IAliveEntity origin, HitArguments arguments)
        {
            if (arguments is { MapX: not null, MapY: not null })
            {
                origin.PositionX = arguments.MapX.Value;
                origin.PositionY = arguments.MapY.Value;
            }
        }

        // NPCs and other non-combat entities expose NoAttack=true; treat them as
        // un-targetable. Without this an upgrade NPC (Smith Malcolm, etc.) could be
        // killed via a crafted UseSkill packet, breaking the n_run flow. The origin
        // check covers the same property for sleeping/vehicled/disabled attackers.
        private static bool CanAttack(IAliveEntity origin, IAliveEntity target)
        {
            if (!origin.IsAlive || !target.IsAlive) return false;
            if (origin.NoAttack) return false;
            if (target.NoAttack) return false;
            return true;
        }

        private static async Task CancelAsync(IAliveEntity origin, IAliveEntity target)
        {
            if (origin is ICharacterEntity character)
            {
                await character.SendPacketAsync(new CancelPacket
                {
                    Type = CancelPacketType.CancelAutoAttack,
                    TargetId = target.VisualId,
                }).ConfigureAwait(false);
            }
        }
    }
}
