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
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // BCard 20/52 on a skill, "There is a %s%% chance that %s%% of the remaining MP is lost":
    // Mana Burn (skill 1483) carries 25% / 99%, the Ninja card's Chain Attack (825) 60% / 30%.
    [TestClass]
    public class RemainingMpLossTests
    {
        private TestWorld World;
        private IAliveEntity Attacker;
        private IAliveEntity Target;
        private readonly List<BuffInstance> Buffs = new();
        private int Roll;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Attacker = (await World.CreateSessionAsync()).Character;
            Target = (await World.CreateSessionAsync()).Character;
            World.Freeze<Mock<IRandomProvider>>().Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(() => Roll);
            World.Freeze<Mock<IBuffService>>().Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(Buffs);
            World.Freeze<Mock<IEquipmentStatsService>>().Setup(g => g.Resolve(It.IsAny<IAliveEntity>()))
                .Returns(EquipmentStats.None);
        }

        [TestMethod]
        public async Task ALuckyHitBurnsTheShareOfTheRemainingMp()
        {
            await new Spec("A Mana Burn that rolls takes 99% of the MP the target has left")
                .Given(TheTargetHas_Mp, 1000)
                .And(TheRollIs_, 0)
                .WhenAsync(HittingWith_, Card(BCardEffect.ReflectionRemainingMpLostChance, 25, 99))
                .Then(TheTargetShouldHave_Mp, 10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedRollBurnsNothing()
        {
            await new Spec("A Mana Burn that does not roll leaves the target's MP")
                .Given(TheTargetHas_Mp, 1000)
                .And(TheRollIs_, 25)
                .WhenAsync(HittingWith_, Card(BCardEffect.ReflectionRemainingMpLostChance, 25, 99))
                .Then(TheTargetShouldHave_Mp, 1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADebuffsDrainWithAnIntervalDoesNotFireOnItsBearersHits()
        {
            await new Spec("A character under Mana Drain does not leech MP from what it hits")
                .Given(TheTargetHas_Mp, 1000)
                .And(TheAttackerHasNoMp)
                .And(TheAttackerIsUnderADebuffWith_, Card(BCardEffect.DrainAndStealLeechEnemyMp, 100, 3, thirdData: 1))
                .WhenAsync(HittingWith_, (BCardDto?)null)
                .Then(TheTargetShouldHave_Mp, 1000)
                .And(TheAttackerShouldHave_Mp, 0)
                .ExecuteAsync();
        }

        private static BCardDto Card(BCardEffect effect, int first, int second, int thirdData = 0) => new()
        {
            Type = effect.Type(), SubType = effect.SubType(), FirstData = first, SecondData = second, ThirdData = thirdData,
        };

        private void TheTargetHas_Mp(int mp) => Target.Mp = mp;

        private void TheRollIs_(int roll) => Roll = roll;

        private void TheAttackerHasNoMp() => Attacker.Mp = 0;

        private void TheAttackerIsUnderADebuffWith_(BCardDto bCard) =>
            Buffs.Add(new BuffInstance(228, BuffType.Bad, null, Instant.MinValue, Instant.MaxValue, [bCard]));

        private Task HittingWith_(BCardDto? skillCard) =>
            World.Create<DamageLeechService>().DrainAsync(Attacker, Target, skillCard == null ? [] : [skillCard]);

        private void TheTargetShouldHave_Mp(int mp) => Assert.AreEqual(mp, Target.Mp);

        private void TheAttackerShouldHave_Mp(int mp) => Assert.AreEqual(mp, Attacker.Mp);
    }
}
