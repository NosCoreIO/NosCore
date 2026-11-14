//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ArenaService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using Microsoft.Extensions.Logging;
using Wolverine;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

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
        ICastDelayService castDelayService,
        IBuffService buffService,
        ILogger<BattleService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IBattleService, ISingletonService
    {
        public async Task<bool> Hit(IAliveEntity origin, IAliveEntity target, HitArguments arguments)
        {
            if (!CanAttack(origin, target) || IsStunned(origin))
            {
                await CancelAsync(origin, target);
                return false;
            }

            var skill = Resolve(origin, arguments);
            if (skill == null)
            {
                await CancelAsync(origin, target).ConfigureAwait(false);
                return false;
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
                    logger.LogError(ex, logLanguage[LogLanguageKey.CAPTURE_FAILED], origin.VisualId, target.VisualId);
                }

                ScheduleCooldownReset(origin, skill);
                return true;
            }

            if (!await CastAsync(origin, target, skill))
            {
                await CancelAsync(origin, target);
                return false;
            }

            var targets = targetResolver.Resolve(origin, target, skill);
            if (skill.HitsSeveral && arguments.ListedTargets is { Count: > 0 } listed)
            {
                targets = targets
                    .Concat(listed.Where(t => CanAttack(origin, t) && ReferenceEquals(t.MapInstance, origin.MapInstance)))
                    .DistinctBy(t => (t.VisualType, t.VisualId))
                    .ToList();
            }

            // Primary target is always first; we pass IsPrimaryTarget so the queue /
            // packet layer can treat the main hit specially (big animation, SkillEffect).
            var presentation = new HitPresentation(origin.PositionX, origin.PositionY, false);
            for (var i = 0; i < targets.Count; i++)
            {
                await TryProcessHitAsync(origin, targets[i], skill, i == 0, presentation);
            }

            ScheduleCooldownReset(origin, skill);
            return true;
        }

        public async Task<bool> HitArea(IAliveEntity origin, short mapX, short mapY, HitArguments arguments)
        {
            var mapInstance = origin.MapInstance;
            var skill = CanCast(origin) && !IsStunned(origin) && mapInstance != null ? Resolve(origin, arguments) : null;
            if (mapInstance == null || skill == null || !skill.HitsSeveral || captureService.IsCaptureSkill(skill))
            {
                await CancelAsync(origin, null);
                return false;
            }

            if (!await CastAsync(origin, null, skill))
            {
                await CancelAsync(origin, null);
                return false;
            }

            await mapInstance.SendPacketAsync(new BsPacket
            {
                VisualType = origin.VisualType,
                VisualId = origin.VisualId,
                MapX = mapX,
                MapY = mapY,
                SkillVnum = skill.SkillVnum,
                SkillCooldown = skill.Cooldown,
                AttackAnimation = skill.AttackAnimation,
                SkillEffect = skill.Effect
            });
            var presentation = new HitPresentation(mapX, mapY, true);
            var targets = targetResolver.ResolveArea(origin, mapInstance, mapX, mapY, skill);
            for (var i = 0; i < targets.Count; i++)
            {
                await TryProcessHitAsync(origin, targets[i], skill, i == 0, presentation);
            }

            ScheduleCooldownReset(origin, skill);
            return true;
        }

        private async Task TryProcessHitAsync(IAliveEntity origin, IAliveEntity target, SkillInfo skill, bool isPrimary,
            HitPresentation presentation)
        {
            try
            {
                await ProcessHitAsync(origin, target, skill, isPrimary, presentation);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, logLanguage[LogLanguageKey.HIT_PROCESSING_FAILED], origin.VisualId, target.VisualId);
            }
        }

        private async Task ProcessHitAsync(IAliveEntity origin, IAliveEntity target, SkillInfo skill, bool isPrimary,
            HitPresentation presentation)
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
            await BroadcastHitAsync(origin, target, skill, outcome, presentation);
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
                    var victimCharacterId = target is PlayerComponentBundle victim ? victim.CharacterId : 0L;
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
        private Task SendTargetStatAsync(IAliveEntity target)
        {
            switch (target)
            {
                case PlayerComponentBundle player:
                    return player.SendPacketAsync(player.GenerateStat());
                case MateComponentBundle mate when sessionRegistry.TryGetCharacter(s => s.CharacterId == mate.OwnerId, out var owner):
                    mate.Mate.Hp = mate.Hp;
                    return owner.SendPacketAsync(mate.Mate.GeneratePst());
                default:
                    return Task.CompletedTask;
            }
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

        private static Task BroadcastHitAsync(IAliveEntity origin, IAliveEntity target, SkillInfo skill, HitOutcome outcome,
            HitPresentation presentation)
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
                SkillEffect = skill.Effect,
                PositionX = presentation.X,
                PositionY = presentation.Y,
                TargetIsAlive = target.IsAlive,
                HpPercentage = target.MaxHp > 0 ? (byte)(target.Hp * 100 / target.MaxHp) : (byte)0,
                Damage = (uint)outcome.Damage,
                HitMode = presentation.InArea ? InArea(outcome.HitMode) : outcome.HitMode,
                SkillTypeMinusOne = (int)skill.Type - 1,
                TargetCurrentHp = target.Hp,
                TargetMaxHp = target.MaxHp,
            };
            return target.MapInstance.SendPacketAsync(packet);
        }

        // A blow from a ground skill is drawn as caught in the area, so the client plays it at the
        // cast cell rather than as a strike from the caster.
        private static SuPacketHitMode InArea(SuPacketHitMode hitMode) => hitMode switch
        {
            SuPacketHitMode.SuccessAttack => SuPacketHitMode.AttackedInAoe,
            SuPacketHitMode.CriticalAttack => SuPacketHitMode.AttackedInAoeCrit,
            SuPacketHitMode.Miss => SuPacketHitMode.MissAoe,
            _ => hitMode,
        };

        // Where su draws the blow from and whether it is drawn as caught in an area.
        private readonly record struct HitPresentation(short X, short Y, bool InArea);

        // The client sends the skill as soon as it is used and leaves the wind-up to the server:
        // ct starts the cast animation, and the hit resolves once the cast time has passed and
        // both sides can still fight.
        private SkillInfo? Resolve(IAliveEntity origin, HitArguments arguments) =>
            arguments.Skill is { } skill ? skillResolver.Describe(skill) : skillResolver.Resolve(origin, arguments.SkillId);

        // A cast at a cell is always announced with ct_n, cast time or not; a cast at an entity
        // only needs ct when there is a wind-up to show.
        private async Task<bool> CastAsync(IAliveEntity origin, IAliveEntity? target, SkillInfo skill)
        {
            var map = origin.MapInstance;
            if (target == null && map != null)
            {
                await map.SendPacketAsync(new CtNPacket
                {
                    VisualType = origin.VisualType,
                    VisualId = origin.VisualId,
                    TargetVisualType = origin.VisualType,
                    CastAnimationId = skill.CastAnimation,
                    CastEffect = skill.CastEffect,
                    SkillId = skill.SkillVnum
                });
            }

            if (skill.CastTime <= 0)
            {
                return true;
            }

            if (target != null && map != null)
            {
                await map.SendPacketAsync(new CtPacket
                {
                    VisualType = origin.VisualType,
                    VisualId = origin.VisualId,
                    TargetVisualType = target.VisualType,
                    TargetId = target.VisualId,
                    CastAnimationId = skill.CastAnimation,
                    CastEffect = skill.CastEffect,
                    SkillId = skill.SkillVnum
                });
            }

            await castDelayService.WaitAsync(Duration.FromMilliseconds(skill.CastTime * 100));
            return ReferenceEquals(origin.MapInstance, map) && !IsStunned(origin)
                && (target == null ? CanCast(origin) : CanAttack(origin, target) && ReferenceEquals(target.MapInstance, map));
        }

        private void ScheduleCooldownReset(IAliveEntity origin, SkillInfo skill)
        {
            if (origin is not PlayerComponentBundle character) return;

            var readyAt = clock.GetCurrentInstant().Plus(Duration.FromMilliseconds(skill.Cooldown * 100));
            character.PendingResets[skill.CastId] = readyAt;
        }

        // NoAttack is a state-only gate (locked / sleeping / vehicled); the faction
        // rule below is what decides who's on whose side. Characters and NPCs are
        // allies — players can't kill a shop NPC via a crafted UseSkill packet, and
        // guards never friendly-fire a player — so any ally pairing bails first.
        // Monsters are opposing-faction to both and are free to hit either. Area
        // targets go through this too, so PvP only needs deciding here.
        public static bool CanAttack(IAliveEntity origin, IAliveEntity target)
        {
            if (!CanCast(origin) || !target.IsAlive) return false;
            if (target.NoAttack) return false;
            if (origin is PlayerComponentBundle attacker && target is PlayerComponentBundle defender)
            {
                return Arena.AreOpponents(attacker, defender) || AreTalentArenaOpponents(attacker, defender)
                    || AreOpponentsOnPvpMap(attacker, defender);
            }
            if (AreAllies(origin, target)) return false;
            return true;
        }

        // Only the two fighters in the ring hit each other, and only once their round has started.
        public static bool AreTalentArenaOpponents(PlayerComponentBundle attacker, PlayerComponentBundle defender)
        {
            return attacker.InTalentArenaRing && defender.InTalentArenaRing
                && attacker.TalentArenaTeam != defender.TalentArenaTeam
                && attacker.MapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance
                && defender.MapInstance?.MapInstanceId == attacker.MapInstance.MapInstanceId;
        }

        public static bool AreOpponentsOnPvpMap(PlayerComponentBundle attacker, PlayerComponentBundle defender)
        {
            return attacker.CharacterId != defender.CharacterId
                && attacker.MapInstance is { IsPvp: true } mapInstance
                && defender.MapInstance?.MapInstanceId == mapInstance.MapInstanceId
                && !attacker.Group.ContainsKey(new Tuple<VisualType, long>(VisualType.Player, defender.VisualId));
        }

        private static bool CanCast(IAliveEntity origin) => origin.IsAlive && !origin.NoAttack;

        private bool IsStunned(IAliveEntity origin) => buffService.HasEffect(origin, BCardEffect.SpecialAttackNoAttack);

        private static bool AreAllies(IAliveEntity a, IAliveEntity b)
        {
            var aAlly = a.VisualType == VisualType.Player || a.VisualType == VisualType.Npc;
            var bAlly = b.VisualType == VisualType.Player || b.VisualType == VisualType.Npc;
            return aAlly && bAlly;
        }

        private static async Task CancelAsync(IAliveEntity origin, IAliveEntity? target)
        {
            if (origin is PlayerComponentBundle character)
            {
                await character.SendPacketAsync(new CancelPacket
                {
                    Type = CancelPacketType.CancelAutoAttack,
                    TargetId = target?.VisualId ?? 0,
                }).ConfigureAwait(false);
            }
        }
    }
}
