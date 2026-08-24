//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Data.Enumerations.Buff;
using NodaTime;
using System.Collections.Generic;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // BCard type 14, "Changes enemy's resistances": 210 declarations, and the mirror of type 13.
    // That one is the resistance of whoever is being hit; this is what the one hitting does to
    // it, so it is carried by the attacker and read against the defender's own number.
    //
    // Getting the sign backwards would make resistance-piercing effects protect the target
    // instead, and nothing would report it - the fight goes on, the numbers are just wrong.
    [TestClass]
    public class EnemyElementResistanceTests
    {
        private static DamageCalculator Calculator()
        {
            var random = new Mock<IRandomProvider>();
            // High roll: the ordinary dodge chance has a floor of 1%, so a low one would make
            // every attack miss and there would be no damage to compare.
            random.Setup(r => r.NextDouble()).Returns(0.99);
            random.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(0);
            return new DamageCalculator(random.Object);
        }

        // Element 1 is fire on both sides, so the elemental step actually runs.
        private static CombatStats Attacker(int enemyFireResistance = 0) => new()
        {
            Level = 50,
            Class = CharacterClassType.Swordsman,
            MinHit = 200,
            MaxHit = 200,
            HitRate = 5000,
            Morale = 50,
            Element = 1,
            ElementRate = 100,
            EnemyFireResistance = enemyFireResistance
        };

        private static CombatStats Defender(int fireResistance) => new()
        {
            Level = 1,
            Element = 2,
            FireResistance = fireResistance
        };

        // The skill has to declare the element too, and the same one as the attacker:
        // ComputeElementalDamage returns zero otherwise, and every number below would be
        // identical for a reason that has nothing to do with resistances.
        private static SkillInfo Melee => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0,
            TargetType: 0, Element: 1, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        [TestMethod]
        public void PiercingTheResistanceDoesMoreDamageThanNotPiercingIt()
        {
            var calculator = Calculator();

            var plain = calculator.Calculate(Attacker(), Defender(50), Melee).Damage;
            var piercing = calculator.Calculate(Attacker(enemyFireResistance: -30), Defender(50), Melee).Damage;

            Assert.IsTrue(piercing > plain,
                $"piercing 30 of the 50 should hurt more: {piercing} against {plain}");
        }

        // The sign is the whole point. A positive value raises the enemy's resistance, which is
        // what subtypes 11 to 51 say, and it must not be read as piercing.
        [TestMethod]
        public void RaisingTheEnemysResistanceDoesLessDamage()
        {
            var calculator = Calculator();

            var plain = calculator.Calculate(Attacker(), Defender(20), Melee).Damage;
            var hardened = calculator.Calculate(Attacker(enemyFireResistance: 30), Defender(20), Melee).Damage;

            Assert.IsTrue(hardened < plain,
                $"raising it by 30 should hurt less: {hardened} against {plain}");
        }

        // It adds to the defender's own rather than replacing it: a defender with no resistance
        // and an attacker that pierces nothing must land exactly the same blow either way.
        [TestMethod]
        public void WithNeitherSideDeclaringAnythingNothingChanges()
        {
            var calculator = Calculator();

            var withoutTheField = calculator.Calculate(Attacker(), Defender(0), Melee).Damage;
            var withZero = calculator.Calculate(Attacker(enemyFireResistance: 0), Defender(0), Melee).Damage;

            Assert.AreEqual(withoutTheField, withZero);
        }

        // Only the attacker's own element is looked up, so a water-piercing effect leaves a fire
        // attack alone.
        // --- the fold, not only what reads it -------------------------------------------------
        //
        // The four tests above set the field by hand, so they say nothing about the step that
        // fills it: folding the water value into the fire field would leave them all green.

        private static CombatStats FoldOf(AdditionalTypes.EnemyElementResistance subType, short value)
        {
            var monster = new NpcMonsterDto();
            var entity = new Mock<INonPlayableEntity>();
            entity.SetupGet(e => e.NpcMonster).Returns(monster);
            entity.SetupGet(e => e.Level).Returns((byte)1);
            entity.SetupGet(e => e.HeroLevel).Returns((byte)0);

            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>
            {
                new(CardId: 1, BuffType: BuffType.Bad, Caster: null,
                    StartedAt: Instant.MinValue, ExpiresAt: Instant.MaxValue,
                    BCards: new[]
                    {
                        new BCardDto
                        {
                            Type = (byte)BCardType.CardType.EnemyElementResistance,
                            SubType = (byte)subType,
                            FirstData = value
                        }
                    })
            });

            return new BattleStatsProvider(buffs.Object).GetStats(entity.Object);
        }

        [TestMethod]
        public void EachElementLandsInItsOwnField()
        {
            var fire = FoldOf(AdditionalTypes.EnemyElementResistance.FireDecreased, 20);

            Assert.AreEqual(-20, fire.EnemyFireResistance);
            Assert.AreEqual(0, fire.EnemyWaterResistance, "only fire was named");
            Assert.AreEqual(0, fire.EnemyLightResistance);
            Assert.AreEqual(0, fire.EnemyDarkResistance);
        }

        [TestMethod]
        public void TheAllSubtypeReachesEveryElement()
        {
            var all = FoldOf(AdditionalTypes.EnemyElementResistance.AllDecreased, 15);

            Assert.AreEqual(-15, all.EnemyFireResistance);
            Assert.AreEqual(-15, all.EnemyWaterResistance);
            Assert.AreEqual(-15, all.EnemyLightResistance);
            Assert.AreEqual(-15, all.EnemyDarkResistance);
        }

        [TestMethod]
        public void TheIncreasingSubtypeKeepsItsSign()
        {
            var hardened = FoldOf(AdditionalTypes.EnemyElementResistance.WaterIncreased, 12);

            Assert.AreEqual(12, hardened.EnemyWaterResistance);
        }

        [TestMethod]
        public void OnlyTheElementBeingAttackedWithIsRead()
        {
            var calculator = Calculator();

            var plain = calculator.Calculate(Attacker(), Defender(50), Melee).Damage;
            var wrongElement = calculator.Calculate(
                Attacker() with { EnemyWaterResistance = -50 }, Defender(50), Melee).Damage;

            Assert.AreEqual(plain, wrongElement);
        }
    }
}
