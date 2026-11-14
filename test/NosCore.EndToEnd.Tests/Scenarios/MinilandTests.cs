//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class MinilandTests
    {
        private const int Player = 1;
        private const int Npc = 2;
        private const int EtcPocket = 2;
        private const int EditIntroduction = 1;
        private const int EditState = 2;
        private const int Open = 0;
        private const int Locked = 2;
        private const int MinilandIsLocked = 182;
        private const int OnlyFromTheMiniland = 307;
        private const int NoSeedForAnInvitation = 308;
        private const int InvitedToAMiniland = 312;
        private const int OnlyWhileLocked = 420;
        private const int RemovalOnlyWhileLocked = 424;
        private const int MinilandIsOpen = 425;

        // A portal is refused for four seconds after arriving on a map.
        private static readonly TimeSpan PortalRest = TimeSpan.FromSeconds(4.2);

        private static Func<string, bool> Told(int message) =>
            packet => packet.Split(' ') is ["infoi", var id, ..] && id == message.ToString(CultureInfo.InvariantCulture);

        private static Func<string, bool> Announced(int message) =>
            packet => packet.Split(' ') is ["msgi", _, var id, ..] && id == message.ToString(CultureInfo.InvariantCulture);

        private static Func<string, bool> InTheMiniland(TestClient who) =>
            packet => packet.StartsWith($"at {who.CharacterId} {WorldData.MinilandMap} 5 8 ", StringComparison.Ordinal);

        private static Task<Player> OwnerAsync() => Harness.AddPlayerAsync("Alice", x: 71, y: 116);

        private static async Task<TestClient> EnterAsync(Player player)
        {
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }

            return client;
        }

        // The owner walks into the miniland through its portal: a portal only takes someone who
        // has moved since arriving, so she steps onto it or, standing on it already, beside it.
        private static async Task<int> GoHomeAsync(TestClient owner, short x = 70)
        {
            await Task.Delay(PortalRest);
            await owner.WalkAsync(x, 116);
            var entered = owner.Mark();
            await owner.SendAsync("preq");
            await owner.ExpectSinceAsync(entered, InTheMiniland(owner), "the miniland");
            await owner.ExpectSinceAsync(entered, p => TestClient.HeaderOf(p) == "mlobjlst", "what stands in it");
            return entered;
        }

        private static async Task BefriendAsync(TestClient one, TestClient other)
        {
            await one.SendAsync($"fins 1 {other.CharacterId}");
            await other.ExpectAsync(p => p.StartsWith($"dlgi2 #fins^1^{one.CharacterId} ", StringComparison.Ordinal), "the friend request");
            var accepted = one.Mark();
            await other.SendAsync($"fins 1 {one.CharacterId}");
            await one.ExpectSinceAsync(accepted, p => p.StartsWith($"finit {other.CharacterId}|", StringComparison.Ordinal), "the new friend");
        }

        private static async Task SetStateAsync(TestClient owner, int state, int announcement)
        {
            var changed = owner.Mark();
            await owner.SendAsync($"mledit {EditState} {state}");
            await owner.ExpectSinceAsync(changed, Announced(announcement), "the state of the miniland announced");
        }

        [TestMethod]
        public async Task TheOwnerWalksIntoTheMinilandAndIsShownItsState()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await Task.Delay(PortalRest);
            await alice.WalkAsync(70, 116);
            var entered = alice.Mark();

            await alice.SendAsync("preq");

            await alice.ExpectSinceAsync(entered, InTheMiniland(alice), "the miniland");
            // mlinfo <music> <points> <?> <visits today> <visits> <?> <state> <music> <message>
            var info = (await alice.ExpectSinceAsync(entered, p => TestClient.HeaderOf(p) == "mlinfo", "the miniland's state")).Split(' ');
            Assert.AreEqual("0 0", $"{info[4]} {info[5]}", "the owner is no visitor");
            Assert.AreEqual(Open.ToString(CultureInfo.InvariantCulture), info[7], "a new miniland is open");
        }

        [TestMethod]
        public async Task AVisitorIsShownTheOwnersMessageAsItWasWritten()
        {
            var owner = await OwnerAsync();
            await using (var context = Harness.Database.CreateContext())
            {
                context.Add(new Miniland
                {
                    MinilandId = Guid.NewGuid(), OwnerId = owner.CharacterId, State = MinilandState.Open,
                    MinilandMessage = "Come in and rest", WelcomeMusicInfo = 3800
                });
                await context.SaveChangesAsync();
            }

            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            var visited = bob.Mark();

            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");

            await bob.ExpectSinceAsync(visited, p => p == "msg 0 Come in and rest", "the owner's message, spaces and all");
        }

        [TestMethod]
        public async Task TheOwnerWritesANewIntroductionAndAVisitorIsShownIt()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            var written = alice.Mark();

            // The client writes the text straight after the kind of change, spaces and all.
            await alice.SendAsync($"mledit {EditIntroduction} Come in and rest");

            await alice.ExpectSinceAsync(written, p => p == "mlintro Come^in^and^rest", "the introduction as the owner's window shows it");
            var visited = bob.Mark();
            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");
            await bob.ExpectSinceAsync(visited, p => p == "msg 0 Come in and rest", "the introduction across the visitor's screen");
            StringAssert.EndsWith(await bob.ExpectSinceAsync(visited, p => TestClient.HeaderOf(p) == "mlinfobr", "whose miniland it is"),
                " Come^in^and^rest");
        }

        [TestMethod]
        public async Task ANewIntroductionIsStoredWhenItsOwnerLeaves()
        {
            var owner = await OwnerAsync();
            await using (var alice = await EnterAsync(owner))
            {
                await GoHomeAsync(alice);
                await alice.SendAsync($"mledit {EditIntroduction} Back in five minutes");
                await alice.ExpectAsync(p => p == "mlintro Back^in^five^minutes", "the introduction as the owner's window shows it");
            }

            await using var back = await Harness.ComeBackAsync(owner);
            await using var context = Harness.Database.CreateContext();
            Assert.AreEqual("Back in five minutes",
                (await context.Set<Miniland>().AsNoTracking().SingleAsync(m => m.OwnerId == owner.CharacterId)).MinilandMessage);
        }

        [TestMethod]
        public async Task AFriendWalksIntoAnOpenMinilandAndIsCountedAsAVisitor()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            var visited = bob.Mark();
            var seen = alice.Mark();

            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");

            await bob.ExpectSinceAsync(visited, InTheMiniland(bob), "the miniland");
            // mlinfobr <music> <owner> <visits today> <visits> <?> <message>
            var info = (await bob.ExpectSinceAsync(visited, p => TestClient.HeaderOf(p) == "mlinfobr", "whose miniland it is")).Split(' ');
            StringAssert.StartsWith(info[2], "Alice");
            Assert.AreEqual("1 1", $"{info[3]} {info[4]}", "the first visitor of the day");
            await alice.ExpectSinceAsync(seen, p => p.StartsWith($"in {Player} ", StringComparison.Ordinal)
                && p.Split(' ')[4] == bob.CharacterId.ToString(CultureInfo.InvariantCulture), "the visitor arriving");
        }

        [TestMethod]
        public async Task AStrangerIsNotLetIntoAMiniland()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await GoHomeAsync(alice);
            var refused = bob.Mark();

            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");

            Assert.IsTrue(await bob.NoneSinceAsync(refused, p => TestClient.HeaderOf(p) is "at" or "c_map", TimeSpan.FromSeconds(1)),
                "he stays where he is");
        }

        [TestMethod]
        public async Task ALockedMinilandRefusesEvenAFriend()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            await SetStateAsync(alice, Locked, MinilandIsLocked);
            var refused = bob.Mark();

            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");

            await bob.ExpectSinceAsync(refused, Told(MinilandIsLocked), "the refusal");
            Assert.IsFalse(bob.Since(refused).Any(p => TestClient.HeaderOf(p) is "at" or "c_map"), "he stays where he is");

            await SetStateAsync(alice, Open, MinilandIsOpen);
            var visited = bob.Mark();
            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");
            await bob.ExpectSinceAsync(visited, InTheMiniland(bob), "the miniland, once it is open again");
        }

        [TestMethod]
        public async Task LockingAMinilandSendsItsVisitorsBackWhereTheyCameFrom()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");
            await bob.ExpectAsync(InTheMiniland(bob), "the miniland");
            var sentOut = bob.Mark();

            await alice.SendAsync($"mledit {EditState} {Locked}");

            await bob.ExpectSinceAsync(sentOut, p => p.StartsWith($"at {bob.CharacterId} {WorldData.Town} 72 116 ", StringComparison.Ordinal), "the town he came from");
        }

        [TestMethod]
        public async Task AVisitorIsSentBackWhenTheOwnerLeavesTheWorld()
        {
            var owner = await OwnerAsync();
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            int sentOut;
            await using (var alice = await EnterAsync(owner))
            {
                await BefriendAsync(alice, bob);
                await GoHomeAsync(alice);
                await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");
                await bob.ExpectAsync(InTheMiniland(bob), "the miniland");
                sentOut = bob.Mark();
            }

            await bob.ExpectSinceAsync(sentOut, p => p.StartsWith($"at {bob.CharacterId} {WorldData.Town} 72 116 ", StringComparison.Ordinal), "the town he came from");
        }

        [TestMethod]
        public async Task SomethingIsStoodInAMinilandOnlyWhileItIsLocked()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using var alice = await EnterAsync(owner);
            var home = await GoHomeAsync(alice);
            // mlobjlst <slot>.<in use>.<x>.<y>.<width>.<height>...
            StringAssert.StartsWith(alice.Since(home).Last(p => TestClient.HeaderOf(p) == "mlobjlst"), "mlobjlst 0.0.0.0.1.1.");
            var refused = alice.Mark();

            await alice.SendAsync("addobj 0 10 12");

            await alice.ExpectSinceAsync(refused, Announced(OnlyWhileLocked), "the refusal");
            Assert.IsFalse(alice.Since(refused).Any(p => TestClient.HeaderOf(p) == "mlobj"), "nothing is stood there");
            await SetStateAsync(alice, Locked, MinilandIsLocked);
            var placed = alice.Mark();

            await alice.SendAsync("addobj 0 10 12");

            // mlobj <in use> <slot> <x> <y> <width> <height> <?> <durability> <?> <warehouse>, as a trace has it.
            await alice.ExpectSinceAsync(placed, p => p == "mlobj 1 0 10 12 1 1 0 0 0 0", "the bench where she stood it");
            await alice.ExpectSinceAsync(placed, p => TestClient.HeaderOf(p) == "mlpt", "the points of the miniland");
        }

        [TestMethod]
        public async Task WhatStandsInAMinilandIsShownToItsVisitors()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            await SetStateAsync(alice, Locked, MinilandIsLocked);
            await alice.SendAsync("addobj 0 10 12");
            await alice.ExpectAsync(p => p.StartsWith("mlobj 1 0 10 12 ", StringComparison.Ordinal), "the bench where she stood it");
            await SetStateAsync(alice, Open, MinilandIsOpen);
            var visited = bob.Mark();

            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");

            // mltobj <vnum>.<slot>.<x>.<y> ...
            Assert.AreEqual($"mltobj {WorldData.Bench}.0.10.12", await bob.ExpectSinceAsync(visited, p => TestClient.HeaderOf(p) == "mltobj", "what stands there"));
        }

        [TestMethod]
        public async Task SomethingIsTakenAwayOnlyWhileTheMinilandIsLocked()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using var alice = await EnterAsync(owner);
            await GoHomeAsync(alice);
            await SetStateAsync(alice, Locked, MinilandIsLocked);
            await alice.SendAsync("addobj 0 10 12");
            await alice.ExpectAsync(p => p.StartsWith("mlobj 1 0 10 12 ", StringComparison.Ordinal), "the bench where she stood it");
            await SetStateAsync(alice, Open, MinilandIsOpen);
            var refused = alice.Mark();

            await alice.SendAsync("rmvobj 0");

            await alice.ExpectSinceAsync(refused, Announced(RemovalOnlyWhileLocked), "the refusal");
            Assert.IsFalse(alice.Since(refused).Any(p => TestClient.HeaderOf(p) == "mlobj"), "the bench stays");
            await SetStateAsync(alice, Locked, MinilandIsLocked);
            var removed = alice.Mark();

            await alice.SendAsync("rmvobj 0");

            await alice.ExpectSinceAsync(removed, p => p.StartsWith("mlobj 0 0 ", StringComparison.Ordinal), "the bench taken away");
        }

        [TestMethod]
        public async Task WhatStandsInAMinilandIsStillThereWhenItsOwnerComesBack()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using (var alice = await EnterAsync(owner))
            {
                await GoHomeAsync(alice);
                await SetStateAsync(alice, Locked, MinilandIsLocked);
                await alice.SendAsync("addobj 0 10 12");
                await alice.ExpectAsync(p => p.StartsWith("mlobj 1 0 10 12 ", StringComparison.Ordinal), "the bench where she stood it");
            }

            await using var back = await Harness.ComeBackAsync(owner);
            var home = await GoHomeAsync(back, 71);

            StringAssert.StartsWith(back.Since(home).Last(p => TestClient.HeaderOf(p) == "mlobjlst"), "mlobjlst 0.1.10.12.1.1.");
            var info = back.Since(home).Last(p => TestClient.HeaderOf(p) == "mlinfo").Split(' ');
            Assert.AreEqual(Locked.ToString(CultureInfo.InvariantCulture), info[7], "and the miniland is still locked");
        }

        [TestMethod]
        public async Task WhatWasTakenAwayFromAMinilandStaysAwayWhenItsOwnerComesBack()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using (var alice = await EnterAsync(owner))
            {
                await GoHomeAsync(alice);
                await SetStateAsync(alice, Locked, MinilandIsLocked);
                await alice.SendAsync("addobj 0 10 12");
                await alice.ExpectAsync(p => p.StartsWith("mlobj 1 0 10 12 ", StringComparison.Ordinal), "the bench where she stood it");
            }

            await using (var again = await Harness.ComeBackAsync(owner))
            {
                var home = await GoHomeAsync(again, 71);
                StringAssert.StartsWith(again.Since(home).Last(p => TestClient.HeaderOf(p) == "mlobjlst"), "mlobjlst 0.1.10.12.");
                await SetStateAsync(again, Locked, MinilandIsLocked);
                await again.SendAsync("rmvobj 0");
                await again.ExpectAsync(p => p.StartsWith("mlobj 0 0 ", StringComparison.Ordinal), "the bench taken away");
            }

            await using var back = await Harness.ComeBackAsync(owner);
            var returned = await GoHomeAsync(back);
            StringAssert.StartsWith(back.Since(returned).Last(p => TestClient.HeaderOf(p) == "mlobjlst"), "mlobjlst 0.0.");
        }

        [TestMethod]
        public async Task AnInvitationCostsASeedOfPowerAndLetsAStrangerIn()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.PowerSeed, 2);
            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            var bobName = bob.Received.First(p => TestClient.HeaderOf(p) == "c_info").Split(' ')[1];
            await GoHomeAsync(alice);
            var invited = alice.Mark();

            await alice.SendAsync($"$Invite {bobName}");

            var asked = await bob.ExpectAsync("dlgi2");
            StringAssert.StartsWith(asked, $"dlgi2 #mjoin^{Player}^{alice.CharacterId} #mjoin^{Npc}^{alice.CharacterId} {InvitedToAMiniland} ");
            await alice.ExpectSinceAsync(invited, p => p.StartsWith($"ivn {EtcPocket} 0.{WorldData.PowerSeed}.1.", StringComparison.Ordinal), "the seed it cost");
            var visited = bob.Mark();

            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");

            await bob.ExpectSinceAsync(visited, InTheMiniland(bob), "the miniland");
        }

        [TestMethod]
        public async Task ADeclinedInvitationLetsNobodyIn()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.PowerSeed, 2);
            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            var bobName = bob.Received.First(p => TestClient.HeaderOf(p) == "c_info").Split(' ')[1];
            await GoHomeAsync(alice);
            await alice.SendAsync($"$Invite {bobName}");
            await bob.ExpectAsync("dlgi2");

            await bob.SendAsync($"mjoin {Npc} {alice.CharacterId}");
            var late = bob.Mark();
            await bob.SendAsync($"mjoin {Player} {alice.CharacterId}");

            Assert.IsTrue(await bob.NoneSinceAsync(late, p => TestClient.HeaderOf(p) is "at" or "c_map", TimeSpan.FromSeconds(1)),
                "he stays where he is");
        }

        [TestMethod]
        public async Task AnInvitationIsNotSentWithoutASeedOfPowerOrFromOutsideTheMiniland()
        {
            var owner = await OwnerAsync();
            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            var bobName = bob.Received.First(p => TestClient.HeaderOf(p) == "c_info").Split(' ')[1];
            var asked = bob.Mark();
            var outside = alice.Mark();

            await alice.SendAsync($"$Invite {bobName}");

            await alice.ExpectSinceAsync(outside, Told(OnlyFromTheMiniland), "the refusal outside the miniland");
            await GoHomeAsync(alice);
            var seedless = alice.Mark();

            await alice.SendAsync($"$Invite {bobName}");

            await alice.ExpectSinceAsync(seedless, Told(NoSeedForAnInvitation), "the refusal without a seed");
            Assert.IsFalse(bob.Since(asked).Any(p => TestClient.HeaderOf(p) == "dlgi2"), "nobody is asked");
        }
    }
}
