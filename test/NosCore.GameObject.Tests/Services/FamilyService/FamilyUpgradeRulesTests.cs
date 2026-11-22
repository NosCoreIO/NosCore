//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.FamilyUpgradeService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.FamilyService;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyUpgradeRulesTests
    {
        private readonly FamilyUpgradeService _capacities = new();

        private static FamilyUpgradeDto Warehouse(byte level, byte minimum) =>
            new() { UpgradeType = FamilyUpgradeType.Warehouse, UpgradeLevel = level, FamilyLevelMinimum = minimum };

        private static FamilyUpgradeDto Members(byte level, byte minimum) =>
            new() { UpgradeType = FamilyUpgradeType.MemberLimit, UpgradeLevel = level, FamilyLevelMinimum = minimum };

        private FamilyDto Family(byte level, byte warehouseSize = 0, byte maxSize = 50) =>
            new() { Name = "Testers", FamilyLevel = level, WarehouseSize = warehouseSize, MaxSize = maxSize, FamilyMessage = string.Empty };

        [TestMethod]
        public void TheFirstWarehouseIsPurchasableOnceTheFamilyReachesItsLevel()
        {
            Assert.AreEqual(FamilyUpgradeState.Purchasable, Family(2).StateOf(Warehouse(0, 2), _capacities));
        }

        [TestMethod]
        public void AnUpgradeAboveTheFamilyLevelIsUnavailable()
        {
            Assert.AreEqual(FamilyUpgradeState.Unavailable, Family(1).StateOf(Warehouse(0, 2), _capacities));
        }

        [TestMethod]
        public void AnExpansionNeedsTheUpgradeBeforeIt()
        {
            Assert.AreEqual(FamilyUpgradeState.Unavailable, Family(10).StateOf(Warehouse(1, 7), _capacities));
        }

        [TestMethod]
        public void AnExpansionIsPurchasableOnceTheUpgradeBeforeItIsOwned()
        {
            var family = Family(10, _capacities.GetWarehouseSize(1));

            Assert.AreEqual(FamilyUpgradeState.Purchased, family.StateOf(Warehouse(0, 2), _capacities));
            Assert.AreEqual(FamilyUpgradeState.Purchasable, family.StateOf(Warehouse(1, 7), _capacities));
        }

        [TestMethod]
        public void AnOwnedUpgradeStaysPurchasedEvenAboveTheFamilyLevel()
        {
            Assert.AreEqual(FamilyUpgradeState.Purchased, Family(1, _capacities.GetWarehouseSize(2)).StateOf(Warehouse(1, 7), _capacities));
        }

        [TestMethod]
        public void TheMemberLineIsReadFromTheMemberLimit()
        {
            var family = Family(10, maxSize: _capacities.GetMemberLimit(1));

            Assert.AreEqual(FamilyUpgradeState.Purchased, family.StateOf(Members(0, 5), _capacities));
            Assert.AreEqual(FamilyUpgradeState.Purchasable, family.StateOf(Members(1, 9), _capacities));
            Assert.AreEqual(FamilyUpgradeState.Purchasable, family.StateOf(Warehouse(0, 2), _capacities));
        }

        [TestMethod]
        public void ApplyingAWarehouseUpgradeGrowsOnlyTheWarehouse()
        {
            var family = Family(2);

            family.Apply(Warehouse(0, 2), _capacities);

            Assert.AreEqual(_capacities.GetWarehouseSize(1), family.WarehouseSize);
            Assert.AreEqual(50, family.MaxSize);
        }

        [TestMethod]
        public void ApplyingAMemberUpgradeRaisesOnlyTheMemberLimit()
        {
            var family = Family(5);

            family.Apply(Members(0, 5), _capacities);

            Assert.AreEqual(_capacities.GetMemberLimit(1), family.MaxSize);
            Assert.AreEqual(0, family.WarehouseSize);
        }
    }
}
