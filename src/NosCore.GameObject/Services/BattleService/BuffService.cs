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
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Battle;

namespace NosCore.GameObject.Services.BattleService;

// Buffs live on a per-entity ConcurrentDictionary keyed by CardId. Re-applying refreshes
// ExpiresAt rather than stacking — matches the one-icon-per-card client convention.
// Expiration is pull-based: TickAsync is called from the world's life loop, which keeps
// this service dependency-free (no timers, no background tasks) and trivial to test.
public sealed class BuffService(IClock clock, ICardCatalog cardCatalog,
    IRandomProvider randomProvider) : IBuffService
{
    private static readonly IReadOnlyCollection<BuffInstance> EmptyBuffs = Array.Empty<BuffInstance>();
    private static readonly IReadOnlyList<BuffInstance> EmptyExpired = Array.Empty<BuffInstance>();

    public async Task ApplyAsync(IAliveEntity target, CardDto card, IReadOnlyList<BCardDto> bCards, IAliveEntity? caster, int overrideDuration = -1)
    {
        var buffs = ResolveState(target);
        if (buffs == null)
        {
            return;
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

        // Notify the client so the buff icon appears + the visual effect plays. The
        // client expects duration in deciseconds (same unit as the `bf` packet docs);
        // BuffLevel is the caster's level so the icon shows the effect tier.
        if (target.MapInstance != null)
        {
            var level = caster?.Level ?? 0;
            await target.MapInstance.SendPacketAsync(new BfPacket
            {
                VisualType = target.VisualType,
                VisualId = target.VisualId,
                Buff = new BfPacket.BuffElementSubPacket
                {
                    ChargeValue = 0,
                    BuffId = card.CardId,
                    Duration = durationMs / 100,
                },
                BuffLevel = level,
            }).ConfigureAwait(false);
        }
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
            BCards: lasting,
            FromSkill: true);

        buffs.AddOrUpdate(skillVnum, buff, (_, _) => buff);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(IAliveEntity target, short cardId)
    {
        var buffs = ResolveState(target);
        buffs?.TryRemove(cardId, out _);
        return Task.CompletedTask;
    }

    public async Task ClearAsync(IAliveEntity target)
    {
        var buffs = ResolveState(target);
        if (buffs == null || buffs.IsEmpty)
        {
            return;
        }

        foreach (var cardId in buffs.Keys)
        {
            if (!buffs.TryRemove(cardId, out var removed) || target.MapInstance == null)
            {
                continue;
            }

            // The client keeps an icon on bfe; a bf with no duration left is what takes it down.
            await target.MapInstance.SendPacketAsync(new BfPacket
            {
                VisualType = target.VisualType,
                VisualId = target.VisualId,
                Buff = new BfPacket.BuffElementSubPacket
                {
                    ChargeValue = 0,
                    BuffId = cardId,
                    Duration = 0,
                },
                BuffLevel = removed.Caster?.Level ?? 0,
            });
        }
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

    public bool HasEffect(IAliveEntity target, BCardEffect effect)
    {
        var buffs = ResolveState(target);
        if (buffs == null)
        {
            return false;
        }

        var now = clock.GetCurrentInstant();
        foreach (var buff in buffs.Values)
        {
            if (buff.ExpiresAt <= now)
            {
                continue;
            }

            foreach (var bCard in buff.BCards)
            {
                if (bCard.Effect() == effect)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<IReadOnlyList<BuffInstance>> TickAsync(IAliveEntity target)
    {
        var buffs = ResolveState(target);
        if (buffs == null || buffs.IsEmpty)
        {
            return EmptyExpired;
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

        // Notify the client for each expired card so icons disappear from the HUD.
        // BfePacket is unicast to the target's map since the effect was map-visible
        // (BfPacket is broadcast the same way on apply).
        if (expired.Count > 0 && target.MapInstance != null)
        {
            foreach (var buff in expired)
            {
                await target.MapInstance.SendPacketAsync(new BfePacket
                {
                    VisualType = target.VisualType,
                    VisualId = target.VisualId,
                    CardId = buff.CardId,
                }).ConfigureAwait(false);
            }
        }

        foreach (var buff in expired)
        {
            await ApplyTimeoutBuffAsync(target, buff);
        }

        return expired;
    }

    // BuffStateComponent lives on the target's ECS world; walking through the bundle
    // gets us its dictionary handle without copying. Returning null means the entity
    // has no buff state (e.g. map items) — caller treats as "no buffs".
    public async Task InflictCardsAsync(IAliveEntity target, IAliveEntity? caster,
        IReadOnlyList<BCardDto> skillBCards)
    {
        for (var i = 0; i < skillBCards.Count; i++)
        {
            var bCard = skillBCards[i];
            var effect = bCard.Effect();
            if (effect is not (BCardEffect.BuffChanceCausing or BCardEffect.BuffChanceRemoving))
            {
                continue;
            }

            if (!Rolls(bCard.FirstData))
            {
                continue;
            }

            var cardId = (short)bCard.SecondData;
            if (effect == BCardEffect.BuffChanceRemoving)
            {
                await RemoveAsync(target, cardId);
                continue;
            }

            var card = cardCatalog.GetCard(cardId);
            if (card == null)
            {
                continue;
            }

            await ApplyAsync(target, card, cardCatalog.GetCardBCards(cardId), caster);
        }
    }

    private async Task ApplyTimeoutBuffAsync(IAliveEntity target, BuffInstance expired)
    {
        if (expired.FromSkill)
        {
            return;
        }

        var card = cardCatalog.GetCard(expired.CardId);
        if (card == null || card.TimeoutBuff == 0 || !Rolls(card.TimeoutBuffChance))
        {
            return;
        }

        var followUp = cardCatalog.GetCard(card.TimeoutBuff);
        if (followUp == null)
        {
            return;
        }

        await ApplyAsync(target, followUp, cardCatalog.GetCardBCards(followUp.CardId), expired.Caster);
    }

    private bool Rolls(int percent) => percent > 0 && randomProvider.Next(0, 100) < percent;

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
