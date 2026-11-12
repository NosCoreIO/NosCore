//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // Type 10 subtypes 4 ("defence is increased by %s%%") and type 5 subtype 3 ("increases
    // damage with a probability of %s%% by %s%%"), the two lines of Ice Flower Oil.
    [TestClass]
    public class DefencePercentAndChanceDamageTests
    {
        private const short BaseDefence = 100;
        private const short BaseDistanceDefence = 200;
        private const short BaseMagicDefence = 300;

        private static BCardDto Card(BCardEffect effect, short first, short second = 0) =>
            new()
            {
                Type = effect.Type(),
                SubType = effect.SubType(),
                FirstData = first,
                SecondData = second
            };

        private static CombatStats StatsWith(params BCardDto[] bCards)
        {
            var monster = new NpcMonsterDto
            {
                CloseDefence = BaseDefence,
                DistanceDefence = BaseDistanceDefence,
                MagicDefence = BaseMagicDefence,
                DamageMinimum = 50,
                DamageMaximum = 50
            };

            var entity = new Mock<INonPlayableEntity>();
            entity.SetupGet(e => e.NpcMonster).Returns(monster);
            entity.SetupGet(e => e.Level).Returns((byte)1);
            entity.SetupGet(e => e.HeroLevel).Returns((byte)0);

            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>
            {
                new(CardId: 1, BuffType: BuffType.Good, Caster: null,
                    StartedAt: Instant.MinValue, ExpiresAt: Instant.MaxValue, BCards: bCards)
            });

            var equipment = new Mock<IEquipmentStatsService>();
            equipment.Setup(s => s.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            return BattleStats.Provider(buffs.Object, equipment.Object).GetStats(entity.Object);
        }

        [TestMethod]
        public void ADefencePercentRaisesEveryDefence()
        {
            var stats = StatsWith(Card(BCardEffect.DodgeAndDefencePercentDefenceIncreased, 5));

            Assert.AreEqual(105, stats.Defence);
            Assert.AreEqual(210, stats.DistanceDefence);
            Assert.AreEqual(315, stats.MagicDefence);
        }

        [TestMethod]
        public void ADefencePercentReductionLowersEveryDefence()
        {
            var stats = StatsWith(Card(BCardEffect.DodgeAndDefencePercentDefenceReduced, 10));

            Assert.AreEqual(90, stats.Defence);
            Assert.AreEqual(180, stats.DistanceDefence);
            Assert.AreEqual(270, stats.MagicDefence);
        }

        [TestMethod]
        public void AChanceDamageCardKeepsBothItsChanceAndItsPercent()
        {
            var stats = StatsWith(
                Card(BCardEffect.CriticalDamageIncreasingPropability, 100, 5),
                Card(BCardEffect.CriticalDamageReducingPropability, 30, 50));

            CollectionAssert.AreEqual(new[] { new ChanceDamage(100, 5), new ChanceDamage(30, -50) },
                new List<ChanceDamage>(stats.ChanceDamage ?? Array.Empty<ChanceDamage>()));
        }

        private static readonly SkillInfo Melee = new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0,
            TargetType: 0, Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        private static int Hit(double roll, params ChanceDamage[] chanceDamage)
        {
            var random = new Mock<IRandomProvider>();
            random.Setup(r => r.NextDouble()).Returns(roll);
            random.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns((int min, int _) => min);
            var attacker = new CombatStats
            {
                Level = 1,
                Class = CharacterClassType.Mage,
                MinHit = 1000,
                MaxHit = 1000,
                ChanceDamage = chanceDamage.Length == 0 ? null : chanceDamage
            };
            var defender = new CombatStats { Level = 1 };
            return new DamageCalculator(random.Object).Calculate(attacker, defender, Melee).Damage;
        }

        [TestMethod]
        public void ACertainDamageIncreaseAddsItsPercent()
        {
            var plain = Hit(0.99);

            Assert.AreEqual(plain * 105 / 100, Hit(0.99, new ChanceDamage(100, 5)));
        }

        [TestMethod]
        public void AChanceThatDoesNotRollLeavesTheDamage()
        {
            Assert.AreEqual(Hit(0.5), Hit(0.5, new ChanceDamage(30, 50)));
        }

        [TestMethod]
        public void AChanceThatRollsReducesTheDamage()
        {
            var plain = Hit(0.2);

            Assert.AreEqual(plain * 50 / 100, Hit(0.2, new ChanceDamage(30, -50)));
        }
    }
}
