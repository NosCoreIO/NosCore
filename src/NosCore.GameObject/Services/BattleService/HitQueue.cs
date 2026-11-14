//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Core.Concurrency;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.BattleService;

// Hits are applied on the target's map executor, one at a time and in arrival order, so
// they are serialized with everything else that changes HP on that map: the life tick's
// regeneration, potions, rewards. A target that has left the map or is gone by the time
// its turn comes is not hit.
public sealed class HitQueue(
    IDamageCalculator damageCalculator,
    IBattleStatsProvider statsProvider,
    IBuffService buffService,
    IRegenerationService regenerationService,
    IVitalityService vitalityService,
    IOnHitBuffService onHitBuffService,
    ILogger<HitQueue> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IHitQueue, ISingletonService
{
    // For entities that are on no map at all.
    private readonly AsyncLock _offMap = new();

    public async Task<HitOutcome> EnqueueAsync(HitRequest request)
    {
        if (!request.Target.IsAlive)
        {
            Cancel(request);
            return await request.Completion.Task;
        }

        try
        {
            var map = request.Target.MapInstance;
            if (map == null)
            {
                using (await _offMap.AcquireAsync())
                {
                    await TryApplyHit(request);
                }
            }
            else if (MapExecutor.IsInsideAny && !map.Executor.IsCurrent)
            {
                // Work on one map does not wait for another; the target is not where the blow is.
                Cancel(request);
            }
            else
            {
                await map.Executor.RunAsync(() => ApplyOnMapAsync(map, request));
            }
        }
        catch (ObjectDisposedException)
        {
            Cancel(request);
        }

        return await request.Completion.Task;
    }

    private Task ApplyOnMapAsync(MapInstance map, HitRequest request)
    {
        if (!map.EcsWorld.IsAlive(request.Target.Handle))
        {
            Cancel(request);
            return Task.CompletedTask;
        }

        return TryApplyHit(request);
    }

    // Damage received in PvP is reduced by at most 75%.
    private const int MaxPvpDefencePercent = 75;

    public static int PvpDamage(int damage, CombatStats attacker, CombatStats defender) =>
        (int)((long)damage / 2
            * Math.Max(0, 100 + attacker.PvpAttackPercent) / 100
            * (100 - Math.Min(MaxPvpDefencePercent, defender.PvpDefencePercent)) / 100);

    private static void Cancel(HitRequest request) =>
        request.Completion.TrySetResult(new HitOutcome(HitStatus.Cancelled, 0, SuPacketHitMode.SuccessAttack, false));

    private async Task RefreshVitalityAsync(IAliveEntity entity)
    {
        if (entity is PlayerComponentBundle character)
        {
            await vitalityService.RefreshAndNotifyAsync(character);
        }
    }

    // async because the effects a blow carries are awaited below: they have to follow the
    // blow, not race it.
    private async Task TryApplyHit(HitRequest request)
    {
        try
        {
            if (request.Cancellation.IsCancellationRequested)
            {
                request.Completion.TrySetResult(new HitOutcome(HitStatus.Cancelled, 0, SuPacketHitMode.SuccessAttack, false));
                return;
            }

            var target = request.Target;
            if (!target.IsAlive)
            {
                request.Completion.TrySetResult(new HitOutcome(HitStatus.Cancelled, 0, SuPacketHitMode.SuccessAttack, false));
                return;
            }

            var isPvp = request.Origin is PlayerComponentBundle && target is PlayerComponentBundle;
            if (buffService.HasEffect(request.Origin, BCardEffect.NoDefeatAndNoDamageNeverCauseDamage)
                || buffService.HasEffect(target, BCardEffect.NoDefeatAndNoDamageNeverReceiveDamage))
            {
                request.Completion.TrySetResult(new HitOutcome(HitStatus.Missed, 0, SuPacketHitMode.Miss, false));
                return;
            }

            var attackerStats = statsProvider.GetStats(request.Origin);
            var defenderStats = statsProvider.GetStats(target);
            var damage = damageCalculator.Calculate(attackerStats, defenderStats, request.Skill);
            if (isPvp)
            {
                damage = damage with { Damage = PvpDamage(damage.Damage, attackerStats, defenderStats) };
            }

            if (damage.HitMode == SuPacketHitMode.Miss || damage.Damage <= 0)
            {
                request.Completion.TrySetResult(new HitOutcome(HitStatus.Missed, 0, damage.HitMode, false));
                return;
            }

            // Damage proportional to HP rather than to the stats, on top of the blow.
            var newHp = target.Hp - damage.Damage - PercentageHpLoss(target, request.Skill.BCards);
            var overkill = 0;
            var killed = false;
            if (newHp <= 0)
            {
                overkill = -newHp;
                newHp = 0;
                killed = true;
            }
            target.Hp = newHp;
            if (killed)
            {
                // HealthComponent.IsAlive is independent of Hp in the ECS (the generated
                // setters only sync what's assigned), so we flip it explicitly here to keep
                // IAliveEntity.IsAlive honest for subsequent attackers and packet fields.
                FlipIsAlive(target, false);
                await buffService.ClearAsync(target);
            }

            // A push, a pull or a charge, if the skill declares one. Before the hit list
            // below rather than after it: the credit for the blow does not depend on where
            // anybody ended up, but a later reader will expect the position to be settled.
            if (!killed)
            {
                await ApplySpecialActionsAsync(request.Skill.BCards, request.Origin, target)
                    .ConfigureAwait(false);
            }

            // Track contribution per attacker so reward distribution can weight by
            // damage dealt. Overkill damage is clipped so late arrivals don't get
            // credit for more than the target actually had.
            var credited = damage.Damage - overkill;
            target.HitList.AddOrUpdate(request.Origin.Handle, credited, (_, existing) => existing + credited);

            // Players get no standing regen until 4s after being hit. Monster damage doesn't need tracking; they don't
            // regen.
            if (target is PlayerComponentBundle hurtCharacter)
            {
                regenerationService.NotifyDamaged(hurtCharacter);
            }

            if (!killed && request.Skill.Duration > 0 && request.Skill.BCards.Count > 0)
            {
                await buffService
                    .ApplySkillBuffAsync(target, request.Skill.SkillVnum, request.Skill.Duration,
                        request.Skill.BCards, request.Origin)
                    .ConfigureAwait(false);

                await RefreshVitalityAsync(target).ConfigureAwait(false);
                if (!ReferenceEquals(request.Origin, target))
                {
                    await RefreshVitalityAsync(request.Origin).ConfigureAwait(false);
                }
            }

            if (!killed && request.Skill.BCards.Count > 0)
            {
                await buffService.InflictCardsAsync(target, request.Origin, request.Skill.BCards);
            }

            await onHitBuffService.TriggerAsync(request.Origin, target, killed);

            request.Completion.TrySetResult(new HitOutcome(HitStatus.Landed, damage.Damage, damage.HitMode, killed));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, logLanguage[LogLanguageKey.HIT_APPLY_FAILED], request.Target.Handle);
            request.Completion.TrySetException(ex);
        }
    }

    /// <summary>
    /// Type 37 subtype 31: "Decreases the opponent's HP by %s%%." 112 of the 122 declarations on
    /// skills are this subtype, and the case was not read at all.
    /// </summary>
    /// <remarks>
    /// The percentage is taken from maximum HP. The file says only "HP by %s%%"; off current HP
    /// the loss would halve and halve again without ever finishing anything, which is not what a
    /// skill declaring 90% is for.
    ///
    /// Subtype 32 hits the caster instead and no skill declares it, so it is left out.
    ///
    /// The loss can kill: it is part of the same subtraction as the blow, so the ordinary death
    /// path handles it and nothing says the effect must leave its victim standing.
    /// </remarks>
    private static int PercentageHpLoss(IAliveEntity target, IReadOnlyList<BCardDto> bCards)
    {
        var loss = 0;
        for (var i = 0; i < bCards.Count; i++)
        {
            var bCard = bCards[i];
            if (bCard.Effect() != BCardEffect.RecoveryAndDamagePercentDecreaseEnemyHp || bCard.FirstData <= 0)
            {
                continue;
            }

            loss += target.MaxHp * bCard.FirstData / 100;
        }

        return loss;
    }

    private static void FlipIsAlive(IAliveEntity entity, bool alive)
    {
        switch (entity)
        {
            case PlayerComponentBundle p: p.IsAlive = alive; break;
            case MonsterComponentBundle m: m.IsAlive = alive; break;
            case NpcComponentBundle n: n.IsAlive = alive; break;
        }
    }

    /// <summary>
    /// Type 40, "Special Actions". Three of its subtypes are used by the game's skills, and all
    /// three move somebody:
    ///
    ///     11: Push your opponent back %s fields.          49 declarations
    ///     21: Draws enemies to %s fields away from you.    64
    ///     31: Charge at enemies within %s fields.          21
    /// </summary>
    /// <remarks>
    /// SUBTYPE 21 IS A PULL, not a taunt. The value is a distance - 0 to 4 across the file, and
    /// 0 means "up against you" - and the skills carrying it are called Drawing Shot, Rotating
    /// Hammer and Spider King's Draw. A taunt would have nothing to do with a number of fields.
    ///
    /// THE RULE FOR ALL THREE IS THE SAME: walk cell by cell and stop in front of the first
    /// obstacle. Jumping straight to the destination would put somebody inside a wall or past the
    /// edge of the map, and from there they cannot get out - the client and the server would
    /// disagree about where they are.
    ///
    /// Subtypes 41 "Run Away!" and 51 "Hide" are declared by no skill in the file and are not
    /// written here: there is nothing to check them against.
    /// </remarks>
    private async Task ApplySpecialActionsAsync(IReadOnlyList<BCardDto> bCards, IAliveEntity origin,
        IAliveEntity target)
    {
        for (var i = 0; i < bCards.Count; i++)
        {
            var bCard = bCards[i];
            var fields = Math.Max(0, (int)bCard.FirstData);
            switch (bCard.Effect())
            {
                // The target slides backwards along the line joining it to whoever struck.
                case BCardEffect.SpecialActionsPushBack:
                    await SlideAsync(target, origin.MapX, origin.MapY, Math.Max(1, fields),
                        away: true, stopAt: 0).ConfigureAwait(false);
                    break;

                // The target is drawn in until it is `fields` away from the caster.
                case BCardEffect.SpecialActionsDrawEnemies:
                    await SlideAsync(target, origin.MapX, origin.MapY, int.MaxValue,
                        away: false, stopAt: fields).ConfigureAwait(false);
                    break;

                // The caster closes on the target and stops beside it.
                case BCardEffect.SpecialActionsCharge:
                    await SlideAsync(origin, target.MapX, target.MapY, Math.Max(1, fields),
                        away: false, stopAt: 1).ConfigureAwait(false);
                    break;
            }
        }
    }

    /// <summary>
    /// Walks <paramref name="entity" /> one cell at a time along the line to (towardsX, towardsY),
    /// for at most <paramref name="steps" /> cells, stopping before the first wall and never
    /// closing nearer than <paramref name="stopAt" /> cells.
    /// </summary>
    private static async Task SlideAsync(IAliveEntity entity, short towardsX, short towardsY,
        int steps, bool away, int stopAt)
    {
        var map = entity.MapInstance?.Map;
        if (map == null)
        {
            return;
        }

        // The step is the sign of the difference, so it runs diagonally when both coordinates
        // differ - the same way everything else in the game moves.
        var stepX = Math.Sign(entity.MapX - towardsX);
        var stepY = Math.Sign(entity.MapY - towardsY);
        if (!away)
        {
            stepX = -stepX;
            stepY = -stepY;
        }

        if (stepX == 0 && stepY == 0)
        {
            return;
        }

        var (x, y) = ForcedMovement.Destination(entity.MapX, entity.MapY, towardsX, towardsY, steps, stepX, stepY,
            stopAt, map.IsWalkable);

        if (x == entity.MapX && y == entity.MapY)
        {
            return;
        }

        // The client has to be told, or it goes on drawing the entity where it was and the two
        // sides stop agreeing about who is within reach of what.
        await entity.TeleportAsync(x, y);
    }
}
