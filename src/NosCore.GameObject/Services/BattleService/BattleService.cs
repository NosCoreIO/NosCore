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
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Services.ArenaService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using NosCore.Core.Observability;

namespace NosCore.GameObject.Services.BattleService
{
    // Orchestrator: validate → resolve skill → resolve targets → enqueue hits → send
    // packets and raise game events. Almost nothing happens here directly; the heavy lifting
    // lives in IDamageCalculator / IHitQueue / IBuffService / IRewardService. This class
    // stays testable because every collaborator is an interface.
    public sealed class BattleService(
        ISkillResolver skillResolver,
        ITargetResolver targetResolver,
        IHitQueue hitQueue,
        IGameEventDispatcher gameEvents,
        ISessionRegistry sessionRegistry,
        IClock clock,
        ICaptureService captureService,
        ICastDelayService castDelayService,
        IBuffService buffService,
        ISkillCastService skillCastService,
        ISkillComboService skillComboService,
        ISummonService summonService,
        IOnyxDragonService onyxDragonService,
        IBattleStatsProvider statsProvider,
        IRandomProvider random,
        ILogger<BattleService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IBattleService, ISingletonService
    {
        public async Task<bool> Hit(IAliveEntity origin, IAliveEntity target, HitArguments arguments)
        {
            if (origin is PlayerComponentBundle or MateComponentBundle && CanCast(origin) && !IsStunned(origin)
                && Resolve(origin, arguments) is { SkillVnum: not 0, TargetType: not SkillReach.TargetedSkill and not SkillReach.GroundSkill } supportSkill)
            {
                return await CastOnSelfOrAllyAsync(origin, target, supportSkill, arguments.SkillId);
            }

            if (!CanAttack(origin, target) || IsStunned(origin))
            {
                await CancelAsync(origin, arguments.SkillId, AttackRefusal(origin, target));
                return false;
            }

            var skill = Resolve(origin, arguments);
            if (skill == null)
            {
                await CancelAsync(origin, arguments.SkillId, CancelPacketType.CancelAutoAttack);
                return false;
            }

            if (origin is PlayerComponentBundle player && skillComboService.Advance(player, skill.SkillVnum) is { } combo)
            {
                skill = skill with { AttackAnimation = combo.Animation, Effect = combo.Effect };
            }

            // A capture deals no damage: CaptureService sends its own animation and outcome, and a
            // failed attempt still costs the cooldown below.
            if (captureService.IsCaptureSkill(skill))
            {
                ScheduleCooldownReset(origin, skill);
                await captureService.TryCaptureAsync(origin, target, skill);
                return true;
            }

            if (!await CastAsync(origin, target, skill))
            {
                await CancelAsync(origin, arguments.SkillId, AttackRefusal(origin, target));
                return false;
            }

            await buffService.RemoveEffectAsync(origin, BCardEffect.SpecialActionsHide);
            skill = await SpendChargeAsync(origin, skill);
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
            for (var i = 0; i < targets.Count; i++)
            {
                await TryProcessHitAsync(origin, targets[i], skill, i == 0, i == 0 ? StrikeOn(origin, skill) : HitPresentation.Direct);
            }

            if (targets.Count == 1 && !skill.HitsSeveral)
            {
                foreach (var bystander in SurroundingTargets(origin, target, skill))
                {
                    await TryProcessHitAsync(origin, bystander, skill, false, HitPresentation.Area);
                }
            }

            await ExplodeAsync(origin, skill);
            await FinishCastAsync(origin, skill);
            return true;
        }

        // Type 29 subtype 2: the attacker blows up, and every enemy within the radius takes the
        // card's damage as it is.
        private async Task ExplodeAsync(IAliveEntity origin, SkillInfo skill)
        {
            var stats = statsProvider.GetStats(origin);
            if (stats.ExplosionRadius <= 0 || stats.ExplosionDamage <= 0 || origin.MapInstance is not { } map)
            {
                return;
            }

            var blast = skill with
            {
                TargetRange = (byte)stats.ExplosionRadius,
                FixedDamage = stats.ExplosionDamage,
                Duration = 0,
                BCards = [],
                ChargeBCards = null,
            };
            var victims = targetResolver.ResolveArea(origin, map, origin.PositionX, origin.PositionY, blast)
                .Where(victim => !(victim.VisualType == origin.VisualType && victim.VisualId == origin.VisualId))
                .Where(victim => !(origin is MonsterComponentBundle && victim is MonsterComponentBundle))
                .ToList();
            foreach (var victim in victims)
            {
                await TryProcessHitAsync(origin, victim, blast, false, HitPresentation.Area);
            }
        }

        // Type 29 subtype 3: a lucky single-target hit also lands on everyone around the target.
        private IReadOnlyList<IAliveEntity> SurroundingTargets(IAliveEntity origin, IAliveEntity target, SkillInfo skill)
        {
            var stats = statsProvider.GetStats(origin);
            if (stats.SurroundingDamageChance <= 0 || stats.SurroundingDamageRadius <= 0 || target.MapInstance is not { } map
                || random.Next(0, 100) >= stats.SurroundingDamageChance)
            {
                return [];
            }

            return targetResolver
                .ResolveArea(origin, map, target.PositionX, target.PositionY, skill with { TargetRange = (byte)stats.SurroundingDamageRadius })
                .Where(e => !(e.VisualType == target.VisualType && e.VisualId == target.VisualId))
                .Where(e => !(e.VisualType == origin.VisualType && e.VisualId == origin.VisualId))
                .ToList();
        }

        public async Task<bool> HitArea(IAliveEntity origin, short mapX, short mapY, HitArguments arguments)
        {
            var mapInstance = origin.MapInstance;
            var skill = CanCast(origin) && !IsStunned(origin) && mapInstance != null ? Resolve(origin, arguments) : null;
            if (mapInstance == null || skill == null || !(skill.HitsSeveral || skill.TargetType == SkillReach.GroundSkill) || captureService.IsCaptureSkill(skill))
            {
                await CancelAsync(origin, arguments.SkillId, CancelPacketType.CancelAutoAttack);
                return false;
            }

            if (!await CastAsync(origin, null, skill))
            {
                await CancelAsync(origin, arguments.SkillId, CancelPacketType.CancelAutoAttack);
                return false;
            }

            await buffService.RemoveEffectAsync(origin, BCardEffect.SpecialActionsHide);
            skill = await SpendChargeAsync(origin, skill);

            if (skill.BCards.Any(b => b.Effect() == BCardEffect.FairyXpIncreaseTeleportToLocation)
                && mapInstance.Map.IsWalkable(mapX, mapY))
            {
                await origin.TeleportAsync(mapX, mapY);
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

            await ExplodeAsync(origin, skill);
            await FinishCastAsync(origin, skill);
            return true;
        }

        // A skill aimed at the caster or a friendly player: a buff for them, a buff for everyone
        // around, or a blow to every enemy around the caster, by its hit type.
        private async Task<bool> CastOnSelfOrAllyAsync(IAliveEntity origin, IAliveEntity target, SkillInfo skill, long castId)
        {
            var mapInstance = origin.MapInstance;
            var recipient = skill.TargetType == SkillReach.AllySkill ? target : origin;
            if (mapInstance == null || !recipient.IsAlive || !ReferenceEquals(recipient.MapInstance, mapInstance)
                || CanAttack(origin, recipient) || !await CastAsync(origin, null, skill))
            {
                await CancelAsync(origin, castId, CancelPacketType.CancelAutoAttack);
                return false;
            }

            var aroundCaster = skill.TargetType == SkillReach.SelfSkill && skill.HitType == TargetHitType.SingleTargetHitCombo;
            await mapInstance.SendPacketAsync(new SuPacket
            {
                VisualType = origin.VisualType,
                VisualId = origin.VisualId,
                TargetVisualType = recipient.VisualType,
                TargetId = recipient.VisualId,
                SkillVnum = skill.SkillVnum,
                SkillCooldown = skill.Cooldown,
                AttackAnimation = skill.AttackAnimation,
                SkillEffect = skill.Effect,
                TargetIsAlive = true,
                HpPercentage = recipient.MaxHp > 0 ? (byte)(recipient.Hp * 100 / recipient.MaxHp) : (byte)0,
                Damage = 0,
                HitMode = aroundCaster ? SuPacketHitMode.NoDamageFail : SuPacketHitMode.NoDamageSuccess,
                TargetCurrentHp = recipient.Hp,
                TargetMaxHp = recipient.MaxHp,
            });

            if (aroundCaster)
            {
                skill = await SpendChargeAsync(origin, skill);
                var targets = targetResolver.ResolveArea(origin, mapInstance, origin.PositionX, origin.PositionY, skill);
                for (var i = 0; i < targets.Count; i++)
                {
                    await TryProcessHitAsync(origin, targets[i], skill, i == 0, HitPresentation.Area);
                }
            }
            else if (skill.IsCharge && origin is PlayerComponentBundle charger)
            {
                charger.ChargeBCards = skill.BCards;
                await SendChargeAsync(charger, ChargeValue(skill.BCards, charger.Level));
            }
            else
            {
                var recipients = skill.TargetType == SkillReach.SelfSkill && skill.HitType == TargetHitType.SingleAoeTargetHit
                    ? FriendliesAround(origin, mapInstance, skill.TargetRange)
                    : new[] { recipient };
                foreach (var friendly in recipients)
                {
                    if (!ReferenceEquals(friendly, recipient))
                    {
                        await mapInstance.SendPacketAsync(new SuPacket
                        {
                            VisualType = origin.VisualType,
                            VisualId = origin.VisualId,
                            TargetVisualType = friendly.VisualType,
                            TargetId = friendly.VisualId,
                            SkillVnum = skill.SkillVnum,
                            SkillCooldown = skill.Cooldown,
                            AttackAnimation = skill.AttackAnimation,
                            SkillEffect = skill.Effect,
                            TargetIsAlive = true,
                            HpPercentage = friendly.MaxHp > 0 ? (byte)(friendly.Hp * 100 / friendly.MaxHp) : (byte)0,
                            HitMode = SuPacketHitMode.AttackedInAoe,
                            TargetCurrentHp = friendly.Hp,
                            TargetMaxHp = friendly.MaxHp,
                        });
                    }

                    await buffService.InflictCardsAsync(friendly, origin, skill.BCards);
                    if (skill.Duration > 0 && skill.BCards.Count > 0)
                    {
                        await buffService.ApplySkillBuffAsync(friendly, skill.SkillVnum, skill.Duration, skill.BCards, origin);
                    }

                    if (friendly is MateComponentBundle)
                    {
                        await SendTargetStatAsync(friendly);
                    }
                }
            }

            await FinishCastAsync(origin, skill);
            return true;
        }

        // Every blow of the next attack carries the charge, whether it lands or not.
        private static async Task<SkillInfo> SpendChargeAsync(IAliveEntity origin, SkillInfo skill)
        {
            if (skill.Kind == SkillKind.Debuff || origin is not PlayerComponentBundle { ChargeBCards: { } charge } player)
            {
                return skill;
            }

            player.ChargeBCards = null;
            await SendChargeAsync(player, 0);
            return skill with { ChargeBCards = charge };
        }

        // A trace shows a level 92 character holding a charge of 2724, far above any charge skill's
        // own bonus, so the live value may be worked out some other way.
        private static int ChargeValue(IReadOnlyList<BCardDto> cards, int level) => cards
            .Where(card => card.Type == BCardType.CardType.AttackPower)
            .Sum(card => BattleStatsProvider.ScaleByLevel(card, level));

        // A charge is drawn as card 0, its value given both as the charge and as the duration.
        private static Task SendChargeAsync(PlayerComponentBundle player, int value) =>
            player.MapInstance?.SendPacketAsync(new BfPacket
            {
                VisualType = player.VisualType,
                VisualId = player.VisualId,
                Buff = new BfPacket.BuffElementSubPacket { ChargeValue = value, BuffId = 0, Duration = value },
                BuffLevel = player.Level,
            }) ?? Task.CompletedTask;

        // The caster, the friendly players around it and the pets and partners those players have out.
        private IReadOnlyList<IAliveEntity> FriendliesAround(IAliveEntity origin, MapInstance mapInstance, byte range)
        {
            bool InRange(IAliveEntity entity) =>
                Math.Abs(entity.PositionX - origin.PositionX) <= range && Math.Abs(entity.PositionY - origin.PositionY) <= range;

            var friendlies = new List<IAliveEntity> { origin };
            foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(mapInstance.MapInstanceId))
            {
                if (!session.HasPlayerEntity) continue;
                var player = session.Character;
                var isCaster = player.VisualId == origin.VisualId && origin is PlayerComponentBundle;
                if (!isCaster && (!player.IsAlive || CanAttack(origin, player))) continue;
                if (!isCaster && InRange(player))
                {
                    friendlies.Add(player);
                }

                foreach (var mate in player.Mates.Values)
                {
                    if (mate.Entity is { IsAlive: true } entity && ReferenceEquals(entity.MapInstance, mapInstance) && InRange(entity))
                    {
                        friendlies.Add(entity);
                    }
                }
            }

            return friendlies;
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
                NosCoreMetrics.Failures.Add(1, new KeyValuePair<string, object?>("boundary", "hit"));
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

            var outcome = await hitQueue.EnqueueAsync(request);
            if (outcome.Status == HitStatus.Cancelled)
            {
                return;
            }

            // stat goes to the target before su: the client paints the damage number first and
            // re-reads its cached HP for the bar, so a stat arriving later leaves the bar frozen.
            await SendTargetStatAsync(target);
            await BroadcastHitAsync(origin, target, skill, outcome, presentation);
            if (outcome.OnyxDamage > 0)
            {
                await onyxDragonService.StrikeAsync(origin, target, skill, outcome.OnyxDamage);
            }

            await BroadcastTargetInfoAsync(target);
            await gameEvents.RaiseAsync(new EntityDamagedEvent(origin, target, outcome.Damage, outcome.Killed, outcome.HpLeft));

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
                        await spectator.SendPacketAsync(diePacket);
                    }
                }
                await gameEvents.RaiseAsync(new EntityDiedEvent(target, origin));
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

        // A dash names the cell its caster lands on. The caster is not moved, so that is where it stands.
        private static HitPresentation StrikeOn(IAliveEntity origin, SkillInfo skill) =>
            skill.IsDash ? new HitPresentation(origin.PositionX, origin.PositionY, false) : HitPresentation.Direct;

        // The cell su names, if any, and whether the blow is drawn as caught in an area.
        private readonly record struct HitPresentation(short X, short Y, bool InArea)
        {
            public static HitPresentation Direct => new(0, 0, false);

            public static HitPresentation Area => new(0, 0, true);
        }

        // The client sends the skill as soon as it is used and leaves the wind-up to the server:
        // ct starts the cast animation, and the hit resolves once the cast time has passed and
        // both sides can still fight.
        private SkillInfo? Resolve(IAliveEntity origin, HitArguments arguments) =>
            arguments.Skill is { } skill ? skillResolver.Describe(skill)
            : arguments.BasicAttack ? skillResolver.ResolveBasic(origin)
            : skillResolver.Resolve(origin, arguments.SkillId);

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

        // After the hits, so a cast never lands on the adds it calls.
        private async Task FinishCastAsync(IAliveEntity origin, SkillInfo skill)
        {
            await buffService.InflictCasterCardsAsync(origin, skill.BCards);
            await summonService.SummonAsync(origin, skill.BCards);
            ScheduleCooldownReset(origin, skill);
            await DisappearAfterHittingAsync(origin);
        }

        // A monster marked to disappear after hitting is gone once its attack is over, shown dying
        // by its own hand. Nobody killed it, so nobody is rewarded for it.
        private async Task DisappearAfterHittingAsync(IAliveEntity origin)
        {
            if (origin is not MonsterComponentBundle { IsAlive: true, NpcMonster.DisappearAfterHitting: true } monster
                || monster.MapInstance is not { } map)
            {
                return;
            }

            await monster.OnMapAsync(async () =>
            {
                monster.Hp = 0;
                monster.IsAlive = false;
                await buffService.ClearAsync(monster);
                await map.SendPacketAsync(new DiePacket
                {
                    VisualType = monster.VisualType,
                    VisualId = monster.VisualId,
                    TargetVisualType = monster.VisualType,
                    TargetId = monster.VisualId,
                });
            });
            await gameEvents.RaiseAsync(new EntityDiedEvent(monster, monster, new Dictionary<Arch.Core.Entity, int>()));
        }

        private void ScheduleCooldownReset(IAliveEntity origin, SkillInfo skill)
        {
            if (origin is not PlayerComponentBundle character) return;

            var readyAt = clock.GetCurrentInstant().Plus(skillCastService.Cooldown(character, skill.Cooldown));
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
                    || AreRainbowBattleOpponents(attacker, defender) || AreOpponentsOnPvpMap(attacker, defender) || AreAct4Opponents(attacker, defender);
            }
            if (AreAllies(origin, target)) return false;
            return !IsOwnSidesBuilding(origin, target);
        }

        // Only the two fighters in the ring hit each other, and only once their round has started.
        public static bool AreTalentArenaOpponents(PlayerComponentBundle attacker, PlayerComponentBundle defender)
        {
            return attacker.InTalentArenaRing && defender.InTalentArenaRing
                && attacker.TalentArenaTeam != defender.TalentArenaTeam
                && attacker.MapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance
                && defender.MapInstance?.MapInstanceId == attacker.MapInstance.MapInstanceId;
        }

        public static bool AreRainbowBattleOpponents(PlayerComponentBundle attacker, PlayerComponentBundle defender)
        {
            return attacker.RainbowBattleTeam is { } team && defender.RainbowBattleTeam is { } other && team != other
                && attacker.RainbowBattleThawsAt == null && defender.RainbowBattleThawsAt == null
                && attacker.MapInstance?.MapInstanceType == MapInstanceType.RainbowBattleInstance
                && defender.MapInstance?.MapInstanceId == attacker.MapInstance.MapInstanceId;
        }

        public static bool AreOpponentsOnPvpMap(PlayerComponentBundle attacker, PlayerComponentBundle defender)
        {
            return attacker.CharacterId != defender.CharacterId
                && attacker.MapInstance is { IsPvp: true } mapInstance
                && defender.MapInstance?.MapInstanceId == mapInstance.MapInstanceId
                && !attacker.Group.ContainsKey(new Tuple<VisualType, long>(VisualType.Player, defender.VisualId));
        }

        public static bool AreAct4Opponents(PlayerComponentBundle attacker, PlayerComponentBundle defender)
        {
            return attacker.MapInstance is { IsAct4: true } mapInstance
                && defender.MapInstance?.MapInstanceId == mapInstance.MapInstanceId
                && attacker.Faction != FactionType.Neutral && defender.Faction != FactionType.Neutral
                && attacker.Faction != defender.Faction;
        }

        // A mate that has lost all loyalty no longer fights for its owner.
        private static bool CanCast(IAliveEntity origin) =>
            origin.IsAlive && !origin.NoAttack && origin is not MateComponentBundle { Mate.Loyalty: <= 0 };

        private bool IsStunned(IAliveEntity origin) => buffService.HasEffect(origin, BCardEffect.SpecialAttackNoAttack);

        // A side's base camp stands in Glacernon for its enemies to tear down, not for its own players.
        private static bool IsOwnSidesBuilding(IAliveEntity origin, IAliveEntity target) =>
            target is MonsterComponentBundle { Faction: not FactionType.Neutral } building
            && origin switch
            {
                PlayerComponentBundle player => player.Faction == building.Faction,
                MateComponentBundle mate => mate.Faction == building.Faction,
                _ => false,
            };

        private static bool AreAllies(IAliveEntity a, IAliveEntity b)
        {
            var aAlly = a.VisualType == VisualType.Player || a.VisualType == VisualType.Npc;
            var bAlly = b.VisualType == VisualType.Player || b.VisualType == VisualType.Npc;
            return aAlly && bAlly;
        }

        // A dead target, a stunned caster or an attack aimed at the caster itself only ends the cast;
        // any other refusal also stops the auto attack.
        private CancelPacketType AttackRefusal(IAliveEntity origin, IAliveEntity target) =>
            IsStunned(origin) || !target.IsAlive || (target.VisualType == origin.VisualType && target.VisualId == origin.VisualId)
                ? CancelPacketType.CancelCast
                : CancelPacketType.CancelAutoAttack;

        private static async Task CancelAsync(IAliveEntity origin, long castId, CancelPacketType type)
        {
            if (origin is PlayerComponentBundle character)
            {
                await character.SendPacketAsync(new CancelPacket { Type = type, TargetId = castId, Unknow = -1 });
            }
        }
    }
}
