//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class CombatRestHandlerTests
    {
        private CombatRestHandler _handler = null!;
        private ClientSession _session = null!;
        private readonly IAliveEntity _monster = new Mock<INonPlayableEntity>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new CombatRestHandler(TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task ASeatedPlayerStandsUpWhenHit()
        {
            await new Spec("A hit stands a seated player up, and the map sees it")
                .Given(CharacterIsSittingWithFoodDigesting)
                .WhenAsync(AMonsterHitsTheCharacterFor_, 100)
                .Then(IsSittingShouldBe_, false)
                .And(TheMapShouldSeeTheCharacterStand)
                .And(NoFoodShouldBeDigesting)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMissStillStandsTheCharacterUp()
        {
            await new Spec("Being attacked is what counts, not the damage")
                .Given(CharacterIsSittingWithFoodDigesting)
                .WhenAsync(AMonsterHitsTheCharacterFor_, 0)
                .Then(IsSittingShouldBe_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStandingPlayerHitIsLeftAlone()
        {
            await new Spec("A hit on a standing player sends no rest packet")
                .Given(CharacterIsStanding)
                .WhenAsync(AMonsterHitsTheCharacterFor_, 100)
                .Then(IsSittingShouldBe_, false)
                .And(TheMapShouldSeeNoRestPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKillingBlowLeavesTheCharacterToTheDeathFlow()
        {
            await new Spec("A dead character does not get a stand-up after the closing hit")
                .Given(CharacterIsSittingWithFoodDigesting)
                .WhenAsync(AMonsterKillsTheCharacter)
                .Then(TheMapShouldSeeNoRestPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHitOnAMonsterIsIgnored()
        {
            await new Spec("Only players are stood up")
                .Given(CharacterIsSittingWithFoodDigesting)
                .WhenAsync(TheCharacterHitsAMonster)
                .Then(TheMapShouldSeeNoRestPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LandingAHitMarksThePlayerAsAttacking()
        {
            await new Spec("A player who lands a hit has just attacked, which keeps them from sitting")
                .Given(CharacterLastAttacked_SecondsAgo, 60)
                .WhenAsync(TheCharacterHitsAMonster)
                .Then(LastAttackShouldBeNow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BeingHitIsNotAttacking()
        {
            await new Spec("Taking a hit does not count as attacking")
                .Given(CharacterLastAttacked_SecondsAgo, 60)
                .WhenAsync(AMonsterHitsTheCharacterFor_, 100)
                .Then(LastAttackShouldBe_SecondsAgo, 60)
                .ExecuteAsync();
        }

        private void CharacterLastAttacked_SecondsAgo(int seconds) =>
            _session.Character.LastAttack = TestHelpers.Instance.Clock.GetCurrentInstant() - Duration.FromSeconds(seconds);

        private void LastAttackShouldBeNow() =>
            Assert.AreEqual(TestHelpers.Instance.Clock.GetCurrentInstant(), _session.Character.LastAttack);

        private void LastAttackShouldBe_SecondsAgo(int seconds) =>
            Assert.AreEqual(TestHelpers.Instance.Clock.GetCurrentInstant() - Duration.FromSeconds(seconds),
                _session.Character.LastAttack);

        private void CharacterIsSittingWithFoodDigesting()
        {
            _session.Character.IsSitting = true;
            _session.Character.Digestion.Food.Add(140, 0);
        }

        private void CharacterIsStanding() => _session.Character.IsSitting = false;

        private async Task AMonsterHitsTheCharacterFor_(int damage)
        {
            _session.Character.MapInstance.LastPackets.Clear();
            await _handler.Handle(new EntityDamagedEvent(_monster, _session.Character, damage, false));
        }

        private async Task AMonsterKillsTheCharacter()
        {
            _session.Character.MapInstance.LastPackets.Clear();
            await _handler.Handle(new EntityDamagedEvent(_monster, _session.Character, 100, true));
        }

        private async Task TheCharacterHitsAMonster()
        {
            _session.Character.MapInstance.LastPackets.Clear();
            await _handler.Handle(new EntityDamagedEvent(_session.Character, _monster, 100, false));
        }

        private void IsSittingShouldBe_(bool expected) => Assert.AreEqual(expected, _session.Character.IsSitting);

        private void TheMapShouldSeeTheCharacterStand()
        {
            var rest = _session.Character.MapInstance.LastPackets.OfType<RestPacket>().SingleOrDefault();
            Assert.IsNotNull(rest);
            Assert.AreEqual(_session.Character.VisualId, rest.VisualId);
            Assert.IsFalse(rest.IsSitting);
        }

        private void TheMapShouldSeeNoRestPacket() =>
            Assert.IsFalse(_session.Character.MapInstance.LastPackets.OfType<RestPacket>().Any());

        private void NoFoodShouldBeDigesting() => Assert.IsTrue(_session.Character.Digestion.Food.IsEmpty);
    }
}
