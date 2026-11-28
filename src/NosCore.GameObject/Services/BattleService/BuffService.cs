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
using NosCore.GameObject.Messaging.Events;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging;
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
    IRandomProvider randomProvider, IDebuffResistanceService debuffResistanceService, IGameEventDispatcher gameEvents) : IBuffService
{
    private static readonly IReadOnlyCollection<BuffInstance> EmptyBuffs = Array.Empty<BuffInstance>();
    private static readonly IReadOnlyList<BuffInstance> EmptyExpired = Array.Empty<BuffInstance>();
    private const byte CardArgument = 3;

    public async Task ApplyAsync(IAliveEntity target, CardDto card, IReadOnlyList<BCardDto> bCards, IAliveEntity? caster, int overrideDuration = -1)
    {
        var buffs = target.BuffState();
        if (buffs == null)
        {
            return;
        }

        var now = clock.GetCurrentInstant();
        var durationMs = overrideDuration >= 0 ? overrideDuration : card.Duration * 100;
        // A card with no duration in the data lasts until something removes it.
        var buff = new BuffInstance(
            CardId: card.CardId,
            BuffType: ClassifyBuffType(card),
            Caster: caster,
            StartedAt: now,
            ExpiresAt: durationMs == 0 && overrideDuration < 0 ? Instant.MaxValue : now.Plus(Duration.FromMilliseconds(durationMs)),
            BCards: bCards)
        {
            CasterLevel = caster?.Level,
            HasLastingEffect = card.HasLastingEffect,
        };

        await CleanseAsync(target, bCards);
        await ReplaceSameGroupAsync(target, buffs, card);
        var alreadyRunning = buffs.TryGetValue(card.CardId, out var running) && running.ExpiresAt > now;
        buffs.AddOrUpdate(card.CardId, buff, (_, current) => Refresh(current, buff, now));
        await NotifyMovementAsync(target, buff);

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

            // Ending the effect first starts it over on a card applied again.
            if (card.HasLastingEffect)
            {
                await target.MapInstance.SendPacketAsync(target.GenerateBfe(card.CardId, Duration.Zero));
                await target.MapInstance.SendPacketAsync(target.GenerateBfe(card.CardId, Duration.FromMilliseconds(durationMs)));
            }
        }

        if (!alreadyRunning)
        {
            await AnnounceAsync(target, buff, Game18NConstString.UnderEffect);
        }
    }

    // A card applied again while it still runs keeps pulsing on the rhythm it started with.
    private static BuffInstance Refresh(BuffInstance current, BuffInstance applied, Instant now) =>
        current.ExpiresAt > now && !current.FromSkill && !current.Static
            ? applied with { StartedAt = current.StartedAt, PulsedUntil = current.PulsedUntil }
            : applied;

    public async Task ApplySkillBuffAsync(IAliveEntity target, short skillVnum, short skillDuration, IReadOnlyList<BCardDto> bCards, IAliveEntity? caster)
    {
        if (skillDuration <= 0 || bCards.Count == 0)
        {
            return;
        }

        var lasting = bCards
            .Where(b => b.Type != BCardType.CardType.Damage)
            .ToArray();
        if (lasting.Length == 0)
        {
            return;
        }

        var buffs = target.BuffState();
        if (buffs == null)
        {
            return;
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
        await NotifyMovementAsync(target, buff);
    }

    public async Task ApplyPermanentAsync(PlayerComponentBundle target, CardDto card, IReadOnlyList<BCardDto> bCards)
    {
        var buffs = target.BuffState();
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

        await CleanseAsync(target, bCards);
        await ReplaceSameGroupAsync(target, buffs, card);
        buffs.AddOrUpdate(card.CardId, buff, (_, _) => buff);
        await NotifyMovementAsync(target, buff);
        await target.SendPacketAsync(new VbPacket { CardId = card.CardId, IsActivated = true });
    }

    public async Task ApplyStaticAsync(PlayerComponentBundle target, CardDto card, IReadOnlyList<BCardDto> bCards, Duration duration)
    {
        var buffs = target.BuffState();
        if (buffs == null)
        {
            return;
        }

        await ReplaceSameGroupAsync(target, buffs, card);
        RestoreStatic(target, card, bCards, duration);
        if (buffs.TryGetValue(card.CardId, out var applied))
        {
            await NotifyMovementAsync(target, applied);
        }

        await target.SendPacketAsync(new VbPacket
        {
            CardId = card.CardId,
            IsActivated = true,
            RemainingTime = (int)(duration.TotalMilliseconds / 100)
        });
    }

    public void RestoreStatic(PlayerComponentBundle target, CardDto card, IReadOnlyList<BCardDto> bCards, Duration duration)
    {
        var buffs = target.BuffState();
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
        var buffs = target.BuffState();
        if (buffs != null && buffs.TryRemove(cardId, out var removed))
        {
            await SendIconRemovalAsync(target, removed);
        }
    }

    public async Task RemoveEffectAsync(IAliveEntity target, BCardEffect effect)
    {
        var buffs = target.BuffState();
        if (buffs == null)
        {
            return;
        }

        foreach (var (cardId, buff) in buffs)
        {
            if (buff.BCards.Any(b => b.Effect() == effect) && buffs.TryRemove(cardId, out var removed))
            {
                await SendIconRemovalAsync(target, removed);
            }
        }
    }

    public Task RemoveBadAsync(IAliveEntity target, byte maxLevel) => RemoveUpToLevelAsync(target, BuffType.Bad, maxLevel);

    private Task RemoveUpToLevelAsync(IAliveEntity target, BuffType buffType, int maxLevel) =>
        RemoveWhereAsync(target, (buff, card) =>
            buff.BuffType == buffType && !(buff.Static && buffType == BuffType.Good) && card.Level <= maxLevel);

    private async Task RemoveWhereAsync(IAliveEntity target, Func<BuffInstance, CardDto, bool> removes)
    {
        var buffs = target.BuffState();
        if (buffs == null)
        {
            return;
        }

        foreach (var (cardId, buff) in buffs)
        {
            if (!buff.FromSkill && cardCatalog.GetCard(cardId) is { } card && removes(buff, card)
                && buffs.TryRemove(cardId, out var removed))
            {
                await SendIconRemovalAsync(target, removed);
            }
        }
    }

    // Effects that strip cards off the entity they land on, whether a skill casts them or a card carries them.
    // A skill's effect lands either on its caster or on its targets, never on both.
    private async Task CleanseAsync(IAliveEntity target, IReadOnlyList<BCardDto> bCards, bool onCaster = false)
    {
        foreach (var bCard in bCards)
        {
            if (bCard.AppliesToCaster != onCaster)
            {
                continue;
            }

            switch (bCard.Effect())
            {
                case BCardEffect.LightAndShadowRemoveBadEffects:
                    await RemoveUpToLevelAsync(target, BuffType.Bad, bCard.FirstData);
                    break;
                case BCardEffect.LightAndShadowRemoveGoodEffects:
                    await RemoveUpToLevelAsync(target, BuffType.Good, bCard.FirstData);
                    break;
                case BCardEffect.SpecialisationBuffResistanceRemoveBadEffects when Rolls(bCard.FirstData):
                    await RemoveUpToLevelAsync(target, BuffType.Bad, bCard.SecondData);
                    break;
                case BCardEffect.SpecialisationBuffResistanceRemoveBuffBelowLevel when Rolls(bCard.FirstData):
                    await RemoveUpToLevelAsync(target, BuffType.Good, bCard.SecondData - 1);
                    break;
                case BCardEffect.BuffCancelGroupOfEffects:
                    await RemoveWhereAsync(target, (buff, card) =>
                        !buff.Static && card.GroupId == bCard.FirstData && card.Level <= bCard.SecondData);
                    break;
                // Every skill carrying it has 3 as its first value, which nothing explains yet.
                case BCardEffect.BuffCounteractPoison:
                    await RemoveWhereAsync(target, (buff, card) =>
                        buff.BuffType == BuffType.Bad && card.Kind == CardKind.Toxic && card.Level <= bCard.SecondData);
                    break;
            }
        }
    }

    public async Task ClearAsync(IAliveEntity target)
    {
        var buffs = target.BuffState();
        if (buffs == null || buffs.IsEmpty)
        {
            return;
        }

        foreach (var (cardId, buff) in buffs)
        {
            if (!buff.Static && buffs.TryRemove(cardId, out var removed))
            {
                await SendIconRemovalAsync(target, removed, announce: false);
            }
        }
    }

    // The client keeps a greyed icon on bf_e; a bf with no duration left is what takes it down.
    // Cards cleared by a death go without a line.
    private async Task SendIconRemovalAsync(IAliveEntity target, BuffInstance buff, bool announce = true)
    {
        if (announce)
        {
            await AnnounceAsync(target, buff, Game18NConstString.LostSideEffect);
        }

        await NotifyMovementAsync(target, buff);
        await SendIconPacketRemovalAsync(target, buff);
    }

    // Only the bearer reads which card started or ended, and only for a card with a bf icon.
    private static Task AnnounceAsync(IAliveEntity target, BuffInstance buff, Game18NConstString message) =>
        target is PlayerComponentBundle player && !buff.Static && !buff.FromSkill
            ? player.SendPacketAsync(player.GenerateSayi(SayColorType.WhiteTimeout, message, CardArgument, buff.CardId.ToString()))
            : Task.CompletedTask;

    private static async Task SendIconPacketRemovalAsync(IAliveEntity target, BuffInstance buff)
    {
        if (buff.Static)
        {
            if (target is PlayerComponentBundle character)
            {
                await character.SendPacketAsync(new VbPacket { CardId = buff.CardId, IsActivated = false });
            }

            return;
        }

        if (target.MapInstance == null || buff.FromSkill)
        {
            return;
        }

        await target.MapInstance.SendPacketAsync(new BfPacket
        {
            VisualType = target.VisualType,
            VisualId = target.VisualId,
            Buff = new BfPacket.BuffElementSubPacket
            {
                ChargeValue = 0,
                BuffId = buff.CardId,
                Duration = 0,
            },
            // A player's card comes down at the bearer's own level, whoever cast it.
            BuffLevel = target is PlayerComponentBundle ? target.Level : buff.CasterLevel ?? 0,
        });
        if (buff.HasLastingEffect)
        {
            await target.MapInstance.SendPacketAsync(target.GenerateBfe(buff.CardId, Duration.Zero));
        }
    }

    public IReadOnlyCollection<BuffInstance> GetActiveBuffs(IAliveEntity target)
    {
        var buffs = target.BuffState();
        if (buffs == null)
        {
            return EmptyBuffs;
        }

        var now = clock.GetCurrentInstant();
        return buffs.Values.Where(b => b.ExpiresAt > now).ToArray();
    }

    public bool HasBuff(IAliveEntity target, short cardId)
    {
        var buffs = target.BuffState();
        return buffs != null && buffs.ContainsKey(cardId);
    }

    public bool HasEffect(IAliveEntity target, BCardEffect effect)
    {
        var buffs = target.BuffState();
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
        var buffs = target.BuffState();
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
            if (target is PlayerComponentBundle player && !buff.FromSkill)
            {
                await gameEvents.RaiseAsync(new BuffExpiredEvent(player, buff.CardId));
            }
        }

        await PulseAsync(target, buffs, now);

        return expired;
    }

    // Periodic damage never takes the last hit point, so it never kills and never awards a kill.
    // A fixed amount that scales with a level scales with the level of whoever applied the card.
    // A drain takes from the bearer and gives what it took to whoever applied the card.
    private async Task PulseAsync(IAliveEntity target, ConcurrentDictionary<short, BuffInstance> buffs, Instant now)
    {
        long hpGainPercent = 0, hpLossPercent = 0, mpGainPercent = 0, mpLossPercent = 0;
        long hpGainFixed = 0, hpLossFixed = 0, mpGainFixed = 0, mpLossFixed = 0;
        List<(IAliveEntity? Caster, long Hp, long Mp)>? drains = null;
        List<int>? mpLosses = null;
        foreach (var (cardId, buff) in buffs)
        {
            if (buff.FromSkill || buff.ExpiresAt <= now || buff.PulsedUntil >= now)
            {
                continue;
            }

            long drainedHp = 0, drainedMp = 0;
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
                    case BCardEffect.RecoveryAndDamagePercentHpRecovered: hpGainPercent += amount; break;
                    case BCardEffect.RecoveryAndDamagePercentHpReduced: hpLossPercent += amount; break;
                    case BCardEffect.RecoveryAndDamagePercentMpRecovered: mpGainPercent += amount; break;
                    case BCardEffect.RecoveryAndDamagePercentMpReduced: mpLossPercent += amount; break;
                    case BCardEffect.HealingBurningAndCastingRestoreHp: hpGainFixed += fixedAmount; break;
                    case BCardEffect.HealingBurningAndCastingDecreaseHp: hpLossFixed += fixedAmount; break;
                    case BCardEffect.HealingBurningAndCastingRestoreMp: mpGainFixed += fixedAmount; break;
                    case BCardEffect.HealingBurningAndCastingDecreaseMp: mpLossFixed += fixedAmount; break;
                    case BCardEffect.DrainTransferEnemyHp: drainedHp += fixedAmount; break;
                    case BCardEffect.DrainAndStealLeechEnemyMp: drainedMp += Successes(times, bCard.FirstData) * bCard.SecondData; break;
                    case BCardEffect.ReflectionRemainingMpLostChance:
                        for (var i = Successes(times, bCard.FirstData); i > 0; i--)
                        {
                            (mpLosses ??= []).Add(bCard.SecondData);
                        }

                        break;
                }
            }

            if (drainedHp > 0 || drainedMp > 0)
            {
                (drains ??= []).Add((buff.Caster, drainedHp, drainedMp));
            }

            buffs.TryUpdate(cardId, buff with { PulsedUntil = now }, buff);
        }

        if ((hpGainPercent == 0 && hpLossPercent == 0 && mpGainPercent == 0 && mpLossPercent == 0
                && hpGainFixed == 0 && hpLossFixed == 0 && mpGainFixed == 0 && mpLossFixed == 0
                && drains == null && mpLosses == null)
            || !target.IsAlive)
        {
            return;
        }

        var noHpLoss = HasEffect(target, BCardEffect.TimeCircleSkillsDisableHpConsumption);
        var noMpLoss = HasEffect(target, BCardEffect.TimeCircleSkillsDisableMpConsumption);
        var hpGain = HasEffect(target, BCardEffect.TimeCircleSkillsDisableHpRecovery) ? 0 : target.MaxHp * hpGainPercent / 100 + hpGainFixed;
        var hpLoss = noHpLoss ? 0 : target.MaxHp * hpLossPercent / 100 + hpLossFixed;
        var mpGain = HasEffect(target, BCardEffect.TimeCircleSkillsDisableMpRecovery) ? 0 : target.MaxMp * mpGainPercent / 100 + mpGainFixed;
        var mpLoss = noMpLoss ? 0 : target.MaxMp * mpLossPercent / 100 + mpLossFixed;
        var hp = (int)Math.Clamp(target.Hp + hpGain - hpLoss, Math.Min(1, target.Hp), target.MaxHp);
        var mp = (int)Math.Clamp(target.Mp + mpGain - mpLoss, 0, target.MaxMp);
        List<(IAliveEntity Caster, int Hp, int Mp)>? gains = null;
        foreach (var (caster, drainHp, drainMp) in drains ?? [])
        {
            var takenHp = (int)Math.Max(0, Math.Min(drainHp, hp - 1));
            var takenMp = (int)Math.Min(drainMp, mp);
            if (noHpLoss)
            {
                takenHp = 0;
            }

            if (noMpLoss)
            {
                takenMp = 0;
            }

            hp -= takenHp;
            mp -= takenMp;
            if (caster != null && target.MapInstance is { } map && IsOn(caster, map))
            {
                (gains ??= []).Add((caster.Live(), takenHp, takenMp));
            }
        }

        foreach (var percent in (noMpLoss ? null : mpLosses) ?? [])
        {
            mp -= mp * Math.Clamp(percent, 0, 100) / 100;
        }

        var hpChange = hp - target.Hp;
        if (hpChange != 0 || mp != target.Mp)
        {
            target.Hp = hp;
            target.Mp = mp;
            await ShowVitalsAsync(target, hpChange);
        }

        foreach (var (caster, gainedHp, gainedMp) in gains ?? [])
        {
            await GiveAsync(caster, gainedHp, gainedMp);
        }
    }

    private int Held(IAliveEntity target, int current, int next, BCardEffect noLoss, BCardEffect noGain) =>
        next < current && HasEffect(target, noLoss) || next > current && HasEffect(target, noGain) ? current : next;

    private async Task GiveAsync(IAliveEntity entity, int hp, int mp)
    {
        if (!entity.IsAlive)
        {
            return;
        }

        var newHp = Held(entity, entity.Hp, Math.Min(entity.MaxHp, entity.Hp + hp),
            BCardEffect.TimeCircleSkillsDisableHpConsumption, BCardEffect.TimeCircleSkillsDisableHpRecovery);
        var newMp = Held(entity, entity.Mp, Math.Min(entity.MaxMp, entity.Mp + mp),
            BCardEffect.TimeCircleSkillsDisableMpConsumption, BCardEffect.TimeCircleSkillsDisableMpRecovery);
        var hpChange = newHp - entity.Hp;
        if (hpChange == 0 && newMp == entity.Mp)
        {
            return;
        }

        entity.Hp = newHp;
        entity.Mp = newMp;
        await ShowVitalsAsync(entity, hpChange);
    }

    // Only an entity on the bearer's map is changed from the pulse: that map's executor is the one running.
    private static bool IsOn(IAliveEntity entity, MapInstance map) => entity.Live() switch
    {
        PlayerComponentBundle p => p.World == map.EcsWorld && map.EcsWorld.IsAlive(p.Entity),
        MonsterComponentBundle m => m.World == map.EcsWorld && map.EcsWorld.IsAlive(m.Entity),
        NpcComponentBundle n => n.World == map.EcsWorld && map.EcsWorld.IsAlive(n.Entity),
        MateComponentBundle mate => mate.World == map.EcsWorld && map.EcsWorld.IsAlive(mate.Entity),
        _ => false,
    };

    private long Successes(long times, int percent)
    {
        long successes = 0;
        for (var i = 0; i < times; i++)
        {
            if (Rolls(percent))
            {
                successes++;
            }
        }

        return successes;
    }

    public Task ApplyCastingEffectsAsync(IAliveEntity caster) => caster.OnMapAsync(async () =>
    {
        long loss = 0;
        foreach (var buff in GetActiveBuffs(caster))
        {
            foreach (var bCard in buff.BCards)
            {
                if (bCard.Effect() == BCardEffect.HealingBurningAndCastingDecreaseHpWhenCasting)
                {
                    loss += BattleStatsProvider.ScaleByLevel(bCard, buff.CasterLevel ?? caster.Level);
                }
            }
        }

        if (loss == 0 || !caster.IsAlive || HasEffect(caster, BCardEffect.TimeCircleSkillsDisableHpConsumption))
        {
            return;
        }

        var hp = (int)Math.Max(Math.Min(1, caster.Hp), caster.Hp - loss);
        var hpChange = hp - caster.Hp;
        if (hpChange == 0)
        {
            return;
        }

        caster.Hp = hp;
        await ShowVitalsAsync(caster, hpChange);
    });

    private static async Task ShowVitalsAsync(IAliveEntity entity, int hpChange)
    {
        if (entity is PlayerComponentBundle player)
        {
            await player.SendPacketAsync(player.GenerateStat());
        }

        if (entity.MapInstance != null)
        {
            await ShowHpChangeAsync(entity, entity.MapInstance, hpChange);
            await entity.MapInstance.SendPacketAsync(entity.GenerateStatInfo());
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
    public Task InflictCardsAsync(IAliveEntity target, IAliveEntity? caster,
        IReadOnlyList<BCardDto> skillBCards) => InflictAsync(target, caster, skillBCards, onCaster: false);

    public Task InflictCasterCardsAsync(IAliveEntity caster, IReadOnlyList<BCardDto> skillBCards) =>
        InflictAsync(caster, caster, skillBCards, onCaster: true);

    private async Task InflictAsync(IAliveEntity target, IAliveEntity? caster, IReadOnlyList<BCardDto> skillBCards,
        bool onCaster)
    {
        await CleanseAsync(target, skillBCards, onCaster);
        for (var i = 0; i < skillBCards.Count; i++)
        {
            var bCard = skillBCards[i];
            var effect = bCard.Effect();
            if (bCard.AppliesToCaster != onCaster
                || effect is not (BCardEffect.BuffChanceCausing or BCardEffect.BuffChanceRemoving or BCardEffect.MeditationSkillSacrifice))
            {
                continue;
            }

            // Sacrifice's own card goes to whoever casts it: they take the blows meant for its target.
            var recipient = effect == BCardEffect.MeditationSkillSacrifice ? caster : target;
            if (recipient == null || !Rolls(bCard.FirstData))
            {
                continue;
            }

            var cardId = (short)bCard.SecondData;
            if (effect == BCardEffect.BuffChanceRemoving)
            {
                await RemoveAsync(recipient, cardId);
                continue;
            }

            var card = cardCatalog.GetCard(cardId);
            if (card == null || Resists(recipient, card))
            {
                continue;
            }

            await ApplyAsync(recipient, card, cardCatalog.GetCardBCards(cardId), caster);
        }
    }

    private bool Resists(IAliveEntity target, CardDto card)
    {
        if (card.BuffType != BuffType.Bad)
        {
            return false;
        }

        if (target is INonPlayableEntity { NpcMonster.CantDebuff: true })
        {
            return true;
        }

        var modifier = debuffResistanceService.ChanceModifier(target, card, GetActiveBuffs(target));
        return modifier < 0 && !Rolls(100 + modifier);
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
        if (followUp == null || Resists(target, followUp))
        {
            return;
        }

        await ApplyAsync(target, followUp, cardCatalog.GetCardBCards(followUp.CardId), expired.Caster);
    }

    private async Task NotifyMovementAsync(IAliveEntity target, BuffInstance buff)
    {
        if (buff.BCards.Any(b => b.Effect() is BCardEffect.SpecialAttackNoAttack or BCardEffect.MoveMovementImpossible
                or BCardEffect.MoveMovementSpeedIncreased or BCardEffect.MoveMovementSpeedDecreased))
        {
            await gameEvents.RaiseAsync(new MovementConditionChangedEvent(target));
        }

        if (buff.BCards.Any(b => b.Effect() == BCardEffect.IncreaseFairyElement))
        {
            await gameEvents.RaiseAsync(new FairyLookChangedEvent(target));
        }

        if (buff.BCards.Any(b => Hides(b.Effect())))
        {
            await gameEvents.RaiseAsync(new HiddenChangedEvent(target));
        }
    }

    private static bool Hides(BCardEffect effect) => effect is BCardEffect.SpecialActionsHide or BCardEffect.FalconSkillHide;

    private bool Rolls(int percent) => percent > 0 && randomProvider.Next(0, 100) < percent;

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
