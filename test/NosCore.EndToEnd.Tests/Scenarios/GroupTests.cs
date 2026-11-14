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
using NosCore.Data.Enumerations;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class GroupTests
    {
        private const int Player = 1;
        private const int Monster = 3;
        private const int MainPocket = 1;
        private const int Invite = 1;
        private const int Accept = 3;
        private const int Decline = 4;
        private const int LootMode = 8;
        private const int GroupIsFull = 230;
        private const int InvitationDeclined = 237;
        private const int GroupDisbanded = 478;
        private const int NotTheLeader = 560;
        private const int SharedByTheGroup = 561;
        private const int YouLeadTheGroup = 599;
        private const int MostHp = 221;

        private static Func<string, bool> Told(int message) =>
            packet => packet.Split(' ') is ["infoi", var id, ..] && id == message.ToString(CultureInfo.InvariantCulture);

        private static Func<string, bool> Announced(int message) =>
            packet => packet.Split(' ') is ["msgi", _, var id, ..] && id == message.ToString(CultureInfo.InvariantCulture);

        private static Func<string, bool> GroupOf(int size) =>
            packet => packet == $"pinit {size}" || packet.StartsWith($"pinit {size} ", StringComparison.Ordinal);

        // pinit <size> <type>|<id>|<place>|<level>|<name>|...
        private static long[] Members(string pinit) =>
            pinit.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(2)
                .Select(member => long.Parse(member.Split('|')[1], CultureInfo.InvariantCulture)).OrderBy(id => id).ToArray();

        private static long[] Ids(params TestClient[] clients) => clients.Select(c => c.CharacterId).OrderBy(id => id).ToArray();

        // The newcomer joins whatever group the inviter is in; the group is then of the given size.
        private static async Task InviteAsync(TestClient inviter, TestClient newcomer, int size)
        {
            await inviter.SendAsync($"pjoin {Invite} {newcomer.CharacterId}");
            await newcomer.ExpectAsync(p => p.Contains($"#pjoin^{Accept}^{inviter.CharacterId} ", StringComparison.Ordinal), "the group invitation");
            var joined = inviter.Mark();
            var accepted = newcomer.Mark();
            await newcomer.SendAsync($"pjoin {Accept} {inviter.CharacterId}");
            await inviter.ExpectSinceAsync(joined, GroupOf(size), $"the group of {size}");
            await newcomer.ExpectSinceAsync(accepted, GroupOf(size), $"the group of {size}");
        }

        [TestMethod]
        public async Task TheOneInvitedIsAskedAndTheInviterIsToldWhoWasAsked()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            await alice.SendAsync($"pjoin {Invite} {bob.CharacterId}");

            var asked = await bob.ExpectAsync("dlgi2");
            StringAssert.StartsWith(asked, $"dlgi2 #pjoin^{Accept}^{alice.CharacterId} #pjoin^{Decline}^{alice.CharacterId} ");
            StringAssert.StartsWith(await alice.ExpectAsync("infoi2"), "infoi2 234 1 Bob");
        }

        [TestMethod]
        public async Task ADeclinedInvitationFormsNoGroupAndTheOneWhoDeclinedIsToldSo()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await alice.SendAsync($"pjoin {Invite} {bob.CharacterId}");
            await bob.ExpectAsync("dlgi2");
            var declined = alice.Mark();

            await bob.SendAsync($"pjoin {Decline} {alice.CharacterId}");

            await bob.ExpectAsync(p => p.StartsWith($"sayi2 {Player} {bob.CharacterId} ", StringComparison.Ordinal)
                && p.Split(' ')[4] == InvitationDeclined.ToString(CultureInfo.InvariantCulture), "the refusal noted");
            Assert.IsFalse(alice.Since(declined).Any(p => p.StartsWith("pinit 2", StringComparison.Ordinal)), "no group is formed");

            // An answer given twice is not an invitation any more.
            var late = alice.Mark();
            await bob.SendAsync($"pjoin {Accept} {alice.CharacterId}");
            Assert.IsTrue(await alice.NoneSinceAsync(late, GroupOf(2), TimeSpan.FromSeconds(1)), "accepting after declining forms no group");
        }

        [TestMethod]
        public async Task AGroupCannotBeJoinedWithoutAnInvitation()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            var uninvited = alice.Mark();

            await bob.SendAsync($"pjoin {Accept} {alice.CharacterId}");

            Assert.IsTrue(await alice.NoneSinceAsync(uninvited, GroupOf(2), TimeSpan.FromSeconds(1)), "no group is formed");
        }

        [TestMethod]
        public async Task AGroupFormedIsShownToItsMembersAndToTheMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await alice.SendAsync($"pjoin {Invite} {bob.CharacterId}");
            await bob.ExpectAsync("dlgi2");
            var formed = alice.Mark();
            var seen = carol.Mark();

            await bob.SendAsync($"pjoin {Accept} {alice.CharacterId}");

            CollectionAssert.AreEqual(Ids(alice, bob), Members(await alice.ExpectSinceAsync(formed, GroupOf(2), "the group of two")));
            CollectionAssert.AreEqual(Ids(alice, bob), Members(await bob.ExpectAsync(GroupOf(2), "the group of two")));
            await alice.ExpectSinceAsync(formed, Told(YouLeadTheGroup), "the inviter told she leads");
            // pidx <group> <type>.<id> ..., as a trace has it: "pidx 35 1.446955 1.317685".
            var shown = (await carol.ExpectSinceAsync(seen, p => TestClient.HeaderOf(p) == "pidx", "the group shown to the map")).Split(' ');
            Assert.AreNotEqual("-1", shown[1], "the group has a number");
            Assert.AreEqual($"{Player}.{alice.CharacterId} {Player}.{bob.CharacterId}", string.Join(' ', shown.Skip(2).OrderBy(member => member)));
        }

        [TestMethod]
        public async Task AMembersBarsCarryThePlaceTheMemberListGivesIt()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            var formed = alice.Mark();

            // The one who invites was created last: the order of joining is not the order of the ids.
            await InviteAsync(bob, alice, 2);

            var list = await alice.ExpectSinceAsync(formed, GroupOf(2), "the group of two");
            var place = list.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(2).Select(member => member.Split('|'))
                .Single(member => member[1] == bob.CharacterId.ToString(CultureInfo.InvariantCulture))[2];
            var bar = (await alice.ExpectSinceAsync(formed, p => p.StartsWith($"pst {Player} {bob.CharacterId} ", StringComparison.Ordinal), "Bob's bars")).Split(' ');
            Assert.AreEqual("1", place, "the one who invited holds the first place");
            Assert.AreEqual(place, bar[3], "his bars are sent for the place the list gave him");
        }

        [TestMethod]
        public async Task MembersAreToldEachOthersHpWhenTheGroupForms()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: c => c.Hp = 100);
            var formed = alice.Mark();

            await InviteAsync(alice, bob, 2);

            // pst <type> <id> <place> <hp %> <mp %> <most hp> <most mp> ...
            var bar = (await alice.ExpectSinceAsync(formed, p => p.StartsWith($"pst {Player} {bob.CharacterId} ", StringComparison.Ordinal), "Bob's bars")).Split(' ');
            Assert.AreEqual((100 * 100 / MostHp).ToString(CultureInfo.InvariantCulture), bar[4], "the part of his HP he has left");
            Assert.AreEqual(MostHp.ToString(CultureInfo.InvariantCulture), bar[6]);
        }

        [TestMethod]
        public async Task AMembersHpChangeIsShownToTheOthers()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            var wounded = await Harness.AddPlayerAsync("Bob", x: 81, y: 116, configure: c => c.Hp = 100);
            await Harness.GiveAsync(wounded, WorldData.Potion, 1, pocket: NoscorePocketType.Main);
            await using var bob = Harness.Client(wounded);
            await bob.EnterWorldAsync(wounded.Slot);
            await InviteAsync(alice, bob, 2);
            var drunk = alice.Mark();

            await bob.SendAsync($"u_i {Player} {bob.CharacterId} {MainPocket} 0 0 0");
            await bob.ExpectAsync(p => p.StartsWith($"stat {100 + WorldData.PotionHp} ", StringComparison.Ordinal), "the HP after the potion");

            // The bars come from a timer: one may be sent before the potion, and regeneration can add
            // to his HP before the next, so the bar is only known to reach at least the potion's share.
            var healed = (100 + WorldData.PotionHp) * 100 / MostHp;
            await alice.ExpectSinceAsync(drunk,
                p => p.StartsWith($"pst {Player} {bob.CharacterId} ", StringComparison.Ordinal)
                    && int.Parse(p.Split(' ')[4], CultureInfo.InvariantCulture) >= healed,
                $"Bob's bars at {healed}% or more");
        }

        [TestMethod]
        public async Task TheOneWhoInvitedSetsHowTheGroupLoots()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            var formed = alice.Mark();
            await InviteAsync(alice, bob, 2);
            await alice.ExpectSinceAsync(formed, Told(YouLeadTheGroup), "the inviter told she leads");
            var refused = bob.Mark();

            await bob.SendAsync($"gop {LootMode} 1");

            await bob.ExpectSinceAsync(refused, Told(NotTheLeader), "the member refused");
            var changed = alice.Mark();
            var heard = bob.Mark();

            await alice.SendAsync($"gop {LootMode} 1");

            await bob.ExpectSinceAsync(heard, Announced(SharedByTheGroup), "the new way of looting announced to the member");
            await alice.ExpectSinceAsync(changed, Announced(SharedByTheGroup), "the new way of looting announced to the leader");
        }

        [TestMethod]
        public async Task AGroupOfTwoIsDisbandedWhenOneLeaves()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await InviteAsync(alice, bob, 2);
            var left = alice.Mark();
            var gone = bob.Mark();
            var seen = carol.Mark();

            await bob.SendAsync("pleave");

            await alice.ExpectSinceAsync(left, GroupOf(0), "Alice on her own");
            await bob.ExpectSinceAsync(gone, GroupOf(0), "Bob on his own");
            await alice.ExpectSinceAsync(left, Announced(GroupDisbanded), "the end of the group announced to Alice");
            await bob.ExpectSinceAsync(gone, Announced(GroupDisbanded), "the end of the group announced to Bob");
            await carol.ExpectSinceAsync(seen, p => p == $"pidx -1 {Player}.{alice.CharacterId}", "Alice shown without a group");
            await carol.ExpectSinceAsync(seen, p => p == $"pidx -1 {Player}.{bob.CharacterId}", "Bob shown without a group");
        }

        [TestMethod]
        public async Task TheEndOfAGroupIsAnnouncedOnceToEachMember()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await InviteAsync(alice, bob, 2);
            var left = alice.Mark();
            var gone = bob.Mark();

            await bob.SendAsync("pleave");

            await alice.ExpectSinceAsync(left, Announced(GroupDisbanded), "the end of the group announced to Alice");
            await bob.ExpectSinceAsync(gone, Announced(GroupDisbanded), "the end of the group announced to Bob");
            Assert.IsTrue(await alice.NoneSinceAsync(left, p => false, TimeSpan.FromSeconds(1)));
            Assert.AreEqual(1, alice.Since(left).Count(Announced(GroupDisbanded)), "announcements to Alice");
            Assert.AreEqual(1, bob.Since(gone).Count(Announced(GroupDisbanded)), "announcements to Bob");
        }

        [TestMethod]
        public async Task ANewGroupCanBeFormedAfterOneWasDisbanded()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await InviteAsync(alice, bob, 2);
            var left = alice.Mark();
            await alice.SendAsync("pleave");
            await alice.ExpectSinceAsync(left, GroupOf(0), "Alice on her own");
            await bob.ExpectAsync(GroupOf(0), "Bob on his own");

            await InviteAsync(bob, alice, 2);

            await alice.SendAsync(";still here");
            StringAssert.EndsWith(await bob.ExpectAsync("spk"), "still here");
        }

        [TestMethod]
        public async Task InAGroupOfThreeTheOthersStayTogetherWhenOneLeaves()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await InviteAsync(alice, bob, 2);
            await InviteAsync(bob, carol, 3);
            var stayed = alice.Mark();
            var kept = bob.Mark();
            var left = carol.Mark();

            await carol.SendAsync("pleave");

            await carol.ExpectSinceAsync(left, GroupOf(0), "Carol on her own");
            CollectionAssert.AreEqual(Ids(alice, bob), Members(await alice.ExpectSinceAsync(stayed, GroupOf(2), "the group of two")));
            CollectionAssert.AreEqual(Ids(alice, bob), Members(await bob.ExpectSinceAsync(kept, GroupOf(2), "the group of two")));
            Assert.IsFalse(alice.Since(stayed).Any(Announced(GroupDisbanded)) || bob.Since(kept).Any(Announced(GroupDisbanded)),
                "the group is not disbanded");

            var said = carol.Mark();
            await alice.SendAsync(";two of us now");
            StringAssert.EndsWith(await bob.ExpectAsync("spk"), "two of us now");
            Assert.IsTrue(await carol.NoneSinceAsync(said, p => TestClient.HeaderOf(p) == "spk", TimeSpan.FromSeconds(1)),
                "the one who left hears the group no more");
        }

        [TestMethod]
        public async Task WhenTheLeaderLeavesAGroupOfThreeTheNextToHaveJoinedLeads()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            var formed = alice.Mark();
            await InviteAsync(alice, bob, 2);
            await alice.ExpectSinceAsync(formed, Told(YouLeadTheGroup), "the inviter told she leads");
            await InviteAsync(bob, carol, 3);
            var promoted = bob.Mark();
            var kept = carol.Mark();
            var left = alice.Mark();

            await alice.SendAsync("pleave");

            await alice.ExpectSinceAsync(left, GroupOf(0), "Alice on her own");
            CollectionAssert.AreEqual(Ids(bob, carol), Members(await bob.ExpectSinceAsync(promoted, GroupOf(2), "the group of two")));
            CollectionAssert.AreEqual(Ids(bob, carol), Members(await carol.ExpectSinceAsync(kept, GroupOf(2), "the group of two")));
            await bob.ExpectSinceAsync(promoted, Told(YouLeadTheGroup), "Bob told he leads");
            Assert.IsFalse(carol.Since(kept).Any(Told(YouLeadTheGroup)), "Carol is not told she leads");
        }

        [TestMethod]
        public async Task AFourthIsRefusedByAFullGroup()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await using var dave = await Harness.EnterAsync("Dave", x: 83, y: 116);
            await InviteAsync(alice, bob, 2);
            await InviteAsync(bob, carol, 3);
            await carol.SendAsync($"pjoin {Invite} {dave.CharacterId}");
            await dave.ExpectAsync(p => p.Contains($"#pjoin^{Accept}^{carol.CharacterId} ", StringComparison.Ordinal), "the group invitation");
            var refused = carol.Mark();
            var turnedAway = dave.Mark();

            await dave.SendAsync($"pjoin {Accept} {carol.CharacterId}");

            await dave.ExpectSinceAsync(turnedAway, Told(GroupIsFull), "the fourth turned away");
            await carol.ExpectSinceAsync(refused, Told(GroupIsFull), "the one who invited told why");
            Assert.IsFalse(dave.Since(turnedAway).Any(p => p.StartsWith("pinit ", StringComparison.Ordinal) && Members(p).Length > 0),
                "the fourth is in no group");

            var asked = dave.Mark();
            await dave.SendAsync($"pjoin {Invite} {alice.CharacterId}");
            await dave.ExpectSinceAsync(asked, Told(GroupIsFull), "the fourth turned away when he asks");
            var said = dave.Mark();
            await alice.SendAsync(";three is enough");
            StringAssert.EndsWith(await carol.ExpectAsync("spk"), "three is enough");
            Assert.IsFalse(dave.Since(said).Any(p => TestClient.HeaderOf(p) == "spk"), "the fourth does not hear the group");
        }

        [TestMethod]
        public async Task AMemberWhoLeavesTheWorldLeavesTheGroup()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            var stayed = alice.Mark();
            await using (var bob = await Harness.EnterAsync("Bob", x: 81, y: 116))
            {
                await InviteAsync(alice, bob, 2);
                await InviteAsync(bob, carol, 3);
                stayed = alice.Mark();
            }

            CollectionAssert.AreEqual(Ids(alice, carol), Members(await alice.ExpectSinceAsync(stayed, GroupOf(2), "the group of two")));
            await alice.SendAsync(";he is gone");
            StringAssert.EndsWith(await carol.ExpectAsync("spk"), "he is gone");
        }

        [TestMethod]
        public async Task AGroupOfTwoEndsWhenOneLeavesTheWorld()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            int left;
            await using (var bob = await Harness.EnterAsync("Bob", x: 81, y: 116))
            {
                await InviteAsync(alice, bob, 2);
                left = alice.Mark();
            }

            await alice.ExpectSinceAsync(left, GroupOf(0), "Alice on her own");
            await alice.ExpectSinceAsync(left, Announced(GroupDisbanded), "the end of the group announced");
        }

        [TestMethod]
        public async Task AKillGivesExperienceToEveryMemberOnTheMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 71);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 32, 72);
            await InviteAsync(alice, bob, 2);
            await alice.ExpectSinceAsync(0, p => p.StartsWith($"in {Monster} {WorldData.PreyVNum} {WorldData.Prey} ", StringComparison.Ordinal), "the prey on the map");
            var killed = alice.Mark();
            var watched = bob.Mark();
            var tried = Stopwatch.StartNew();

            // A hit may miss; the prey falls to the first one that lands.
            while (true)
            {
                var cast = alice.Mark();
                await alice.SendAsync($"u_s 0 {Monster} {WorldData.Prey}");
                var hit = (await alice.ExpectSinceAsync(cast, p => p.StartsWith($"su {Player} {alice.CharacterId} {Monster} {WorldData.Prey} ", StringComparison.Ordinal),
                    "the hit")).Split(' ');
                if (hit[11] == "0")
                {
                    break;
                }

                Assert.IsTrue(tried.Elapsed < TimeSpan.FromSeconds(30), "no hit landed in thirty seconds");
                await alice.ExpectSinceAsync(cast, p => p == "sr 0", "the skill ready again");
            }

            var hers = long.Parse((await alice.ExpectSinceAsync(killed, p => TestClient.HeaderOf(p) == "lev", "her experience")).Split(' ')[2], CultureInfo.InvariantCulture);
            var his = long.Parse((await bob.ExpectSinceAsync(watched, p => TestClient.HeaderOf(p) == "lev", "his experience")).Split(' ')[2], CultureInfo.InvariantCulture);
            Assert.IsTrue(hers > 0, "the one who killed gains experience");
            Assert.AreEqual(hers, his, "the one who only watched gains as much");
        }
    }
}
