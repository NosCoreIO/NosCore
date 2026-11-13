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
    public class TradeTests
    {
        [TestMethod]
        public async Task GoldOfferedInATradeChangesHands()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: c => c.Gold = 1000);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            await alice.SendAsync($"req_exc 1 {bob.CharacterId}");
            await bob.ExpectAsync(p => p.StartsWith($"dlgi2 #req_exc^2^{alice.CharacterId} ", StringComparison.Ordinal), "the trade invitation");
            await bob.SendAsync($"req_exc 2 {alice.CharacterId}");
            await alice.ExpectAsync("exc_list");
            await bob.ExpectAsync("exc_list");

            await alice.SendAsync("exc_list 400 0");
            await bob.ExpectAsync(p => p.StartsWith($"exc_list 1 {alice.CharacterId} 400 0", StringComparison.Ordinal), "Alice's offer");
            await bob.SendAsync("exc_list 0 0");
            await alice.ExpectAsync(p => p.StartsWith($"exc_list 1 {bob.CharacterId} 0 0", StringComparison.Ordinal), "Bob's offer");

            await alice.SendAsync("req_exc 3");
            await bob.SendAsync("req_exc 3");

            await alice.ExpectAsync(p => p == "gold 600 0", "Alice's gold");
            await alice.ExpectAsync(p => p == "exc_close 1", "the trade closing");
            await bob.ExpectAsync(p => p == "gold 400 0", "Bob's gold");
            await bob.ExpectAsync(p => p == "exc_close 1", "the trade closing");
            Assert.AreEqual(600, (await Harness.StoredAsync(alice.CharacterId)).Gold, "a settled trade is stored at once");
            Assert.AreEqual(400, (await Harness.StoredAsync(bob.CharacterId)).Gold);
        }

        [TestMethod]
        public async Task ADeclinedTradeMovesNothing()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: c => c.Gold = 1000);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            var asked = alice.Mark();
            await alice.SendAsync($"req_exc 1 {bob.CharacterId}");
            await bob.ExpectAsync("dlgi2");
            await bob.SendAsync($"req_exc 5 {alice.CharacterId}");

            // 178 is the client's line for a trade the other side cancelled.
            await alice.ExpectAsync(p => p.StartsWith($"sayi 1 {alice.CharacterId} 10 178 ", StringComparison.Ordinal), "sayi 178");
            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "exc_list" or "gold", TimeSpan.FromSeconds(1)),
                "no trade window opens and no gold moves");
        }
    }
}
