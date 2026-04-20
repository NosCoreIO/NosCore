//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using Arch.Core;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Infastructure;
using NosCore.Packets.Enumerations;
using Serilog;

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
    ILogger logger) : IHitQueue, ISingletonService
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
            logger.Error(ex, "Hit queue worker for entity {Handle} crashed", target.Handle);
        }
        finally
        {
            _channels.TryRemove(target.Handle, out _);
        }
    }

    private Task TryApplyHit(HitRequest request)
    {
        try
        {
            if (request.Cancellation.IsCancellationRequested)
            {
                request.Completion.TrySetResult(new HitOutcome(HitStatus.Cancelled, 0, SuPacketHitMode.SuccessAttack, false));
                return Task.CompletedTask;
            }

            var target = request.Target;
            if (!target.IsAlive)
            {
                request.Completion.TrySetResult(new HitOutcome(HitStatus.Cancelled, 0, SuPacketHitMode.SuccessAttack, false));
                return Task.CompletedTask;
            }

            var attackerStats = statsProvider.GetStats(request.Origin);
            var defenderStats = statsProvider.GetStats(target);
            var damage = damageCalculator.Calculate(attackerStats, defenderStats, request.Skill);

            if (damage.HitMode == SuPacketHitMode.Miss || damage.Damage <= 0)
            {
                request.Completion.TrySetResult(new HitOutcome(HitStatus.Missed, 0, damage.HitMode, false));
                return Task.CompletedTask;
            }

            var newHp = target.Hp - damage.Damage;
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
            logger.Error(ex, "Failed to apply hit to entity {Handle}", request.Target.Handle);
            request.Completion.TrySetException(ex);
        }
        return Task.CompletedTask;
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
}
