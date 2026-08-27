//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Frozen;
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

        var totals = new CardTotals();

        foreach (var source in CardSources(buffs, equipment))
        {
            foreach (var card in source)
            {
                if (CardEffects.TryGetValue(card.Effect(), out var apply))
                {
                    apply(totals, ScaleByLevel(card, target.Level));
                }
            }
        }

        // Fold element-specific flat bonuses into ElementRate based on attacker's own
        // element. Non-matching element buffs don't apply.
        var elementFlatBonus = totals.ElementAll + stats.Element switch
        {
            1 => totals.ElementFire,
            2 => totals.ElementWater,
            3 => totals.ElementLight,
            4 => totals.ElementDark,
            _ => 0,
        };

        return stats with
        {
            Morale = stats.Morale + totals.Morale,
            MinHit = (int)((stats.MinHit + totals.AttackAll + totals.AttackMelee) * (1 + (totals.DamageAll + totals.DamageMelee) / 100.0)),
            MaxHit = (int)((stats.MaxHit + totals.AttackAll + totals.AttackMelee) * (1 + (totals.DamageAll + totals.DamageMelee) / 100.0)),
            MinDistance = (int)((stats.MinDistance + totals.AttackAll + totals.AttackRanged) * (1 + (totals.DamageAll + totals.DamageRanged) / 100.0)),
            MaxDistance = (int)((stats.MaxDistance + totals.AttackAll + totals.AttackRanged) * (1 + (totals.DamageAll + totals.DamageRanged) / 100.0)),
            EnemyFireResistance = stats.EnemyFireResistance + totals.FoeAll + totals.FoeFire,
            EnemyWaterResistance = stats.EnemyWaterResistance + totals.FoeAll + totals.FoeWater,
            EnemyLightResistance = stats.EnemyLightResistance + totals.FoeAll + totals.FoeLight,
            EnemyDarkResistance = stats.EnemyDarkResistance + totals.FoeAll + totals.FoeDark,
            HitRate = stats.HitRate + totals.HitRate,
            DistanceRate = stats.DistanceRate + totals.HitRate,
            CriticalChance = stats.CriticalChance + totals.CritInflicting,
            CriticalRate = stats.CriticalRate + totals.CritDamage,
            DistanceCriticalChance = stats.DistanceCriticalChance + totals.CritInflicting,
            DistanceCriticalRate = stats.DistanceCriticalRate + totals.CritDamage,
            Defence = stats.Defence + totals.DefenceAll + totals.DefenceMelee,
            DistanceDefence = stats.DistanceDefence + totals.DefenceAll + totals.DefenceRanged,
            MagicDefence = stats.MagicDefence + totals.DefenceAll + totals.DefenceMagical,
            DefenceDodge = stats.DefenceDodge + totals.Dodge,
            DistanceDefenceDodge = stats.DistanceDefenceDodge + totals.Dodge,
            // "All" adds to each of the four rather than living in a fifth field: the
            // damage step reads one resistance, picked by the attacker's element, and a
            // separate total would have to be remembered at every one of those reads.
            FireResistance = stats.FireResistance + totals.ResistAll + totals.ResistFire,
            WaterResistance = stats.WaterResistance + totals.ResistAll + totals.ResistWater,
            LightResistance = stats.LightResistance + totals.ResistAll + totals.ResistLight,
            DarkResistance = stats.DarkResistance + totals.ResistAll + totals.ResistDark,
            ElementRate = stats.ElementRate + elementFlatBonus,
            GuaranteedHitChance = stats.GuaranteedHitChance + totals.GuaranteedHit,
            GuaranteedDodgeChance = stats.GuaranteedDodgeChance + totals.GuaranteedDodge,
        };
    }

    private sealed class CardTotals
    {
        public int AttackAll, AttackMelee, AttackRanged, AttackMagical;
        public int DamageAll, DamageMelee, DamageRanged, DamageMagical;
        public int CritInflicting, CritDamage;
        public int DefenceAll, DefenceMelee, DefenceRanged, DefenceMagical;
        public int HitRate, Dodge, Morale;
        public int FoeAll, FoeFire, FoeWater, FoeLight, FoeDark;
        public int ResistAll, ResistFire, ResistWater, ResistLight, ResistDark;
        public int ElementAll, ElementFire, ElementWater, ElementLight, ElementDark;
        public int GuaranteedHit, GuaranteedDodge;
    }

    private static readonly FrozenDictionary<BCardEffect, Action<CardTotals, int>> CardEffects =
        new Dictionary<BCardEffect, Action<CardTotals, int>>
        {
            [BCardEffect.AttackPowerAllAttacksIncreased] = (t, v) => t.AttackAll += v,
            [BCardEffect.AttackPowerAllAttacksDecreased] = (t, v) => t.AttackAll -= v,
            [BCardEffect.AttackPowerMeleeAttacksIncreased] = (t, v) => t.AttackMelee += v,
            [BCardEffect.AttackPowerMeleeAttacksDecreased] = (t, v) => t.AttackMelee -= v,
            [BCardEffect.AttackPowerRangedAttacksIncreased] = (t, v) => t.AttackRanged += v,
            [BCardEffect.AttackPowerRangedAttacksDecreased] = (t, v) => t.AttackRanged -= v,
            [BCardEffect.AttackPowerMagicalAttacksIncreased] = (t, v) => t.AttackMagical += v,
            [BCardEffect.AttackPowerMagicalAttacksDecreased] = (t, v) => t.AttackMagical -= v,

            [BCardEffect.DamageDamageIncreased] = (t, v) => t.DamageAll += v,
            [BCardEffect.DamageDamageDecreased] = (t, v) => t.DamageAll -= v,
            [BCardEffect.DamageMeleeIncreased] = (t, v) => t.DamageMelee += v,
            [BCardEffect.DamageMeleeDecreased] = (t, v) => t.DamageMelee -= v,
            [BCardEffect.DamageRangedIncreased] = (t, v) => t.DamageRanged += v,
            [BCardEffect.DamageRangedDecreased] = (t, v) => t.DamageRanged -= v,
            [BCardEffect.DamageMagicalIncreased] = (t, v) => t.DamageMagical += v,
            [BCardEffect.DamageMagicalDecreased] = (t, v) => t.DamageMagical -= v,

            [BCardEffect.CriticalInflictingIncreased] = (t, v) => t.CritInflicting += v,
            [BCardEffect.CriticalInflictingReduced] = (t, v) => t.CritInflicting -= v,
            [BCardEffect.CriticalDamageIncreased] = (t, v) => t.CritDamage += v,
            [BCardEffect.CriticalDamageIncreasedInflictingReduced] = (t, v) => t.CritDamage -= v,

            [BCardEffect.DefenceAllIncreased] = (t, v) => t.DefenceAll += v,
            [BCardEffect.DefenceAllDecreased] = (t, v) => t.DefenceAll -= v,
            [BCardEffect.DefenceMeleeIncreased] = (t, v) => t.DefenceMelee += v,
            [BCardEffect.DefenceMeleeDecreased] = (t, v) => t.DefenceMelee -= v,
            [BCardEffect.DefenceRangedIncreased] = (t, v) => t.DefenceRanged += v,
            [BCardEffect.DefenceRangedDecreased] = (t, v) => t.DefenceRanged -= v,
            [BCardEffect.DefenceMagicalIncreased] = (t, v) => t.DefenceMagical += v,
            [BCardEffect.DefenceMagicalDecreased] = (t, v) => t.DefenceMagical -= v,

            // Type 14, and the mirror of type 13: that one is the resistance of whoever
            // is being hit, this is what the one hitting does to it. Both end up in the
            // same subtraction in ComputeElementalDamage.
            [BCardEffect.EnemyElementResistanceAllIncreased] = (t, v) => t.FoeAll += v,
            [BCardEffect.EnemyElementResistanceAllDecreased] = (t, v) => t.FoeAll -= v,
            [BCardEffect.EnemyElementResistanceFireIncreased] = (t, v) => t.FoeFire += v,
            [BCardEffect.EnemyElementResistanceFireDecreased] = (t, v) => t.FoeFire -= v,
            [BCardEffect.EnemyElementResistanceWaterIncreased] = (t, v) => t.FoeWater += v,
            [BCardEffect.EnemyElementResistanceWaterDecreased] = (t, v) => t.FoeWater -= v,
            [BCardEffect.EnemyElementResistanceLightIncreased] = (t, v) => t.FoeLight += v,
            [BCardEffect.EnemyElementResistanceLightDecreased] = (t, v) => t.FoeLight -= v,
            [BCardEffect.EnemyElementResistanceDarkIncreased] = (t, v) => t.FoeDark += v,
            [BCardEffect.EnemyElementResistanceDarkDecreased] = (t, v) => t.FoeDark -= v,

            [BCardEffect.TargetAllHitRateIncreased] = (t, v) => t.HitRate += v,
            [BCardEffect.TargetAllHitRateDecreased] = (t, v) => t.HitRate -= v,

            [BCardEffect.DodgeAndDefencePercentDodgeIncreased] = (t, v) => t.Dodge += v,
            [BCardEffect.DodgeAndDefencePercentDodgeDecreased] = (t, v) => t.Dodge -= v,

            [BCardEffect.MoraleMoraleIncreased] = (t, v) => t.Morale += v,
            [BCardEffect.MoraleMoraleDecreased] = (t, v) => t.Morale -= v,

            // Type 16. The file gives X1 and X2 the same sentence, so the second slot
            // carries no meaning of its own and both add - and in the data no skill
            // declares one with a negative value anyway.
            [BCardEffect.GuarantedDodgeRangedAttackAttackHitChance] = (t, v) => t.GuaranteedHit += v,
            [BCardEffect.GuarantedDodgeRangedAttackAttackHitChanceNegated] = (t, v) => t.GuaranteedHit += v,
            [BCardEffect.GuarantedDodgeRangedAttackAlwaysDodgePropability] = (t, v) => t.GuaranteedDodge += v,
            [BCardEffect.GuarantedDodgeRangedAttackAlwaysDodgePropabilityNegated] = (t, v) => t.GuaranteedDodge += v,

            [BCardEffect.ElementAllIncreased] = (t, v) => t.ElementAll += v,
            [BCardEffect.ElementAllDecreased] = (t, v) => t.ElementAll -= v,
            [BCardEffect.ElementFireIncreased] = (t, v) => t.ElementFire += v,
            [BCardEffect.ElementFireDecreased] = (t, v) => t.ElementFire -= v,
            [BCardEffect.ElementWaterIncreased] = (t, v) => t.ElementWater += v,
            [BCardEffect.ElementWaterDecreased] = (t, v) => t.ElementWater -= v,
            [BCardEffect.ElementLightIncreased] = (t, v) => t.ElementLight += v,
            [BCardEffect.ElementLightDecreased] = (t, v) => t.ElementLight -= v,
            [BCardEffect.ElementDarkIncreased] = (t, v) => t.ElementDark += v,
            [BCardEffect.ElementDarkDecreased] = (t, v) => t.ElementDark -= v,

            // Type 13, the defender's side of the elemental exchange. Not to be confused
            // with type 7 above, which is the attacker's element rate: these four are read
            // in ComputeElementalDamage as a percentage taken off the incoming elemental
            // damage.
            [BCardEffect.ElementResistanceAllIncreased] = (t, v) => t.ResistAll += v,
            [BCardEffect.ElementResistanceAllDecreased] = (t, v) => t.ResistAll -= v,
            [BCardEffect.ElementResistanceFireIncreased] = (t, v) => t.ResistFire += v,
            [BCardEffect.ElementResistanceFireDecreased] = (t, v) => t.ResistFire -= v,
            [BCardEffect.ElementResistanceWaterIncreased] = (t, v) => t.ResistWater += v,
            [BCardEffect.ElementResistanceWaterDecreased] = (t, v) => t.ResistWater -= v,
            [BCardEffect.ElementResistanceLightIncreased] = (t, v) => t.ResistLight += v,
            [BCardEffect.ElementResistanceLightDecreased] = (t, v) => t.ResistLight -= v,
            [BCardEffect.ElementResistanceDarkIncreased] = (t, v) => t.ResistDark += v,
            [BCardEffect.ElementResistanceDarkDecreased] = (t, v) => t.ResistDark -= v,
        }.ToFrozenDictionary();

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
