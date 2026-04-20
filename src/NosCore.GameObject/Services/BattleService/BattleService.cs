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
using NosCore.Shared.Enumerations;
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

            // Order matters: OpenNos ships stat-to-target BEFORE the SuPacket so the
            // client HUD bar settles to the new HP before the damage floater animates.
            // Swapping the order makes the bar appear "frozen" even though the packet
            // arrived — the client paints the floater first, re-reads its cached HP
            // for the bar, then finally applies the stat update out-of-band.
            await SendTargetStatAsync(target).ConfigureAwait(false);
            await BroadcastHitAsync(origin, target, skill, outcome).ConfigureAwait(false);
            await BroadcastTargetInfoAsync(target).ConfigureAwait(false);
            await messageBus.PublishAsync(new EntityDamagedEvent(origin, target, outcome.Damage, outcome.Killed)).ConfigureAwait(false);

            if (outcome.Killed)
            {
                // Players get a DiePacket so the client plays the death pose + revive
                // dialog. Monsters don't — OpenNos MapMonster.MonsterLife emits only the
                // closing su (alive=0, hp%=0) for natural kills and the client drives the
                // collapse animation from that alone. MonsterRespawnHandler ships the
                // OutPacket a moment later to clear the sprite.
                if (target.MapInstance != null && target.VisualType == VisualType.Player)
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

        // The attacked character's own top-left HP/MP HUD reads off `stat`. OpenNos
        // emits this unicast to just the target (`ReceiverType.OnlySomeone`) BEFORE
        // the SuPacket — BattleEntity.TargetHit builds GenerateStat, broadcasts it to
        // only the target, then broadcasts SuPacket to the whole map. We mirror that
        // order so the client settles the bar before animating damage.
        private static Task SendTargetStatAsync(IAliveEntity target)
        {
            if (target is PlayerComponentBundle player)
            {
                return player.SendPacketAsync(player.GenerateStat());
            }
            return Task.CompletedTask;
        }

        // StPacket broadcast — the selected-target portrait at the top of every
        // client's screen reads off `st`. SuPacket's HpPercentage animates the damage
        // floater but doesn't reliably update the target portrait for monsters (the
        // bar stays at 100/100 until the player re-selects). Broadcasting `st` after
        // the hit keeps every spectator who has this target selected in sync.
        private static Task BroadcastTargetInfoAsync(IAliveEntity target)
        {
            if (target.MapInstance == null) return Task.CompletedTask;
            return target.MapInstance.SendPacketAsync(target.GenerateStatInfo());
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
