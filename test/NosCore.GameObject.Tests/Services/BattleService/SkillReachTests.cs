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
    public class SkillReachTests
    {
        [DataTestMethod]
        [DataRow(1, 1, true)]
        [DataRow(1, 2, true)]
        [DataRow(1, 3, false)]
        [DataRow(7, 8, true)]
        [DataRow(7, 9, false)]
        public void ATargetedSkillReachesItsRangePlusOneCell(int range, int distance, bool inReach)
        {
            Assert.AreEqual(inReach, SkillReach.IsInReach(targetType: 0, (byte)range, distance));
        }

        [TestMethod]
        public void ARangeOfZeroIsNotGated()
        {
            Assert.IsTrue(SkillReach.IsInReach(targetType: 0, range: 0, distance: 50));
        }

        [TestMethod]
        public void ACasterCentredSkillIsNotGated()
        {
            Assert.IsTrue(SkillReach.IsInReach(targetType: 1, range: 2, distance: 50));
        }

        [DataTestMethod]
        [DataRow(0, 10, true)]
        [DataRow(5, 10, true)]
        [DataRow(6, 10, true)]
        [DataRow(7, 10, false)]
        [DataRow(1, 1, true)]
        [DataRow(2, 1, false)]
        [DataRow(2, 2, true)]
        [DataRow(3, 2, false)]
        public void AStepIsHalfTheSpeedPlusTheCellLeftBehind(int distance, int speed, bool withinOneStep)
        {
            Assert.AreEqual(withinOneStep, SkillReach.IsWithinOneStep(distance, (byte)speed));
        }
    }
}
