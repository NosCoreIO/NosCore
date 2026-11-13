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
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Shared.Enumerations;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FamilyTests
    {
        private const long CreationPrice = 200000;
        private const int Head = 0;
        private const int Assistant = 1;
        private const int Member = 3;

        private static Action<Character> Swordsman(long gold = 0) => c =>
        {
            c.Class = CharacterClassType.Swordsman;
            c.Gold = gold;
        };

        // ginfo <family> <head> <head gender> <level> <xp> <xp to level> <members> <capacity> <own authority> ...
        private static Func<string, bool> FamilyInfo(string family, int members, int authority) =>
            packet => packet.Split(' ') is ["ginfo", var name, _, _, _, _, _, var count, _, var own, ..]
                && name == family && count == members.ToString() && own == authority.ToString();

        // The others ask to join in turn: one player may only send a group request every five seconds.
        private static async Task GroupAsync(TestClient first, params TestClient[] others)
        {
            var size = 1;
            foreach (var other in others)
            {
                size++;
                await other.SendAsync($"pjoin 1 {first.CharacterId}");
                await first.ExpectAsync(p => p.Contains($"#pjoin^3^{other.CharacterId}", StringComparison.Ordinal), "the group request");
                var joined = other.Mark();
                await first.SendAsync($"pjoin 3 {other.CharacterId}");
                await other.ExpectSinceAsync(joined, p => p.StartsWith($"pinit {size} ", StringComparison.Ordinal), $"the group of {size}");
            }
        }

        private static async Task<FamilyCharacter[]> StoredMembersAsync(string family)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<FamilyCharacter>().AsNoTracking().Where(m => m.Family.Name == family).ToArrayAsync();
        }

        private static async Task<FamilyCharacter?> StoredMembershipAsync(long characterId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<FamilyCharacter>().AsNoTracking().SingleOrDefaultAsync(m => m.CharacterId == characterId);
        }

        [TestMethod]
        public async Task ThreeGroupedPlayersWithAClassFoundAFamilyForItsPrice()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: Swordsman(1000000));
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: Swordsman());
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116, configure: Swordsman());
            await using var dave = await Harness.EnterAsync("Dave", x: 83, y: 116);
            await GroupAsync(alice, bob, carol);
            var name = Harness.FamilyName();
            var founded = alice.Mark();
            var watched = dave.Mark();

            await alice.SendAsync($"glmk {name}");

            await alice.ExpectSinceAsync(founded, p => p == $"gold {1000000 - CreationPrice} 0", "the gold left after the price");
            // 262 is the client's "The family %s has been founded."
            await dave.ExpectSinceAsync(watched, p => p.StartsWith($"msgi2 0 262 1 {name} ", StringComparison.Ordinal), "the announcement");
            await alice.ExpectSinceAsync(founded, FamilyInfo(name, 3, Head), "the family, for its head");
            await bob.ExpectAsync(FamilyInfo(name, 3, Assistant), "the family, for a founder");
            await carol.ExpectAsync(FamilyInfo(name, 3, Assistant), "the family, for a founder");
            var stored = await StoredMembersAsync(name);
            CollectionAssert.AreEquivalent(new[] { alice.CharacterId, bob.CharacterId, carol.CharacterId },
                stored.Select(m => m.CharacterId).ToArray());
            Assert.AreEqual(FamilyAuthority.Head, stored.Single(m => m.CharacterId == alice.CharacterId).Authority);
            var familyId = stored[0].FamilyId;
            await dave.ExpectSinceAsync(watched, p => p.StartsWith($"gidx 1 {alice.CharacterId} {familyId}.-1 {name}(", StringComparison.Ordinal),
                "the family under Alice's name, for a bystander");
        }

        [TestMethod]
        public async Task ThePriceOfAFamilyIsStoredWithTheFamily()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: 80, y: 116, configure: Swordsman(1000000));
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: Swordsman());
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116, configure: Swordsman());
            var name = Harness.FamilyName();
            await using (var alice = Harness.Client(player))
            {
                await alice.EnterWorldAsync(player.Slot);
                await GroupAsync(alice, bob, carol);
                await alice.SendAsync($"glmk {name}");
                await alice.ExpectAsync(FamilyInfo(name, 3, Head), "the family");
            }

            var waited = Stopwatch.StartNew();
            while ((await Harness.StoredAsync(player.CharacterId)).Gold != 1000000 - CreationPrice)
            {
                Assert.IsTrue(waited.Elapsed < TimeSpan.FromSeconds(15), "the price was not stored within 15 s of the founder leaving");
                await Task.Delay(250);
            }
        }

        [TestMethod]
        public async Task AFamilyNeedsAGroupOfThree()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: Swordsman(1000000));
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: Swordsman());
            await GroupAsync(alice, bob);
            var name = Harness.FamilyName();
            var asked = alice.Mark();

            await alice.SendAsync($"glmk {name}");

            // 256 is the client's "%d members are needed to found a family."
            await alice.ExpectSinceAsync(asked, p => p == "infoi2 256 4 3", "the three-members notice");
            Assert.AreEqual(0, (await StoredMembersAsync(name)).Length, "no family is stored");
            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "gold" or "ginfo", TimeSpan.FromSeconds(1)),
                "no gold is taken");
        }

        [TestMethod]
        public async Task AFamilyCannotBeFoundedWithAnAdventurer()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: Swordsman(1000000));
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: Swordsman());
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await GroupAsync(alice, bob, carol);
            var name = Harness.FamilyName();

            await alice.SendAsync($"glmk {name}");

            // 259 is the client's notice that a member has to change class first.
            await alice.ExpectAsync(p => p.StartsWith("infoi 259 ", StringComparison.Ordinal), "the change-class notice");
            Assert.AreEqual(0, (await StoredMembersAsync(name)).Length, "no family is stored");
        }

        [TestMethod]
        public async Task AFamilyCannotBeFoundedWithoutItsPrice()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: Swordsman(CreationPrice - 1));
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: Swordsman());
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116, configure: Swordsman());
            await GroupAsync(alice, bob, carol);
            var name = Harness.FamilyName();
            var asked = alice.Mark();

            await alice.SendAsync($"glmk {name}");

            // 254 is the client's notice that founding a family costs gold.
            await alice.ExpectSinceAsync(asked, p => p.StartsWith("infoi 254 ", StringComparison.Ordinal), "the not-enough-gold notice");
            Assert.AreEqual(0, (await StoredMembersAsync(name)).Length, "no family is stored");
            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) == "gold", TimeSpan.FromSeconds(1)), "no gold is taken");
        }

        [TestMethod]
        public async Task ANameAlreadyTakenFoundsNothing()
        {
            var taken = await Harness.AddFamilyAsync(await Harness.AddPlayerAsync("Eve"));
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: Swordsman(1000000));
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: Swordsman());
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116, configure: Swordsman());
            await GroupAsync(alice, bob, carol);
            var asked = alice.Mark();

            await alice.SendAsync($"glmk {taken.Name}");

            // 261 is the client's notice that a family of that name exists.
            await alice.ExpectSinceAsync(asked, p => p.StartsWith("infoi 261 ", StringComparison.Ordinal), "the name-taken notice");
            Assert.IsNull(await StoredMembershipAsync(alice.CharacterId));
            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) == "gold", TimeSpan.FromSeconds(1)), "no gold is taken");
        }

        [TestMethod]
        public async Task AFamilyCannotBeFoundedWithSomeoneWhoHasOne()
        {
            var member = await Harness.AddPlayerAsync("Carol", x: 82, y: 116, configure: Swordsman());
            await Harness.AddFamilyAsync(await Harness.AddPlayerAsync("Eve"), member);
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: Swordsman(1000000));
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: Swordsman());
            await using var carol = Harness.Client(member);
            await carol.EnterWorldAsync(member.Slot);
            await GroupAsync(alice, bob, carol);
            var name = Harness.FamilyName();

            await alice.SendAsync($"glmk {name}");

            // 257 is the client's notice that a member already has a family.
            await alice.ExpectAsync(p => p.StartsWith("infoi 257 ", StringComparison.Ordinal), "the already-in-a-family notice");
            Assert.AreEqual(0, (await StoredMembersAsync(name)).Length, "no family is stored");
        }

        [TestMethod]
        public async Task TheFoundingDialogAsksForAName()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: Swordsman(1000000));
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: Swordsman());
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116, configure: Swordsman());
            await GroupAsync(alice, bob, carol);

            // 23 is the n_run that founds or disbands a family; 191 is the client's "Enter a family name".
            await alice.SendAsync("n_run 23 0");

            Assert.AreEqual("inbox #glmk^ 14 1 191", await alice.ExpectAsync("inbox"));
        }

        [TestMethod]
        public async Task SomeoneInAnotherFamilyCannotBeInvited()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            await Harness.AddFamilyAsync(head);
            var other = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            await Harness.AddFamilyAsync(other);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);
            await using var bob = Harness.Client(other);
            await bob.EnterWorldAsync(other.Slot);
            var asked = bob.Mark();

            await alice.SendAsync($"%Familyinvite {other.Name}");

            // 272 is the client's notice that the player is in another family.
            await alice.ExpectAsync(p => p.StartsWith("infoi 272 ", StringComparison.Ordinal), "the in-another-family notice");
            Assert.IsTrue(await bob.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) == "dlgi2", TimeSpan.FromSeconds(1)), "nobody is asked");
        }

        [TestMethod]
        public async Task AMemberIsToldItsFamilyOnEnteringAndBystandersSeeIt()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await using var bob = Harness.Client(member);

            var entry = await bob.EnterWorldAsync(member.Slot);

            Assert.IsTrue(entry.Any(FamilyInfo(family.Name, 2, Member)), "the family is sent to its member");
            await carol.ExpectAsync(p => p.StartsWith($"gidx 1 {member.CharacterId} {family.FamilyId}.-1 {family.Name}(", StringComparison.Ordinal),
                "the family under Bob's name");
        }

        [TestMethod]
        public async Task WhatIsSaidToTheFamilyReachesItsMembersOnly()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var away = await Harness.AddPlayerAsync("Dave", WorldData.Meadow, 20, 20);
            await Harness.AddFamilyAsync(head, member, away);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);
            await using var bob = Harness.Client(member);
            await bob.EnterWorldAsync(member.Slot);
            await using var dave = Harness.Client(away);
            await dave.EnterWorldAsync(away.Slot);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            var said = carol.Mark();

            await alice.SendAsync(":meet at the gate");

            Assert.AreEqual($"say 1 {alice.CharacterId} 6 meet at the gate", await bob.ExpectAsync("say"));
            Assert.AreEqual($"spk 1 {alice.CharacterId} 1 {head.Name} meet at the gate", await bob.ExpectAsync("spk"));
            // A member on another map cannot see who speaks, so the line names her.
            Assert.AreEqual($"say 1 {alice.CharacterId} 6 [{head.Name}]:meet at the gate", await dave.ExpectAsync("say"));
            Assert.IsTrue(await carol.NoneSinceAsync(said, p => TestClient.HeaderOf(p) is "say" or "spk", TimeSpan.FromSeconds(1)),
                "someone outside the family hears nothing");
        }

        [TestMethod]
        public async Task WhatIsSaidToTheFamilyReachesAMemberOnAnotherChannel()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            await Harness.AddFamilyAsync(head, member);
            await using var bob = Harness.Client(member, channel: 1);
            await bob.EnterWorldAsync(member.Slot);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);

            await alice.SendAsync(":anyone on channel two");

            Assert.AreEqual($"spk 1 {alice.CharacterId} 1 {head.Name} anyone on channel two", await bob.ExpectAsync("spk"));
        }

        [TestMethod]
        public async Task SomeoneWithoutAFamilySaysNothingToAnyFamily()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            await Harness.AddFamilyAsync(head);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            var said = alice.Mark();

            await carol.SendAsync(":is anyone there");

            Assert.IsTrue(await alice.NoneSinceAsync(said, p => TestClient.HeaderOf(p) is "say" or "spk", TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public async Task AnAcceptedInvitationAddsAMember()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var family = await Harness.AddFamilyAsync(head);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);
            var invited = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            await using var bob = Harness.Client(invited);
            await bob.EnterWorldAsync(invited.Slot);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);

            await alice.SendAsync($"%Familyinvite {invited.Name}");

            // 275 is the client's question whether to join the family %s.
            StringAssert.StartsWith(await bob.ExpectAsync("dlgi2"),
                $"dlgi2 #gjoin^1^{alice.CharacterId} #gjoin^2^{alice.CharacterId} 275 1 {family.Name} ");
            var joined = alice.Mark();
            var watched = carol.Mark();
            await bob.SendAsync($"gjoin 1 {alice.CharacterId}");

            await bob.ExpectAsync(FamilyInfo(family.Name, 2, Member), "the family, for its new member");
            await alice.ExpectSinceAsync(joined, FamilyInfo(family.Name, 2, Head), "the family with two members, for its head");
            // 276 is the client's "%s has joined the family."
            await alice.ExpectSinceAsync(joined, p => p.StartsWith($"msgi2 0 276 1 {invited.Name} ", StringComparison.Ordinal), "the welcome");
            await carol.ExpectSinceAsync(watched, p => p.StartsWith($"gidx 1 {bob.CharacterId} {family.FamilyId}.-1 {family.Name}(", StringComparison.Ordinal),
                "the family under Bob's name, for a bystander");
            var stored = await StoredMembershipAsync(bob.CharacterId);
            Assert.AreEqual(family.FamilyId, stored?.FamilyId);
            Assert.AreEqual(FamilyAuthority.Member, stored?.Authority);
        }

        [TestMethod]
        public async Task ADeclinedInvitationAddsNobody()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var family = await Harness.AddFamilyAsync(head);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);
            var invited = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            await using var bob = Harness.Client(invited);
            await bob.EnterWorldAsync(invited.Slot);

            await alice.SendAsync($"%Familyinvite {invited.Name}");
            await bob.ExpectAsync("dlgi2");
            var answered = bob.Mark();
            await bob.SendAsync($"gjoin 2 {alice.CharacterId}");

            // 278 is the client's notice that the invitation was refused.
            await alice.ExpectAsync(p => p.StartsWith("infoi 278 ", StringComparison.Ordinal), "the refusal");
            Assert.IsNull(await StoredMembershipAsync(bob.CharacterId));
            Assert.AreEqual(1, (await StoredMembersAsync(family.Name)).Length);
            Assert.IsTrue(await bob.NoneSinceAsync(answered, p => TestClient.HeaderOf(p) == "ginfo", TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public async Task OnlyAnInvitedPlayerCanJoin()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var family = await Harness.AddFamilyAsync(head);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            var asked = bob.Mark();

            await bob.SendAsync($"gjoin 1 {alice.CharacterId}");

            Assert.IsTrue(await bob.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) == "ginfo", TimeSpan.FromSeconds(1)));
            Assert.IsNull(await StoredMembershipAsync(bob.CharacterId));
            Assert.AreEqual(1, (await StoredMembersAsync(family.Name)).Length);
        }

        [TestMethod]
        public async Task AMemberCannotInvite()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            await Harness.AddFamilyAsync(head, member);
            await using var bob = Harness.Client(member);
            await bob.EnterWorldAsync(member.Slot);
            var outsider = await Harness.AddPlayerAsync("Carol", x: 82, y: 116);
            await using var carol = Harness.Client(outsider);
            await carol.EnterWorldAsync(outsider.Slot);
            var asked = carol.Mark();

            await bob.SendAsync($"%Familyinvite {outsider.Name}");

            // 270 is the client's "You are not authorised to accept a family."
            await bob.ExpectAsync(p => p.StartsWith("infoi 270 ", StringComparison.Ordinal), "the not-authorised notice");
            Assert.IsTrue(await carol.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) == "dlgi2", TimeSpan.FromSeconds(1)),
                "nobody is asked");
        }

        [TestMethod]
        public async Task AMemberWhoLeavesIsOutOfTheFamily()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);
            await using var bob = Harness.Client(member);
            await bob.EnterWorldAsync(member.Slot);
            var left = alice.Mark();

            await bob.SendAsync("%Familyleave");

            // 304 is the client's notice of having left the family.
            await bob.ExpectAsync(p => p.StartsWith("infoi 304 ", StringComparison.Ordinal), "the left-the-family notice");
            await alice.ExpectSinceAsync(left, FamilyInfo(family.Name, 1, Head), "the family with one member, for its head");
            await alice.ExpectSinceAsync(left, p => p.StartsWith($"gidx 1 {bob.CharacterId} -1 ", StringComparison.Ordinal), "Bob without a family");
            Assert.IsNull(await StoredMembershipAsync(bob.CharacterId));

            var said = bob.Mark();
            await alice.SendAsync(":it is quiet here");
            await alice.ExpectAsync("spk");
            Assert.IsTrue(await bob.NoneSinceAsync(said, p => TestClient.HeaderOf(p) is "say" or "spk", TimeSpan.FromSeconds(1)),
                "the family's words no longer reach him");
        }

        [TestMethod]
        public async Task TheHeadCannotLeave()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);

            await alice.SendAsync("%Familyleave");

            // 296 is the client's notice that the head cannot leave.
            await alice.ExpectAsync(p => p.StartsWith("infoi 296 ", StringComparison.Ordinal), "the head-cannot-leave notice");
            Assert.AreEqual(2, (await StoredMembersAsync(family.Name)).Length);
        }

        [TestMethod]
        public async Task ADisbandedFamilyIsGoneForEveryMember()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);
            await using var bob = Harness.Client(member);
            await bob.EnterWorldAsync(member.Slot);
            var disbanded = bob.Mark();

            await alice.SendAsync("glrm");

            // 268 is the client's notice that the family was disbanded.
            await alice.ExpectAsync(p => p.StartsWith("infoi 268 ", StringComparison.Ordinal), "the disbanded notice");
            await bob.ExpectSinceAsync(disbanded, p => p.StartsWith($"gidx 1 {alice.CharacterId} -1 ", StringComparison.Ordinal), "Alice without a family");
            await bob.ExpectSinceAsync(disbanded, p => p.StartsWith($"gidx 1 {bob.CharacterId} -1 ", StringComparison.Ordinal), "Bob without a family");
            Assert.AreEqual(0, (await StoredMembersAsync(family.Name)).Length);
            await using var context = Harness.Database.CreateContext();
            Assert.IsFalse(await context.Set<Family>().AnyAsync(f => f.FamilyId == family.FamilyId), "the family row is gone");
        }

        [TestMethod]
        public async Task OnlyTheHeadCanDisband()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var bob = Harness.Client(member);
            await bob.EnterWorldAsync(member.Slot);

            await bob.SendAsync("glrm");

            // 265 is the client's "Only the family head can disband a family."
            await bob.ExpectAsync(p => p.StartsWith("infoi 265 ", StringComparison.Ordinal), "the only-the-head notice");
            Assert.AreEqual(2, (await StoredMembersAsync(family.Name)).Length);
        }

        [TestMethod]
        public async Task TheMemberListNamesEveryMemberAndWhoIsOnline()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = Harness.Client(head);
            await alice.EnterWorldAsync(head.Slot);

            await alice.SendAsync("glist 0 2");

            // id|family|name|level|class|authority|rank|online|hero level
            var list = (await alice.ExpectAsync("gmbr")).Split(' ');
            CollectionAssert.AreEquivalent(new[]
            {
                $"{head.CharacterId}|{family.FamilyId}|{head.Name}|1|0|{Head}|0|1|0",
                $"{member.CharacterId}|{family.FamilyId}|{member.Name}|1|0|{Member}|0|0|0"
            }, list.Skip(2).ToArray());
        }
    }
}
