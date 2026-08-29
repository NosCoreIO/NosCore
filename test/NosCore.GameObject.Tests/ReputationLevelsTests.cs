//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using System.Collections.Generic;

namespace NosCore.GameObject.Tests
{
    // Both sides of every boundary: a ladder can be off by one at every step and still
    // look right in the middle of each band.
    [TestClass]
    public class ReputationLevelsTests
    {
        // (last reputation in the band, the icon that band draws). Twenty-seven rows, one per band,
        // in the client's order.
        private static readonly (long Top, ReputationType Icon)[] Bands =
        {
            (50, ReputationType.GreenBeginner),
            (150, ReputationType.BlueBeginner),
            (250, ReputationType.RedBeginner),
            (500, ReputationType.GreenTrainee),
            (750, ReputationType.BlueTrainee),
            (1_000, ReputationType.RedTrainee),
            (2_250, ReputationType.GreenExperienced),
            (3_500, ReputationType.BlueExperienced),
            (5_000, ReputationType.RedExperienced),
            (9_500, ReputationType.GreenSoldier),
            (19_000, ReputationType.BlueSoldier),
            (25_000, ReputationType.RedSoldier),
            (40_000, ReputationType.GreenExpert),
            (60_000, ReputationType.BlueExpert),
            (85_000, ReputationType.RedExpert),
            (115_000, ReputationType.GreenLeader),
            (150_000, ReputationType.BlueLeader),
            (190_000, ReputationType.RedLeader),
            (235_000, ReputationType.GreenMaster),
            (285_000, ReputationType.BlueMaster),
            (350_000, ReputationType.RedMaster),
            (500_000, ReputationType.GreenNos),
            (1_500_000, ReputationType.BlueNos),
            (2_500_000, ReputationType.RedNos),
            (3_750_000, ReputationType.GreenElite),
            (5_000_000, ReputationType.BlueElite),
            (long.MaxValue, ReputationType.RedElite)
        };

        [TestInitialize]
        public void LoadLadder() => ReputationLevels.Load(ClientLadders.ReputationLevels());

        [TestMethod]
        public void TheLastValueOfEachBandDrawsThatBandsIcon()
        {
            foreach ((long top, ReputationType icon) in Bands)
            {
                Assert.AreEqual(icon, ReputationLevels.FromReputation(top),
                    $"reputation {top} is the last value of the {icon} band");
            }
        }

        [TestMethod]
        public void TheFirstValueOfEachBandDrawsThatBandsIcon()
        {
            long previousTop = -1;
            foreach ((long top, ReputationType icon) in Bands)
            {
                long first = previousTop + 1;
                Assert.AreEqual(icon, ReputationLevels.FromReputation(first),
                    $"reputation {first} is the first value of the {icon} band");
                previousTop = top;
            }
        }

        [TestMethod]
        public void EveryBoundaryActuallyChangesTheIcon()
        {
            // Without this, a ladder that returned the same icon for two neighbouring bands would
            // still pass both tests above.
            for (int i = 1; i < Bands.Length; i++)
            {
                Assert.AreNotEqual(Bands[i - 1].Icon, Bands[i].Icon,
                    "two neighbouring bands must not draw the same icon");
                Assert.AreNotEqual(
                    ReputationLevels.FromReputation(Bands[i - 1].Top),
                    ReputationLevels.FromReputation(Bands[i - 1].Top + 1),
                    $"crossing {Bands[i - 1].Top} must change the icon");
            }
        }

        [TestMethod]
        public void TheLadderIsTwentySevenBandsAndStartsAtOne()
        {
            Assert.AreEqual(27, Bands.Length);

            // The client's first band is icon 1, not 0 and not 16: the enum's numbering IS the
            // icon the client draws, and the old thirteen-tier ladder in PlayerBundleExtensions
            // started at 16 for no reason anyone recorded.
            Assert.AreEqual(1, (int)ReputationLevels.FromReputation(0));
            Assert.AreEqual(27, (int)ReputationLevels.FromReputation(long.MaxValue));
        }

        [TestMethod]
        public void NegativeReputationStillDrawsTheLowestIcon()
        {
            // Reputation can go negative in play. It must not fall off the bottom of the switch
            // into something that is not an icon at all.
            Assert.AreEqual(ReputationType.GreenBeginner, ReputationLevels.FromReputation(-1));
            Assert.AreEqual(ReputationType.GreenBeginner, ReputationLevels.FromReputation(long.MinValue));
        }

        [TestMethod]
        public void TheLadderIsWhateverWasImported()
        {
            ReputationLevels.Load(new List<ReputationLevelDto>
            {
                new() { ReputationLevelId = (byte)ReputationType.GreenBeginner, MinReputation = 0, MaxReputation = 9 },
                new() { ReputationLevelId = (byte)ReputationType.BlueBeginner, MinReputation = 10, MaxReputation = null }
            });

            Assert.AreEqual(ReputationType.GreenBeginner, ReputationLevels.FromReputation(9));
            Assert.AreEqual(ReputationType.BlueBeginner, ReputationLevels.FromReputation(10));
            Assert.AreEqual(ReputationType.BlueBeginner, ReputationLevels.FromReputation(long.MaxValue));
        }
    }
}
