//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // Type 24 subtypes 1 and 2: "increases (reduces) damage against %s by %s".
    [TestClass]
    public class RaceSpecialisationTests
    {
        private static BCardDto Card(BCardEffect effect, short race, short attack) =>
            new()
            {
                Type = effect.Type(),
                SubType = effect.SubType(),
                FirstData = race,
                SecondData = attack
            };

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
        public void AnIncreaseCardAddsAttackAgainstTheRaceItNames()
        {
            var stats = StatsWith(Card(BCardEffect.SpecialisationBuffResistanceIncreaseDamageAgainst, 10, 150));

            CollectionAssert.AreEqual(
                new[] { new RaceAttack(MonsterRace.HighLevel, (byte)MonsterRaceType.Plant, 150) },
                (System.Collections.ICollection?)stats.RaceAttack);
        }

        [TestMethod]
        public void AReduceCardTakesAttackAwayAgainstTheRaceItNames()
        {
            var stats = StatsWith(Card(BCardEffect.SpecialisationBuffResistanceReduceDamageAgainst, 52, 40));

            CollectionAssert.AreEqual(
                new[] { new RaceAttack(MonsterRace.Undead, (byte)MonsterRaceType.Monster, -40) },
                (System.Collections.ICollection?)stats.RaceAttack);
        }

        [TestMethod]
        public void WithoutACardThereIsNoRaceAttack()
        {
            Assert.IsNull(StatsWith().RaceAttack);
        }

        [TestMethod]
        public void ACardNamesTheRaceAsTensAndTheTypeAsUnits()
        {
            var card = RaceAttack.FromCard(10, 80);

            Assert.IsTrue(card.Matches(MonsterRace.HighLevel, (byte)MonsterRaceType.Plant));
            Assert.IsFalse(card.Matches(MonsterRace.LowLevel, (byte)MonsterRaceType.Animal));
        }
    }
}
