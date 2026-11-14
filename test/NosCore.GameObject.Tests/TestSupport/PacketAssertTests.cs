//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.TestSupport
{
    [TestClass]
    public class PacketAssertTests
    {
        private static EInfoShellPacket Shell(int value) => new()
        {
            ItemVNum = 565,
            ShellOptionCount = 1,
            ShellOptions = [new EInfoShellOptionSubPacket { Level = 1, Type = 1, Value = value }],
        };

        [TestMethod]
        public void PacketsWithTheSameValuesAreEqual()
        {
            PacketAssert.AreEqual(Shell(50), Shell(50));
        }

        [TestMethod]
        public void TheHeaderIsNotAField()
        {
            var sent = Shell(50);
            sent.Header = "e_info";

            PacketAssert.AreEqual(Shell(50), sent);
        }

        [TestMethod]
        public void ADifferenceInAListNamesTheField()
        {
            var failure = Assert.ThrowsExactly<AssertFailedException>(() => PacketAssert.AreEqual(Shell(50), Shell(51)));

            StringAssert.Contains(failure.Message, "EInfoShellPacket.ShellOptions[0].Value: expected 50, was 51");
        }

        [TestMethod]
        public void AMissingListEntryIsADifference()
        {
            var actual = Shell(50);
            actual.ShellOptions = [];

            var failure = Assert.ThrowsExactly<AssertFailedException>(() => PacketAssert.AreEqual(Shell(50), actual));

            StringAssert.Contains(failure.Message, "EInfoShellPacket.ShellOptions: expected 1 entries, was 0");
        }
    }
}
