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

        // OpenNos dodge formula peaks around multiplier=5 with ~80% dodge chance. With
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
            // Type 2 (magic) skill skips dodge entirely per OpenNos. Even with insane
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
            // Big armor, small hit — damage should floor (OpenNos randomises 1..5 when
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
        public void AdventurerGetsFlatDamageBonus()
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

            Assert.IsTrue(adv.Damage > sword.Damage, $"Adventurer bonus should beat Swordsman baseline: {adv.Damage} vs {sword.Damage}");
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
