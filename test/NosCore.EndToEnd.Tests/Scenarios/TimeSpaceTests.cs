//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.MiniMap;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class TimeSpaceTests
    {
        private static Func<WpPacket, bool> TheStone =>
            packet => packet.PositionX == WorldData.StoneX && packet.PositionY == WorldData.StoneY;

        [TestMethod]
        public async Task AStoneClearedOnAnEarlierSessionIsShownDone()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 10, 90);
            await Harness.CompleteTimeSpaceAsync(player, WorldData.StoneTimeSpace);
            await using var alice = Harness.Client(player);

            await alice.EnterWorldAsync(player.Slot);

            var stone = await alice.ExpectSinceAsync(0, TheStone, "the time-space stone");
            Assert.AreEqual(WpPortalType.NormalTsDone, stone.PortalType);
        }

        [TestMethod]
        public async Task AStoneNotClearedIsShownOpen()
        {
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 10, 90);

            var stone = await bob.ExpectSinceAsync(0, TheStone, "the time-space stone");
            Assert.AreEqual(WpPortalType.NormalTs, stone.PortalType);
        }
    }
}
