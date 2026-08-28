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
    public class AdditionalVitalityTests
    {
        [TestMethod]
        public void TheBoostAppliesToTheAdditionalPartAndNotTheWhole()
        {
            // 400 additional out of a 1000 maximum, boosted by 50%: 200, not 500.
            Assert.AreEqual(200, VitalityService.BoostedAddition(400, 1000, 50, 100));
        }

        [TestMethod]
        public void TheCeilingStopsTheBoost()
        {
            Assert.AreEqual(100, VitalityService.BoostedAddition(400, 1000, 50, 50));
        }

        [TestMethod]
        public void AtTheCeilingTheEffectAddsNothingRatherThanTakingAway()
        {
            Assert.AreEqual(0, VitalityService.BoostedAddition(600, 1000, 50, 50));
            Assert.AreEqual(0, VitalityService.BoostedAddition(900, 1000, 50, 50));
        }

        [TestMethod]
        public void WithoutACeilingTheBoostIsWhateverThePercentageSays()
        {
            // A card that declares no second number is not a card with a ceiling of zero.
            Assert.AreEqual(200, VitalityService.BoostedAddition(400, 1000, 50, 0));
        }

        [TestMethod]
        public void NothingAdditionalMeansNothingToBoost()
        {
            Assert.AreEqual(0, VitalityService.BoostedAddition(0, 1000, 50, 50));
            Assert.AreEqual(0, VitalityService.BoostedAddition(-50, 1000, 50, 50));
        }

        [TestMethod]
        public void AZeroOrNegativePercentageDoesNothing()
        {
            Assert.AreEqual(0, VitalityService.BoostedAddition(400, 1000, 0, 50));
            Assert.AreEqual(0, VitalityService.BoostedAddition(400, 1000, -20, 50));
        }
    }
}
