//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Buffs live on a per-entity ConcurrentDictionary keyed by CardId. Re-applying refreshes
// ExpiresAt rather than stacking — matches the one-icon-per-card client convention.
// Expiration is pull-based: TickAsync is called from the world's life loop, which keeps
// this service dependency-free (no timers, no background tasks) and trivial to test.
public sealed class BuffService(IClock clock) : IBuffService
{
    private static readonly IReadOnlyCollection<BuffInstance> EmptyBuffs = Array.Empty<BuffInstance>();
    private static readonly IReadOnlyList<BuffInstance> EmptyExpired = Array.Empty<BuffInstance>();

    public Task ApplyAsync(IAliveEntity target, CardDto card, IReadOnlyList<BCardDto> bCards, IAliveEntity? caster, int overrideDuration = -1)
    {
        var buffs = ResolveState(target);
        if (buffs == null)
        {
            return Task.CompletedTask;
        }

        var now = clock.GetCurrentInstant();
        var durationMs = overrideDuration >= 0 ? overrideDuration : card.Duration * 100;
        var buff = new BuffInstance(
            CardId: card.CardId,
            BuffType: ClassifyBuffType(card),
            Caster: caster,
            StartedAt: now,
            ExpiresAt: now.Plus(Duration.FromMilliseconds(durationMs)),
            BCards: bCards);

        buffs.AddOrUpdate(card.CardId, buff, (_, _) => buff);
        return Task.CompletedTask;
    }

    public Task ApplySkillBuffAsync(IAliveEntity target, short skillVnum, short skillDuration, IReadOnlyList<BCardDto> bCards, IAliveEntity? caster)
    {
        if (skillDuration <= 0 || bCards.Count == 0)
        {
            return Task.CompletedTask;
        }

        var lasting = bCards
            .Where(b => (BCardType.CardType)b.Type != BCardType.CardType.Damage)
            .ToArray();
        if (lasting.Length == 0)
        {
            return Task.CompletedTask;
        }

        var buffs = ResolveState(target);
        if (buffs == null)
        {
            return Task.CompletedTask;
        }

        var now = clock.GetCurrentInstant();
        var buff = new BuffInstance(
            CardId: skillVnum,
            BuffType: BuffType.Neutral,
            Caster: caster,
            StartedAt: now,
            ExpiresAt: now.Plus(Duration.FromMilliseconds(skillDuration * 100)),
            BCards: lasting);

        buffs.AddOrUpdate(skillVnum, buff, (_, _) => buff);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(IAliveEntity target, short cardId)
    {
        var buffs = ResolveState(target);
        buffs?.TryRemove(cardId, out _);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<BuffInstance> GetActiveBuffs(IAliveEntity target)
    {
        var buffs = ResolveState(target);
        return buffs == null ? EmptyBuffs : buffs.Values.ToArray();
    }

    public bool HasBuff(IAliveEntity target, short cardId)
    {
        var buffs = ResolveState(target);
        return buffs != null && buffs.ContainsKey(cardId);
    }

    public Task<IReadOnlyList<BuffInstance>> TickAsync(IAliveEntity target)
    {
        var buffs = ResolveState(target);
        if (buffs == null || buffs.IsEmpty)
        {
            return Task.FromResult(EmptyExpired);
        }

        var now = clock.GetCurrentInstant();
        var expired = new List<BuffInstance>();
        foreach (var kvp in buffs)
        {
            if (kvp.Value.ExpiresAt <= now && buffs.TryRemove(kvp.Key, out var removed))
            {
                expired.Add(removed);
            }
        }

        return Task.FromResult<IReadOnlyList<BuffInstance>>(expired);
    }

    // BuffStateComponent lives on the target's ECS world; walking through the bundle
    // gets us its dictionary handle without copying. Returning null means the entity
    // has no buff state (e.g. map items) — caller treats as "no buffs".
    private static ConcurrentDictionary<short, BuffInstance>? ResolveState(IAliveEntity target)
    {
        return target switch
        {
            PlayerComponentBundle p => p.World.TryGetComponent<BuffStateComponent>(p.Entity)?.ActiveBuffs,
            MonsterComponentBundle m => m.World.TryGetComponent<BuffStateComponent>(m.Entity)?.ActiveBuffs,
            NpcComponentBundle n => n.World.TryGetComponent<BuffStateComponent>(n.Entity)?.ActiveBuffs,
            _ => null,
        };
    }

    // Cards are classified by their Propability field in the original content files:
    // 0 means good, 1 neutral, 2 bad. The CardDto exposes it as Propability (sic).
    private static BuffType ClassifyBuffType(CardDto card) => card.Propability switch
    {
        0 => BuffType.Good,
        2 => BuffType.Bad,
        _ => BuffType.Neutral,
    };
}
