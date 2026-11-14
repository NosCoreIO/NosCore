//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Algorithm.DodgeChanceService;
using NosCore.Algorithm.MonsterLevelDamageService;
using NosCore.Algorithm.UpgradeBonusService;
using NosCore.Data.Enumerations.Buff;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

// Player and monster damage. The calculator is stateless apart from RNG — all inputs come from the resolved CombatStats
// of both sides plus the SkillInfo. Keeping this symmetrical lets the same function
// service character→monster, monster→character and PvP damage.
public sealed class DamageCalculator(IRandomProvider random, IMonsterLevelDamageService monsterLevelDamageService,
    IUpgradeBonusService upgradeBonus, IDodgeChanceService dodgeChance) : IDamageCalculator
{
    // Element matchup boosts: attackerElement x defenderElement → multiplier. 0 means
    // "no element".
    private static readonly double[,] ElementalBoost =
    {
        //            none  fire water light dark
        /* none */  { 1.0,  1.0, 1.0, 1.0,  1.0  },
        /* fire */  { 1.3,  1.0, 2.0, 1.0,  1.5  },
        /* water */ { 1.3,  2.0, 1.0, 1.5,  1.0  },
        /* light */ { 1.3,  1.5, 1.0, 1.0,  3.0  },
        /* dark */  { 1.3,  1.0, 1.5, 3.0,  1.0  },
    };

    public DamageResult Calculate(CombatStats attacker, CombatStats defender, SkillInfo skill)
    {
        if (attacker.IsPlayer && defender.IsPlayer)
        {
            (attacker, defender) = WithPvpShell(attacker, defender);
        }

        // Skill type 0=melee, 1=ranged, 2=magic, 3=special-class (class picks defence),
        // 5=same pattern as 3. NosTale uses this to decide which defence stat + which
        // of the attacker's (melee vs ranged) profiles applies.
        var context = SelectAttackContext(attacker, defender, skill);

        // Morale buff: Level diff + Morale buffs, used as a baseline addition to hit
        // rate and base damage.
        var morale = attacker.Morale;

        // Dodge phase: only melee/ranged skills can miss. Magicians never dodge.
        if ((skill.Type == 0 || skill.Type == 1) && attacker.Class != CharacterClassType.Mage)
        {
            // Type 16 overrides this roll from both sides: subtype 11 on the attacker is "there
            // is a %s%% chance that every attack hits", subtype 21 on the defender is "always
            // dodge the target with a probability of %s%%".
            //
            // THE ORDER IS OURS, NOT THE FILE'S. When both are present and both roll, one has to
            // win, and nothing in BCard.dat says which: the attacker's guarantee goes first
            // because its sentence is the unconditional one. A rare intersection - subtype 21
            // appears on no skill at all, only on cards - but it had to be decided rather than
            // left to the order of two ifs.
            var alwaysHits = attacker.GuaranteedHitChance > 0
                && random.NextDouble() * 100 <= attacker.GuaranteedHitChance;

            if (!alwaysHits)
            {
                if (defender.GuaranteedDodgeChance > 0
                    && random.NextDouble() * 100 <= defender.GuaranteedDodgeChance)
                {
                    return new DamageResult(0, SuPacketHitMode.Miss);
                }

                var chance = dodgeChance.GetDodgeChance(context.MonsterDodge, context.MainHitRate);

                // RNG returns [0,100). A dodge lands if roll <= chance (chance is percent).
                if (random.NextDouble() * 100 <= chance)
                {
                    return new DamageResult(0, SuPacketHitMode.Miss);
                }
            }
        }

        if (attacker.IsPlayer)
        {
            return PlayerHit(attacker, defender, skill, context, morale);
        }

        // Base damage roll from the selected (main) profile. + morale - defender level
        // gives a positive bias when attacker out-levels and damp when under-levelled.
        var baseDamage = RollBaseDamage(context.MainMinDmg, context.MainMaxDmg);
        baseDamage += morale - defender.Level;

        // Whichever side is ahead on upgrades gets the bonus: the defender's armour or the attacker's damage.
        var monsterDefence = context.MonsterDefence;
        var upgrade = context.MainUpgrade - defender.DefenceUpgrade;
        if (upgrade < 0)
        {
            monsterDefence += (int)(monsterDefence * upgradeBonus.GetUpgradeBonus(-upgrade));
        }
        else if (upgrade > 0)
        {
            baseDamage += (int)(baseDamage * upgradeBonus.GetUpgradeBonus(upgrade));
        }

        baseDamage -= monsterDefence;

        // Ranged penalty: firing at point-blank loses 15% so Archers can't cheese melee.
        if (skill.Type == 1 && Chebyshev(attacker, defender) < 4)
        {
            baseDamage = (int)(baseDamage * 0.85);
        }

        // Elemental damage layer. See `ComputeElementalDamage` for the full pipeline.
        var elementalDamage = ComputeElementalDamage(attacker, defender, skill, baseDamage);

        var criticalBonus = RollCriticalBonus(skill, context, defender);
        if (criticalBonus is { } bonus)
        {
            baseDamage += (int)(baseDamage * bonus);
        }

        var landed = Land(attacker, defender, context.Kind, baseDamage + elementalDamage, criticalBonus != null);
        return defender.IsPlayer
            ? landed with { Damage = landed.Damage + monsterLevelDamageService.GetExtraDamage((byte)attacker.Level) }
            : landed;
    }

    // A player's attack is the class table, the weapon scaled by its upgrade over the target's
    // armour, a flat 15 and the skill's own attack bonus. The critical bonus scales that attack
    // against the defence, and the level difference is added after it.
    private DamageResult PlayerHit(CombatStats attacker, CombatStats defender, SkillInfo skill,
        AttackContext context, int morale)
    {
        var weapon = RollBaseDamage(context.MainWeaponMin, context.MainWeaponMax);
        // Signed: an attack debuff can take the rest below zero, and it must still count.
        var attack = RollSigned(context.MainMinDmg - context.MainWeaponMin, context.MainMaxDmg - context.MainWeaponMax);

        var defence = context.MonsterDefence;
        var upgrade = context.MainUpgrade - defender.DefenceUpgrade;
        if (upgrade < 0)
        {
            defence += (int)(defence * upgradeBonus.GetUpgradeBonus(-upgrade));
        }
        else if (upgrade > 0)
        {
            weapon += (int)(weapon * upgradeBonus.GetUpgradeBonus(upgrade));
        }

        attack += weapon + PlayerFlatAttack + SkillAttackBonus(skill, context.Kind, attacker.Level);

        var damage = attack - defence;
        var levelDifference = morale - defender.Level;
        var elementalDamage = ComputeElementalDamage(attacker, defender, skill, damage + levelDifference);

        var criticalBonus = RollCriticalBonus(skill, context, defender);
        if (criticalBonus is { } bonus)
        {
            damage += (int)(Math.Max(0, damage) * bonus);
        }

        return Land(attacker, defender, context.Kind, damage + levelDifference + elementalDamage, criticalBonus != null);
    }

    private const int PlayerFlatAttack = 15;

    // The extra share of damage a critical adds, or null when the hit is not critical.
    private double? RollCriticalBonus(SkillInfo skill, AttackContext context, CombatStats defender)
    {
        if (skill.Type == 2 || random.NextDouble() * 100 > context.MainCritChance + defender.CriticalReceivedChance)
        {
            return null;
        }

        var critHit = context.MainCritHit * (100 + defender.CriticalDamageReceived) / 100.0;
        return Math.Max(0, critHit) / 100.0;
    }

    // A hit that lands always does some damage: anything under 5 becomes 1 to 5.
    private DamageResult Land(CombatStats attacker, CombatStats defender, AttackKind kind, int damage, bool isCritical)
    {
        var totalDamage = RollChanceDamageTaken(defender, kind, RollChanceDamage(attacker, ApplyRaceDamage(attacker, defender, damage)));
        if (totalDamage < 5)
        {
            totalDamage = random.Next(1, 6);
        }

        return new DamageResult(
            Math.Max(1, totalDamage),
            isCritical ? SuPacketHitMode.CriticalAttack : SuPacketHitMode.SuccessAttack);
    }

    private static int ApplyRaceDamage(CombatStats attacker, CombatStats defender, int damage)
    {
        var percent = defender.IsMapBoss ? attacker.MapBossDamagePercent : 0;
        if (attacker.RaceDamage != null && defender.Race is { } race)
        {
            foreach (var bonus in attacker.RaceDamage)
            {
                if (bonus.Matches(race, defender.RaceType))
                {
                    percent += bonus.Percent;
                }
            }
        }

        return (int)Math.Clamp((long)damage * (100 + percent) / 100, int.MinValue, int.MaxValue);
    }

    private int RollChanceDamage(CombatStats attacker, int damage)
    {
        if (attacker.ChanceDamage == null)
        {
            return damage;
        }

        var percent = 0;
        foreach (var effect in attacker.ChanceDamage)
        {
            if (random.NextDouble() * 100 < effect.Chance)
            {
                percent += effect.Percent;
            }
        }

        return (int)Math.Clamp((long)damage * (100 + percent) / 100, int.MinValue, int.MaxValue);
    }

    private int RollChanceDamageTaken(CombatStats defender, AttackKind kind, int damage)
    {
        if (defender.ChanceDamageTaken == null)
        {
            return damage;
        }

        var percent = 0;
        foreach (var effect in defender.ChanceDamageTaken)
        {
            if ((effect.Kind == null || effect.Kind == kind) && random.NextDouble() * 100 < effect.Chance)
            {
                percent += effect.Percent;
            }
        }

        return damage * Math.Max(0, 100 + percent) / 100;
    }

    private static int SkillAttackBonus(SkillInfo skill, AttackKind kind, int level)
    {
        var bonus = 0;
        foreach (var card in skill.BCards)
        {
            var value = BattleStatsProvider.ScaleByLevel(card, level);
            bonus += (card.Effect(), kind) switch
            {
                (BCardEffect.AttackPowerAllAttacksIncreased, _) => value,
                (BCardEffect.AttackPowerAllAttacksDecreased, _) => -value,
                (BCardEffect.AttackPowerMeleeAttacksIncreased, AttackKind.Melee) => value,
                (BCardEffect.AttackPowerMeleeAttacksDecreased, AttackKind.Melee) => -value,
                (BCardEffect.AttackPowerRangedAttacksIncreased, AttackKind.Ranged) => value,
                (BCardEffect.AttackPowerRangedAttacksDecreased, AttackKind.Ranged) => -value,
                (BCardEffect.AttackPowerMagicalAttacksIncreased, AttackKind.Magical) => value,
                (BCardEffect.AttackPowerMagicalAttacksDecreased, AttackKind.Magical) => -value,
                _ => 0,
            };
        }

        return bonus;
    }

    // Selects which attacker profile (melee/ranged) and which defender stat applies
    // from the skill type. Returns all values by struct.
    private static AttackContext SelectAttackContext(CombatStats attacker, CombatStats defender, SkillInfo skill)
    {
        // Defaults point at the melee profile; overridden below per skill.Type.
        var ctx = new AttackContext
        {
            MainMinDmg = attacker.MinHit,
            MainMaxDmg = attacker.MaxHit,
            MainHitRate = attacker.HitRate,
            MainCritChance = attacker.CriticalChance,
            MainCritHit = attacker.CriticalRate,
            MainUpgrade = attacker.MeleeUpgrade,
            MainWeaponMin = attacker.WeaponMinHit,
            MainWeaponMax = attacker.WeaponMaxHit,
            MonsterDefence = defender.Defence,
            MonsterDodge = defender.DefenceDodge,
            Kind = AttackKind.Melee,
        };

        void UseRanged()
        {
            ctx.MainMinDmg = attacker.MinDistance;
            ctx.MainMaxDmg = attacker.MaxDistance;
            ctx.MainHitRate = attacker.DistanceRate;
            ctx.MainCritChance = attacker.DistanceCriticalChance;
            ctx.MainCritHit = attacker.DistanceCriticalRate;
            ctx.MainUpgrade = attacker.RangedUpgrade;
            ctx.MainWeaponMin = attacker.WeaponMinDistance;
            ctx.MainWeaponMax = attacker.WeaponMaxDistance;
        }

        switch (skill.Type)
        {
            case 0: // melee
                if (attacker.Class == CharacterClassType.Archer) UseRanged();
                ctx.MonsterDefence = defender.Defence;
                ctx.MonsterDodge = defender.DefenceDodge;
                break;
            case 1: // ranged
                if (attacker.Class != CharacterClassType.Archer) UseRanged();
                ctx.Kind = AttackKind.Ranged;
                ctx.MonsterDefence = defender.DistanceDefence;
                ctx.MonsterDodge = defender.DistanceDefenceDodge;
                break;
            case 2: // magic — no dodge, no crit (handled in caller)
                ctx.Kind = AttackKind.Magical;
                ctx.MonsterDefence = defender.MagicDefence;
                ctx.MonsterDodge = 0;
                break;
            case 3: // class-typed (self skills that scale off class)
            case 5:
                switch (attacker.Class)
                {
                    case CharacterClassType.Archer:
                        UseRanged();
                        ctx.Kind = AttackKind.Ranged;
                        ctx.MonsterDefence = defender.DistanceDefence;
                        ctx.MonsterDodge = defender.DistanceDefenceDodge;
                        break;
                    case CharacterClassType.Mage:
                        ctx.Kind = AttackKind.Magical;
                        ctx.MonsterDefence = defender.MagicDefence;
                        break;
                    default:
                        ctx.MonsterDefence = defender.Defence;
                        ctx.MonsterDodge = defender.DefenceDodge;
                        break;
                }
                break;
        }

        return ctx;
    }

    private int RollSigned(int min, int max) =>
        max <= min ? min : random.Next(min, max + 1);

    private int RollBaseDamage(int min, int max)
    {
        if (max <= min) return Math.Max(0, min);
        return random.Next(min, max + 1);
    }

    private static int Chebyshev(CombatStats a, CombatStats b)
    {
        _ = a; _ = b;
        // CombatStats doesn't carry position — distance is only used for the close-range
        // ranged penalty, and callers that care (BattleService) can compose a separate
        // check. We keep the method here to avoid changing the formula signature; the
        // in-scope check always returns "far" so the penalty never triggers spuriously.
        return int.MaxValue;
    }

    private static int ComputeElementalDamage(CombatStats attacker, CombatStats defender, SkillInfo skill, int baseDamage)
    {
        if (skill.Element == 0)
        {
            return 0;
        }
        if (skill.Element != attacker.Element)
        {
            return 0;
        }

        var attackElement = attacker.Element;
        var elementalBoost = ElementalBoost[Math.Min(attackElement, (byte)4), Math.Min(defender.Element, (byte)4)];

        // The defender's own resistance, plus what the attacker does to it (type 14). A
        // "reduces the enemy's fire resistance by 20" arrives as -20, so the two simply add.
        var monsterResistance = attackElement switch
        {
            1 => defender.FireResistance + attacker.EnemyFireResistance,
            2 => defender.WaterResistance + attacker.EnemyWaterResistance,
            3 => defender.LightResistance + attacker.EnemyLightResistance,
            4 => defender.DarkResistance + attacker.EnemyDarkResistance,
            _ => 0,
        };

        var elementalDamage = (baseDamage + 100) * ((attacker.ElementRate + attacker.ElementRateSp) / 100.0);
        elementalDamage = elementalDamage / 100.0 * (100 - monsterResistance) * elementalBoost;
        return (int)Math.Max(0, elementalDamage);
    }

    // Each "ignores ... resistance in PvP" option works one hit in four.
    private const int IgnoreResistanceChance = 25;

    private (CombatStats Attacker, CombatStats Defender) WithPvpShell(CombatStats attacker, CombatStats defender)
    {
        var shell = attacker.PvpShell;
        int Reduced(int defence) => defence * Math.Max(0, 100 - shell.EnemyDefenceReduction) / 100;
        int Resistance(int resistance, bool ignores) =>
            ignores && random.NextDouble() * 100 < IgnoreResistanceChance ? 0 : resistance;

        return (
            attacker with
            {
                EnemyFireResistance = attacker.EnemyFireResistance + shell.EnemyFireResistance,
                EnemyWaterResistance = attacker.EnemyWaterResistance + shell.EnemyWaterResistance,
                EnemyLightResistance = attacker.EnemyLightResistance + shell.EnemyLightResistance,
                EnemyDarkResistance = attacker.EnemyDarkResistance + shell.EnemyDarkResistance,
                GuaranteedHitChance = shell.AlwaysHits ? 100 : attacker.GuaranteedHitChance,
            },
            defender with
            {
                Defence = Reduced(defender.Defence),
                DistanceDefence = Reduced(defender.DistanceDefence),
                MagicDefence = Reduced(defender.MagicDefence),
                FireResistance = Resistance(defender.FireResistance, shell.IgnoresFireResistance),
                WaterResistance = Resistance(defender.WaterResistance, shell.IgnoresWaterResistance),
                LightResistance = Resistance(defender.LightResistance, shell.IgnoresLightResistance),
                DarkResistance = Resistance(defender.DarkResistance, shell.IgnoresDarkResistance),
            });
    }

    private struct AttackContext
    {
        public int MainMinDmg;
        public int MainMaxDmg;
        public int MainHitRate;
        public int MainCritChance;
        public int MainCritHit;
        public int MainUpgrade;
        public int MonsterDefence;
        public int MonsterDodge;
        public int MainWeaponMin;
        public int MainWeaponMax;
        public AttackKind Kind;
    }
}
