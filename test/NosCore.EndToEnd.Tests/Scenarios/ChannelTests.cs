//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ChannelTests
    {
        private const int Second = 1;

        [TestMethod]
        public async Task TheLoginServerListsEveryChannel()
        {
            await using var client = Harness.Client(await Harness.AddPlayerAsync("Alice"));

            var reply = await client.LoginAsync();

            Assert.AreEqual(2, reply.Channels.Count, reply.Raw);
            Assert.AreNotEqual(reply.Channels[0].Port, reply.Channels[1].Port);
        }

        [TestMethod]
        public async Task PlayersOnDifferentChannelsDoNotSeeEachOther()
        {
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116, channel: Second);
            var arrived = bob.Mark();

            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);

            Assert.IsTrue(await bob.NoneSinceAsync(arrived, p => p.StartsWith("in 1 Alice", StringComparison.Ordinal), TimeSpan.FromSeconds(1)),
                "a player on the other channel is not shown");
            Assert.IsTrue(await alice.NoneSinceAsync(0, p => p.StartsWith("in 1 Bob", StringComparison.Ordinal), TimeSpan.Zero));
        }

        [TestMethod]
        public async Task AWhisperReachesAPlayerOnAnotherChannel()
        {
            var bobPlayer = await Harness.AddPlayerAsync("Bob");
            await using var bob = Harness.Client(bobPlayer, channel: Second);
            await bob.EnterWorldAsync(bobPlayer.Slot);
            await using var alice = await Harness.EnterAsync("Alice");

            await alice.SendAsync($"/{bobPlayer.Name} are you on channel two");

            var heard = await bob.ExpectAsync("spk");
            StringAssert.StartsWith(heard, $"spk 1 {alice.CharacterId} ");
            StringAssert.Contains(heard, "are you on channel two");
        }

        [TestMethod]
        public async Task AnAccountInTheWorldIsRefusedOnTheOtherChannelToo()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var first = Harness.Client(player);
            await first.EnterWorldAsync(player.Slot);
            await using var second = Harness.Client(player, channel: Second);

            var reply = await second.LoginAsync();

            Assert.AreEqual("failc 4", reply.Refusal);
        }
    }
}
