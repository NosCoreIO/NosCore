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
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using GListPacket = NosCore.Packets.ClientPackets.Families.GListPacket;
using JoinFamilyPacket = NosCore.Packets.ClientPackets.Families.JoinFamilyPacket;
using StoredAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FamilyManagementTests
    {
        private static Func<GInfoPacket, bool> FamilyInfo(string family, string head, int members, FamilyAuthority authority) =>
            packet => packet.FamilyName == family && packet.CharacterName == head && packet.MembersCount == members
                && packet.CharacterFamilyAuthority == authority;

        private static Func<InfoiPacket, bool> Told(Game18NConstString message) => packet => packet.Message == message;

        private static Func<GidxPacket, bool> WithoutAFamily(TestClient who) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == who.CharacterId && packet.FamilyId == null;

        // A shout still goes out as plain text.
#pragma warning disable CS0618
        private static bool Heard(IPacket packet) => packet is SayPacket or SpeakPacket or MsgPacket;

        private static Func<MsgPacket, bool> Shouted(string words) => packet => packet.Message?.EndsWith(words, StringComparison.Ordinal) == true;
#pragma warning restore CS0618

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

            await alice.SendAsync(new FamilyDismissPacket { CharacterName = member.Name });
            await alice.ExpectSinceAsync(dismissed, Told(Game18NConstString.Dismissed2), "the dismissed notice");
            await alice.ExpectSinceAsync(dismissed, FamilyInfo(family.Name, head.Name, 1, FamilyAuthority.Head), "the family with one member");
            await alice.ExpectSinceAsync(dismissed, WithoutAFamily(bob), "Bob without a family");
            Assert.IsNull(await StoredMembershipAsync(member.CharacterId));

            var said = bob.Mark();
            await alice.SendAsync(new FamilyChatPacket { Message = "it is quiet here" });
            await alice.ExpectAsync<SpeakPacket>();
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(said, Heard, TimeSpan.FromSeconds(1)), "the family's words no longer reach him");
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
            var told = bob.Mark();

            await alice.SendAsync(new FamilyDismissPacket { CharacterName = member.Name });
            await alice.ExpectSinceAsync(dismissed, Told(Game18NConstString.Dismissed2), "the dismissed notice");
            Assert.IsNull(await StoredMembershipAsync(member.CharacterId));
            await bob.ExpectSinceAsync(told, WithoutAFamily(bob), "Bob told on his channel");

            var said = bob.Mark();
            await alice.SendAsync(new FamilyChatPacket { Message = "it is quiet here" });
            await alice.ExpectAsync<SpeakPacket>();
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(said, Heard, TimeSpan.FromSeconds(1)), "the family's words no longer reach him");
            await bob.SendAsync(new GListPacket { Type = GListPacketType.MemberList });
            Assert.IsTrue(await bob.NoneSinceAsync<GmbrPacket>(said, null, TimeSpan.FromSeconds(1)),
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

            await bob.SendAsync(new FamilyDismissPacket { CharacterName = other.Name });
            await bob.ExpectAsync(Told(Game18NConstString.NotAuthorizedToAcceptFamily), "the not-authorised notice");
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

            await alice.SendAsync(new FamilyNoticePacket { Message = "raid at nine" });

            var threeMembers = FamilyInfo(family.Name, head.Name, 3, FamilyAuthority.Member);
            await bob.ExpectSinceAsync<GInfoPacket>(noticed, p => threeMembers(p) && p.FamilyMessage == "raid at nine", "the family with its notice");
            Assert.AreEqual("raid at nine", (await StoredFamilyAsync(family.FamilyId)).FamilyMessage);

            await using var carol = Harness.Client(later);
            var entering = carol.Mark();
            await carol.EnterWorldAsync(later.Slot);
            await carol.ExpectSinceAsync<GInfoPacket>(entering, p => p.FamilyMessage == "raid at nine", "a member who enters later is shown it too");
        }

        [TestMethod]
        public async Task AMemberCannotChangeTheNotice()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var bob = await EnterAsync(member);

            await bob.SendAsync(new FamilyNoticePacket { Message = "mine now" });
            await bob.ExpectAsync(Told(Game18NConstString.NotAuthorizedToChangeNoticed), "the not-authorised notice");
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

            await alice.SendAsync(new FamilyShoutPacket { Message = "everyone to the gate" });

            await bob.ExpectAsync(Shouted("everyone to the gate"), "the shout");
            await dave.ExpectAsync(Shouted("everyone to the gate"), "the shout on another channel");
            Assert.IsTrue(await carol.NoneSinceAsync<IPacket>(shouted, Heard, TimeSpan.FromSeconds(1)), "someone outside the family hears nothing");
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

            await bob.SendAsync(new FamilyShoutPacket { Message = "listen to me" });

            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(shouted, Heard, TimeSpan.FromSeconds(1)));
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

            await alice.SendAsync(new FamilyDeputyPacket { CharacterName = member.Name });
            await alice.ExpectAsync(Told(Game18NConstString.NewFamilyDeputyAppointed), "the appointed notice");
            await bob.ExpectSinceAsync(appointed, FamilyInfo(family.Name, head.Name, 2, FamilyAuthority.Assistant), "the family, for its new deputy");
            Assert.AreEqual(StoredAuthority.Assistant, (await StoredMembershipAsync(member.CharacterId))?.Authority);

            await bob.SendAsync(new FamilyInvitePacket { CharacterName = invited.Name });

            var asked = await carol.ExpectAsync<Dlgi2Packet>();
            PacketAssert.AreEqual(new JoinFamilyPacket { Type = FamilyInviteAnswerType.Accept, CharacterId = bob.CharacterId }, asked.YesPacket as JoinFamilyPacket);
            PacketAssert.AreEqual(new JoinFamilyPacket { Type = FamilyInviteAnswerType.Decline, CharacterId = bob.CharacterId }, asked.NoPacket as JoinFamilyPacket);
            Assert.AreEqual((Game18NConstString.AskMemberFamily, (byte)1, (object)family.Name), (asked.Question, asked.ArgumentType, asked.Game18NArguments[0]));
        }

        [TestMethod]
        public async Task TheHeadHandsTheFamilyToADeputy()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);

            await alice.SendAsync(new FamilyHeadPacket { CharacterName = member.Name });
            await alice.ExpectAsync(Told(Game18NConstString.OnlyFamilyDeputyCanBecomeHEad), "the only-a-deputy notice");
            Assert.AreEqual(StoredAuthority.Member, (await StoredMembershipAsync(member.CharacterId))?.Authority);

            await alice.SendAsync(new FamilyDeputyPacket { CharacterName = member.Name });
            await alice.ExpectAsync(Told(Game18NConstString.NewFamilyDeputyAppointed), "the appointed notice");
            var handed = alice.Mark();
            var received = bob.Mark();
            await alice.SendAsync(new FamilyHeadPacket { CharacterName = member.Name });
            await alice.ExpectSinceAsync(handed, Told(Game18NConstString.FamilyHeadChanged), "the head-changed notice");
            await alice.ExpectSinceAsync(handed, FamilyInfo(family.Name, member.Name, 2, FamilyAuthority.Assistant), "the family under Bob, for Alice");
            await bob.ExpectSinceAsync(received, FamilyInfo(family.Name, member.Name, 2, FamilyAuthority.Head), "the family under Bob, for Bob");
            Assert.AreEqual(StoredAuthority.Head, (await StoredMembershipAsync(member.CharacterId))?.Authority);
            Assert.AreEqual(StoredAuthority.Assistant, (await StoredMembershipAsync(head.CharacterId))?.Authority);
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
            await alice.SendAsync(new FamilyInvitePacket { CharacterName = invited.Name });
            await bob.ExpectAsync<Dlgi2Packet>();
            var joined = dave.Mark();

            await bob.SendAsync(new JoinFamilyPacket { Type = FamilyInviteAnswerType.Accept, CharacterId = alice.CharacterId });

            await dave.ExpectSinceAsync(joined, FamilyInfo(family.Name, head.Name, 3, FamilyAuthority.Member), "the family with three members");
            await dave.ExpectSinceAsync<Msgi2Packet>(joined, p => p is { Type: MessageType.Default, Message: Game18NConstString.JoinedFamily, ArgumentType: 1 }
                && Equals(p.Game18NArguments[0], invited.Name), "the welcome");
        }
    }
}
