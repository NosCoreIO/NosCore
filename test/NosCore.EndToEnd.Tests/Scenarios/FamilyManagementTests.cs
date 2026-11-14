//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FamilyManagementTests
    {
        private const int Head = 0;
        private const int Assistant = 1;
        private const int Member = 3;

        // ginfo <family> <head> <head gender> <level> <xp> <xp to level> <members> <capacity> <own authority> ...
        private static Func<string, bool> FamilyInfo(string family, string head, int members, int authority) =>
            packet => packet.Split(' ') is ["ginfo", var name, var leader, _, _, _, _, var count, _, var own, ..]
                && name == family && leader == head && count == members.ToString() && own == authority.ToString();

        private static bool Heard(string packet) => TestClient.HeaderOf(packet) is "say" or "spk" or "msg";

        private static async Task<TestClient> EnterAsync(Player player, int channel = 0)
        {
            var client = Harness.Client(player, channel: channel);
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

        private static async Task<FamilyCharacter?> StoredMembershipAsync(long characterId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<FamilyCharacter>().AsNoTracking().SingleOrDefaultAsync(m => m.CharacterId == characterId);
        }

        private static async Task<Family> StoredFamilyAsync(long familyId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<Family>().AsNoTracking().SingleAsync(f => f.FamilyId == familyId);
        }

        [TestMethod]
        public async Task AMemberTheHeadDismissesIsOutOfTheFamily()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            var dismissed = alice.Mark();

            await alice.SendAsync($"%Familydismiss {member.Name}");

            // 301 is the client's "Dismissed".
            await alice.ExpectSinceAsync(dismissed, p => p.StartsWith("infoi 301 ", StringComparison.Ordinal), "the dismissed notice");
            await alice.ExpectSinceAsync(dismissed, FamilyInfo(family.Name, head.Name, 1, Head), "the family with one member");
            await alice.ExpectSinceAsync(dismissed, p => p.StartsWith($"gidx 1 {bob.CharacterId} -1 ", StringComparison.Ordinal), "Bob without a family");
            Assert.IsNull(await StoredMembershipAsync(member.CharacterId));

            var said = bob.Mark();
            await alice.SendAsync(":it is quiet here");
            await alice.ExpectAsync("spk");
            Assert.IsTrue(await bob.NoneSinceAsync(said, Heard, TimeSpan.FromSeconds(1)), "the family's words no longer reach him");
        }

        [TestMethod]
        public async Task AMemberOnAnotherChannelIsDismissedThereToo()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            await Harness.AddFamilyAsync(head, member);
            await using var bob = await EnterAsync(member, channel: 1);
            await using var alice = await EnterAsync(head);
            var dismissed = alice.Mark();

            await alice.SendAsync($"%Familydismiss {member.Name}");
            await alice.ExpectSinceAsync(dismissed, p => p.StartsWith("infoi 301 ", StringComparison.Ordinal), "the dismissed notice");
            Assert.IsNull(await StoredMembershipAsync(member.CharacterId));

            var said = bob.Mark();
            await alice.SendAsync(":it is quiet here");
            await alice.ExpectAsync("spk");
            Assert.IsTrue(await bob.NoneSinceAsync(said, Heard, TimeSpan.FromSeconds(1)), "the family's words no longer reach him");
            await bob.SendAsync("glist 0 2");
            Assert.IsTrue(await bob.NoneSinceAsync(said, p => TestClient.HeaderOf(p) == "gmbr", TimeSpan.FromSeconds(1)),
                "and he is shown no member list");
        }

        [TestMethod]
        public async Task AMemberDismissesNobody()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var other = await Harness.AddPlayerAsync("Carol", x: 82, y: 116);
            await Harness.AddFamilyAsync(head, member, other);
            await using var bob = await EnterAsync(member);

            await bob.SendAsync($"%Familydismiss {other.Name}");

            // 270 is the client's "You are not authorised to accept a family."
            await bob.ExpectAsync(p => p.StartsWith("infoi 270 ", StringComparison.Ordinal), "the not-authorised notice");
            Assert.IsNotNull(await StoredMembershipAsync(other.CharacterId));
        }

        [TestMethod]
        public async Task TheHeadsNoticeIsShownToMembersAndKept()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var later = await Harness.AddPlayerAsync("Carol", x: 82, y: 116);
            var family = await Harness.AddFamilyAsync(head, member, later);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            var noticed = bob.Mark();

            await alice.SendAsync("%Notice raid at nine");

            var threeMembers = FamilyInfo(family.Name, head.Name, 3, Member);
            // The notice ends the line, with its spaces written as carets.
            await bob.ExpectSinceAsync(noticed, p => threeMembers(p) && p.EndsWith(" raid^at^nine", StringComparison.Ordinal),
                "the family with its notice");
            Assert.AreEqual("raid at nine", (await StoredFamilyAsync(family.FamilyId)).FamilyMessage);

            await using var carol = Harness.Client(later);
            var entering = carol.Mark();
            await carol.EnterWorldAsync(later.Slot);
            await carol.ExpectSinceAsync(entering,
                p => TestClient.HeaderOf(p) == "ginfo" && p.EndsWith(" raid^at^nine", StringComparison.Ordinal),
                "a member who enters later is shown it too");
        }

        [TestMethod]
        public async Task AMemberCannotChangeTheNotice()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var bob = await EnterAsync(member);

            await bob.SendAsync("%Notice mine now");

            // 552 is the client's notice of not being allowed to change the notice.
            await bob.ExpectAsync(p => p.StartsWith("infoi 552 ", StringComparison.Ordinal), "the not-authorised notice");
            Assert.AreEqual(string.Empty, (await StoredFamilyAsync(family.FamilyId)).FamilyMessage);
        }

        [TestMethod]
        public async Task AShoutByTheHeadReachesEveryMemberAndNobodyElse()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", WorldData.Meadow, 20, 20);
            var away = await Harness.AddPlayerAsync("Dave", x: 83, y: 116);
            await Harness.AddFamilyAsync(head, member, away);
            await using var dave = await EnterAsync(away, channel: 1);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            var shouted = carol.Mark();

            await alice.SendAsync("%Familyshout everyone to the gate");

            StringAssert.EndsWith(await bob.ExpectAsync("msg"), "everyone to the gate");
            StringAssert.EndsWith(await dave.ExpectAsync("msg"), "everyone to the gate");
            Assert.IsTrue(await carol.NoneSinceAsync(shouted, Heard, TimeSpan.FromSeconds(1)), "someone outside the family hears nothing");
        }

        [TestMethod]
        public async Task AMemberCannotShout()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            await Harness.AddFamilyAsync(head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            var shouted = alice.Mark();

            await bob.SendAsync("%Familyshout listen to me");

            Assert.IsTrue(await alice.NoneSinceAsync(shouted, Heard, TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public async Task TheHeadAppointsADeputyWhoCanThenInvite()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            var invited = await Harness.AddPlayerAsync("Carol", x: 82, y: 116);
            await using var carol = await EnterAsync(invited);
            var appointed = bob.Mark();

            await alice.SendAsync($"%Familydeputy {member.Name}");

            // 282 is the client's notice that a new deputy was appointed.
            await alice.ExpectAsync(p => p.StartsWith("infoi 282 ", StringComparison.Ordinal), "the appointed notice");
            await bob.ExpectSinceAsync(appointed, FamilyInfo(family.Name, head.Name, 2, Assistant), "the family, for its new deputy");
            Assert.AreEqual(FamilyAuthority.Assistant, (await StoredMembershipAsync(member.CharacterId))?.Authority);

            await bob.SendAsync($"%Familyinvite {invited.Name}");

            StringAssert.StartsWith(await carol.ExpectAsync("dlgi2"), $"dlgi2 #gjoin^1^{bob.CharacterId} #gjoin^2^{bob.CharacterId} 275 1 {family.Name} ");
        }

        [TestMethod]
        public async Task TheHeadHandsTheFamilyToADeputy()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);

            await alice.SendAsync($"%Familyhead {member.Name}");

            // 294 is the client's notice that only a deputy can become the head.
            await alice.ExpectAsync(p => p.StartsWith("infoi 294 ", StringComparison.Ordinal), "the only-a-deputy notice");
            Assert.AreEqual(FamilyAuthority.Member, (await StoredMembershipAsync(member.CharacterId))?.Authority);

            await alice.SendAsync($"%Familydeputy {member.Name}");
            await alice.ExpectAsync(p => p.StartsWith("infoi 282 ", StringComparison.Ordinal), "the appointed notice");
            var handed = alice.Mark();
            var received = bob.Mark();
            await alice.SendAsync($"%Familyhead {member.Name}");

            // 563 is the client's notice that the head has changed.
            await alice.ExpectSinceAsync(handed, p => p.StartsWith("infoi 563 ", StringComparison.Ordinal), "the head-changed notice");
            await alice.ExpectSinceAsync(handed, FamilyInfo(family.Name, member.Name, 2, Assistant), "the family under Bob, for Alice");
            await bob.ExpectSinceAsync(received, FamilyInfo(family.Name, member.Name, 2, Head), "the family under Bob, for Bob");
            Assert.AreEqual(FamilyAuthority.Head, (await StoredMembershipAsync(member.CharacterId))?.Authority);
            Assert.AreEqual(FamilyAuthority.Assistant, (await StoredMembershipAsync(head.CharacterId))?.Authority);
        }

        [TestMethod]
        public async Task SomeoneJoiningIsToldToMembersOnAnotherChannel()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Dave", x: 83, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var dave = await EnterAsync(member, channel: 1);
            await using var alice = await EnterAsync(head);
            var invited = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            await using var bob = await EnterAsync(invited);
            await alice.SendAsync($"%Familyinvite {invited.Name}");
            await bob.ExpectAsync("dlgi2");
            var joined = dave.Mark();

            await bob.SendAsync($"gjoin 1 {alice.CharacterId}");

            await dave.ExpectSinceAsync(joined, FamilyInfo(family.Name, head.Name, 3, Member), "the family with three members");
            // 276 is the client's "%s has joined the family."
            await dave.ExpectSinceAsync(joined, p => p.StartsWith($"msgi2 0 276 1 {invited.Name} ", StringComparison.Ordinal), "the welcome");
        }
    }
}
