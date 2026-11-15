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
        private IAliveEntity Target;
        private List<BuffInstance> Buffs;
        private int Roll;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Attacker = (await World.CreateSessionAsync()).Character;
            Target = (await World.CreateSessionAsync()).Character;
            Roll = 0;
            World.Freeze<Mock<IRandomProvider>>().Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(() => Roll);
            Buffs = new List<BuffInstance>();
            World.Freeze<Mock<IBuffService>>().Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(Buffs);
            World.Freeze<Mock<IEquipmentStatsService>>().Setup(g => g.Resolve(It.IsAny<IAliveEntity>()))
                .Returns(EquipmentStats.None);
        }

        private void Under(BCardEffect effect, int percent) =>
            Buffs.Add(new BuffInstance(1, BuffType.Good, null, Instant.MinValue, Instant.MaxValue,
                new[] { new BCardDto { Type = effect.Type(), SubType = effect.SubType(), FirstData = percent } }));

        private Task HitFor(int damage) => World.Create<DamageLeechService>().ApplyAsync(Attacker, damage);

        private static BCardDto Card(BCardEffect effect, int first, int second = 0, bool levelScaled = false) =>
            new() { Type = effect.Type(), SubType = effect.SubType(), FirstData = first, SecondData = second, IsLevelScaled = levelScaled };

        private Task DrainWith(params BCardDto[] skillCards) =>
            World.Create<DamageLeechService>().DrainAsync(Attacker, Target, skillCards);

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

        [TestMethod]
        public async Task ALuckyLeechMovesHpFromTheTargetToTheAttacker()
        {
            Attacker.Hp = Attacker.MaxHp / 2;
            var attackerBefore = Attacker.Hp;
            var targetBefore = Target.Hp;

            await DrainWith(Card(BCardEffect.DrainAndStealLeechEnemyHp, 50, 30));

            Assert.AreEqual(targetBefore - 30, Target.Hp);
            Assert.AreEqual(attackerBefore + 30, Attacker.Hp);
        }

        [TestMethod]
        public async Task AFailedLeechRollTakesNothing()
        {
            Attacker.Hp = Attacker.MaxHp / 2;
            var attackerBefore = Attacker.Hp;
            var targetBefore = Target.Hp;
            Roll = 50;

            await DrainWith(Card(BCardEffect.DrainAndStealLeechEnemyHp, 50, 30));

            Assert.AreEqual(targetBefore, Target.Hp);
            Assert.AreEqual(attackerBefore, Attacker.Hp);
        }

        [TestMethod]
        public async Task ALeechNeverKillsTheTarget()
        {
            Target.Hp = 10;

            await DrainWith(Card(BCardEffect.DrainAndStealLeechEnemyHp, 100, 2500));

            Assert.AreEqual(1, Target.Hp);
        }

        [TestMethod]
        public async Task AnMpLeechTakesNoMoreThanTheTargetHas()
        {
            Attacker.Mp = 0;
            Target.Mp = 5;

            await DrainWith(Card(BCardEffect.DrainAndStealLeechEnemyMp, 100, 200));

            Assert.AreEqual(0, Target.Mp);
            Assert.AreEqual(5, Attacker.Mp);
        }

        [TestMethod]
        public async Task ConvertingTakesHpFromTheTargetAsMpScaledByLevel()
        {
            Attacker.Mp = 0;
            var targetBefore = Target.Hp;

            await DrainWith(Card(BCardEffect.DrainAndStealConvertEnemyHptoMp, 8, levelScaled: true));

            var amount = 8 * Attacker.Level;
            Assert.AreEqual(targetBefore - amount, Target.Hp);
            Assert.AreEqual(amount, Attacker.Mp);
        }

        [TestMethod]
        public async Task ALeechOnAnActiveBuffAppliesToo()
        {
            Attacker.Hp = Attacker.MaxHp / 2;
            var attackerBefore = Attacker.Hp;
            Buffs.Add(new BuffInstance(1, BuffType.Good, null, Instant.MinValue, Instant.MaxValue,
                new[] { Card(BCardEffect.DrainAndStealLeechEnemyHp, 100, 40) }));

            await DrainWith();

            Assert.AreEqual(attackerBefore + 40, Attacker.Hp);
        }
    }
}
