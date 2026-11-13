//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class MapTests
    {
        // A portal cannot be taken in the four seconds after arriving on a map.
        private static readonly TimeSpan PortalRest = TimeSpan.FromSeconds(4.2);

        [TestMethod]
        public async Task APlayerWhoArrivesIsShownToThoseAlreadyThere()
        {
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            var alice = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            await using var aliceClient = Harness.Client(alice);

            await aliceClient.EnterWorldAsync(alice.Slot);

            var arrival = await bob.ExpectAsync(p => p.StartsWith($"in 1 {alice.Name} ", StringComparison.Ordinal), "in for Alice");
            StringAssert.Contains(arrival, $" {alice.CharacterId} 80 116 ");
            Assert.IsTrue(aliceClient.Received.Any(p => p.StartsWith("in 1 Bob", StringComparison.Ordinal)),
                "the newcomer is shown who was already there");
        }

        [TestMethod]
        public async Task AStepIsSeenByTheOtherPlayer()
        {
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);

            await alice.WalkAsync(81, 117);

            var step = await bob.ExpectAsync(p => p.StartsWith($"mv 1 {alice.CharacterId} ", StringComparison.Ordinal), "mv for Alice");
            Assert.AreEqual($"mv 1 {alice.CharacterId} 81 117 {alice.Speed}", step);
        }

        [TestMethod]
        public async Task WhatIsSaidIsHeardByTheOthersOnly()
        {
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);

            await alice.SendAsync("say hello there");

            var heard = await bob.ExpectAsync("say");
            Assert.AreEqual($"say 1 {alice.CharacterId} 0 hello there", heard);
            Assert.IsNull(await alice.WaitAsync(p => TestClient.HeaderOf(p) == "say", TimeSpan.FromSeconds(1)),
                "the speaker's client draws its own line");
        }

        [TestMethod]
        public async Task APortalLeadsToTheOtherMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 89, y: 116);
            await Task.Delay(PortalRest);

            await alice.WalkAsync(90, 116);
            await alice.SendAsync("preq");

            var at = await alice.ExpectAsync("at");
            Assert.AreEqual($"at {alice.CharacterId} {WorldData.Meadow} 10 10", string.Join(' ', at.Split(' ').Take(5)));
        }

        [TestMethod]
        public async Task APortalIsRefusedRightAfterArriving()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 90, y: 116);

            await alice.SendAsync("preq");

            // 94 is the client's "You will be able to move shortly".
            await alice.ExpectAsync(p => p == $"sayi 1 {alice.CharacterId} 10 94 0 0 0 0 0", "sayi 94");
            Assert.IsNull(await alice.WaitAsync(p => TestClient.HeaderOf(p) == "at", TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public async Task APlayerWhoLeavesIsSavedWhereTheyStood()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            await using (var client = Harness.Client(player))
            {
                await client.EnterWorldAsync(player.Slot);
                await client.WalkAsync(81, 117);
                await Task.Delay(500);
            }

            await SavedAtAsync(player, 81, 117);

            await using var back = Harness.Client(player);
            var entry = await back.EnterWorldAsync(player.Slot);
            var at = entry.Single(p => TestClient.HeaderOf(p) == "at").Split(' ');
            Assert.AreEqual($"{WorldData.Town} 81 117", string.Join(' ', at.Skip(2).Take(3)));
        }

        private static async Task SavedAtAsync(Player player, short x, short y)
        {
            var waited = Stopwatch.StartNew();
            while (waited.Elapsed < TimeSpan.FromSeconds(15))
            {
                await using var context = Harness.Database.CreateContext();
                var saved = await context.Set<NosCore.Database.Entities.Character>().AsNoTracking().SingleAsync(c => c.CharacterId == player.CharacterId);
                if (saved.MapX == x && saved.MapY == y)
                {
                    return;
                }

                await Task.Delay(250);
            }

            Assert.Fail($"{player.Name} was not saved at {x},{y} within 15 s of leaving.");
        }
    }
}
