//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using Arch.Core;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Infastructure;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Services.BattleService;

// Per-target FIFO queue. When the first attacker hits a target we lazily spin up a
// Channel + background worker task keyed by Entity handle. Subsequent attackers enqueue
// into the same channel; the worker drains hits sequentially so HP arithmetic is
// race-free without needing SemaphoreSlim.WaitAsync at the callsite.
//
// The worker exits when its channel stays idle long enough (checked on each drain
// iteration) which keeps us from leaking a task per corpse.
public sealed class HitQueue(
    IDamageCalculator damageCalculator,
    IBattleStatsProvider statsProvider,
    IBuffService buffService,
    IRegenerationService regenerationService,
    ILogger<HitQueue> logger) : IHitQueue, ISingletonService
{
    private readonly ConcurrentDictionary<Entity, Channel<HitRequest>> _channels = new();

    public Task<HitOutcome> EnqueueAsync(HitRequest request)
    {
        // Dead targets bypass the queue entirely — no point spinning up a worker for
        // someone we're not going to touch. The orchestrator also checks but racing
        // callers might slip past that check before the worker processes; this is a
        // second line of defense.
        if (!request.Target.IsAlive)
        {
            request.Completion.TrySetResult(new HitOutcome(HitStatus.Cancelled, 0, SuPacketHitMode.SuccessAttack, false));
            return request.Completion.Task;
        }

        var channel = _channels.GetOrAdd(request.Target.Handle, _ => CreateChannel(request.Target));
        if (!channel.Writer.TryWrite(request))
        {
            request.Completion.TrySetResult(new HitOutcome(HitStatus.Cancelled, 0, SuPacketHitMode.SuccessAttack, false));
        }
        return request.Completion.Task;
    }

    private Channel<HitRequest> CreateChannel(IAliveEntity target)
    {
        var channel = Channel.CreateUnbounded<HitRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            // Writers are the ECS/packet handlers; multiple attackers write in parallel.
            SingleWriter = false,
        });
        _ = Task.Run(() => ProcessAsync(target, channel));
        return channel;
    }

    private async Task ProcessAsync(IAliveEntity target, Channel<HitRequest> channel)
    {
        try
        {
            while (await channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (channel.Reader.TryRead(out var request))
                {
                    await TryApplyHit(request).ConfigureAwait(false);
                }

                // If the target died during this batch, drain the rest as cancelled so
                // queued attackers see a consistent "target was dead when you swung"
                // outcome. We also close the channel so the worker exits.
                if (!target.IsAlive)
                {
                    channel.Writer.TryComplete();
                    DrainAsCancelled(channel);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hit queue worker for entity {Handle} crashed", target.Handle);
        }
        finally
        {
            _channels.TryRemove(target.Handle, out _);
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

            var attackerStats = statsProvider.GetStats(request.Origin);
            var defenderStats = statsProvider.GetStats(target);
            var damage = damageCalculator.Calculate(attackerStats, defenderStats, request.Skill);

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

            // Players get a 4s "no standing regen" grace after being hit — matches
            // OpenNos HealthHPLoad which zeros the standing rate until LastDefence
            // is 4s in the past. Monster damage doesn't need tracking; they don't
            // regen.
            if (target is ICharacterEntity hurtCharacter)
            {
                regenerationService.NotifyDamaged(hurtCharacter.CharacterId);
            }

            // Skill BCards that don't describe damage (i.e. stat modifiers) become a
            // buff on the target lasting the skill's Duration. Fire-and-forget is fine:
            // the worker is already serialising per-target, so ordering is preserved.
            if (!killed && request.Skill.Duration > 0 && request.Skill.BCards.Count > 0)
            {
                _ = buffService.ApplySkillBuffAsync(target, request.Skill.SkillVnum, request.Skill.Duration, request.Skill.BCards, request.Origin);
            }

            request.Completion.TrySetResult(new HitOutcome(HitStatus.Landed, damage.Damage, damage.HitMode, killed));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply hit to entity {Handle}", request.Target.Handle);
            request.Completion.TrySetException(ex);
        }
    }

    /// <summary>
    /// Type 37 subtype 31: "Decreases the opponent's HP by %s%%." 112 of the 122 declarations on
    /// skills are this subtype, and the case was not read at all.
    /// </summary>
    /// <remarks>
    /// ONE ASSUMPTION IS OURS, and it is the same one the sibling codebase carries so the two do
    /// not drift: the percentage is taken from MAXIMUM HP. The file says only "HP by %s%%" and
    /// does not distinguish, and on current HP the loss would halve and halve again without ever
    /// finishing anything - which is not what a skill declaring 90% is for.
    ///
    /// Subtype 32 hits the caster instead, and no skill in the file declares it; it is left out
    /// rather than written blind against no data.
    ///
    /// It can kill, and that is a deliberate difference from the sibling codebase, which floors
    /// the victim at 1 HP. That floor is a workaround for where the effect runs there - outside
    /// the blow, so a kill would bypass the death sequence. Here the loss is part of the same
    /// subtraction as the blow itself, so the ordinary path handles the death, and nothing in the
    /// file says the effect must leave its victim standing.
    /// </remarks>
    private static int PercentageHpLoss(IAliveEntity target, IReadOnlyList<BCardDto> bCards)
    {
        var loss = 0;
        for (var i = 0; i < bCards.Count; i++)
        {
            var bCard = bCards[i];
            if ((BCardType.CardType)bCard.Type != BCardType.CardType.RecoveryAndDamagePercent
                || bCard.SubType != (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseEnemyHp
                || bCard.FirstData <= 0)
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

    private static void DrainAsCancelled(Channel<HitRequest> channel)
    {
        while (channel.Reader.TryRead(out var pending))
        {
            pending.Completion.TrySetResult(new HitOutcome(HitStatus.Cancelled, 0, SuPacketHitMode.SuccessAttack, false));
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
            if ((BCardType.CardType)bCard.Type != BCardType.CardType.SpecialActions)
            {
                continue;
            }

            var fields = Math.Max(0, (int)bCard.FirstData);
            switch ((AdditionalTypes.SpecialActions)bCard.SubType)
            {
                // The target slides backwards along the line joining it to whoever struck.
                case AdditionalTypes.SpecialActions.PushBack:
                    await SlideAsync(target, origin.MapX, origin.MapY, Math.Max(1, fields),
                        away: true, stopAt: 0).ConfigureAwait(false);
                    break;

                // The target is drawn in until it is `fields` away from the caster.
                case AdditionalTypes.SpecialActions.DrawEnemies:
                    await SlideAsync(target, origin.MapX, origin.MapY, int.MaxValue,
                        away: false, stopAt: fields).ConfigureAwait(false);
                    break;

                // The caster closes on the target and stops beside it.
                case AdditionalTypes.SpecialActions.Charge:
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

        if ((x == entity.MapX && y == entity.MapY) || !TryPlace(entity, x, y))
        {
            return;
        }

        // The client has to be told, or it goes on drawing the entity where it was and the two
        // sides stop agreeing about who is within reach of what.
        if (entity.MapInstance != null)
        {
            await entity.MapInstance.SendPacketAsync(new TpPacket
            {
                VisualType = entity.VisualType,
                VisualId = entity.VisualId,
                X = x,
                Y = y,
            }).ConfigureAwait(false);
        }
    }

    // The position lives on a component, and only the concrete bundles can write it - the
    // interface exposes it read-only. Same shape as FlipIsAlive below.
    private static bool TryPlace(IAliveEntity entity, short x, short y)
    {
        switch (entity)
        {
            case PlayerComponentBundle p: p.PositionX = x; p.PositionY = y; return true;
            case MonsterComponentBundle m: m.PositionX = x; m.PositionY = y; return true;
            case NpcComponentBundle n: n.PositionX = x; n.PositionY = y; return true;
            default: return false;
        }
    }
}
