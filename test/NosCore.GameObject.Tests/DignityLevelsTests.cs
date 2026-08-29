//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;

namespace NosCore.GameObject.Tests
{
    // Both sides of every boundary, for the same reason as ReputationLevelsTests: the second
    // dignity ladder that used to live in PlayerBundleExtensions was off by one at every step
    // and looked right in the middle of each band.
    [TestClass]
    public class DignityLevelsTests
    {
        // (highest dignity still in the band, the icon that band draws), worst first.
        private static readonly (short Top, DignityType Icon)[] Bands =
        {
            (-801, DignityType.Failed),
            (-601, DignityType.Useless),
            (-401, DignityType.Unqualified),
            (-201, DignityType.Dreadful),
            (-100, DignityType.Dubious)
        };

        [TestInitialize]
        [TestCleanup]
        public void ResetLadder() => DignityLevels.ResetToClientLadder();

        [TestMethod]
        public void TheHighestValueOfEachBandDrawsThatBandsIcon()
        {
            foreach ((short top, DignityType icon) in Bands)
            {
                Assert.AreEqual(icon, DignityLevels.FromDignity(top),
                    $"dignity {top} is the highest value of the {icon} band");
            }
        }

        [TestMethod]
        public void OneAboveEachBandDrawsTheGentlerIcon()
        {
            for (var i = 0; i < Bands.Length - 1; i++)
            {
                Assert.AreEqual(Bands[i + 1].Icon, DignityLevels.FromDignity((short)(Bands[i].Top + 1)),
                    $"dignity {Bands[i].Top + 1} sits one above the {Bands[i].Icon} band");
            }

            Assert.AreEqual(DignityType.Default, DignityLevels.FromDignity((short)(Bands[^1].Top + 1)));
        }

        [TestMethod]
        public void EveryBoundaryActuallyChangesTheIcon()
        {
            foreach ((short top, DignityType icon) in Bands)
            {
                Assert.AreNotEqual(icon, DignityLevels.FromDignity((short)(top + 1)),
                    $"crossing {top} must change the icon");
            }
        }

        [TestMethod]
        public void UntouchedDignityIsDefault()
        {
            Assert.AreEqual(DignityType.Default, DignityLevels.FromDignity(0));
            Assert.AreEqual(DignityType.Default, DignityLevels.FromDignity(100));

            // The client declares no band between -1 and -99, so Default has to carry them.
            Assert.AreEqual(DignityType.Default, DignityLevels.FromDignity(-1));
            Assert.AreEqual(DignityType.Default, DignityLevels.FromDignity(-99));
        }

        [TestMethod]
        public void DignityBelowTheLastBandStillDrawsTheWorstIcon()
        {
            // The client stops at -1000 but nothing clamps dignity there.
            Assert.AreEqual(DignityType.Failed, DignityLevels.FromDignity(-1000));
            Assert.AreEqual(DignityType.Failed, DignityLevels.FromDignity(short.MinValue));
        }

        [TestMethod]
        public void TheImportedLadderReplacesTheBuiltInOne()
        {
            DignityLevels.Load(new List<DignityLevelDto>
            {
                new() { DignityLevelId = (byte)DignityType.Default, MaxDignity = null },
                new() { DignityLevelId = (byte)DignityType.Dubious, MaxDignity = -5 }
            });

            Assert.AreEqual(DignityType.Default, DignityLevels.FromDignity(-4));
            Assert.AreEqual(DignityType.Dubious, DignityLevels.FromDignity(-5));
            Assert.AreEqual(DignityType.Dubious, DignityLevels.FromDignity(short.MinValue));
        }

        [TestMethod]
        public void AnEmptyTableKeepsTheBuiltInLadder()
        {
            DignityLevels.Load(new List<DignityLevelDto>());

            Assert.AreEqual(DignityType.Failed, DignityLevels.FromDignity(-801));
            Assert.AreEqual(DignityType.Default, DignityLevels.FromDignity(0));
        }
    }
}
