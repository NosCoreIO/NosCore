//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class DamageLeechTests
    {
        private TestWorld World;
        private IAliveEntity Attacker;
        private List<BuffInstance> Buffs;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Attacker = (await World.CreateSessionAsync()).Character;
            Buffs = new List<BuffInstance>();
            World.Freeze<Mock<IBuffService>>().Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(Buffs);
            World.Freeze<Mock<IEquipmentStatsService>>().Setup(g => g.Resolve(It.IsAny<IAliveEntity>()))
                .Returns(EquipmentStats.None);
        }

        private void Under(BCardEffect effect, int percent) =>
            Buffs.Add(new BuffInstance(1, BuffType.Good, null, Instant.MinValue, Instant.MaxValue,
                new[] { new BCardDto { Type = effect.Type(), SubType = effect.SubType(), FirstData = percent } }));

        private Task HitFor(int damage) => World.Create<DamageLeechService>().ApplyAsync(Attacker, damage);

        [TestMethod]
        public async Task AGiantismHealsTheAttackerByAShareOfTheDamage()
        {
            Attacker.Hp = Attacker.MaxHp / 2;
            var before = Attacker.Hp;
            Under(BCardEffect.ReflectionHpIncreased, 20);

            await HitFor(100);

            Assert.AreEqual(before + 20, Attacker.Hp);
        }

        [TestMethod]
        public async Task TheHealStopsAtMaximumHp()
        {
            Attacker.Hp = Attacker.MaxHp - 1;
            Under(BCardEffect.ReflectionHpIncreased, 20);

            await HitFor(100);

            Assert.AreEqual(Attacker.MaxHp, Attacker.Hp);
        }

        [TestMethod]
        public async Task ACurseCostsTheAttackerAShareOfTheDamage()
        {
            Attacker.Hp = Attacker.MaxHp;
            Under(BCardEffect.ReflectionHpDecreased, 10);

            await HitFor(100);

            Assert.AreEqual(Attacker.MaxHp - 10, Attacker.Hp);
        }

        [TestMethod]
        public async Task TheLossNeverExceedsHalfOfMaximumHpNorKills()
        {
            Attacker.Hp = Attacker.MaxHp;
            Under(BCardEffect.ReflectionHpDecreased, 100);

            await HitFor(Attacker.MaxHp * 10);

            Assert.AreEqual(Attacker.MaxHp - Attacker.MaxHp / 2, Attacker.Hp);

            await HitFor(Attacker.MaxHp * 10);

            Assert.AreEqual(1, Attacker.Hp);
        }

        [TestMethod]
        public async Task WithoutTheseCardsNothingChanges()
        {
            Attacker.Hp = Attacker.MaxHp / 2;
            var before = Attacker.Hp;

            await HitFor(100);

            Assert.AreEqual(before, Attacker.Hp);
        }
    }
}
