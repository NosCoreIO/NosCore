//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class LootTests
    {
        private const int Player = 1;
        private const int Monster = 3;
        private const int EtcPocket = 2;
        private const int Invite = 1;
        private const int Accept = 3;
        private const int LootMode = 8;
        private const int CannotPickUp = 112;
        private const int SharedByTheGroup = 561;

        private static Func<string, bool> Pebbles(int amount) =>
            packet => packet.StartsWith($"ivn {EtcPocket} 0.{WorldData.Pebble}.{amount}.", StringComparison.Ordinal);

        private static Func<string, bool> PreyOnTheMap =>
            packet => packet.StartsWith($"in {Monster} {WorldData.PreyVNum} {WorldData.Prey} ", StringComparison.Ordinal);

        // Kills the prey once it is on the map, however many hits that takes, and returns what it left:
        // drop <vnum> <id> <x> <y> <amount> <quest> <owner>
        private static async Task<string[]> KillPreyAsync(TestClient killer, int since)
        {
            await killer.ExpectSinceAsync(since, PreyOnTheMap, "the prey on the map");
            var tried = Stopwatch.StartNew();
            while (true)
            {
                var cast = killer.Mark();
                await killer.SendAsync($"u_s 0 {Monster} {WorldData.Prey}");
                var hit = (await killer.ExpectSinceAsync(cast, p => p.StartsWith($"su {Player} {killer.CharacterId} {Monster} {WorldData.Prey} ", StringComparison.Ordinal),
                    "the hit")).Split(' ');
                if (hit[11] == "0")
                {
                    return (await killer.ExpectSinceAsync(cast, p => p.StartsWith($"drop {WorldData.Pebble} ", StringComparison.Ordinal), "what it left")).Split(' ');
                }

                Assert.IsTrue(tried.Elapsed < TimeSpan.FromSeconds(30), "no hit landed in thirty seconds");
                await killer.ExpectSinceAsync(cast, p => p == "sr 0", "the skill ready again");
            }
        }

        private static async Task GroupAsync(TestClient inviter, TestClient newcomer)
        {
            await inviter.SendAsync($"pjoin {Invite} {newcomer.CharacterId}");
            await newcomer.ExpectAsync(p => p.Contains($"#pjoin^{Accept}^{inviter.CharacterId} ", StringComparison.Ordinal), "the group invitation");
            var joined = inviter.Mark();
            await newcomer.SendAsync($"pjoin {Accept} {inviter.CharacterId}");
            await inviter.ExpectSinceAsync(joined, p => p.StartsWith("pinit 2 ", StringComparison.Ordinal), "the group of two");
            await newcomer.ExpectAsync(p => p.StartsWith("pinit 2 ", StringComparison.Ordinal), "the group of two");
        }

        [TestMethod]
        public async Task WhatAMonsterLeavesBelongsToItsKillerAtFirst()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 71);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 31, 71);

            var drop = await KillPreyAsync(alice, 0);

            Assert.AreEqual(WorldData.PreyDrop.ToString(CultureInfo.InvariantCulture), drop[5]);
            Assert.AreEqual(alice.CharacterId.ToString(CultureInfo.InvariantCulture), drop[7], "the killer owns it");
            var refused = bob.Mark();
            var watched = alice.Mark();

            await bob.SendAsync($"get {Player} {bob.CharacterId} {drop[2]} 0");

            await bob.ExpectSinceAsync(refused, p => p.StartsWith($"sayi {Player} {bob.CharacterId} ", StringComparison.Ordinal)
                && p.Split(' ')[4] == CannotPickUp.ToString(CultureInfo.InvariantCulture), "the refusal");
            Assert.IsFalse(bob.Since(refused).Any(p => TestClient.HeaderOf(p) == "ivn"), "nothing reaches the bystander's bag");
            Assert.IsFalse(alice.Since(watched).Any(p => p.StartsWith($"get {Player} {bob.CharacterId} ", StringComparison.Ordinal)), "nothing is picked up");
            var picked = alice.Mark();

            await alice.SendAsync($"get {Player} {alice.CharacterId} {drop[2]} 0");

            await alice.ExpectSinceAsync(picked, Pebbles(WorldData.PreyDrop), "the pebbles in the killer's bag");
            await bob.ExpectSinceAsync(refused, p => p.StartsWith($"get {Player} {alice.CharacterId} {drop[2]}", StringComparison.Ordinal), "the pick-up");
        }

        [TestMethod]
        public async Task AGroupLootingInTurnHandsEachDropToTheNextMember()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 71);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 31, 71);
            await GroupAsync(alice, bob);

            var first = await KillPreyAsync(alice, 0);
            var again = alice.Mark();
            var second = await KillPreyAsync(alice, again);

            CollectionAssert.AreEquivalent(
                new[] { alice.CharacterId.ToString(CultureInfo.InvariantCulture), bob.CharacterId.ToString(CultureInfo.InvariantCulture) },
                new[] { first[7], second[7] }, "one drop each, whoever killed");

            var owner = second[7] == bob.CharacterId.ToString(CultureInfo.InvariantCulture) ? bob : alice;
            var other = owner == bob ? alice : bob;
            var refused = other.Mark();
            await other.SendAsync($"get {Player} {other.CharacterId} {second[2]} 0");
            await other.ExpectSinceAsync(refused, p => p.StartsWith($"sayi {Player} {other.CharacterId} ", StringComparison.Ordinal)
                && p.Split(' ')[4] == CannotPickUp.ToString(CultureInfo.InvariantCulture), "the member whose turn it is not, refused");
            var picked = owner.Mark();
            await owner.SendAsync($"get {Player} {owner.CharacterId} {second[2]} 0");
            await owner.ExpectSinceAsync(picked, Pebbles(WorldData.PreyDrop), "the pebbles in the bag of the member whose turn it is");
        }

        [TestMethod]
        public async Task WhenAGroupSharesItsLootAnyMemberMayPickUpWhatAnotherKilledFor()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 71);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 31, 71);
            await GroupAsync(alice, bob);
            var shared = bob.Mark();
            // Whichever of the two leads: the other one is refused, and only one of them asks at a time.
            await alice.SendAsync($"gop {LootMode} 1");
            await alice.ExpectAsync("stat");
            await bob.SendAsync($"gop {LootMode} 1");
            await bob.ExpectSinceAsync(shared, p => p.Split(' ') is ["msgi", _, var id, ..] && id == SharedByTheGroup.ToString(CultureInfo.InvariantCulture),
                "the new way of looting announced");

            var drop = await KillPreyAsync(alice, 0);

            Assert.AreEqual(alice.CharacterId.ToString(CultureInfo.InvariantCulture), drop[7], "the killer owns it");
            var picked = bob.Mark();
            await bob.SendAsync($"get {Player} {bob.CharacterId} {drop[2]} 0");
            await bob.ExpectSinceAsync(picked, Pebbles(WorldData.PreyDrop), "the pebbles in the other member's bag");
        }

        [TestMethod]
        public async Task SomethingLyingTooFarAwayCannotBePickedUp()
        {
            var owner = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 20, 85);
            await Harness.GiveAsync(owner, WorldData.Seed, 5);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 32, 85);
            await using var alice = Harness.Client(owner);
            await alice.EnterWorldAsync(owner.Slot);
            await alice.SendAsync($"put {EtcPocket} 0 5");
            var drop = (await bob.ExpectAsync(p => p.StartsWith($"drop {WorldData.Seed} ", StringComparison.Ordinal), "the drop")).Split(' ');
            var reached = bob.Mark();
            var watched = alice.Mark();

            await bob.SendAsync($"get {Player} {bob.CharacterId} {drop[2]} 0");

            Assert.IsTrue(await alice.NoneSinceAsync(watched, p => p.StartsWith($"get {Player} {bob.CharacterId} ", StringComparison.Ordinal),
                TimeSpan.FromSeconds(1)), "nothing is picked up from eleven cells away");
            Assert.IsFalse(bob.Since(reached).Any(p => TestClient.HeaderOf(p) == "ivn"), "and nothing reaches his bag");
            var picked = alice.Mark();
            await alice.SendAsync($"get {Player} {alice.CharacterId} {drop[2]} 0");
            await alice.ExpectSinceAsync(picked, p => p.StartsWith($"ivn {EtcPocket} 0.{WorldData.Seed}.5.", StringComparison.Ordinal), "the seeds still there for her");
        }
    }
}
