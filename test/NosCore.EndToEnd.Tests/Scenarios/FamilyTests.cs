//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using CreateFamilyPacket = NosCore.Packets.ClientPackets.Families.CreateFamilyPacket;
using DisbandFamilyPacket = NosCore.Packets.ClientPackets.Families.FamilyDismissPacket;
using GListPacket = NosCore.Packets.ClientPackets.Families.GListPacket;
using JoinFamilyPacket = NosCore.Packets.ClientPackets.Families.JoinFamilyPacket;
using StoredAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FamilyTests
    {
        private const long CreationPrice = 200000;

        private static Action<Character> Swordsman(long gold = 0) => c =>
        {
            c.Class = CharacterClassType.Swordsman;
            c.Gold = gold;
        };

        private static Func<GInfoPacket, bool> FamilyInfo(string family, int members, FamilyAuthority authority) =>
            packet => packet.FamilyName == family && packet.MembersCount == members && packet.CharacterFamilyAuthority == authority;

        private static Func<InfoiPacket, bool> Told(Game18NConstString message) => packet => packet.Message == message;

        private static Func<GidxPacket, bool> InTheFamily(TestClient who, long familyId, string family) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == who.CharacterId && packet.FamilyId == familyId
                && packet.FamilyName?.StartsWith($"{family}(", StringComparison.Ordinal) == true;

        private static Func<GidxPacket, bool> WithoutAFamily(TestClient who) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == who.CharacterId && packet.FamilyId == null;

        private static bool Heard(IPacket packet) => packet is SayPacket or SpeakPacket;

        private static CreateFamilyPacket Found(string name) => new() { FamilyName = name };

        private static JoinFamilyPacket Answer(FamilyInviteAnswerType answer, TestClient inviter) => new() { Type = answer, CharacterId = inviter.CharacterId };

        // The others ask to join in turn: one player may only send a group request every five seconds.
        private static async Task GroupAsync(TestClient first, params TestClient[] others)
        {
            var size = 1;
            foreach (var other in others)
            {
                size++;
                await other.SendAsync(new PjoinPacket { RequestType = GroupRequestType.Invited, CharacterId = first.CharacterId });
                await first.ExpectAsync<Dlgi2Packet>(p => p.YesPacket is PjoinPacket { RequestType: GroupRequestType.Accepted } yes
                    && yes.CharacterId == other.CharacterId, "the group request");
                var joined = other.Mark();
                await first.SendAsync(new PjoinPacket { RequestType = GroupRequestType.Accepted, CharacterId = other.CharacterId });
                var groupSize = size;
                await other.ExpectSinceAsync<PinitPacket>(joined, p => p.GroupSize == groupSize, $"the group of {size}");
            }
        }

        private static SayPacket Said(TestClient speaker, string words) =>
            new() { VisualType = VisualType.Player, VisualId = speaker.CharacterId, Type = SayColorType.Blue, Message = words };

        private static SpeakPacket Spoken(TestClient speaker, string name, string words) => new()
        {
            VisualType = VisualType.Player, VisualId = speaker.CharacterId, SpeakType = SpeakType.Family, EntityName = name, Message = words,
        };

        private static GmbrSubPacket Listed(Player player, long familyId, FamilyAuthority authority, bool online) => new()
        {
            CharacterId = player.CharacterId, FamilyId = familyId, Name = player.Name, Level = 1, Class = CharacterClassType.Adventurer,
            Authority = authority, Rank = FamilyMemberRank.Nothing, IsConnected = online, HeroLevel = 0,
        };

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

            await alice.SendAsync(Found(name));

            await alice.ExpectSinceAsync<GoldPacket>(founded, p => p.Gold == 1000000 - CreationPrice && p.Unknown == 0, "the gold left after the price");
            await dave.ExpectSinceAsync<Msgi2Packet>(watched, p => p is { Type: MessageType.Default, Message: Game18NConstString.FamilyCreated, ArgumentType: 1 }
                && Equals(p.Game18NArguments[0], name), "the announcement");
            await alice.ExpectSinceAsync(founded, FamilyInfo(name, 3, FamilyAuthority.Head), "the family, for its head");
            await bob.ExpectAsync(FamilyInfo(name, 3, FamilyAuthority.Assistant), "the family, for a founder");
            await carol.ExpectAsync(FamilyInfo(name, 3, FamilyAuthority.Assistant), "the family, for a founder");
            var stored = await StoredMembersAsync(name);
            CollectionAssert.AreEquivalent(new[] { alice.CharacterId, bob.CharacterId, carol.CharacterId },
                stored.Select(m => m.CharacterId).ToArray());
            Assert.AreEqual(StoredAuthority.Head, stored.Single(m => m.CharacterId == alice.CharacterId).Authority);
            var familyId = stored[0].FamilyId;
            await dave.ExpectSinceAsync(watched, InTheFamily(alice, familyId, name), "the family under Alice's name, for a bystander");
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
                await alice.SendAsync(Found(name));
                await alice.ExpectAsync(FamilyInfo(name, 3, FamilyAuthority.Head), "the family");
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

            await alice.SendAsync(Found(name));

            PacketAssert.AreEqual(new Infoi2Packet { Message = Game18NConstString.NeedThreeMember, ArgumentType = 4, Game18NArguments = { 3 } },
                await alice.ExpectSinceAsync<Infoi2Packet>(asked, null, "the three-members notice"));
            Assert.AreEqual(0, (await StoredMembersAsync(name)).Length, "no family is stored");
            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, p => p is GoldPacket or GInfoPacket, TimeSpan.FromSeconds(1)),
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

            await alice.SendAsync(Found(name));
            await alice.ExpectAsync(Told(Game18NConstString.OneOrMoreMemberHaveToChangeClass), "the change-class notice");
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

            await alice.SendAsync(Found(name));
            await alice.ExpectSinceAsync(asked, Told(Game18NConstString.NeedMoneyForFamily), "the not-enough-gold notice");
            Assert.AreEqual(0, (await StoredMembersAsync(name)).Length, "no family is stored");
            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, p => p is GoldPacket, TimeSpan.FromSeconds(1)), "no gold is taken");
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

            await alice.SendAsync(Found(taken.Name));
            await alice.ExpectSinceAsync(asked, Told(Game18NConstString.FamilyAlreadyExist), "the name-taken notice");
            Assert.IsNull(await StoredMembershipAsync(alice.CharacterId));
            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, p => p is GoldPacket, TimeSpan.FromSeconds(1)), "no gold is taken");
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

            await alice.SendAsync(Found(name));
            await alice.ExpectAsync(Told(Game18NConstString.OneMemberAlreadyHaveAFamily), "the already-in-a-family notice");
            Assert.AreEqual(0, (await StoredMembersAsync(name)).Length, "no family is stored");
        }

        [TestMethod]
        public async Task TheFoundingDialogAsksForAName()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: Swordsman(1000000));
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116, configure: Swordsman());
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116, configure: Swordsman());
            await GroupAsync(alice, bob, carol);

            await alice.SendAsync(new NrunPacket { Runner = NrunRunnerType.FamilyStartOrDisband, Type = 0 });

            var inbox = await alice.ExpectAsync<InboxPacket>();
            Assert.IsInstanceOfType<CreateFamilyPacket>(inbox.BoxPacket, "a name is asked to found the family");
            Assert.AreEqual(((byte)14, (byte)1, Game18NConstString.EnterFamilyName), (inbox.MaxLength, inbox.Type, inbox.Message));
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

            await alice.SendAsync(new FamilyInvitePacket { CharacterName = other.Name });
            await alice.ExpectAsync(Told(Game18NConstString.PlayerAlreadyInAnotherFamily), "the in-another-family notice");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(asked, p => p is Dlgi2Packet, TimeSpan.FromSeconds(1)), "nobody is asked");
        }

        [TestMethod]
        public async Task AMemberIsToldItsFamilyOnEnteringAndBystandersSeeIt()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(head, member);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await using var bob = Harness.Client(member);

            await bob.EnterWorldAsync(member.Slot);

            Assert.IsTrue(bob.Received.OfType<GInfoPacket>().Any(FamilyInfo(family.Name, 2, FamilyAuthority.Member)), "the family is sent to its member");
            await carol.ExpectAsync(InTheFamily(bob, family.FamilyId, family.Name), "the family under Bob's name");
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

            await alice.SendAsync(new FamilyChatPacket { Message = "meet at the gate" });

            PacketAssert.AreEqual(Said(alice, "meet at the gate"), await bob.ExpectAsync<SayPacket>());
            PacketAssert.AreEqual(Spoken(alice, head.Name, "meet at the gate"), await bob.ExpectAsync<SpeakPacket>());
            // A member on another map cannot see who speaks, so the line names her.
            PacketAssert.AreEqual(Said(alice, $"[{head.Name}]:meet at the gate"), await dave.ExpectAsync<SayPacket>());
            Assert.IsTrue(await carol.NoneSinceAsync<IPacket>(said, Heard, TimeSpan.FromSeconds(1)),
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

            await alice.SendAsync(new FamilyChatPacket { Message = "anyone on channel two" });

            PacketAssert.AreEqual(Spoken(alice, head.Name, "anyone on channel two"), await bob.ExpectAsync<SpeakPacket>());
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

            await carol.SendAsync(new FamilyChatPacket { Message = "is anyone there" });

            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(said, Heard, TimeSpan.FromSeconds(1)));
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

            await alice.SendAsync(new FamilyInvitePacket { CharacterName = invited.Name });

            var asked = await bob.ExpectAsync<Dlgi2Packet>();
            PacketAssert.AreEqual(Answer(FamilyInviteAnswerType.Accept, alice), asked.YesPacket as JoinFamilyPacket);
            PacketAssert.AreEqual(Answer(FamilyInviteAnswerType.Decline, alice), asked.NoPacket as JoinFamilyPacket);
            Assert.AreEqual((Game18NConstString.AskMemberFamily, (byte)1, (object)family.Name), (asked.Question, asked.ArgumentType, asked.Game18NArguments[0]));
            var joined = alice.Mark();
            var watched = carol.Mark();
            await bob.SendAsync(Answer(FamilyInviteAnswerType.Accept, alice));

            await bob.ExpectAsync(FamilyInfo(family.Name, 2, FamilyAuthority.Member), "the family, for its new member");
            await alice.ExpectSinceAsync(joined, FamilyInfo(family.Name, 2, FamilyAuthority.Head), "the family with two members, for its head");
            await alice.ExpectSinceAsync<Msgi2Packet>(joined, p => p is { Type: MessageType.Default, Message: Game18NConstString.JoinedFamily, ArgumentType: 1 }
                && Equals(p.Game18NArguments[0], invited.Name), "the welcome");
            await carol.ExpectSinceAsync(watched, InTheFamily(bob, family.FamilyId, family.Name), "the family under Bob's name, for a bystander");
            var stored = await StoredMembershipAsync(bob.CharacterId);
            Assert.AreEqual(family.FamilyId, stored?.FamilyId);
            Assert.AreEqual(StoredAuthority.Member, stored?.Authority);
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

            await alice.SendAsync(new FamilyInvitePacket { CharacterName = invited.Name });
            await bob.ExpectAsync<Dlgi2Packet>();
            var answered = bob.Mark();
            await bob.SendAsync(Answer(FamilyInviteAnswerType.Decline, alice));
            await alice.ExpectAsync(Told(Game18NConstString.PlayerRefusedFamilyInvite), "the refusal");
            Assert.IsNull(await StoredMembershipAsync(bob.CharacterId));
            Assert.AreEqual(1, (await StoredMembersAsync(family.Name)).Length);
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(answered, p => p is GInfoPacket, TimeSpan.FromSeconds(1)));
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

            await bob.SendAsync(Answer(FamilyInviteAnswerType.Accept, alice));

            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(asked, p => p is GInfoPacket, TimeSpan.FromSeconds(1)));
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

            await bob.SendAsync(new FamilyInvitePacket { CharacterName = outsider.Name });
            await bob.ExpectAsync(Told(Game18NConstString.NotAuthorizedToAcceptFamily), "the not-authorised notice");
            Assert.IsTrue(await carol.NoneSinceAsync<IPacket>(asked, p => p is Dlgi2Packet, TimeSpan.FromSeconds(1)),
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

            await bob.SendAsync(new FamilyLeavePacket());
            await bob.ExpectAsync(Told(Game18NConstString.LeftFamily), "the left-the-family notice");
            await alice.ExpectSinceAsync(left, FamilyInfo(family.Name, 1, FamilyAuthority.Head), "the family with one member, for its head");
            await alice.ExpectSinceAsync(left, WithoutAFamily(bob), "Bob without a family");
            Assert.IsNull(await StoredMembershipAsync(bob.CharacterId));

            var said = bob.Mark();
            await alice.SendAsync(new FamilyChatPacket { Message = "it is quiet here" });
            await alice.ExpectAsync<SpeakPacket>();
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(said, Heard, TimeSpan.FromSeconds(1)),
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

            await alice.SendAsync(new FamilyLeavePacket());
            await alice.ExpectAsync(Told(Game18NConstString.FamilyHeadCanNotLeave), "the head-cannot-leave notice");
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

            await alice.SendAsync(new DisbandFamilyPacket());
            await alice.ExpectAsync(Told(Game18NConstString.FamilyDisbanded), "the disbanded notice");
            await bob.ExpectSinceAsync(disbanded, WithoutAFamily(alice), "Alice without a family");
            await bob.ExpectSinceAsync(disbanded, WithoutAFamily(bob), "Bob without a family");
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

            await bob.SendAsync(new DisbandFamilyPacket());
            await bob.ExpectAsync(Told(Game18NConstString.OnlyHeadCanDisbandFamily), "the only-the-head notice");
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

            await alice.SendAsync(new GListPacket { Type = GListPacketType.MemberList });

            var list = (await alice.ExpectAsync<GmbrPacket>()).Members ?? [];
            PacketAssert.AreEqual(new List<GmbrSubPacket?>
            {
                Listed(head, family.FamilyId, FamilyAuthority.Head, true),
                Listed(member, family.FamilyId, FamilyAuthority.Member, false),
            }, list.OrderBy(m => m?.CharacterId == head.CharacterId ? 0 : 1).ToList());
        }
    }
}
