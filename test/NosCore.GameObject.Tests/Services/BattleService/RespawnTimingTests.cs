//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // A unit mistake here raises nothing at all: the server runs, the monsters come back, and
    // only counting the seconds shows they come back a hundred times too fast.
    [TestClass]
    public class RespawnTimingTests
    {
        [TestMethod]
        public void FourHundredIsFortySecondsNotFourTenths()
        {
            // monster.dat PREATT field five. An ordinary wolf carries 400, and the atlagaming
            // API exposes the same field as respTimeSek = 40.0.
            var delay = RespawnTiming.For(new NpcMonsterDto { RespawnTime = 400 });

            Assert.AreEqual(40, delay.TotalSeconds);
        }

        [TestMethod]
        public void AMonsterWithNoDeclaredTimeStillGetsASecond()
        {
            // Some event monsters carry zero. Respawning them the instant they die would mean
            // never letting them die.
            var delay = RespawnTiming.For(new NpcMonsterDto { RespawnTime = 0 });

            Assert.AreEqual(1, delay.TotalSeconds);
        }

        [TestMethod]
        public void AVeryShortTimeIsRaisedToTheFloorRatherThanKept()
        {
            // Five tenths is half a second, under the floor.
            var delay = RespawnTiming.For(new NpcMonsterDto { RespawnTime = 5 });

            Assert.AreEqual(1, delay.TotalSeconds);
        }

        [TestMethod]
        public void ALongTimeDoesNotOverflow()
        {
            // The multiplication is done in 64 bits on purpose: RespawnTime is an int, and the
            // largest values in the file times 100 leave the range of one.
            var delay = RespawnTiming.For(new NpcMonsterDto { RespawnTime = int.MaxValue });

            Assert.IsTrue(delay.TotalSeconds > 0, "a long respawn wrapped round into the past");
        }
    }
}
