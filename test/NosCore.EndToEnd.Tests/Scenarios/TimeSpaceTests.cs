//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
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
            Assert.AreEqual(WpPortalType.NormalTsReachable, stone.PortalType);
        }

        [TestMethod]
        public async Task SteppingOntoAStoneOpensItsCard()
        {
            await using var carol = await Harness.EnterAsync("Carol", WorldData.Meadow, WorldData.SampleTimeSpaceX,
                (short)(WorldData.SampleTimeSpaceY + 1));
            await carol.WalkAsync(WorldData.SampleTimeSpaceX, WorldData.SampleTimeSpaceY);
            var stepping = carol.Mark();

            await carol.SendAsync(new WreqPacket());

            var card = await carol.ExpectSinceAsync<RbrPacket>(stepping, description: "the time-space card");
            Assert.IsFalse(card.LoserMode);
        }
    }
}
