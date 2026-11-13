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
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;

namespace NosCore.GameObject.Services.BattleService;

// Buffs live on a per-entity ConcurrentDictionary keyed by CardId. Re-applying refreshes
// ExpiresAt rather than stacking — matches the one-icon-per-card client convention.
// Expiration is pull-based: the map's life loop asks BuffSystem which entities are due and
// calls TickAsync on those, so the service needs no timers or background tasks.
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
            BCards: bCards)
        {
            CasterLevel = caster?.Level,
        };

        await ReplaceSameGroupAsync(target, buffs, card);
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
            });
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

    public async Task ApplyPermanentAsync(PlayerComponentBundle target, CardDto card, IReadOnlyList<BCardDto> bCards)
    {
        var buffs = ResolveState(target);
        if (buffs == null)
        {
            return;
        }

        var buff = new BuffInstance(
            CardId: card.CardId,
            BuffType: ClassifyBuffType(card),
            Caster: null,
            StartedAt: clock.GetCurrentInstant(),
            ExpiresAt: Instant.MaxValue,
            BCards: bCards,
            Static: true);

        await ReplaceSameGroupAsync(target, buffs, card);
        buffs.AddOrUpdate(card.CardId, buff, (_, _) => buff);
        await target.SendPacketAsync(new VbPacket { CardId = card.CardId, IsActivated = true, RemainingTime = -1 });
    }

    public async Task ApplyStaticAsync(PlayerComponentBundle target, CardDto card, IReadOnlyList<BCardDto> bCards, Duration duration)
    {
        var buffs = ResolveState(target);
        if (buffs == null)
        {
            return;
        }

        await ReplaceSameGroupAsync(target, buffs, card);
        RestoreStatic(target, card, bCards, duration);
        await target.SendPacketAsync(new VbPacket
        {
            CardId = card.CardId,
            IsActivated = true,
            RemainingTime = (int)(duration.TotalMilliseconds / 100)
        });
    }

    public void RestoreStatic(PlayerComponentBundle target, CardDto card, IReadOnlyList<BCardDto> bCards, Duration duration)
    {
        var buffs = ResolveState(target);
        if (buffs == null)
        {
            return;
        }

        var now = clock.GetCurrentInstant();
        var buff = new BuffInstance(
            CardId: card.CardId,
            BuffType: ClassifyBuffType(card),
            Caster: null,
            StartedAt: now,
            ExpiresAt: now.Plus(duration),
            BCards: bCards,
            Static: true);

        buffs.AddOrUpdate(card.CardId, buff, (_, _) => buff);
    }

    public async Task ShowStaticAsync(PlayerComponentBundle target)
    {
        var now = clock.GetCurrentInstant();
        foreach (var buff in GetActiveBuffs(target).Where(s => s.Static && s.ExpiresAt != Instant.MaxValue))
        {
            await target.SendPacketAsync(new VbPacket
            {
                CardId = buff.CardId,
                IsActivated = true,
                RemainingTime = (int)((buff.ExpiresAt - now).TotalMilliseconds / 100)
            });
        }
    }

    public async Task RemoveAsync(IAliveEntity target, short cardId)
    {
        var buffs = ResolveState(target);
        if (buffs != null && buffs.TryRemove(cardId, out var removed))
        {
            await SendIconRemovalAsync(target, removed);
        }
    }

    public async Task RemoveBadAsync(IAliveEntity target, byte maxLevel)
    {
        var buffs = ResolveState(target);
        if (buffs == null)
        {
            return;
        }

        foreach (var (cardId, buff) in buffs)
        {
            if (buff.BuffType == BuffType.Bad && cardCatalog.GetCard(cardId)?.Level <= maxLevel
                && buffs.TryRemove(cardId, out var removed))
            {
                await SendIconRemovalAsync(target, removed);
            }
        }
    }

    public async Task ClearAsync(IAliveEntity target)
    {
        var buffs = ResolveState(target);
        if (buffs == null || buffs.IsEmpty)
        {
            return;
        }

        foreach (var (cardId, buff) in buffs)
        {
            if (!buff.Static && buffs.TryRemove(cardId, out var removed))
            {
                await SendIconRemovalAsync(target, removed);
            }
        }
    }

    // The client keeps a greyed icon on bf_e; a bf with no duration left is what takes it down.
    private static Task SendIconRemovalAsync(IAliveEntity target, BuffInstance buff)
    {
        if (buff.Static)
        {
            return target is PlayerComponentBundle character
                ? character.SendPacketAsync(new VbPacket { CardId = buff.CardId, IsActivated = false, RemainingTime = -1 })
                : Task.CompletedTask;
        }

        if (target.MapInstance == null || buff.FromSkill)
        {
            return Task.CompletedTask;
        }

        return target.MapInstance.SendPacketAsync(new BfPacket
        {
            VisualType = target.VisualType,
            VisualId = target.VisualId,
            Buff = new BfPacket.BuffElementSubPacket
            {
                ChargeValue = 0,
                BuffId = buff.CardId,
                Duration = 0,
            },
            BuffLevel = buff.CasterLevel ?? 0,
        });
    }

    public IReadOnlyCollection<BuffInstance> GetActiveBuffs(IAliveEntity target)
    {
        var buffs = ResolveState(target);
        if (buffs == null)
        {
            return EmptyBuffs;
        }

        var now = clock.GetCurrentInstant();
        return buffs.Values.Where(b => b.ExpiresAt > now).ToArray();
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

        foreach (var buff in expired)
        {
            await SendIconRemovalAsync(target, buff);
            await ApplyTimeoutBuffAsync(target, buff);
        }

        await PulseAsync(target, buffs, now);

        return expired;
    }

    // Periodic damage never takes the last hit point, so it never kills and never awards a kill.
    // A fixed amount that scales with a level scales with the level of whoever applied the card.
    private static async Task PulseAsync(IAliveEntity target, ConcurrentDictionary<short, BuffInstance> buffs, Instant now)
    {
        long hpPercent = 0, mpPercent = 0, hpFixed = 0, mpFixed = 0;
        foreach (var (cardId, buff) in buffs)
        {
            if (buff.FromSkill || buff.ExpiresAt <= now || buff.PulsedUntil >= now)
            {
                continue;
            }

            foreach (var bCard in buff.BCards)
            {
                var times = buff.PulsesDue(bCard, now);
                if (times <= 0)
                {
                    continue;
                }

                var amount = times * BattleStatsProvider.ScaleByLevel(bCard, target.Level);
                var fixedAmount = times * BattleStatsProvider.ScaleByLevel(bCard, buff.CasterLevel ?? target.Level);
                switch (bCard.Effect())
                {
                    case BCardEffect.RecoveryAndDamagePercentHpRecovered: hpPercent += amount; break;
                    case BCardEffect.RecoveryAndDamagePercentHpReduced: hpPercent -= amount; break;
                    case BCardEffect.RecoveryAndDamagePercentMpRecovered: mpPercent += amount; break;
                    case BCardEffect.RecoveryAndDamagePercentMpReduced: mpPercent -= amount; break;
                    case BCardEffect.HealingBurningAndCastingRestoreHp: hpFixed += fixedAmount; break;
                    case BCardEffect.HealingBurningAndCastingDecreaseHp: hpFixed -= fixedAmount; break;
                    case BCardEffect.HealingBurningAndCastingRestoreMp: mpFixed += fixedAmount; break;
                    case BCardEffect.HealingBurningAndCastingDecreaseMp: mpFixed -= fixedAmount; break;
                }
            }

            buffs.TryUpdate(cardId, buff with { PulsedUntil = now }, buff);
        }

        if ((hpPercent == 0 && mpPercent == 0 && hpFixed == 0 && mpFixed == 0) || !target.IsAlive)
        {
            return;
        }

        var hp = (int)Math.Clamp(target.Hp + target.MaxHp * hpPercent / 100 + hpFixed, Math.Min(1, target.Hp), target.MaxHp);
        var mp = (int)Math.Clamp(target.Mp + target.MaxMp * mpPercent / 100 + mpFixed, 0, target.MaxMp);
        var hpChange = hp - target.Hp;
        if (hpChange == 0 && mp == target.Mp)
        {
            return;
        }

        target.Hp = hp;
        target.Mp = mp;
        if (target is PlayerComponentBundle player)
        {
            await player.SendPacketAsync(player.GenerateStat());
        }

        if (target.MapInstance != null)
        {
            await ShowHpChangeAsync(target, target.MapInstance, hpChange);
            await target.MapInstance.SendPacketAsync(target.GenerateStatInfo());
        }
    }

    // The number floats over the target for everyone on the map; MP has no such number.
    private static Task ShowHpChangeAsync(IAliveEntity target, MapInstance map, int hpChange)
    {
        if (hpChange == 0)
        {
            return Task.CompletedTask;
        }

        return hpChange > 0
            ? map.SendPacketAsync(new RcPacket
            {
                VisualType = target.VisualType,
                VisualId = target.VisualId,
                HealAmount = hpChange,
                Unknown = 0
            })
            : map.SendPacketAsync(new DmPacket
            {
                VisualType = target.VisualType,
                VisualId = target.VisualId,
                Damages = -hpChange,
                DamageType = DamagePacketType.Damage
            });
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
            if (card == null || (card.BuffType == BuffType.Bad && IsImmuneToDebuffs(target)))
            {
                continue;
            }

            await ApplyAsync(target, card, cardCatalog.GetCardBCards(cardId), caster);
        }
    }

    private static bool IsImmuneToDebuffs(IAliveEntity target) =>
        target is INonPlayableEntity { NpcMonster.CantDebuff: true };

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
        if (followUp == null || (followUp.BuffType == BuffType.Bad && IsImmuneToDebuffs(target)))
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

    private static BuffType ClassifyBuffType(CardDto card) => card.BuffType;

    // Group 0 holds unrelated cards (the bleedings among them), so only a real family replaces.
    private async Task ReplaceSameGroupAsync(IAliveEntity target, ConcurrentDictionary<short, BuffInstance> buffs, CardDto card)
    {
        if (card.GroupId == 0)
        {
            return;
        }

        foreach (var (cardId, buff) in buffs)
        {
            if (cardId != card.CardId && !buff.FromSkill && cardCatalog.GetCard(cardId)?.GroupId == card.GroupId
                && buffs.TryRemove(cardId, out var removed))
            {
                await SendIconRemovalAsync(target, removed);
            }
        }
    }
}
