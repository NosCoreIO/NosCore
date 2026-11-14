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
    public class PvpAttackDefenceTests
    {
        private static BCardDto Card(BCardEffect effect, short first) =>
            new() { Type = effect.Type(), SubType = effect.SubType(), FirstData = first };

        private static CombatStats StatsWith(params BCardDto[] bCards)
        {
            var entity = new Mock<INonPlayableEntity>();
            entity.SetupGet(e => e.NpcMonster).Returns(new NpcMonsterDto());
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
        public void PvpBonusesAreSummed()
        {
            var stats = StatsWith(
                Card(BCardEffect.LeonaPassiveSkillAttackIncreasedInPvp, 10),
                Card(BCardEffect.LeonaPassiveSkillAttackDecreasedInPvp, 3),
                Card(BCardEffect.LeonaPassiveSkillDefenceIncreasedInPvp, 8));

            Assert.AreEqual(7, stats.PvpAttackPercent);
            Assert.AreEqual(8, stats.PvpDefencePercent);
        }

        [TestMethod]
        public void WithoutBonusesPvpDamageIsHalved()
        {
            Assert.AreEqual(500, HitQueue.PvpDamage(1000, StatsWith(), StatsWith()));
        }

        [TestMethod]
        public void PvpAttackRaisesTheDamageDealt()
        {
            var attacker = StatsWith(Card(BCardEffect.LeonaPassiveSkillAttackIncreasedInPvp, 20));

            Assert.AreEqual(600, HitQueue.PvpDamage(1000, attacker, StatsWith()));
        }

        [TestMethod]
        public void PvpDefenceLowersTheDamageTaken()
        {
            var defender = StatsWith(Card(BCardEffect.LeonaPassiveSkillDefenceIncreasedInPvp, 20));

            Assert.AreEqual(400, HitQueue.PvpDamage(1000, StatsWith(), defender));
        }

        [TestMethod]
        public void PvpDefenceIsCapped()
        {
            var defender = StatsWith(Card(BCardEffect.LeonaPassiveSkillDefenceIncreasedInPvp, 100));

            Assert.AreEqual(125, HitQueue.PvpDamage(1000, StatsWith(), defender));
        }
    }
}
