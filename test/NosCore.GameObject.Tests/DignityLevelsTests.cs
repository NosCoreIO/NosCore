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
    // Both sides of every boundary: the second dignity ladder that used to live in
    // PlayerBundleExtensions was off by one at every step and looked right mid-band.
    [TestClass]
    public class DignityLevelsTests
    {
        private static readonly (short Top, DignityType Icon)[] Bands =
        {
            (-801, DignityType.Failed),
            (-601, DignityType.Useless),
            (-401, DignityType.Unqualified),
            (-201, DignityType.Dreadful),
            (-100, DignityType.Dubious)
        };

        private readonly LadderCatalog _ladders = new([], ClientLadders.DignityLevels());

        [TestMethod]
        public void TheHighestValueOfEachBandDrawsThatBandsIcon()
        {
            foreach ((short top, DignityType icon) in Bands)
            {
                Assert.AreEqual(icon, _ladders.FromDignity(top),
                    $"dignity {top} is the highest value of the {icon} band");
            }
        }

        [TestMethod]
        public void OneAboveEachBandDrawsTheGentlerIcon()
        {
            for (var i = 0; i < Bands.Length - 1; i++)
            {
                Assert.AreEqual(Bands[i + 1].Icon, _ladders.FromDignity((short)(Bands[i].Top + 1)),
                    $"dignity {Bands[i].Top + 1} sits one above the {Bands[i].Icon} band");
            }

            Assert.AreEqual(DignityType.Default, _ladders.FromDignity((short)(Bands[^1].Top + 1)));
        }

        [TestMethod]
        public void EveryBoundaryActuallyChangesTheIcon()
        {
            foreach ((short top, DignityType icon) in Bands)
            {
                Assert.AreNotEqual(icon, _ladders.FromDignity((short)(top + 1)),
                    $"crossing {top} must change the icon");
            }
        }

        [TestMethod]
        public void UntouchedDignityIsDefault()
        {
            Assert.AreEqual(DignityType.Default, _ladders.FromDignity(0));
            Assert.AreEqual(DignityType.Default, _ladders.FromDignity(100));

            // The client declares no band between -1 and -99, so Default has to carry them.
            Assert.AreEqual(DignityType.Default, _ladders.FromDignity(-1));
            Assert.AreEqual(DignityType.Default, _ladders.FromDignity(-99));
        }

        [TestMethod]
        public void DignityBelowTheLastBandStillDrawsTheWorstIcon()
        {
            Assert.AreEqual(DignityType.Failed, _ladders.FromDignity(-1000));
            Assert.AreEqual(DignityType.Failed, _ladders.FromDignity(short.MinValue));
        }

        [TestMethod]
        public void TheMaximumIsTheCeilingOfTheDefaultBand()
        {
            Assert.AreEqual((short)100, _ladders.MaxDignity);
        }

        [TestMethod]
        public void ALadderWithoutADefaultCeilingHasNoMaximum()
        {
            var ladders = new LadderCatalog([], new List<DignityLevelDto>
            {
                new() { DignityLevelId = (byte)DignityType.Default, MaxDignity = null },
                new() { DignityLevelId = (byte)DignityType.Dubious, MaxDignity = -5 }
            });

            Assert.IsNull(ladders.MaxDignity);
        }

        [TestMethod]
        public void TheLadderIsWhateverWasImported()
        {
            var ladders = new LadderCatalog([], new List<DignityLevelDto>
            {
                new() { DignityLevelId = (byte)DignityType.Default, MaxDignity = null },
                new() { DignityLevelId = (byte)DignityType.Dubious, MaxDignity = -5 }
            });

            Assert.AreEqual(DignityType.Default, ladders.FromDignity(-4));
            Assert.AreEqual(DignityType.Dubious, ladders.FromDignity(-5));
            Assert.AreEqual(DignityType.Dubious, ladders.FromDignity(short.MinValue));
        }
    }
}
