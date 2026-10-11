//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.DigestionService;

namespace NosCore.GameObject.Tests.Services.DigestionService
{
    [TestClass]
    public class DigestionScheduleTests
    {
        [TestMethod]
        public void OneItemHealsFiveEqualTicksThenNothing()
        {
            var schedule = new DigestionSchedule();
            schedule.Add(140, 140);

            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual((140, 140), schedule.Tick());
            }

            Assert.IsTrue(schedule.IsEmpty);
            Assert.AreEqual((0, 0), schedule.Tick());
        }

        [TestMethod]
        public void OverlappingItemsAddUp()
        {
            var schedule = new DigestionSchedule();
            schedule.Add(140, 0);
            schedule.Add(140, 0);

            Assert.AreEqual((280, 0), schedule.Tick());
        }

        [TestMethod]
        public void TheCapClampsEachTickAndTheExcessIsLost()
        {
            var schedule = new DigestionSchedule();
            for (var i = 0; i < 5; i++)
            {
                schedule.Add(140, 0);
            }

            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual((600, 0), schedule.Tick(600));
            }

            Assert.AreEqual((0, 0), schedule.Tick(600));
        }

        [TestMethod]
        public void HpAndMpAreCappedIndependently()
        {
            var schedule = new DigestionSchedule();
            schedule.Add(700, 100);

            Assert.AreEqual((600, 100), schedule.Tick(600));
        }

        [TestMethod]
        public void ALaterItemOnlyExtendsAsFarAsItsOwnFiveTicks()
        {
            var schedule = new DigestionSchedule();
            schedule.Add(100, 0);
            schedule.Tick();
            schedule.Tick();
            schedule.Add(50, 0);

            Assert.AreEqual((150, 0), schedule.Tick());
            Assert.AreEqual((150, 0), schedule.Tick());
            Assert.AreEqual((150, 0), schedule.Tick());
            Assert.AreEqual((50, 0), schedule.Tick());
            Assert.AreEqual((50, 0), schedule.Tick());
            Assert.IsTrue(schedule.IsEmpty);
        }

        [TestMethod]
        public void TheSixthFiftyReachesTheSnackLimit()
        {
            var schedule = new DigestionSchedule();
            for (var i = 0; i < 4; i++)
            {
                schedule.Add(50, 50);
            }

            Assert.IsFalse(schedule.WouldReach(50, 50, 300));
            schedule.Add(50, 50);
            Assert.IsTrue(schedule.WouldReach(50, 50, 300));
        }

        [TestMethod]
        public void AnEmptyScheduleNeverReachesTheLimit()
        {
            Assert.IsFalse(new DigestionSchedule().WouldReach(400, 400, 300));
        }

        [TestMethod]
        public void TheFifthFoodExceedsTheCap()
        {
            var schedule = new DigestionSchedule();
            for (var i = 0; i < 3; i++)
            {
                schedule.Add(140, 140);
            }

            Assert.IsFalse(schedule.WouldExceed(140, 140, 600));
            schedule.Add(140, 140);
            Assert.IsTrue(schedule.WouldExceed(140, 140, 600));
        }

        [TestMethod]
        public void ClearEmptiesTheSchedule()
        {
            var schedule = new DigestionSchedule();
            schedule.Add(140, 140);

            schedule.Clear();

            Assert.IsTrue(schedule.IsEmpty);
            Assert.AreEqual((0, 0), schedule.Tick());
        }

        [TestMethod]
        public void AValueNotDivisibleByFiveLosesItsRemainder()
        {
            var schedule = new DigestionSchedule();
            schedule.Add(23 / 5, 0);

            var total = 0;
            for (var i = 0; i < 5; i++)
            {
                total += schedule.Tick().Hp;
            }

            Assert.AreEqual(20, total);
        }
    }
}
