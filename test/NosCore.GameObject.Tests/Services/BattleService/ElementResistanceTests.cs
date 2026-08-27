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
    // BCard type 13, "Changes elemental resistance". 561 declarations - 408 on items, 153 on
    // cards - and the fold did not have a case for it, so a resistance buff changed nothing.
    //
    // The four fields it feeds are read in ComputeElementalDamage as a percentage taken off the
    // incoming elemental damage, so getting them wrong moves numbers and raises nothing.
    [TestClass]
    public class ElementResistanceTests
    {
        private const short BaseFire = 10;

        private static BCardDto Resistance(BCardEffect effect, short value) =>
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
                FireResistance = BaseFire,
                WaterResistance = 0,
                LightResistance = 0,
                DarkResistance = 0
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
        public void AFireResistanceBuffAddsToTheMonstersOwn()
        {
            var stats = StatsWith(Resistance(BCardEffect.ElementResistanceFireIncreased, 25));

            Assert.AreEqual(BaseFire + 25, stats.FireResistance);
            Assert.AreEqual(0, stats.WaterResistance, "only fire was named");
        }

        // "All elemental resistance is increased by %s" reaches every one of the four. Keeping it
        // in a fifth field would mean remembering to add it at each of the four reads.
        [TestMethod]
        public void TheAllSubtypeReachesEveryElement()
        {
            var stats = StatsWith(Resistance(BCardEffect.ElementResistanceAllIncreased, 15));

            Assert.AreEqual(BaseFire + 15, stats.FireResistance);
            Assert.AreEqual(15, stats.WaterResistance);
            Assert.AreEqual(15, stats.LightResistance);
            Assert.AreEqual(15, stats.DarkResistance);
        }

        [TestMethod]
        public void AllAndOneElementAddUpOnThatElement()
        {
            var stats = StatsWith(
                Resistance(BCardEffect.ElementResistanceAllIncreased, 15),
                Resistance(BCardEffect.ElementResistanceWaterIncreased, 10));

            Assert.AreEqual(25, stats.WaterResistance);
            Assert.AreEqual(15, stats.LightResistance);
        }

        // The X1/X2 pairs of this type carry different sentences - "increased by" against
        // "decreased by" - so X2 really is the negation, unlike the types where the file repeats
        // the same line in both slots.
        [TestMethod]
        public void TheDecreasingSubtypeTakesAway()
        {
            var stats = StatsWith(Resistance(BCardEffect.ElementResistanceFireDecreased, 4));

            Assert.AreEqual(BaseFire - 4, stats.FireResistance);
        }

        [TestMethod]
        public void WithoutAnyOfTheseTheMonsterKeepsItsOwn()
        {
            var stats = StatsWith();

            Assert.AreEqual(BaseFire, stats.FireResistance);
            Assert.AreEqual(0, stats.DarkResistance);
        }
    }
}
