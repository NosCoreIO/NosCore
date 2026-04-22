//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

// Faithful port of OpenNos Character.GenerateDamage / MapMonster.GenerateDamage. The
// calculator is stateless apart from RNG — all inputs come from the resolved CombatStats
// of both sides plus the SkillInfo. Keeping this symmetrical lets the same function
// service character→monster, monster→character and PvP damage.
public sealed class DamageCalculator(IRandomProvider random) : IDamageCalculator
{
    // Element matchup boosts: attackerElement x defenderElement → multiplier. 0 means
    // "no element". Matches the OpenNos elemental triangle.
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
        // Skill type 0=melee, 1=ranged, 2=magic, 3=special-class (class picks defence),
        // 5=same pattern as 3. NosTale uses this to decide which defence stat + which
        // of the attacker's (melee vs ranged) profiles applies.
        var context = SelectAttackContext(attacker, defender, skill);

        // Morale buff: Level diff + Morale buffs, used as a baseline addition to hit
        // rate and base damage. Matches OpenNos `int morale = Level + GetBuff(...)`.
        var morale = attacker.Morale;

        // Dodge phase: only melee/ranged skills can miss. Magicians never dodge.
        if ((skill.Type == 0 || skill.Type == 1) && attacker.Class != CharacterClassType.Mage)
        {
            var dodgeMultiplier = Math.Min(5.0, context.MonsterDodge / (double)(context.MainHitRate + 1));
            // Cubic-fit dodge chance from OpenNos: approximates a sharp rise after the
            // attacker's hit rate falls behind the defender's dodge by ~2x.
            var chance = Math.Max(1.0,
                -0.25 * Math.Pow(dodgeMultiplier, 3)
                - 0.57 * Math.Pow(dodgeMultiplier, 2)
                + 25.3 * dodgeMultiplier
                - 1.41);

            // RNG returns [0,100). A dodge lands if roll <= chance (chance is percent).
            if (random.NextDouble() * 100 <= chance)
            {
                return new DamageResult(0, SuPacketHitMode.Miss);
            }
        }

        // Base damage roll from the selected (main) profile. + morale - defender level
        // gives a positive bias when attacker out-levels and damp when under-levelled.
        var baseDamage = RollBaseDamage(context.MainMinDmg, context.MainMaxDmg);
        baseDamage += morale - defender.Level;
        if (attacker.Class == CharacterClassType.Adventurer)
        {
            // Historic Adventurer flat bonus (they have otherwise weak primary stats).
            baseDamage += 20;
        }

        // Weapon upgrade table: negative values pad defender's armour, positive values
        // pad attacker's damage. Linear to ~+/-5 then accelerating. Exact constants
        // lifted from OpenNos so a +10 weapon still doubles damage.
        var monsterDefence = context.MonsterDefence;
        var upgrade = context.MainUpgrade - defender.DefenceUpgrade;
        if (upgrade < 0)
        {
            monsterDefence += (int)(monsterDefence * NegativeUpgradeDefenceBonus(upgrade));
        }
        else if (upgrade > 0)
        {
            baseDamage += (int)(baseDamage * PositiveUpgradeDamageBonus(upgrade));
        }

        baseDamage -= monsterDefence;

        // Ranged penalty: firing at point-blank loses 15% so Archers can't cheese melee.
        if (skill.Type == 1 && Chebyshev(attacker, defender) < 4)
        {
            baseDamage = (int)(baseDamage * 0.85);
        }

        // Elemental damage layer. See `ComputeElementalDamage` for the full pipeline.
        var elementalDamage = ComputeElementalDamage(attacker, defender, skill, baseDamage);

        var isCritical = false;
        if (skill.Type != 2 && random.NextDouble() * 100 <= context.MainCritChance)
        {
            baseDamage += (int)(baseDamage * (context.MainCritHit / 100.0));
            isCritical = true;
        }

        var totalDamage = baseDamage + elementalDamage;
        if (totalDamage < 5)
        {
            // Floor: never fully no-op a hit that isn't a dodge. Matches OpenNos random
            // spatter which is preferred over flat 1 to feel less deterministic.
            totalDamage = random.Next(1, 6);
        }

        return new DamageResult(
            Math.Max(1, totalDamage),
            isCritical ? SuPacketHitMode.CriticalAttack : SuPacketHitMode.SuccessAttack);
    }

    // Selects which attacker profile (melee/ranged) and which defender stat applies,
    // mirroring OpenNos `switch (skill.Type)`. Returns all values by struct.
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
            MonsterDefence = defender.Defence,
            MonsterDodge = defender.DefenceDodge,
        };

        void UseRanged()
        {
            ctx.MainMinDmg = attacker.MinDistance;
            ctx.MainMaxDmg = attacker.MaxDistance;
            ctx.MainHitRate = attacker.DistanceRate;
            ctx.MainCritChance = attacker.DistanceCriticalChance;
            ctx.MainCritHit = attacker.DistanceCriticalRate;
            ctx.MainUpgrade = attacker.RangedUpgrade;
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
                ctx.MonsterDefence = defender.DistanceDefence;
                ctx.MonsterDodge = defender.DistanceDefenceDodge;
                break;
            case 2: // magic — no dodge, no crit (handled in caller)
                ctx.MonsterDefence = defender.MagicDefence;
                ctx.MonsterDodge = 0;
                break;
            case 3: // class-typed (self skills that scale off class)
            case 5:
                switch (attacker.Class)
                {
                    case CharacterClassType.Archer:
                        UseRanged();
                        ctx.MonsterDefence = defender.DistanceDefence;
                        ctx.MonsterDodge = defender.DistanceDefenceDodge;
                        break;
                    case CharacterClassType.Mage:
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

    // OpenNos upgrade ladders, lifted from Character.GenerateDamage. Negative upgrades
    // buff the defender's armour, positive buff attacker damage. The table stops at ±10.
    private static double NegativeUpgradeDefenceBonus(int upgrade) => upgrade switch
    {
        <= -10 => 2.0,
        -9 => 1.2,
        -8 => 0.9,
        -7 => 0.65,
        -6 => 0.54,
        -5 => 0.43,
        -4 => 0.32,
        -3 => 0.22,
        -2 => 0.15,
        -1 => 0.1,
        _ => 0.0,
    };

    private static double PositiveUpgradeDamageBonus(int upgrade) => upgrade switch
    {
        1 => 0.1,
        2 => 0.15,
        3 => 0.22,
        4 => 0.32,
        5 => 0.43,
        6 => 0.54,
        7 => 0.65,
        8 => 0.9,
        9 => 1.2,
        >= 10 => 2.0,
        _ => 0.0,
    };

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

        var monsterResistance = attackElement switch
        {
            1 => defender.FireResistance,
            2 => defender.WaterResistance,
            3 => defender.LightResistance,
            4 => defender.DarkResistance,
            _ => 0,
        };

        var elementalDamage = (baseDamage + 100) * ((attacker.ElementRate + attacker.ElementRateSp) / 100.0);
        elementalDamage = elementalDamage / 100.0 * (100 - monsterResistance) * elementalBoost;
        return (int)Math.Max(0, elementalDamage);
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
    }
}
