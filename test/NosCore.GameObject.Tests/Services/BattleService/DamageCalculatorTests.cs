//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class DamageCalculatorTests
    {
        private static readonly SkillInfo MeleeSkill = new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0, TargetType: 0,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        private static readonly SkillInfo MagicSkill = MeleeSkill with { Type = 2 };

        private static Mock<IRandomProvider> Rng(double[] doubles, int[]? ints = null)
        {
            var dq = new Queue<double>(doubles);
            var iq = ints == null ? new Queue<int>() : new Queue<int>(ints);
            var mock = new Mock<IRandomProvider>();
            mock.Setup(r => r.NextDouble()).Returns(() => dq.Count > 0 ? dq.Dequeue() : 0.5);
            mock.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>()))
                .Returns<int, int>((lo, _) => iq.Count > 0 ? iq.Dequeue() : lo);
            return mock;
        }

        // The dodge formula peaks around multiplier=5 with ~80% dodge chance. With
        // attacker HitRate 0 and defender Dodge 100 → multiplier = 100 / 1 = 100 → clamp 5
        // → chance ≈ 79.6%. Forcing RNG to 0 (roll below chance) produces a miss.
        [TestMethod]
        public void MissWhenDodgeRollLanding()
        {
            var calc = new DamageCalculator(Rng(doubles: new[] { 0.0 }).Object); // 0 * 100 = 0, <= 79 = dodge
            var attacker = StatsBuilder.Default() with { HitRate = 0, Class = CharacterClassType.Swordsman };
            var defender = StatsBuilder.Default() with { DefenceDodge = 100 };

            var result = calc.Calculate(attacker, defender, MeleeSkill);

            Assert.AreEqual(SuPacketHitMode.Miss, result.HitMode);
            Assert.AreEqual(0, result.Damage);
        }

        [TestMethod]
        public void MageSkillsIgnoreDodge()
        {
            // Type 2 (magic) skill skips dodge entirely. Even with insane
            // defender dodge the hit lands.
            var calc = new DamageCalculator(Rng(doubles: new[] { 0.0, 1.0 }, ints: new[] { 100 }).Object);
            var attacker = StatsBuilder.Default() with { Class = CharacterClassType.Mage, MinHit = 100, MaxHit = 100 };
            var defender = StatsBuilder.Default() with { DefenceDodge = 999 };

            var result = calc.Calculate(attacker, defender, MagicSkill);

            Assert.AreNotEqual(SuPacketHitMode.Miss, result.HitMode);
            Assert.IsTrue(result.Damage > 0);
        }

        [TestMethod]
        public void DamageFlooredWhenDefenceExceedsAttack()
        {
            // Big armor, small hit — damage should floor (it is randomised in 1..5 when
            // total < 5 so we can't assert exact, but it must be positive).
            var calc = new DamageCalculator(Rng(doubles: new[] { 1.0, 1.0 }, ints: new[] { 50, 3 }).Object);
            var attacker = StatsBuilder.Default() with { HitRate = 100, MinHit = 50, MaxHit = 50 };
            var defender = StatsBuilder.Default() with { Defence = 9999, DefenceDodge = 0 };

            var result = calc.Calculate(attacker, defender, MeleeSkill);

            Assert.IsTrue(result.Damage >= 1, "damage should floor to at least 1");
        }

        [TestMethod]
        public void CriticalBoostsDamage()
        {
            // Hit roll 1.0 (no dodge), crit roll 0.0 (crit lands, critChance=100).
            var calc = new DamageCalculator(Rng(doubles: new[] { 1.0, 0.0 }, ints: new[] { 100 }).Object);
            var attacker = StatsBuilder.Default() with
            {
                HitRate = 100, MinHit = 100, MaxHit = 100,
                CriticalChance = 100, CriticalRate = 200,
            };
            var defender = StatsBuilder.Default();

            var result = calc.Calculate(attacker, defender, MeleeSkill);

            Assert.AreEqual(SuPacketHitMode.CriticalAttack, result.HitMode);
            Assert.IsTrue(result.Damage >= 150, $"expected ≥150, got {result.Damage}");
        }

        // A defender's critical damage decrease shrinks the critical bonus, not the whole hit.
        [TestMethod]
        public void ADefendersCriticalDamageDecreaseShrinksTheCriticalBonus()
        {
            var attacker = StatsBuilder.Default() with
            {
                HitRate = 100, MinHit = 100, MaxHit = 100,
                CriticalChance = 100, CriticalRate = 200,
            };

            var full = new DamageCalculator(Rng(doubles: new[] { 1.0, 0.0 }, ints: new[] { 100 }).Object)
                .Calculate(attacker, StatsBuilder.Default(), MeleeSkill);
            var halved = new DamageCalculator(Rng(doubles: new[] { 1.0, 0.0 }, ints: new[] { 100 }).Object)
                .Calculate(attacker, StatsBuilder.Default() with { CriticalDamageReceived = -50 }, MeleeSkill);

            var baseDamage = full.Damage / 3;
            Assert.AreEqual(SuPacketHitMode.CriticalAttack, halved.HitMode);
            Assert.AreEqual(baseDamage * 2, halved.Damage);
        }

        [TestMethod]
        public void MoraleAddsFlatDamage()
        {
            var calc = new DamageCalculator(Rng(doubles: new[] { 1.0, 1.0 }, ints: new[] { 10 }).Object);
            var without = calc.Calculate(
                StatsBuilder.Default() with { HitRate = 100, MinHit = 10, MaxHit = 10, Morale = 10 },
                StatsBuilder.Default() with { Level = 0 },
                MeleeSkill);

            var calc2 = new DamageCalculator(Rng(doubles: new[] { 1.0, 1.0 }, ints: new[] { 10 }).Object);
            var with = calc2.Calculate(
                StatsBuilder.Default() with { HitRate = 100, MinHit = 10, MaxHit = 10, Morale = 50 },
                StatsBuilder.Default() with { Level = 0 },
                MeleeSkill);

            Assert.IsTrue(with.Damage > without.Damage, $"higher morale should deal more damage: {with.Damage} vs {without.Damage}");
        }

        [TestMethod]
        public void ElementBonusAddsDamage()
        {
            var baseCalc = new DamageCalculator(Rng(doubles: new[] { 1.0, 1.0 }, ints: new[] { 100 }).Object);
            var elementCalc = new DamageCalculator(Rng(doubles: new[] { 1.0, 1.0 }, ints: new[] { 100 }).Object);
            var skill = MeleeSkill with { Element = 1 };

            var baseline = baseCalc.Calculate(
                StatsBuilder.Default() with { HitRate = 100, MinHit = 100, MaxHit = 100, Element = 0, ElementRate = 0 },
                StatsBuilder.Default(),
                skill with { Element = 0 });

            var fire = elementCalc.Calculate(
                StatsBuilder.Default() with { HitRate = 100, MinHit = 100, MaxHit = 100, Element = 1, ElementRate = 100 },
                StatsBuilder.Default() with { Element = 2 /*water: weak to fire*/ },
                skill);

            Assert.IsTrue(fire.Damage > baseline.Damage, $"fire vs water should exceed neutral: {fire.Damage} vs {baseline.Damage}");
        }

        [TestMethod]
        public void AMonstersHitGetsNoFlatBonusFromItsClass()
        {
            var advCalc = new DamageCalculator(Rng(doubles: new[] { 1.0, 1.0 }, ints: new[] { 10 }).Object);
            var otherCalc = new DamageCalculator(Rng(doubles: new[] { 1.0, 1.0 }, ints: new[] { 10 }).Object);

            var adv = advCalc.Calculate(
                StatsBuilder.Default() with { HitRate = 100, MinHit = 10, MaxHit = 10, Class = CharacterClassType.Adventurer },
                StatsBuilder.Default(),
                MeleeSkill);
            var sword = otherCalc.Calculate(
                StatsBuilder.Default() with { HitRate = 100, MinHit = 10, MaxHit = 10, Class = CharacterClassType.Swordsman },
                StatsBuilder.Default(),
                MeleeSkill);

            Assert.AreEqual(sword.Damage, adv.Damage);
        }

        // A level 1 chicken: close defence 18, ranged 17, magic 13, no dodge.
        private static CombatStats Chicken() => StatsBuilder.Default() with
        {
            Level = 1, Morale = 1, Defence = 18, DistanceDefence = 17, MagicDefence = 13,
            DefenceDodge = 0, DistanceDefenceDodge = 0,
        };

        private static CombatStats Player(CharacterClassType cls, int level, int attack, int weapon = 0, int upgrade = 0) =>
            StatsBuilder.Default() with
            {
                IsPlayer = true, Class = cls, Level = level, Morale = level, HitRate = 999,
                MinHit = attack + weapon, MaxHit = attack + weapon, WeaponMinHit = weapon, WeaponMaxHit = weapon,
                MeleeUpgrade = upgrade,
            };

        private static int Hit(CombatStats attacker, CombatStats defender, SkillInfo skill, double roll = 0.5) =>
            new DamageCalculator(Rng(doubles: new[] { roll, roll }).Object).Calculate(attacker, defender, skill).Damage;

        // A naked level 10 swordsman's basic attack does 32 to a chicken: 26 + 15 - 18 + (10 - 1).
        [TestMethod]
        public void APlayersAttackIsItsClassTableAndFifteenLessDefencePlusTheLevelGap()
        {
            Assert.AreEqual(32, Hit(Player(CharacterClassType.Swordsman, 10, attack: 26), Chicken(), MeleeSkill));
        }

        [TestMethod]
        public void AnAdventurerGetsNoExtraFlatDamage()
        {
            Assert.AreEqual(25, Hit(Player(CharacterClassType.Adventurer, 10, attack: 19), Chicken(), MeleeSkill));
        }

        // The mage's basic attack carries "magic attack +60" of its own: 26 + 15 + 60 - 13 + 9.
        [TestMethod]
        public void ASkillsOwnAttackBonusAddsToTheMatchingAttack()
        {
            var basicMagic = MagicSkill with
            {
                BCards = new[] { new BCardDto { Type = 3, SubType = 41, FirstData = 60 } },
            };

            Assert.AreEqual(97, Hit(Player(CharacterClassType.Mage, 10, attack: 26), Chicken(), basicMagic));
            Assert.AreEqual(32, Hit(Player(CharacterClassType.Swordsman, 10, attack: 26), Chicken(),
                MeleeSkill with { BCards = basicMagic.BCards }));
        }

        // Level 30 swordsman, 100 weapon attack at +5: the upgrade adds 43% of the weapon only.
        [TestMethod]
        public void AWeaponUpgradeScalesOnlyTheWeaponsAttack()
        {
            Assert.AreEqual(188, Hit(Player(CharacterClassType.Swordsman, 30, attack: 62, weapon: 100), Chicken(), MeleeSkill));
            Assert.AreEqual(231, Hit(Player(CharacterClassType.Swordsman, 30, attack: 62, weapon: 100, upgrade: 5), Chicken(), MeleeSkill));
        }

        // An attack decrease of 86 on 62 + 100 leaves the rest at -24, which still counts: 76 + 15 - 18 + 29.
        [TestMethod]
        public void AnAttackDecreaseLargerThanTheClassAttackStillCounts()
        {
            Assert.AreEqual(102, Hit(Player(CharacterClassType.Swordsman, 30, attack: -24, weapon: 100), Chicken(), MeleeSkill));
        }

        // An archer's melee skill swings the secondary weapon, but melee bonuses still apply: 26 + 15 + 20 - 18 + 9.
        [TestMethod]
        public void AnArchersMeleeSkillTakesTheMeleeAttackBonus()
        {
            var archer = StatsBuilder.Default() with
            {
                IsPlayer = true, Class = CharacterClassType.Archer, Level = 10, Morale = 10, HitRate = 999,
                DistanceRate = 999, MinDistance = 26, MaxDistance = 26,
            };
            var meleeBonus = MeleeSkill with
            {
                BCards = new[] { new BCardDto { Type = 3, SubType = 21, FirstData = 20 } },
            };

            Assert.AreEqual(52, Hit(archer, Chicken(), meleeBonus));
        }

        // A 70% critical adds 70% of attack less defence, 159, and not of the 29 level gap.
        [TestMethod]
        public void ACriticalHitLeavesTheLevelGapOutOfItsBonus()
        {
            var attacker = Player(CharacterClassType.Swordsman, 30, attack: 62, weapon: 100) with
            {
                CriticalChance = 100, CriticalRate = 70,
            };

            Assert.AreEqual(299, Hit(attacker, Chicken(), MeleeSkill, roll: 0.5));
        }

        private static class StatsBuilder
        {
            // Sane defaults that produce a hit at parity under the cubic dodge curve
            // (multiplier ≈ 1 → 23% dodge). Per test overrides with `with { ... }`.
            public static CombatStats Default() => new(
                Level: 10, HeroLevel: 0, Class: CharacterClassType.Adventurer, Morale: 10,
                MinHit: 10, MaxHit: 10, HitRate: 50, CriticalChance: 0, CriticalRate: 0, MeleeUpgrade: 0,
                MinDistance: 10, MaxDistance: 10, DistanceRate: 50, DistanceCriticalChance: 0, DistanceCriticalRate: 0, RangedUpgrade: 0,
                Element: 0, ElementRate: 0, ElementRateSp: 0,
                Defence: 10, DefenceRate: 10, DistanceDefence: 10, DistanceDefenceRate: 10, MagicDefence: 10,
                DefenceDodge: 50, DistanceDefenceDodge: 50, DefenceUpgrade: 0,
                FireResistance: 0, WaterResistance: 0, LightResistance: 0, DarkResistance: 0);
        }
    }
}
