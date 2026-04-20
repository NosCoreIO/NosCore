//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.Buff;
using NosCore.Shared.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Builds CombatStats from the underlying ECS components. Players synthesise their
// "base" profile from level+class (matching OpenNos CharacterHelper) and then fold in
// equipment (CombatComponent, once populated by the inventory/equipment system) plus
// active buffs. Monsters read straight from NpcMonsterDto with their own buff pass.
public sealed class BattleStatsProvider(IBuffService buffService) : IBattleStatsProvider
{
    public CombatStats GetStats(IAliveEntity entity)
    {
        var baseStats = ResolveBaseStats(entity);
        var buffs = buffService.GetActiveBuffs(entity);
        var withBuffs = ApplyBuffs(baseStats, buffs, entity);
        return withBuffs;
    }

    private static CombatStats ResolveBaseStats(IAliveEntity entity) => entity switch
    {
        INonPlayableEntity npc => FromMonster(npc.NpcMonster, entity.Level, entity.HeroLevel),
        ICharacterEntity character => FromCharacter(character),
        _ => default,
    };

    private static CombatStats FromMonster(NpcMonsterDto mob, byte level, byte heroLevel) => new(
        Level: level,
        HeroLevel: heroLevel,
        Class: CharacterClassType.Adventurer, // monsters use skill.Type directly; class is unused for mobs
        Morale: level,
        MinHit: mob.DamageMinimum,
        MaxHit: mob.DamageMaximum,
        HitRate: mob.Concentrate,
        CriticalChance: mob.CriticalChance,
        CriticalRate: mob.CriticalRate,
        MeleeUpgrade: mob.AttackUpgrade,
        MinDistance: mob.DamageMinimum,
        MaxDistance: mob.DamageMaximum,
        DistanceRate: mob.Concentrate,
        DistanceCriticalChance: mob.CriticalChance,
        DistanceCriticalRate: mob.CriticalRate,
        RangedUpgrade: mob.AttackUpgrade,
        Element: mob.Element,
        ElementRate: mob.ElementRate,
        ElementRateSp: 0,
        Defence: mob.CloseDefence,
        DefenceRate: mob.DefenceDodge,
        DistanceDefence: mob.DistanceDefence,
        DistanceDefenceRate: mob.DistanceDefenceDodge,
        MagicDefence: mob.MagicDefence,
        DefenceDodge: mob.DefenceDodge,
        DistanceDefenceDodge: mob.DistanceDefenceDodge,
        DefenceUpgrade: mob.DefenceUpgrade,
        FireResistance: mob.FireResistance,
        WaterResistance: mob.WaterResistance,
        LightResistance: mob.LightResistance,
        DarkResistance: mob.DarkResistance);

    private static CombatStats FromCharacter(ICharacterEntity character)
    {
        var level = character.Level;
        var cls = character.Class;
        var combat = ReadCombat(character);

        // CharacterHelper base tables: level+class formulas used when the inventory
        // system hasn't populated CombatComponent yet (fresh char, test fixtures).
        // Numbers lifted from OpenNos CharacterHelper.LoadStats for parity.
        var baseMinHit = cls switch
        {
            CharacterClassType.Swordsman => 2 * level + 5,
            CharacterClassType.Mage => 2 * level + 9,
            CharacterClassType.Archer => 9 + 3 * level,
            _ => 2 * level + 2,
        };
        var baseMaxHit = baseMinHit;
        var baseHitRate = cls switch
        {
            CharacterClassType.Swordsman => level + 27,
            CharacterClassType.Mage => 24 + level,
            CharacterClassType.Archer => 20 + 2 * level,
            _ => level + 10,
        };
        var baseDefence = cls switch
        {
            CharacterClassType.Swordsman => level + 2,
            CharacterClassType.Mage => level,
            CharacterClassType.Archer => level,
            _ => level,
        };
        var baseMagicDefence = cls switch
        {
            CharacterClassType.Mage => level + 4,
            _ => level,
        };
        var baseDodge = cls switch
        {
            CharacterClassType.Mage => 24 + level,
            _ => level + 12,
        };

        return new CombatStats(
            Level: level,
            HeroLevel: character.HeroLevel,
            Class: cls,
            Morale: level, // OpenNos morale is Level + MoraleUp buffs; buffs added later
            MinHit: Math.Max(baseMinHit, combat.MinHit),
            MaxHit: Math.Max(baseMaxHit, combat.MaxHit),
            HitRate: combat.HitRate > 0 ? combat.HitRate : baseHitRate,
            CriticalChance: combat.CriticalChance,
            CriticalRate: combat.CriticalRate,
            MeleeUpgrade: 0,
            MinDistance: Math.Max(baseMinHit, combat.MinDistance),
            MaxDistance: Math.Max(baseMaxHit, combat.MaxDistance),
            DistanceRate: combat.DistanceRate > 0 ? combat.DistanceRate : baseHitRate,
            DistanceCriticalChance: combat.DistanceCriticalChance,
            DistanceCriticalRate: combat.DistanceCriticalRate,
            RangedUpgrade: 0,
            Element: (byte)combat.Element,
            ElementRate: combat.ElementRate,
            ElementRateSp: 0,
            Defence: combat.Defence > 0 ? combat.Defence : baseDefence,
            DefenceRate: combat.DefenceRate,
            DistanceDefence: combat.DistanceDefence > 0 ? combat.DistanceDefence : baseDefence,
            DistanceDefenceRate: combat.DistanceDefenceRate,
            MagicDefence: combat.MagicDefence > 0 ? combat.MagicDefence : baseMagicDefence,
            DefenceDodge: combat.DefenceRate > 0 ? combat.DefenceRate : baseDodge,
            DistanceDefenceDodge: combat.DistanceDefenceRate > 0 ? combat.DistanceDefenceRate : baseDodge,
            DefenceUpgrade: 0,
            FireResistance: combat.FireResistance,
            WaterResistance: combat.WaterResistance,
            LightResistance: combat.LightResistance,
            DarkResistance: combat.DarkResistance);
    }

    private static CombatComponent ReadCombat(IAliveEntity entity)
    {
        if (entity is PlayerComponentBundle player)
        {
            var comp = player.World.TryGetComponent<CombatComponent>(player.Entity);
            if (comp.HasValue) return comp.Value;
        }
        return default;
    }

    // Buff folding: mirrors OpenNos' GetBuff queries. We scan each active card's BCards
    // once, summing +/− per (Type, SubType) pair, then inject the net effect into the
    // CombatStats record. Unknown BCard types fall through silently so content can ship
    // them ahead of server-side handling.
    private static CombatStats ApplyBuffs(CombatStats stats, IReadOnlyCollection<BuffInstance> buffs, IAliveEntity target)
    {
        if (buffs.Count == 0) return stats;

        int attackAllFlat = 0, attackMeleeFlat = 0, attackRangedFlat = 0, attackMagicalFlat = 0;
        int damageAllPct = 0, damageMeleePct = 0, damageRangedPct = 0, damageMagicalPct = 0;
        int critInflicting = 0, critDamage = 0;
        int defenceAll = 0, defenceMelee = 0, defenceRanged = 0, defenceMagical = 0;
        int hitRateFlat = 0, dodgeFlat = 0;
        int moraleFlat = 0;
        int elementAll = 0, elementFire = 0, elementWater = 0, elementLight = 0, elementDark = 0;

        foreach (var buff in buffs)
        {
            foreach (var card in buff.BCards)
            {
                var first = ScaleByLevel(card, target.Level);
                var type = (BCardType.CardType)card.Type;
                var sub = card.SubType;

                switch (type)
                {
                    case BCardType.CardType.AttackPower:
                        if (sub == (byte)AdditionalTypes.AttackPower.AllAttacksIncreased) attackAllFlat += first;
                        else if (sub == (byte)AdditionalTypes.AttackPower.AllAttacksDecreased) attackAllFlat -= first;
                        else if (sub == (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased) attackMeleeFlat += first;
                        else if (sub == (byte)AdditionalTypes.AttackPower.MeleeAttacksDecreased) attackMeleeFlat -= first;
                        else if (sub == (byte)AdditionalTypes.AttackPower.RangedAttacksIncreased) attackRangedFlat += first;
                        else if (sub == (byte)AdditionalTypes.AttackPower.RangedAttacksDecreased) attackRangedFlat -= first;
                        else if (sub == (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased) attackMagicalFlat += first;
                        else if (sub == (byte)AdditionalTypes.AttackPower.MagicalAttacksDecreased) attackMagicalFlat -= first;
                        break;
                    case BCardType.CardType.Damage:
                        if (sub == (byte)AdditionalTypes.Damage.DamageIncreased) damageAllPct += first;
                        else if (sub == (byte)AdditionalTypes.Damage.DamageDecreased) damageAllPct -= first;
                        else if (sub == (byte)AdditionalTypes.Damage.MeleeIncreased) damageMeleePct += first;
                        else if (sub == (byte)AdditionalTypes.Damage.MeleeDecreased) damageMeleePct -= first;
                        else if (sub == (byte)AdditionalTypes.Damage.RangedIncreased) damageRangedPct += first;
                        else if (sub == (byte)AdditionalTypes.Damage.RangedDecreased) damageRangedPct -= first;
                        else if (sub == (byte)AdditionalTypes.Damage.MagicalIncreased) damageMagicalPct += first;
                        else if (sub == (byte)AdditionalTypes.Damage.MagicalDecreased) damageMagicalPct -= first;
                        break;
                    case BCardType.CardType.Critical:
                        if (sub == (byte)AdditionalTypes.Critical.InflictingIncreased) critInflicting += first;
                        else if (sub == (byte)AdditionalTypes.Critical.InflictingReduced) critInflicting -= first;
                        else if (sub == (byte)AdditionalTypes.Critical.DamageIncreased) critDamage += first;
                        else if (sub == (byte)AdditionalTypes.Critical.DamageIncreasedInflictingReduced) critDamage -= first;
                        break;
                    case BCardType.CardType.Defence:
                        if (sub == (byte)AdditionalTypes.Defence.AllIncreased) defenceAll += first;
                        else if (sub == (byte)AdditionalTypes.Defence.AllDecreased) defenceAll -= first;
                        else if (sub == (byte)AdditionalTypes.Defence.MeleeIncreased) defenceMelee += first;
                        else if (sub == (byte)AdditionalTypes.Defence.MeleeDecreased) defenceMelee -= first;
                        else if (sub == (byte)AdditionalTypes.Defence.RangedIncreased) defenceRanged += first;
                        else if (sub == (byte)AdditionalTypes.Defence.RangedDecreased) defenceRanged -= first;
                        else if (sub == (byte)AdditionalTypes.Defence.MagicalIncreased) defenceMagical += first;
                        else if (sub == (byte)AdditionalTypes.Defence.MagicalDecreased) defenceMagical -= first;
                        break;
                    case BCardType.CardType.Target:
                        if (sub == (byte)AdditionalTypes.Target.AllHitRateIncreased) hitRateFlat += first;
                        else if (sub == (byte)AdditionalTypes.Target.AllHitRateDecreased) hitRateFlat -= first;
                        break;
                    case BCardType.CardType.DodgeAndDefencePercent:
                        if (sub == (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased) dodgeFlat += first;
                        else if (sub == (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeDecreased) dodgeFlat -= first;
                        break;
                    case BCardType.CardType.Morale:
                        if (sub == (byte)AdditionalTypes.Morale.MoraleIncreased) moraleFlat += first;
                        else if (sub == (byte)AdditionalTypes.Morale.MoraleDecreased) moraleFlat -= first;
                        break;
                    case BCardType.CardType.Element:
                        if (sub == (byte)AdditionalTypes.Element.AllIncreased) elementAll += first;
                        else if (sub == (byte)AdditionalTypes.Element.AllDecreased) elementAll -= first;
                        else if (sub == (byte)AdditionalTypes.Element.FireIncreased) elementFire += first;
                        else if (sub == (byte)AdditionalTypes.Element.FireDecreased) elementFire -= first;
                        else if (sub == (byte)AdditionalTypes.Element.WaterIncreased) elementWater += first;
                        else if (sub == (byte)AdditionalTypes.Element.WaterDecreased) elementWater -= first;
                        else if (sub == (byte)AdditionalTypes.Element.LightIncreased) elementLight += first;
                        else if (sub == (byte)AdditionalTypes.Element.LightDecreased) elementLight -= first;
                        else if (sub == (byte)AdditionalTypes.Element.DarkIncreased) elementDark += first;
                        else if (sub == (byte)AdditionalTypes.Element.DarkDecreased) elementDark -= first;
                        break;
                }
            }
        }

        // Fold element-specific flat bonuses into ElementRate based on attacker's own
        // element. Non-matching element buffs don't apply.
        var elementFlatBonus = elementAll + stats.Element switch
        {
            1 => elementFire,
            2 => elementWater,
            3 => elementLight,
            4 => elementDark,
            _ => 0,
        };

        return stats with
        {
            Morale = stats.Morale + moraleFlat,
            MinHit = (int)((stats.MinHit + attackAllFlat + attackMeleeFlat) * (1 + (damageAllPct + damageMeleePct) / 100.0)),
            MaxHit = (int)((stats.MaxHit + attackAllFlat + attackMeleeFlat) * (1 + (damageAllPct + damageMeleePct) / 100.0)),
            MinDistance = (int)((stats.MinDistance + attackAllFlat + attackRangedFlat) * (1 + (damageAllPct + damageRangedPct) / 100.0)),
            MaxDistance = (int)((stats.MaxDistance + attackAllFlat + attackRangedFlat) * (1 + (damageAllPct + damageRangedPct) / 100.0)),
            HitRate = stats.HitRate + hitRateFlat,
            DistanceRate = stats.DistanceRate + hitRateFlat,
            CriticalChance = stats.CriticalChance + critInflicting,
            CriticalRate = stats.CriticalRate + critDamage,
            DistanceCriticalChance = stats.DistanceCriticalChance + critInflicting,
            DistanceCriticalRate = stats.DistanceCriticalRate + critDamage,
            Defence = stats.Defence + defenceAll + defenceMelee,
            DistanceDefence = stats.DistanceDefence + defenceAll + defenceRanged,
            MagicDefence = stats.MagicDefence + defenceAll + defenceMagical,
            DefenceDodge = stats.DefenceDodge + dodgeFlat,
            DistanceDefenceDodge = stats.DistanceDefenceDodge + dodgeFlat,
            ElementRate = stats.ElementRate + elementFlatBonus,
        };
    }

    private static int ScaleByLevel(BCardDto card, int level)
    {
        // Matches OpenNos: IsLevelScaled + IsLevelDivided together means "first/level",
        // IsLevelScaled alone means "first * level". Default path uses FirstData as-is.
        if (!card.IsLevelScaled) return card.FirstData;
        if (card.IsLevelDivided) return card.FirstData == 0 ? 0 : Math.Max(1, level / Math.Max(1, card.FirstData));
        return card.FirstData * Math.Max(1, level);
    }
}
