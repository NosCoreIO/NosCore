//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // BCard types 34 and 35 - "attack power is multiplied by %s", "defence is multiplied by
    // %s". They are the only two families that state a factor rather than a percentage, their
    // values are small whole numbers, and neither was folded in: a card promising a fivefold
    // defence changed nothing.
    //
    // Nothing raises when a factor is read as a percentage either, which is why the fivefold
    // and the halving are both held here.
    [TestClass]
    public class MultiplyingStatTests
    {
        private const short BaseDefence = 100;
        private const short BaseDistanceDefence = 200;
        private const short BaseMagicDefence = 300;
        private const short BaseDamage = 50;

        private static BCardDto Card(BCardEffect effect, short value) =>
            new()
            {
                Type = effect.Type(),
                SubType = effect.SubType(),
                FirstData = value
            };

        private static CombatStats StatsWith(params BCardDto[] bCards)
        {
            var monster = new NpcMonsterDto
            {
                CloseDefence = BaseDefence,
                DistanceDefence = BaseDistanceDefence,
                MagicDefence = BaseMagicDefence,
                DamageMinimum = BaseDamage,
                DamageMaximum = BaseDamage
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

            return new BattleStatsProvider(buffs.Object, NoEquipment()).GetStats(entity.Object);
        }

        private static IEquipmentStatsService NoEquipment()
        {
            var equipment = new Mock<IEquipmentStatsService>();
            equipment.Setup(s => s.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            return equipment.Object;
        }

        [TestMethod]
        public void WithoutAFactorEveryDefenceStaysAsItIs()
        {
            var stats = StatsWith();

            Assert.AreEqual(BaseDefence, stats.Defence);
            Assert.AreEqual(BaseDistanceDefence, stats.DistanceDefence);
            Assert.AreEqual(BaseMagicDefence, stats.MagicDefence);
        }

        [TestMethod]
        public void TheAllSubtypeMultipliesEveryDefence()
        {
            var stats = StatsWith(Card(BCardEffect.MultDefenceAllDefenceIncreased, 3));

            Assert.AreEqual(BaseDefence * 3, stats.Defence);
            Assert.AreEqual(BaseDistanceDefence * 3, stats.DistanceDefence);
            Assert.AreEqual(BaseMagicDefence * 3, stats.MagicDefence);
        }

        [TestMethod]
        public void AMeleeFactorLeavesTheOtherTwoAlone()
        {
            var stats = StatsWith(Card(BCardEffect.MultDefenceMeleeDefenceIncreased, 5));

            Assert.AreEqual(BaseDefence * 5, stats.Defence);
            Assert.AreEqual(BaseDistanceDefence, stats.DistanceDefence);
            Assert.AreEqual(BaseMagicDefence, stats.MagicDefence);
        }

        [TestMethod]
        public void ARangedFactorReachesOnlyRangedDefence()
        {
            var stats = StatsWith(Card(BCardEffect.MultDefenceRangedDefenceIncreased, 5));

            Assert.AreEqual(BaseDefence, stats.Defence);
            Assert.AreEqual(BaseDistanceDefence * 5, stats.DistanceDefence);
        }

        [TestMethod]
        public void AMagicFactorReachesOnlyMagicDefence()
        {
            var stats = StatsWith(Card(BCardEffect.MultDefenceMagicalDefenceIncreased, 5));

            Assert.AreEqual(BaseDefence, stats.Defence);
            Assert.AreEqual(BaseMagicDefence * 5, stats.MagicDefence);
        }

        // The decreasing half of both types says "divided by", so it halves rather than taking
        // two away.
        [TestMethod]
        public void TheDecreasingHalfDivides()
        {
            var stats = StatsWith(Card(BCardEffect.MultDefenceAllDefenceDecreased, 2));

            Assert.AreEqual(BaseDefence / 2, stats.Defence);
            Assert.AreEqual(BaseMagicDefence / 2, stats.MagicDefence);
        }

        [TestMethod]
        public void AnAttackFactorMultipliesBothEndsOfTheMeleeRoll()
        {
            var stats = StatsWith(Card(BCardEffect.MultAttackAllAttackIncreased, 3));

            Assert.AreEqual(BaseDamage * 3, stats.MinHit);
            Assert.AreEqual(BaseDamage * 3, stats.MaxHit);
        }

        [TestMethod]
        public void AMeleeAttackFactorDoesNotTouchTheRangedRoll()
        {
            var stats = StatsWith(Card(BCardEffect.MultAttackMeleeAttackIncreased, 2));

            Assert.AreEqual(BaseDamage * 2, stats.MaxHit);
            Assert.AreEqual(BaseDamage, stats.MaxDistance);
        }

        // A factor of one is the identity in both directions, and a decrease of one must not
        // wipe the stat out.
        [TestMethod]
        public void AFactorOfOneChangesNothingEitherWay()
        {
            Assert.AreEqual(BaseDefence,
                StatsWith(Card(BCardEffect.MultDefenceAllDefenceIncreased, 1)).Defence);
            Assert.AreEqual(BaseDefence,
                StatsWith(Card(BCardEffect.MultDefenceAllDefenceDecreased, 1)).Defence);
        }

        // "All" and the per-kind subtype are one factor between them, the way the flat halves
        // of types 3 and 9 are one amount.
        [TestMethod]
        public void AllAndAKindAddUpIntoOneFactor()
        {
            var stats = StatsWith(
                Card(BCardEffect.MultDefenceAllDefenceIncreased, 2),
                Card(BCardEffect.MultDefenceMeleeDefenceIncreased, 3));

            Assert.AreEqual(BaseDefence * 5, stats.Defence);
            Assert.AreEqual(BaseDistanceDefence * 2, stats.DistanceDefence);
        }
    }
}
