//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Groups;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class GroupTests
    {
        private const int MostHp = 221;

        private static Func<InfoiPacket, bool> Told(Game18NConstString message) => packet => packet.Message == message;

        private static Func<MsgiPacket, bool> Announced(Game18NConstString message) => packet => packet.Message == message;

        private static Func<PinitPacket, bool> GroupOf(int size) => packet => packet.GroupSize == size;

        private static long[] Members(PinitPacket pinit) =>
            (pinit.PinitSubPackets ?? []).OfType<PinitSubPacket>().Select(member => member.VisualId).OrderBy(id => id).ToArray();

        private static long[] Ids(params TestClient[] clients) => clients.Select(c => c.CharacterId).OrderBy(id => id).ToArray();

        private static PjoinPacket Join(GroupRequestType type, TestClient other) => new() { RequestType = type, CharacterId = other.CharacterId };

        private static Func<Dlgi2Packet, bool> InvitedBy(TestClient other) =>
            packet => packet.YesPacket is PjoinPacket { RequestType: GroupRequestType.Accepted } yes && yes.CharacterId == other.CharacterId;

        private static Func<PstPacket, bool> BarsOf(TestClient member) =>
            packet => packet.Type == VisualType.Player && packet.VisualId == member.CharacterId;

        private static Func<PidxPacket, bool> Ungrouped(TestClient player) =>
            packet => packet.GroupId == -1 && packet.SubPackets?.SingleOrDefault()?.VisualId == player.CharacterId;

        private static readonly GopPacket ShareTheLoot = new() { Option = GopPacketType.GroupSharing, IsActive = true };

        // The newcomer joins whatever group the inviter is in; the group is then of the given size.
        private static async Task InviteAsync(TestClient inviter, TestClient newcomer, int size)
        {
            await inviter.SendAsync(Join(GroupRequestType.Invited, newcomer));
            await newcomer.ExpectAsync(InvitedBy(inviter), "the group invitation");
            var joined = inviter.Mark();
            var accepted = newcomer.Mark();
            await newcomer.SendAsync(Join(GroupRequestType.Accepted, inviter));
            await inviter.ExpectSinceAsync(joined, GroupOf(size), $"the group of {size}");
            await newcomer.ExpectSinceAsync(accepted, GroupOf(size), $"the group of {size}");
        }

        private static async Task<string?> HeardAsync(TestClient listener) => (await listener.ExpectAsync<SpeakPacket>()).Message;

        [TestMethod]
        public async Task TheOneInvitedIsAskedAndTheInviterIsToldWhoWasAsked()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            await alice.SendAsync(Join(GroupRequestType.Invited, bob));

            var asked = await bob.ExpectAsync<Dlgi2Packet>();
            Assert.IsTrue(InvitedBy(alice)(asked), "the invitation to accept");
            Assert.IsTrue(asked.NoPacket is PjoinPacket { RequestType: GroupRequestType.Declined } no && no.CharacterId == alice.CharacterId,
                "and to decline");
            var told = await alice.ExpectAsync<Infoi2Packet>();
            Assert.AreEqual((Game18NConstString.YouInvitedToGroup, (byte)1), (told.Message, told.ArgumentType));
            StringAssert.StartsWith(told.Game18NArguments[0].ToString(), "Bob");
        }

        [TestMethod]
        public async Task ADeclinedInvitationFormsNoGroupAndTheOneWhoDeclinedIsToldSo()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await alice.SendAsync(Join(GroupRequestType.Invited, bob));
            await bob.ExpectAsync<Dlgi2Packet>();
            var declined = alice.Mark();

            await bob.SendAsync(Join(GroupRequestType.Declined, alice));

            await bob.ExpectAsync<Sayi2Packet>(p => p.VisualType == VisualType.Player && p.VisualId == bob.CharacterId
                && p.Message == Game18NConstString.GroupInviteRejected, "the refusal noted");
            Assert.IsFalse(alice.Since<PinitPacket>(declined).Any(GroupOf(2)), "no group is formed");

            // An answer given twice is not an invitation any more.
            var late = alice.Mark();
            await bob.SendAsync(Join(GroupRequestType.Accepted, alice));
            Assert.IsTrue(await alice.NoneSinceAsync(late, GroupOf(2), TimeSpan.FromSeconds(1)), "accepting after declining forms no group");
        }

        [TestMethod]
        public async Task AGroupCannotBeJoinedWithoutAnInvitation()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            var uninvited = alice.Mark();

            await bob.SendAsync(Join(GroupRequestType.Accepted, alice));

            Assert.IsTrue(await alice.NoneSinceAsync(uninvited, GroupOf(2), TimeSpan.FromSeconds(1)), "no group is formed");
        }

        [TestMethod]
        public async Task AGroupFormedIsShownToItsMembersAndToTheMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await alice.SendAsync(Join(GroupRequestType.Invited, bob));
            await bob.ExpectAsync<Dlgi2Packet>();
            var formed = alice.Mark();
            var seen = carol.Mark();

            await bob.SendAsync(Join(GroupRequestType.Accepted, alice));

            CollectionAssert.AreEqual(Ids(alice, bob), Members(await alice.ExpectSinceAsync(formed, GroupOf(2), "the group of two")));
            CollectionAssert.AreEqual(Ids(alice, bob), Members(await bob.ExpectAsync(GroupOf(2), "the group of two")));
            await alice.ExpectSinceAsync(formed, Told(Game18NConstString.YouAreNowGroupLeader), "the inviter told she leads");
            var shown = await carol.ExpectSinceAsync<PidxPacket>(seen, null, "the group shown to the map");
            Assert.AreNotEqual(-1, shown.GroupId, "the group has a number");
            CollectionAssert.AreEqual(Ids(alice, bob),
                (shown.SubPackets ?? []).OfType<PidxSubPacket>().Select(member => member.VisualId).OrderBy(id => id).ToArray());
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
            var place = (list.PinitSubPackets ?? []).OfType<PinitSubPacket>().Single(member => member.VisualId == bob.CharacterId).GroupPosition;
            var bar = await alice.ExpectSinceAsync(formed, BarsOf(bob), "Bob's bars");
            Assert.AreEqual(1, place, "the one who invited holds the first place");
            Assert.AreEqual(place, bar.GroupOrder, "his bars are sent for the place the list gave him");
        }

        [TestMethod]
        public async Task MembersAreToldEachOthersHpWhenTheGroupForms()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: c => c.Hp = 100);
            var formed = alice.Mark();

            await InviteAsync(alice, bob, 2);

            var bar = await alice.ExpectSinceAsync(formed, BarsOf(bob), "Bob's bars");
            Assert.AreEqual(100 * 100 / MostHp, bar.HpLeft, "the part of his HP he has left");
            Assert.AreEqual(MostHp, bar.HpLoad);
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

            await bob.SendAsync(new UseItemPacket
            {
                VisualType = VisualType.Player, VisualId = bob.CharacterId, Type = PocketType.Main, Slot = 0, Mode = 0, Parameter = 0
            });
            await bob.ExpectAsync<StatPacket>(p => p.Hp == 100 + WorldData.PotionHp, "the HP after the potion");

            // The bars come from a timer: one may be sent before the potion, and regeneration can add
            // to his HP before the next, so the bar is only known to reach at least the potion's share.
            var healed = (100 + WorldData.PotionHp) * 100 / MostHp;
            await alice.ExpectSinceAsync<PstPacket>(drunk, p => BarsOf(bob)(p) && p.HpLeft >= healed, $"Bob's bars at {healed}% or more");
        }

        [TestMethod]
        public async Task TheOneWhoInvitedSetsHowTheGroupLoots()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            var formed = alice.Mark();
            await InviteAsync(alice, bob, 2);
            await alice.ExpectSinceAsync(formed, Told(Game18NConstString.YouAreNowGroupLeader), "the inviter told she leads");
            var refused = bob.Mark();

            await bob.SendAsync(ShareTheLoot);

            await bob.ExpectSinceAsync(refused, Told(Game18NConstString.CanNotChangeGroupMode), "the member refused");
            var changed = alice.Mark();
            var heard = bob.Mark();

            await alice.SendAsync(ShareTheLoot);

            await bob.ExpectSinceAsync(heard, Announced(Game18NConstString.ShareAsGroupChanged), "the new way of looting announced to the member");
            await alice.ExpectSinceAsync(changed, Announced(Game18NConstString.ShareAsGroupChanged), "the new way of looting announced to the leader");
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

            await bob.SendAsync(new PleavePacket());

            await alice.ExpectSinceAsync(left, GroupOf(0), "Alice on her own");
            await bob.ExpectSinceAsync(gone, GroupOf(0), "Bob on his own");
            await alice.ExpectSinceAsync(left, Announced(Game18NConstString.PartyDisbanded), "the end of the group announced to Alice");
            await bob.ExpectSinceAsync(gone, Announced(Game18NConstString.PartyDisbanded), "the end of the group announced to Bob");
            await carol.ExpectSinceAsync(seen, Ungrouped(alice), "Alice shown without a group");
            await carol.ExpectSinceAsync(seen, Ungrouped(bob), "Bob shown without a group");
        }

        [TestMethod]
        public async Task TheEndOfAGroupIsAnnouncedOnceToEachMember()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await InviteAsync(alice, bob, 2);
            var left = alice.Mark();
            var gone = bob.Mark();

            await bob.SendAsync(new PleavePacket());

            await alice.ExpectSinceAsync(left, Announced(Game18NConstString.PartyDisbanded), "the end of the group announced to Alice");
            await bob.ExpectSinceAsync(gone, Announced(Game18NConstString.PartyDisbanded), "the end of the group announced to Bob");
            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.AreEqual(1, alice.Since<MsgiPacket>(left).Count(Announced(Game18NConstString.PartyDisbanded)), "announcements to Alice");
            Assert.AreEqual(1, bob.Since<MsgiPacket>(gone).Count(Announced(Game18NConstString.PartyDisbanded)), "announcements to Bob");
        }

        [TestMethod]
        public async Task ANewGroupCanBeFormedAfterOneWasDisbanded()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await InviteAsync(alice, bob, 2);
            var left = alice.Mark();
            await alice.SendAsync(new PleavePacket());
            await alice.ExpectSinceAsync(left, GroupOf(0), "Alice on her own");
            await bob.ExpectAsync(GroupOf(0), "Bob on his own");

            await InviteAsync(bob, alice, 2);

            await alice.SendAsync(new GroupTalkPacket { Message = "still here" });
            StringAssert.EndsWith(await HeardAsync(bob), "still here");
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

            await carol.SendAsync(new PleavePacket());

            await carol.ExpectSinceAsync(left, GroupOf(0), "Carol on her own");
            CollectionAssert.AreEqual(Ids(alice, bob), Members(await alice.ExpectSinceAsync(stayed, GroupOf(2), "the group of two")));
            CollectionAssert.AreEqual(Ids(alice, bob), Members(await bob.ExpectSinceAsync(kept, GroupOf(2), "the group of two")));
            Assert.IsFalse(alice.Since<MsgiPacket>(stayed).Any(Announced(Game18NConstString.PartyDisbanded))
                || bob.Since<MsgiPacket>(kept).Any(Announced(Game18NConstString.PartyDisbanded)),
                "the group is not disbanded");

            var said = carol.Mark();
            await alice.SendAsync(new GroupTalkPacket { Message = "two of us now" });
            StringAssert.EndsWith(await HeardAsync(bob), "two of us now");
            Assert.IsTrue(await carol.NoneSinceAsync<SpeakPacket>(said, null, TimeSpan.FromSeconds(1)),
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
            await alice.ExpectSinceAsync(formed, Told(Game18NConstString.YouAreNowGroupLeader), "the inviter told she leads");
            await InviteAsync(bob, carol, 3);
            var promoted = bob.Mark();
            var kept = carol.Mark();
            var left = alice.Mark();

            await alice.SendAsync(new PleavePacket());

            await alice.ExpectSinceAsync(left, GroupOf(0), "Alice on her own");
            CollectionAssert.AreEqual(Ids(bob, carol), Members(await bob.ExpectSinceAsync(promoted, GroupOf(2), "the group of two")));
            CollectionAssert.AreEqual(Ids(bob, carol), Members(await carol.ExpectSinceAsync(kept, GroupOf(2), "the group of two")));
            await bob.ExpectSinceAsync(promoted, Told(Game18NConstString.YouAreNowGroupLeader), "Bob told he leads");
            Assert.IsFalse(carol.Since<InfoiPacket>(kept).Any(Told(Game18NConstString.YouAreNowGroupLeader)), "Carol is not told she leads");
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
            await carol.SendAsync(Join(GroupRequestType.Invited, dave));
            await dave.ExpectAsync(InvitedBy(carol), "the group invitation");
            var refused = carol.Mark();
            var turnedAway = dave.Mark();

            await dave.SendAsync(Join(GroupRequestType.Accepted, carol));

            await dave.ExpectSinceAsync(turnedAway, Told(Game18NConstString.GroupIsFull), "the fourth turned away");
            await carol.ExpectSinceAsync(refused, Told(Game18NConstString.GroupIsFull), "the one who invited told why");
            Assert.IsFalse(dave.Since<PinitPacket>(turnedAway).Any(p => Members(p).Length > 0),
                "the fourth is in no group");

            var asked = dave.Mark();
            await dave.SendAsync(Join(GroupRequestType.Invited, alice));
            await dave.ExpectSinceAsync(asked, Told(Game18NConstString.GroupIsFull), "the fourth turned away when he asks");
            var said = dave.Mark();
            await alice.SendAsync(new GroupTalkPacket { Message = "three is enough" });
            StringAssert.EndsWith(await HeardAsync(carol), "three is enough");
            Assert.IsFalse(dave.Since<SpeakPacket>(said).Any(), "the fourth does not hear the group");
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
            await alice.SendAsync(new GroupTalkPacket { Message = "he is gone" });
            StringAssert.EndsWith(await HeardAsync(carol), "he is gone");
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
            await alice.ExpectSinceAsync(left, Announced(Game18NConstString.PartyDisbanded), "the end of the group announced");
        }

        [TestMethod]
        public async Task AKillGivesExperienceToEveryMemberOnTheMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 71);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 32, 72);
            await InviteAsync(alice, bob, 2);
            await alice.ExpectSinceAsync<InPacket>(0, p => p.VisualType == VisualType.Monster && p.VisualId == WorldData.Prey, "the prey on the map");
            var killed = alice.Mark();
            var watched = bob.Mark();

            await Steps.KillAsync(alice, WorldData.Prey);

            var hers = (await alice.ExpectSinceAsync<LevPacket>(killed, null, "her experience")).LevelXp;
            var his = (await bob.ExpectSinceAsync<LevPacket>(watched, null, "his experience")).LevelXp;
            Assert.IsTrue(hers > 0, "the one who killed gains experience");
            Assert.AreEqual(hers, his, "the one who only watched gains as much");
        }
    }
}
