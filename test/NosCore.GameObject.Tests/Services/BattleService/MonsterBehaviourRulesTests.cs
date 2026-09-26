//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class MonsterBehaviourRulesTests
    {
        private static readonly MonsterSituation Roaming = new(
            IsAlive: true, HasTarget: false, IsHostile: false, HasScanned: false, CanWalk: true,
            IsMoving: true, AtHome: true, Distance: 0, AttackRange: 1);

        private static MonsterBehaviour Decide(MonsterSituation situation) => MonsterBehaviourRules.Decide(situation);

        [TestMethod]
        public void ADeadMonsterIsDeadWhateverElseHolds()
        {
            var situation = Roaming with { IsAlive = false, HasTarget = true, IsHostile = true };

            Assert.AreEqual(MonsterBehaviour.Dead, Decide(situation));
        }

        [TestMethod]
        public void ATargetWithinReachIsAttacked()
        {
            var situation = Roaming with { HasTarget = true, Distance = 1, AttackRange = 1 };

            Assert.AreEqual(MonsterBehaviour.Attack, Decide(situation));
        }

        [TestMethod]
        public void AttackReachIsInclusive()
        {
            Assert.AreEqual(MonsterBehaviour.Attack, Decide(Roaming with { HasTarget = true, Distance = 7, AttackRange = 7 }));
            Assert.AreEqual(MonsterBehaviour.Chase, Decide(Roaming with { HasTarget = true, Distance = 8, AttackRange = 7 }));
        }

        [TestMethod]
        public void ATargetOutOfReachIsChasedByAMonsterThatWalks()
        {
            var situation = Roaming with { HasTarget = true, Distance = 4, AttackRange = 1 };

            Assert.AreEqual(MonsterBehaviour.Chase, Decide(situation));
        }

        [TestMethod]
        public void ATargetOutOfReachIsWaitedForByAMonsterThatCannotWalk()
        {
            var situation = Roaming with { HasTarget = true, Distance = 4, AttackRange = 1, CanWalk = false };

            Assert.AreEqual(MonsterBehaviour.Idle, Decide(situation));
        }

        [TestMethod]
        public void ATargetInReachIsAttackedEvenByAMonsterThatCannotWalk()
        {
            var situation = Roaming with { HasTarget = true, Distance = 1, AttackRange = 1, CanWalk = false };

            Assert.AreEqual(MonsterBehaviour.Attack, Decide(situation));
        }

        [TestMethod]
        public void AHostileMonsterWithoutATargetLooksForOne()
        {
            var situation = Roaming with { IsHostile = true };

            Assert.AreEqual(MonsterBehaviour.AcquireTarget, Decide(situation));
        }

        [TestMethod]
        public void AHostileMonsterOnlyLooksOncePerTick()
        {
            var situation = Roaming with { IsHostile = true, HasScanned = true };

            Assert.AreEqual(MonsterBehaviour.Wander, Decide(situation));
        }

        [TestMethod]
        public void APeacefulMonsterNeverLooksForATarget()
        {
            Assert.AreEqual(MonsterBehaviour.Wander, Decide(Roaming));
        }

        [TestMethod]
        public void AMonsterAwayFromHomeWithoutATargetReturns()
        {
            var situation = Roaming with { AtHome = false };

            Assert.AreEqual(MonsterBehaviour.Return, Decide(situation));
        }

        [TestMethod]
        public void AMonsterThatDoesNotRoamStaysIdle()
        {
            Assert.AreEqual(MonsterBehaviour.Idle, Decide(Roaming with { IsMoving = false }));
            Assert.AreEqual(MonsterBehaviour.Idle, Decide(Roaming with { IsMoving = false, AtHome = false }));
        }
    }
}
