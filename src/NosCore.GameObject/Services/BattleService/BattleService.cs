//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
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
        IClock clock,
        ICaptureService captureService,
        ILogger<BattleService> logger) : IBattleService
    {
        // CharacterId (VisualId) → CastId → ReadyAt. Populated by ScheduleCooldownReset
        // and drained by TickCooldownResetsAsync on each map's 400ms life tick.
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<long, Instant>> _pendingCooldownResets = new();

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

            // A capture deals no damage: CaptureService sends its own animation and outcome, and a
            // failed attempt still costs the cooldown below.
            if (captureService.IsCaptureSkill(skill))
            {
                try
                {
                    await captureService.TryCaptureAsync(origin, target, skill).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Capture attempt failed: {Attacker} -> {Target}", origin.VisualId, target.VisualId);
                }

                ScheduleCooldownReset(origin, skill);
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

            // stat goes to the target before su: the client paints the damage number first and
            // re-reads its cached HP for the bar, so a stat arriving later leaves the bar frozen.
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

        // The target's own HP/MP bars read off `stat`, which goes to the target alone before the su
        // goes to the map, so the bar settles before the damage animates.
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

            var readyAt = clock.GetCurrentInstant().Plus(Duration.FromMilliseconds(skill.Cooldown * 100));
            _pendingCooldownResets
                .GetOrAdd(character.VisualId, _ => new ConcurrentDictionary<long, Instant>())[skill.CastId] = readyAt;
        }

        public async Task TickCooldownResetsAsync(MapInstance mapInstance)
        {
            if (_pendingCooldownResets.IsEmpty) return;
            var now = clock.GetCurrentInstant();
            foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(mapInstance.MapInstanceId))
            {
                if (!session.HasPlayerEntity) continue;
                var character = session.Character;
                if (!_pendingCooldownResets.TryGetValue(character.VisualId, out var skills)) continue;
                foreach (var (castId, readyAt) in skills)
                {
                    if (readyAt > now) continue;
                    if (!skills.TryRemove(castId, out _)) continue;
                    if (character.IsDisconnecting) continue;
                    try
                    {
                        await character.SendPacketAsync(new SkillResetPacket { CastId = castId }).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to reset cooldown for skill {CastId}", castId);
                    }
                }
            }
        }

        private static void UpdateAttackerPosition(IAliveEntity origin, HitArguments arguments)
        {
            if (arguments is not { MapX: not null, MapY: not null })
            {
                return;
            }

            if (origin.MapInstance?.Map.IsWalkable(arguments.MapX.Value, arguments.MapY.Value) == false)
            {
                return;
            }

            origin.PositionX = arguments.MapX.Value;
            origin.PositionY = arguments.MapY.Value;
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
