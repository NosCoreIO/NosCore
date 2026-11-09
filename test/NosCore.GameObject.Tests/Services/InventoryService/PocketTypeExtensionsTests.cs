//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using System;

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    [TestClass]
    public class PocketTypeExtensionsTests
    {
        [TestMethod]
        public void WornEquipmentIsOneHundredOnTheWireAndEightInStorage()
        {
            Assert.AreEqual(PocketType.Wear, NoscorePocketType.Wear.ToPocketType());
            Assert.AreEqual(NoscorePocketType.Wear, PocketType.Wear.ToNoscorePocketType());
        }

        [TestMethod]
        public void EveryWirePocketComesBackUnchanged()
        {
            foreach (var pocket in Enum.GetValues<PocketType>())
            {
                Assert.AreEqual(pocket, pocket.ToNoscorePocketType().ToPocketType());
            }
        }

        [TestMethod]
        public void EveryOtherStoragePocketKeepsItsNumber()
        {
            foreach (var pocket in Enum.GetValues<NoscorePocketType>())
            {
                if (pocket == NoscorePocketType.Wear)
                {
                    continue;
                }

                Assert.AreEqual((byte)pocket, (byte)pocket.ToPocketType());
            }
        }
    }
}
