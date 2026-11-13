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
