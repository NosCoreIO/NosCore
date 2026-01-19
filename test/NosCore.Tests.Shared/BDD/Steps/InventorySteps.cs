//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Networking.ClientSession;
using System.Linq;

namespace NosCore.Tests.Shared.BDD.Steps
{
    public static class InventorySteps
    {
        public static void AssertInventoryEmpty(this ClientSession session)
        {
            Assert.AreEqual(0, session.Character.InventoryService.Count);
        }

        public static void AssertInventoryCount(this ClientSession session, int expected)
        {
            Assert.AreEqual(expected, session.Character.InventoryService.Count);
        }

        public static void AssertInventoryContains(this ClientSession session, int vnum, int amount)
        {
            var item = session.Character.InventoryService.FirstOrDefault(
                i => i.Value.ItemInstance.ItemVNum == vnum);
            Assert.IsNotNull(item.Value, $"Expected item {vnum} not found in inventory");
            Assert.AreEqual(amount, item.Value.ItemInstance.Amount);
        }

        public static void AssertInventoryItemAmount(this ClientSession session, int slot, int expectedAmount)
        {
            var item = session.Character.InventoryService.FirstOrDefault();
            Assert.IsNotNull(item.Value, "No item found in inventory");
            Assert.AreEqual(expectedAmount, item.Value.ItemInstance.Amount);
        }

        public static void AssertGold(this ClientSession session, long expected)
        {
            Assert.AreEqual(expected, session.Character.Gold);
        }

        public static void AssertGoldGreaterThan(this ClientSession session, long minimum)
        {
            Assert.IsTrue(session.Character.Gold > minimum,
                $"Expected gold > {minimum}, but was {session.Character.Gold}");
        }

        public static void AssertInventorySlotEmpty(this ClientSession session, short slot, NoscorePocketType type)
        {
            Assert.IsNull(session.Character.InventoryService.LoadBySlotAndType(slot, type));
        }
    }
}
