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
// "base" profile from level+class (matching OpenNos CharacterHelper), then add the worn
// equipment and fold in every active effect. Monsters read straight from NpcMonsterDto.
public sealed class BattleStatsProvider(
    IBuffService buffService,
    EquipmentService.IEquipmentStatsService equipmentStatsService) : IBattleStatsProvider
{
    public CombatStats GetStats(IAliveEntity entity)
    {
        var gear = entity is ICharacterEntity
            ? equipmentStatsService.Resolve(entity)
            : EquipmentService.EquipmentStats.None;

        // Flat equipment first: a card that raises attack by a percentage has to see the weapon.
        var withGear = ApplyEquipment(ResolveBaseStats(entity), gear);

        return ApplyCards(withGear, buffService.GetActiveBuffs(entity), gear.BCards, entity);
    }

    // Adds what the worn pieces carry.
    //
    // Nothing was doing this. CombatComponent is created all zeros in MapWorld and never written
    // to, so ReadCombat below always came back empty and a character fought on the level+class
    // base tables alone - in full gear exactly as naked. It raises nothing, because those tables
    // give plausible numbers: the only way to see it is to compare two characters.
    private static CombatStats ApplyEquipment(CombatStats stats, EquipmentService.EquipmentStats gear) =>
        stats with
        {
            MinHit = stats.MinHit + gear.MinHit,
            MaxHit = stats.MaxHit + gear.MaxHit,
            HitRate = stats.HitRate + gear.HitRate,
            CriticalChance = stats.CriticalChance + gear.CriticalChance,
            CriticalRate = stats.CriticalRate + gear.CriticalRate,
            MeleeUpgrade = stats.MeleeUpgrade + gear.MainWeaponUpgrade,
            MinDistance = stats.MinDistance + gear.MinDistance,
            MaxDistance = stats.MaxDistance + gear.MaxDistance,
            DistanceRate = stats.DistanceRate + gear.DistanceRate,
            DistanceCriticalChance = stats.DistanceCriticalChance + gear.DistanceCriticalChance,
            DistanceCriticalRate = stats.DistanceCriticalRate + gear.DistanceCriticalRate,
            RangedUpgrade = stats.RangedUpgrade + gear.SecondaryWeaponUpgrade,
            Defence = stats.Defence + gear.CloseDefence,
            DistanceDefence = stats.DistanceDefence + gear.DistanceDefence,
            MagicDefence = stats.MagicDefence + gear.MagicDefence,
            DefenceDodge = stats.DefenceDodge + gear.DefenceDodge,
            DistanceDefenceDodge = stats.DistanceDefenceDodge + gear.DistanceDefenceDodge,
            DefenceUpgrade = stats.DefenceUpgrade + gear.ArmourUpgrade,
            ElementRate = stats.ElementRate + gear.ElementRate,
            FireResistance = stats.FireResistance + gear.FireResistance,
            WaterResistance = stats.WaterResistance + gear.WaterResistance,
            LightResistance = stats.LightResistance + gear.LightResistance,
            DarkResistance = stats.DarkResistance + gear.DarkResistance,
        };

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
    private static CombatStats ApplyCards(CombatStats stats, IReadOnlyCollection<BuffInstance> buffs,
        IReadOnlyList<BCardDto> equipment, IAliveEntity target)
    {
        if (buffs.Count == 0 && equipment.Count == 0) return stats;

        int attackAllFlat = 0, attackMeleeFlat = 0, attackRangedFlat = 0, attackMagicalFlat = 0;
        int damageAllPct = 0, damageMeleePct = 0, damageRangedPct = 0, damageMagicalPct = 0;
        int critInflicting = 0, critDamage = 0;
        int defenceAll = 0, defenceMelee = 0, defenceRanged = 0, defenceMagical = 0;
        int hitRateFlat = 0, dodgeFlat = 0;
        int foeAll = 0, foeFire = 0, foeWater = 0, foeLight = 0, foeDark = 0;
        int moraleFlat = 0;
        int resistAll = 0, resistFire = 0, resistWater = 0, resistLight = 0, resistDark = 0;
        int elementAll = 0, elementFire = 0, elementWater = 0, elementLight = 0, elementDark = 0;
        int guaranteedHit = 0, guaranteedDodge = 0;

        foreach (var source in CardSources(buffs, equipment))
        {
            foreach (var card in source)
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
                    // Type 14, and the mirror of type 13: that one is the resistance of whoever
                    // is being hit, this is what the one hitting does to it. Both end up in the
                    // same subtraction in ComputeElementalDamage.
                    case BCardType.CardType.EnemyElementResistance:
                        if (sub == (byte)AdditionalTypes.EnemyElementResistance.AllIncreased) foeAll += first;
                        else if (sub == (byte)AdditionalTypes.EnemyElementResistance.AllDecreased) foeAll -= first;
                        else if (sub == (byte)AdditionalTypes.EnemyElementResistance.FireIncreased) foeFire += first;
                        else if (sub == (byte)AdditionalTypes.EnemyElementResistance.FireDecreased) foeFire -= first;
                        else if (sub == (byte)AdditionalTypes.EnemyElementResistance.WaterIncreased) foeWater += first;
                        else if (sub == (byte)AdditionalTypes.EnemyElementResistance.WaterDecreased) foeWater -= first;
                        else if (sub == (byte)AdditionalTypes.EnemyElementResistance.LightIncreased) foeLight += first;
                        else if (sub == (byte)AdditionalTypes.EnemyElementResistance.LightDecreased) foeLight -= first;
                        else if (sub == (byte)AdditionalTypes.EnemyElementResistance.DarkIncreased) foeDark += first;
                        else if (sub == (byte)AdditionalTypes.EnemyElementResistance.DarkDecreased) foeDark -= first;
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
                    // Type 16. The file gives X1 and X2 the same sentence, so the second slot
                    // carries no meaning of its own and both add - and in the data no skill
                    // declares one with a negative value anyway.
                    case BCardType.CardType.GuarantedDodgeRangedAttack:
                        if (sub is (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AttackHitChance
                            or (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AttackHitChanceNegated)
                        {
                            guaranteedHit += first;
                        }
                        else if (sub is (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AlwaysDodgePropability
                            or (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AlwaysDodgePropabilityNegated)
                        {
                            guaranteedDodge += first;
                        }

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
                    // Type 13, the defender's side of the elemental exchange. Not to be
                    // confused with type 7 below, which is the attacker's element rate: these
                    // four are read in ComputeElementalDamage as a percentage taken off the
                    // incoming elemental damage.
                    case BCardType.CardType.ElementResistance:
                        if (sub == (byte)AdditionalTypes.ElementResistance.AllIncreased) resistAll += first;
                        else if (sub == (byte)AdditionalTypes.ElementResistance.AllDecreased) resistAll -= first;
                        else if (sub == (byte)AdditionalTypes.ElementResistance.FireIncreased) resistFire += first;
                        else if (sub == (byte)AdditionalTypes.ElementResistance.FireDecreased) resistFire -= first;
                        else if (sub == (byte)AdditionalTypes.ElementResistance.WaterIncreased) resistWater += first;
                        else if (sub == (byte)AdditionalTypes.ElementResistance.WaterDecreased) resistWater -= first;
                        else if (sub == (byte)AdditionalTypes.ElementResistance.LightIncreased) resistLight += first;
                        else if (sub == (byte)AdditionalTypes.ElementResistance.LightDecreased) resistLight -= first;
                        else if (sub == (byte)AdditionalTypes.ElementResistance.DarkIncreased) resistDark += first;
                        else if (sub == (byte)AdditionalTypes.ElementResistance.DarkDecreased) resistDark -= first;
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
            EnemyFireResistance = stats.EnemyFireResistance + foeAll + foeFire,
            EnemyWaterResistance = stats.EnemyWaterResistance + foeAll + foeWater,
            EnemyLightResistance = stats.EnemyLightResistance + foeAll + foeLight,
            EnemyDarkResistance = stats.EnemyDarkResistance + foeAll + foeDark,
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
            // "All" adds to each of the four rather than living in a fifth field: the
            // damage step reads one resistance, picked by the attacker's element, and a
            // separate total would have to be remembered at every one of those reads.
            FireResistance = stats.FireResistance + resistAll + resistFire,
            WaterResistance = stats.WaterResistance + resistAll + resistWater,
            LightResistance = stats.LightResistance + resistAll + resistLight,
            DarkResistance = stats.DarkResistance + resistAll + resistDark,
            ElementRate = stats.ElementRate + elementFlatBonus,
            GuaranteedHitChance = stats.GuaranteedHitChance + guaranteedHit,
            GuaranteedDodgeChance = stats.GuaranteedDodgeChance + guaranteedDodge,
        };
    }

    // Worn pieces fold in the same pass as the buffs, the way GetBuff sums both in the sibling
    // codebase. Two passes would apply each percentage to a different base.
    private static IEnumerable<IReadOnlyList<BCardDto>> CardSources(
        IReadOnlyCollection<BuffInstance> buffs, IReadOnlyList<BCardDto> equipment)
    {
        foreach (var buff in buffs)
        {
            yield return buff.BCards;
        }

        yield return equipment;
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
