//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // Threat on a real monster bundle: who becomes the target, who takes it over, when it lapses.
    [TestClass]
    public class AggroThreatTests
    {
        private FakeClock _clock = null!;
        private AggroService _aggro = null!;
        private MonsterComponentBundle _monster;

        [TestInitialize]
        public void Setup()
        {
            _clock = new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0));
            _aggro = new AggroService(_clock);
            var world = new MapWorld();
            _monster = new MonsterComponentBundle(
                world.World.Create(new AggroComponent(VisualType.Object, 0, 0, Instant.MinValue)), world);
        }

        private static IAliveEntity Attacker(long visualId)
        {
            var attacker = new Mock<IAliveEntity>();
            attacker.Setup(a => a.VisualId).Returns(visualId);
            attacker.Setup(a => a.VisualType).Returns(VisualType.Player);
            return attacker.Object;
        }

        [TestMethod]
        public void AMonsterWithoutThreatHasNoTarget()
        {
            Assert.IsFalse(_aggro.Current(_monster).HasTarget);
        }

        [TestMethod]
        public void TheFirstAttackerBecomesTheTarget()
        {
            _aggro.AddThreat(_monster, Attacker(7), 10);

            var snapshot = _aggro.Current(_monster);

            Assert.IsTrue(snapshot.HasTarget);
            Assert.AreEqual(7, snapshot.TargetVisualId);
            Assert.AreEqual(10, snapshot.ThreatScore);
        }

        [TestMethod]
        public void TheSameAttackerAccumulatesThreat()
        {
            var attacker = Attacker(7);

            _aggro.AddThreat(_monster, attacker, 10);
            _aggro.AddThreat(_monster, attacker, 15);

            Assert.AreEqual(25, _aggro.Current(_monster).ThreatScore);
        }

        [TestMethod]
        public void AttackerWhoDoesNotOutdoTheTargetByTheStickyBonusDoesNotTakeOver()
        {
            _aggro.AddThreat(_monster, Attacker(7), 50);

            _aggro.AddThreat(_monster, Attacker(8), 150);

            var snapshot = _aggro.Current(_monster);
            Assert.AreEqual(7, snapshot.TargetVisualId);
            Assert.AreEqual(49, snapshot.ThreatScore, "the incumbent's threat ages down by one");
        }

        [TestMethod]
        public void AnAttackerBeyondTheStickyBonusTakesTheTarget()
        {
            _aggro.AddThreat(_monster, Attacker(7), 50);

            _aggro.AddThreat(_monster, Attacker(8), 151);

            var snapshot = _aggro.Current(_monster);
            Assert.AreEqual(8, snapshot.TargetVisualId);
            Assert.AreEqual(151, snapshot.ThreatScore);
        }

        [TestMethod]
        public void TheTargetLapsesAfterTwentySecondsWithoutNewThreat()
        {
            _aggro.AddThreat(_monster, Attacker(7), 10);

            _clock.Advance(Duration.FromSeconds(19));
            Assert.IsTrue(_aggro.Current(_monster).HasTarget);

            _clock.Advance(Duration.FromSeconds(1));
            Assert.IsFalse(_aggro.Current(_monster).HasTarget);
        }

        [TestMethod]
        public void NewThreatRestartsTheTwentySeconds()
        {
            var attacker = Attacker(7);
            _aggro.AddThreat(_monster, attacker, 10);
            _clock.Advance(Duration.FromSeconds(15));

            _aggro.AddThreat(_monster, attacker, 1);
            _clock.Advance(Duration.FromSeconds(15));

            Assert.IsTrue(_aggro.Current(_monster).HasTarget);
        }

        [TestMethod]
        public void ClearingDropsTheTargetAtOnce()
        {
            _aggro.AddThreat(_monster, Attacker(7), 10);

            _aggro.Clear(_monster);

            Assert.IsFalse(_aggro.Current(_monster).HasTarget);
        }

        [TestMethod]
        public void NoDamageAddsNoThreat()
        {
            _aggro.AddThreat(_monster, Attacker(7), 0);

            Assert.IsFalse(_aggro.Current(_monster).HasTarget);
        }
    }
}
