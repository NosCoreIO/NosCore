//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class MonsterPassiveBCardTests
    {
        private const short Vnum = 1490;

        [TestMethod]
        public void AMonstersBasicCardsApplyAndItsTriggeredCardsDoNot()
        {
            var stats = StatsOf(
                Card(BCardEffect.DamageTakenChanceMeleeDecreased, 95, 75, slot: 0),
                Card(BCardEffect.DamageTakenChanceRangedDecreased, 50, 50, slot: 2));

            Assert.AreEqual(new ChanceDamageTaken(95, -75, AttackKind.Melee), stats.ChanceDamageTaken?.Single());
        }

        [TestMethod]
        public void MagicalAttackCardsRaiseTheMagicAttackOnly()
        {
            var stats = StatsOf(
                Card(BCardEffect.AttackPowerMagicalAttacksIncreased, 50, 0, slot: 0),
                Card(BCardEffect.DamageMagicalIncreased, 10, 0, slot: 0));

            Assert.AreEqual((100 + 50) * 110 / 100, stats.MinMagic);
            Assert.AreEqual((200 + 50) * 110 / 100, stats.MaxMagic);
            Assert.AreEqual(100, stats.MinHit);
            Assert.AreEqual(200, stats.MaxHit);
        }

        [TestMethod]
        public void MeleeAttackCardsLeaveTheMagicAttackAlone()
        {
            var stats = StatsOf(
                Card(BCardEffect.AttackPowerMeleeAttacksIncreased, 50, 0, slot: 0),
                Card(BCardEffect.DamageMeleeIncreased, 10, 0, slot: 0));

            Assert.AreEqual(100, stats.MinMagic);
            Assert.AreEqual((100 + 50) * 110 / 100, stats.MinHit);
        }

        [TestMethod]
        public void AMonsterWithoutCardsKeepsItsOwnStats()
        {
            Assert.IsNull(StatsOf().ChanceDamageTaken);
        }

        private static BCardDto Card(BCardEffect effect, short first, short second, byte slot) => new()
        {
            NpcMonsterVNum = Vnum,
            Type = effect.Type(),
            SubType = effect.SubType(),
            FirstData = first,
            SecondData = second,
            Slot = slot,
        };

        private static CombatStats StatsOf(params BCardDto[] cards)
        {
            var monster = new Mock<INonPlayableEntity>();
            monster.SetupGet(e => e.NpcMonster).Returns(new NpcMonsterDto { NpcMonsterVNum = Vnum, DamageMinimum = 100, DamageMaximum = 200 });
            monster.SetupGet(e => e.Level).Returns((byte)1);

            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>());
            var equipment = new Mock<IEquipmentStatsService>();
            equipment.Setup(s => s.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            var catalog = new NpcCombatCatalog(new List<NpcMonsterSkillDto>(), new List<DropDto>(), cards.ToList());

            return BattleStats.Provider(buffs.Object, equipment.Object, npcCombatCatalog: catalog).GetStats(monster.Object);
        }
    }
}
