//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using Microsoft.Extensions.Logging;
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
        ISessionRegistry sessionRegistry,
        ILogger<BattleService> logger) : IBattleService
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
                    logger.LogError(ex, "Hit processing failed: {Attacker} -> {Target}", origin.VisualId, currentTarget.VisualId);
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
                // DiePacket plays the collapsed death pose on other spectators' screens.
                // We deliberately skip the victim — they get the dialog/stat flow from
                // PlayerRevivalHandler, and their own client drives the death pose off
                // the closing su (alive=0, hp%=0). Monsters don't get a DiePacket at
                // all; the su alone is enough and sending an OutPacket would cut the
                // collapse animation short.
                if (target.MapInstance != null && target.VisualType == VisualType.Player)
                {
                    var diePacket = new DiePacket
                    {
                        VisualType = target.VisualType,
                        VisualId = target.VisualId,
                        TargetVisualType = origin.VisualType,
                        TargetId = origin.VisualId,
                    };
                    var victimCharacterId = target is ICharacterEntity victim ? victim.CharacterId : 0L;
                    foreach (var spectator in sessionRegistry
                        .GetClientSessionsByMapInstance(target.MapInstance.MapInstanceId)
                        .Where(s => s.HasSelectedCharacter && s.Character.CharacterId != victimCharacterId))
                    {
                        await spectator.SendPacketAsync(diePacket).ConfigureAwait(false);
                    }
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
                TargetCurrentHp = target.Hp,
                TargetMaxHp = target.MaxHp,
            };
            return target.MapInstance.SendPacketAsync(packet);
        }

        private void ScheduleCooldownReset(IAliveEntity origin, SkillInfo skill)
        {
            if (origin is not ICharacterEntity character) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(skill.Cooldown * 100).ConfigureAwait(false);
                    if (character.IsDisconnecting) return;
                    await character.SendPacketAsync(new SkillResetPacket { CastId = skill.CastId }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to reset cooldown for skill {CastId}", skill.CastId);
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

        // NoAttack is a state-only gate (locked / sleeping / vehicled); the faction
        // rule below is what decides who's on whose side. Characters and NPCs are
        // allies — players can't kill a shop NPC via a crafted UseSkill packet, and
        // guards never friendly-fire a player — so any ally pairing bails first.
        // Monsters are opposing-faction to both and are free to hit either.
        private static bool CanAttack(IAliveEntity origin, IAliveEntity target)
        {
            if (!origin.IsAlive || !target.IsAlive) return false;
            if (origin.NoAttack) return false;
            if (target.NoAttack) return false;
            if (AreAllies(origin, target)) return false;
            return true;
        }

        private static bool AreAllies(IAliveEntity a, IAliveEntity b)
        {
            var aAlly = a.VisualType == VisualType.Player || a.VisualType == VisualType.Npc;
            var bAlly = b.VisualType == VisualType.Player || b.VisualType == VisualType.Npc;
            return aAlly && bAlly;
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
