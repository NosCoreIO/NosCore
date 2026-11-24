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
    // Type 43 subtype 4: "ambush attacks cause %s additional damage".
    [TestClass]
    public class AmbushDamageTests
    {
        private static CombatStats StatsWith(byte level, params BCardDto[] bCards)
        {
            var entity = new Mock<INonPlayableEntity>();
            entity.SetupGet(e => e.NpcMonster).Returns(new NpcMonsterDto());
            entity.SetupGet(e => e.Level).Returns(level);
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

        private static BCardDto Ambush(short value, bool levelScaled = false) => new()
        {
            Type = BCardEffect.LightAndShadowAdditionalDamageWhenHidden.Type(),
            SubType = BCardEffect.LightAndShadowAdditionalDamageWhenHidden.SubType(),
            FirstData = value,
            IsLevelScaled = levelScaled,
        };

        [TestMethod]
        public void AnAmbushCardOnABuffAddsItsDamage()
        {
            Assert.AreEqual(20, StatsWith(40, Ambush(20)).AmbushDamage);
        }

        [TestMethod]
        public void ALevelScaledAmbushCardGrowsWithTheBearersLevel()
        {
            Assert.AreEqual(40, StatsWith(40, Ambush(1, levelScaled: true)).AmbushDamage);
        }

        [TestMethod]
        public void WithoutTheCardThereIsNoAmbushDamage()
        {
            Assert.AreEqual(0, StatsWith(40).AmbushDamage);
        }
    }
}
