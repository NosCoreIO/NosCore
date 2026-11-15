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
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class DefenceNullifiedTests
    {
        private static BCardDto Card(BCardEffect effect, short first = 0) =>
            new() { Type = effect.Type(), SubType = effect.SubType(), FirstData = first };

        private static CombatStats StatsWith(params BCardDto[] bCards)
        {
            var monster = new NpcMonsterDto
            {
                CloseDefence = 100,
                DistanceDefence = 200,
                MagicDefence = 300,
                DefenceDodge = 40,
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
                new(CardId: 69, BuffType: BuffType.Bad, Caster: null,
                    StartedAt: Instant.MinValue, ExpiresAt: Instant.MaxValue, BCards: bCards)
            });

            var equipment = new Mock<IEquipmentStatsService>();
            equipment.Setup(s => s.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            return BattleStats.Provider(buffs.Object, equipment.Object).GetStats(entity.Object);
        }

        [TestMethod]
        public void AllDefenceNullifiedRemovesEveryDefence()
        {
            var stats = StatsWith(Card(BCardEffect.SpecialDefenceAllDefenceNullified));

            Assert.AreEqual(0, stats.Defence);
            Assert.AreEqual(0, stats.DistanceDefence);
            Assert.AreEqual(0, stats.MagicDefence);
        }

        [TestMethod]
        public void AllDefenceNullifiedOutweighsADefenceBonus()
        {
            var stats = StatsWith(
                Card(BCardEffect.SpecialDefenceAllDefenceNullified),
                Card(BCardEffect.DodgeAndDefencePercentDefenceIncreased, 50));

            Assert.AreEqual(0, stats.Defence);
        }

        [TestMethod]
        public void AllDefenceNullifiedLeavesDodgeAlone()
        {
            var stats = StatsWith(Card(BCardEffect.SpecialDefenceAllDefenceNullified));

            Assert.AreEqual(40, stats.DefenceDodge);
        }

        [TestMethod]
        public void WithoutTheEffectDefenceStays()
        {
            var stats = StatsWith();

            Assert.AreEqual(100, stats.Defence);
        }
    }
}
