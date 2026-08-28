//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // Standing regeneration is half the resting rate and falls in bands, not a constant.
    [TestClass]
    public class RegenerationRateTests
    {
        private const int Adventurer = 30;
        private const int Swordsman = 80;
        private const int Archer = 60;
        private const int Mage = 30;
        private const int MartialArtist = 70;

        [TestMethod]
        public void AtLevelOneStandingIsExactlyHalfOfResting()
        {
            // Ten pairs out of ten, health and mana, all five classes. This is the observation
            // the whole band rule was built on.
            Assert.AreEqual(15, RegenerationService.StandingRate(Adventurer, 1));
            Assert.AreEqual(40, RegenerationService.StandingRate(Swordsman, 1));
            Assert.AreEqual(30, RegenerationService.StandingRate(Archer, 1));
            Assert.AreEqual(15, RegenerationService.StandingRate(Mage, 1));
            Assert.AreEqual(35, RegenerationService.StandingRate(MartialArtist, 1));

            Assert.AreEqual(5, RegenerationService.StandingRate(10, 1));
            Assert.AreEqual(15, RegenerationService.StandingRate(30, 1));
            Assert.AreEqual(25, RegenerationService.StandingRate(50, 1));
            Assert.AreEqual(40, RegenerationService.StandingRate(80, 1));
            Assert.AreEqual(20, RegenerationService.StandingRate(40, 1));
        }

        [TestMethod]
        public void TheBandEdgeIsBetweenTwentyAndTwentyOne()
        {
            // Measured either side of the step, because an off-by-one in a band boundary is
            // exactly the sort of thing that survives a test written only on round numbers.
            Assert.AreEqual(35, RegenerationService.StandingRate(MartialArtist, 20));
            Assert.AreEqual(28, RegenerationService.StandingRate(MartialArtist, 21));
        }

        [TestMethod]
        public void ThePredictedValuesAreTheMeasuredOnes()
        {
            // Predicted from the rule, then measured, on a class the rule was not derived from.
            Assert.AreEqual(21, RegenerationService.StandingRate(MartialArtist, 50));
            Assert.AreEqual(14, RegenerationService.StandingRate(MartialArtist, 70));
        }

        [TestMethod]
        public void EveryBandIsDistinct()
        {
            // A constant would satisfy any single band. Four different answers for the same
            // resting rate are what says the shape is right and not just one point.
            Assert.AreEqual(40, RegenerationService.StandingRate(Swordsman, 20));
            Assert.AreEqual(32, RegenerationService.StandingRate(Swordsman, 40));
            Assert.AreEqual(24, RegenerationService.StandingRate(Swordsman, 60));
            Assert.AreEqual(16, RegenerationService.StandingRate(Swordsman, 61));
            Assert.AreEqual(16, RegenerationService.StandingRate(Swordsman, 99));
        }

        [TestMethod]
        public void TheShareIsTakenBeforeTheDivision()
        {
            // 30 * 30 / 100 = 9 and not 30 * (30 / 100) = 0. Dividing first would truncate the
            // remainder and then multiply it away, which is the silent-integer-division trap
            // this codebase has already been bitten by twice.
            Assert.AreEqual(9, RegenerationService.StandingRate(Mage, 50));
            Assert.AreEqual(6, RegenerationService.StandingRate(Mage, 99));
        }

        [TestMethod]
        public void AZeroRestingRateStaysZero()
        {
            Assert.AreEqual(0, RegenerationService.StandingRate(0, 1));
            Assert.AreEqual(0, RegenerationService.StandingRate(0, 99));
        }
    }
}
