//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.MovementService;

namespace NosCore.GameObject.Tests.Services.MovementService
{
    [TestClass]
    public class WalkAllowanceTests
    {
        // Walks a long way in the longest stretches a packet may carry, each packet arriving after
        // the stretch before it took to walk, scaled by how early the client is.
        private static bool LongWalkIsAccepted(byte speed, double earliness)
        {
            var stretch = speed / 2 + 1;
            var owed = 0D;
            var elapsed = 0D;
            for (var packet = 0; packet < 200; packet++)
            {
                var cost = WalkAllowance.Cost(stretch, speed);
                owed = WalkAllowance.OwedAfter(owed, elapsed, cost);
                if (!WalkAllowance.IsAffordable(owed, speed))
                {
                    return false;
                }

                elapsed = cost * earliness;
            }

            return true;
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(11)]
        [DataRow(14)]
        [DataRow(20)]
        [DataRow(28)]
        [DataRow(40)]
        [DataRow(60)]
        public void WalkingAtThePaceOfAnySpeedIsAccepted(int speed)
        {
            Assert.IsTrue(LongWalkIsAccepted((byte)speed, 1));
            Assert.IsTrue(LongWalkIsAccepted((byte)speed, 0.8));
        }

        [DataTestMethod]
        [DataRow(11)]
        [DataRow(20)]
        [DataRow(40)]
        public void WalkingAtTwiceThePaceIsRefused(int speed)
        {
            Assert.IsFalse(LongWalkIsAccepted((byte)speed, 0.5));
        }

        [TestMethod]
        public void ACharacterWithNoSpeedCannotStep()
        {
            Assert.IsFalse(WalkAllowance.IsAffordable(WalkAllowance.OwedAfter(0, 10_000, WalkAllowance.Cost(1, 0)), 0));
        }
    }
}
