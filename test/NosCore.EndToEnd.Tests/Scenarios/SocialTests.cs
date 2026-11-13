//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class SocialTests
    {
        [TestMethod]
        public async Task AnAcceptedFriendRequestPutsEachInTheOthersList()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            await alice.SendAsync($"fins 1 {bob.CharacterId}");
            await bob.ExpectAsync(p => p.StartsWith($"dlgi2 #fins^1^{alice.CharacterId} #fins^2^{alice.CharacterId} ", StringComparison.Ordinal),
                "the friend request");
            var accepted = alice.Mark();
            await bob.SendAsync($"fins 1 {alice.CharacterId}");

            await alice.ExpectSinceAsync(accepted, p => p.StartsWith($"finit {bob.CharacterId}|", StringComparison.Ordinal), "Bob in Alice's list");
            await bob.ExpectSinceAsync(0, p => p.StartsWith($"finit {alice.CharacterId}|", StringComparison.Ordinal), "Alice in Bob's list");
        }

        [TestMethod]
        public async Task AFriendMessageReachesTheFriend()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await alice.SendAsync($"fins 1 {bob.CharacterId}");
            await bob.ExpectAsync("dlgi2");
            await bob.SendAsync($"fins 1 {alice.CharacterId}");
            await alice.ExpectAsync(p => p.StartsWith($"finit {bob.CharacterId}|", StringComparison.Ordinal), "Bob in Alice's list");

            await alice.SendAsync($"btk {bob.CharacterId} lunch at noon");

            Assert.AreEqual($"talk {alice.CharacterId} lunch at noon", await bob.ExpectAsync("talk"));
        }

        [TestMethod]
        public async Task AnAcceptedInvitationFormsAGroup()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            await alice.SendAsync($"pjoin 1 {bob.CharacterId}");
            await bob.ExpectAsync(p => p.StartsWith("dlgi", StringComparison.Ordinal) && p.Contains($"#pjoin^3^{alice.CharacterId}", StringComparison.Ordinal),
                "the group invitation");
            var joined = alice.Mark();
            await bob.SendAsync($"pjoin 3 {alice.CharacterId}");

            var group = await alice.ExpectSinceAsync(joined, p => p.StartsWith("pinit 2 ", StringComparison.Ordinal), "the group of two");
            StringAssert.Contains(group, $"|{bob.CharacterId}|");
            StringAssert.Contains(group, $"|{alice.CharacterId}|");
        }

        [TestMethod]
        public async Task GroupedPlayersWhoLeaveAtTheSameMomentAreAllStoredWhereTheyStood()
        {
            var players = new[]
            {
                await Harness.AddPlayerAsync("Alice", x: 80, y: 116),
                await Harness.AddPlayerAsync("Bob", x: 81, y: 116),
                await Harness.AddPlayerAsync("Carol", x: 82, y: 116)
            };
            var clients = players.Select(player => Harness.Client(player)).ToArray();
            try
            {
                for (var i = 0; i < clients.Length; i++)
                {
                    await clients[i].EnterWorldAsync(players[i].Slot);
                }

                var alice = clients[0];
                // The others ask to join in turn: one player may only send a group request every five seconds.
                for (var i = 1; i < clients.Length; i++)
                {
                    await clients[i].SendAsync($"pjoin 1 {alice.CharacterId}");
                    await alice.ExpectAsync(p => p.Contains($"#pjoin^3^{clients[i].CharacterId}", StringComparison.Ordinal), "the group request");
                    var joined = clients[i].Mark();
                    await alice.SendAsync($"pjoin 3 {clients[i].CharacterId}");
                    await clients[i].ExpectSinceAsync(joined, p => p.StartsWith($"pinit {i + 1} ", StringComparison.Ordinal), $"the group of {i + 1}");
                }

                for (var i = 0; i < clients.Length; i++)
                {
                    var watcher = clients[(i + 1) % clients.Length];
                    await clients[i].WalkAsync((short)(80 + i), 117);
                    await watcher.ExpectAsync(p => p.StartsWith($"mv 1 {clients[i].CharacterId} {80 + i} 117 ", StringComparison.Ordinal), "the step");
                }
            }
            finally
            {
                await Task.WhenAll(clients.Select(client => client.DisposeAsync().AsTask()));
            }

            var waited = Stopwatch.StartNew();
            var pending = players.ToList();
            while (pending.Count > 0 && waited.Elapsed < TimeSpan.FromSeconds(15))
            {
                foreach (var player in pending.ToList())
                {
                    if ((await Harness.StoredAsync(player.CharacterId)).MapY == 117)
                    {
                        pending.Remove(player);
                    }
                }

                await Task.Delay(250);
            }

            Assert.AreEqual(string.Empty, string.Join(", ", pending.Select(p => p.Name)), "not stored where they stood within 15 s of leaving");
        }

        [TestMethod]
        public async Task WhatIsSaidToTheGroupReachesItsMembersOnly()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await alice.SendAsync($"pjoin 1 {bob.CharacterId}");
            await bob.ExpectAsync(p => p.Contains($"#pjoin^3^{alice.CharacterId}", StringComparison.Ordinal), "the group invitation");
            await bob.SendAsync($"pjoin 3 {alice.CharacterId}");
            await alice.ExpectAsync(p => p.StartsWith("pinit 2 ", StringComparison.Ordinal), "the group of two");
            var said = carol.Mark();

            await alice.SendAsync(";pull the next one");

            var heard = await bob.ExpectAsync("spk");
            StringAssert.StartsWith(heard, $"spk 1 {alice.CharacterId} 3 ");
            StringAssert.EndsWith(heard, "pull the next one");
            Assert.IsTrue(await carol.NoneSinceAsync(said, p => TestClient.HeaderOf(p) == "spk", TimeSpan.FromSeconds(1)),
                "someone outside the group hears nothing");
        }
    }
}
