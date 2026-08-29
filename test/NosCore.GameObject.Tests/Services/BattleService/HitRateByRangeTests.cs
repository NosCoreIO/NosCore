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
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class HitRateByRangeTests
    {
        private const int BaseHitRate = 40;
        private const int BaseDistanceRate = 70;

        private static BCardDto Target(BCardEffect effect, short value) =>
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
                Concentrate = BaseHitRate,
                DistanceDefence = BaseDistanceRate
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

            return new BattleStatsProvider(buffs.Object, equipment.Object).GetStats(entity.Object);
        }

        [TestMethod]
        public void AMeleeHitRateBonusLeavesTheRangedRateAlone()
        {
            var baseline = StatsWith();
            var stats = StatsWith(Target(BCardEffect.TargetMeleeHitRateIncreased, 25));

            Assert.AreEqual(baseline.HitRate + 25, stats.HitRate);
            Assert.AreEqual(baseline.DistanceRate, stats.DistanceRate);
        }

        [TestMethod]
        public void ARangedHitRateBonusLeavesTheMeleeRateAlone()
        {
            var baseline = StatsWith();
            var stats = StatsWith(Target(BCardEffect.TargetRangedHitRateIncreased, 25));

            Assert.AreEqual(baseline.DistanceRate + 25, stats.DistanceRate);
            Assert.AreEqual(baseline.HitRate, stats.HitRate);
        }

        [TestMethod]
        public void TheAllSubtypeStillFeedsBothRates()
        {
            var baseline = StatsWith();
            var stats = StatsWith(Target(BCardEffect.TargetAllHitRateIncreased, 25));

            Assert.AreEqual(baseline.HitRate + 25, stats.HitRate);
            Assert.AreEqual(baseline.DistanceRate + 25, stats.DistanceRate);
        }

        [TestMethod]
        public void TheDecreasingHalvesSubtract()
        {
            var baseline = StatsWith();
            var stats = StatsWith(
                Target(BCardEffect.TargetMeleeHitRateDecreased, 10),
                Target(BCardEffect.TargetRangedHitRateDecreased, 15));

            Assert.AreEqual(baseline.HitRate - 10, stats.HitRate);
            Assert.AreEqual(baseline.DistanceRate - 15, stats.DistanceRate);
        }

        [TestMethod]
        public void AllAndMeleeStack()
        {
            var baseline = StatsWith();
            var stats = StatsWith(
                Target(BCardEffect.TargetAllHitRateIncreased, 10),
                Target(BCardEffect.TargetMeleeHitRateIncreased, 5));

            Assert.AreEqual(baseline.HitRate + 15, stats.HitRate);
            Assert.AreEqual(baseline.DistanceRate + 10, stats.DistanceRate);
        }
    }
}
