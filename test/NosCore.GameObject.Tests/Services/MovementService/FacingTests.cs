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
    public class FacingTests
    {
        [TestMethod]
        public void EachDirectionFacesTheWayItSteps()
        {
            for (byte direction = 0; direction < 8; direction++)
            {
                var (x, y) = Facing.Step(direction);
                Assert.AreEqual(direction, Facing.Towards(x, y, 255));
            }
        }

        [TestMethod]
        public void AMostlySidewaysWalkFacesSideways() => Assert.AreEqual((byte)1, Facing.Towards(5, -1, 0));

        [TestMethod]
        public void StandingStillKeepsTheDirection() => Assert.AreEqual((byte)3, Facing.Towards(0, 0, 3));
    }
}
