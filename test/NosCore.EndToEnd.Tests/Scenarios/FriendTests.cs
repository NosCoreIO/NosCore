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
using NosCore.Data.CommandPackets;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FriendTests
    {
        private static Func<InfoiPacket, bool> Told(Game18NConstString message) => packet => packet.Message == message;

        private static FinsPacket Request(FinsPacketType type, TestClient other) => new() { Type = type, CharacterId = other.CharacterId };

        private static Func<Dlgi2Packet, bool> Asked(TestClient by) =>
            packet => packet.YesPacket is FinsPacket { Type: FinsPacketType.Accepted } yes && yes.CharacterId == by.CharacterId
                && packet.NoPacket is FinsPacket { Type: FinsPacketType.Rejected } no && no.CharacterId == by.CharacterId
                && packet.Question == Game18NConstString.AskBecomeFriend;

        private static List<FinitSubPacket?> FriendsIn(FinitPacket finit) => finit.SubPackets ?? [];

        private static List<BlinitSubPacket?> BlacklistedIn(BlinitPacket blinit) => blinit.SubPackets ?? [];

        private static List<FinitSubPacket?> Friends(params (long Id, string Name, bool Online)[] friends) =>
            friends.Select(friend => (FinitSubPacket?)new FinitSubPacket
            {
                CharacterId = friend.Id, RelationType = CharacterRelationType.Friend, IsOnline = friend.Online, CharacterName = friend.Name
            }).ToList();

        private static List<BlinitSubPacket?> Blacklisted(params TestClient[] players) =>
            players.Select(player => (BlinitSubPacket?)new BlinitSubPacket { RelatedCharacterId = player.CharacterId, CharacterName = NameOf(player) })
                .ToList();

        private static string NameOf(TestClient client) => client.Received.OfType<CInfoPacket>().First().Name ?? string.Empty;

        private static Func<FinfoPacket, bool> Online(Player friend, bool online) =>
            packet => packet.FriendList?.SingleOrDefault() is { } entry
                && entry.CharacterId == friend.CharacterId && entry.IsConnected == online && entry.CharacterName == friend.Name;

        private static Func<SpeakPacket, bool> Whisper(TestClient from, string words) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == from.CharacterId && packet.Message?.Contains(words, StringComparison.Ordinal) == true;

        private static WhisperPacket WhisperTo(string name, string words) => new() { Message = $"{name} {words}" };

        private static bool IsRegistration(IPacket packet) => packet is FinitPacket or InfoiPacket { Message: Game18NConstString.Registered };

        private static Func<Dlgi2Packet, bool> InvitedToAGroupBy(TestClient other) =>
            packet => packet.YesPacket is PjoinPacket { RequestType: GroupRequestType.Accepted } yes && yes.CharacterId == other.CharacterId;

        private static Func<Dlgi2Packet, bool> OfferedATradeBy(TestClient other) =>
            packet => packet.YesPacket is ExchangeRequestPacket { RequestType: RequestExchangeType.List } yes && yes.VisualId == other.CharacterId;

        private static bool IsARefusal(IPacket packet) => packet is InfoiPacket or Infoi2Packet or SayiPacket;

        // The lists of friends and of the blacklisted are sent a moment after the character is in
        // the world; a scenario starts once they have arrived, so that it reads its own.
        private static async Task<TestClient> EnterAsync(Player player)
        {
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
                await client.ExpectSinceAsync<BlinitPacket>(0, null, "the blacklist sent on entering");
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }

            return client;
        }

        private static async Task<TestClient> ArriveAsync(string name, short mapId = WorldData.Town, short x = 80, short y = 116,
            Action<Character>? configure = null) =>
            await EnterAsync(await Harness.AddPlayerAsync(name, mapId, x, y, configure));

        // "from>to:relation" for every stored relation between the two, in a fixed order.
        private static async Task<string[]> RelationsAsync(long first, long second)
        {
            await using var context = Harness.Database.CreateContext();
            var rows = await context.Set<CharacterRelation>().AsNoTracking()
                .Where(r => (r.CharacterId == first && r.RelatedCharacterId == second) || (r.CharacterId == second && r.RelatedCharacterId == first))
                .ToArrayAsync();
            return rows.OrderBy(r => r.CharacterId == first ? 0 : 1).ThenBy(r => r.RelationType)
                .Select(r => $"{(r.CharacterId == first ? "first" : "second")}:{r.RelationType}").ToArray();
        }

        private static void Same(string[] expected, string[] actual, string? what = null) =>
            Assert.AreEqual(string.Join(' ', expected), string.Join(' ', actual), what);

        private static async Task BefriendAsync(TestClient asker, TestClient asked)
        {
            var request = asked.Mark();
            await asker.SendAsync(Request(FinsPacketType.Accepted, asked));
            await asked.ExpectSinceAsync(request, Asked(asker), "the friend request");
            var hers = asker.Mark();
            var his = asked.Mark();
            await asked.SendAsync(Request(FinsPacketType.Accepted, asker));
            await asker.ExpectSinceAsync<FinitPacket>(hers, p => FriendsIn(p).Any(f => f?.CharacterId == asked.CharacterId), "the new friend in the list");
            await asked.ExpectSinceAsync<FinitPacket>(his, p => FriendsIn(p).Any(f => f?.CharacterId == asker.CharacterId), "the new friend in the other list");
        }

        private static async Task BlacklistAsync(TestClient who, TestClient whom)
        {
            var listed = who.Mark();
            await who.SendAsync(new BlInsPacket { CharacterId = whom.CharacterId });
            await who.ExpectSinceAsync<BlinitPacket>(listed, p => BlacklistedIn(p).Any(b => b?.RelatedCharacterId == whom.CharacterId),
                "the blacklist with the other in it");
        }

        [TestMethod]
        public async Task ARequestIsAQuestionToTheOtherAndStoresNothing()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 30);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 30);
            var asking = alice.Mark();
            var request = bob.Mark();

            await alice.SendAsync(Request(FinsPacketType.Accepted, bob));

            var asked = await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
            PacketAssert.AreEqual(new Dlgi2Packet
            {
                YesPacket = Request(FinsPacketType.Accepted, alice),
                NoPacket = Request(FinsPacketType.Rejected, alice),
                Question = Game18NConstString.AskBecomeFriend,
                ArgumentType = 1,
                Game18NArguments = { NameOf(alice) },
            }, asked);
            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asking, IsRegistration, TimeSpan.FromSeconds(1)),
                "nothing is registered yet");
            Same([], await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task AnAcceptedRequestIsStoredBothWaysAndShownToBoth()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 32);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 32);
            var request = bob.Mark();
            await alice.SendAsync(Request(FinsPacketType.Accepted, bob));
            await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
            var hers = alice.Mark();
            var his = bob.Mark();

            await bob.SendAsync(Request(FinsPacketType.Accepted, alice));

            await alice.ExpectSinceAsync(hers, Told(Game18NConstString.Registered), "the notice to her");
            await bob.ExpectSinceAsync(his, Told(Game18NConstString.Registered), "the notice to him");
            PacketAssert.AreEqual(Friends((bob.CharacterId, NameOf(bob), true)), FriendsIn(await alice.ExpectSinceAsync<FinitPacket>(hers, null, "her list")));
            PacketAssert.AreEqual(Friends((alice.CharacterId, NameOf(alice), true)), FriendsIn(await bob.ExpectSinceAsync<FinitPacket>(his, null, "his list")));
            Same(new[] { "first:Friend", "second:Friend" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task ADeclinedRequestTellsTheOneWhoAskedAndStoresNothing()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 34);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 34);
            var request = bob.Mark();
            await alice.SendAsync(Request(FinsPacketType.Accepted, bob));
            await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
            var hers = alice.Mark();
            var his = bob.Mark();

            await bob.SendAsync(Request(FinsPacketType.Rejected, alice));

            await alice.ExpectSinceAsync(hers, Told(Game18NConstString.YouAreBlocked), "the notice that he declined");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(his, IsRegistration, TimeSpan.FromSeconds(1)), "nothing is registered");
            Assert.IsFalse(alice.Since(hers).Any(IsRegistration), "nothing is registered for her either");
            Same([], await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task AFriendIsToldWhenTheOtherLeavesAndComesBack()
        {
            var leaver = await Harness.AddPlayerAsync("Alice", WorldData.Market, 60, 36);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 36);
            int left;
            await using (var alice = await EnterAsync(leaver))
            {
                await BefriendAsync(alice, bob);
                left = bob.Mark();
            }

            await bob.ExpectSinceAsync(left, Online(leaver, false), "her going offline");
            var returned = bob.Mark();
            await using var back = await Harness.ComeBackAsync(leaver);

            await bob.ExpectSinceAsync(returned, Online(leaver, true), "her coming online");
            PacketAssert.AreEqual(Friends((bob.CharacterId, NameOf(bob), true)), FriendsIn(await back.ExpectSinceAsync<FinitPacket>(0, null, "her list on coming back")));
        }

        [TestMethod]
        public async Task AFriendWhoIsAwayIsListedAsAway()
        {
            var stayer = await Harness.AddPlayerAsync("Alice", WorldData.Market, 60, 38);
            var leaver = await Harness.AddPlayerAsync("Bob", WorldData.Market, 61, 38);
            await using (var alice = await EnterAsync(stayer))
            {
                await using (var bob = await EnterAsync(leaver))
                {
                    await BefriendAsync(alice, bob);
                }

                await alice.ExpectAsync(Online(leaver, false), "his going offline");
            }

            await using var back = await Harness.ComeBackAsync(stayer);
            PacketAssert.AreEqual(Friends((leaver.CharacterId, leaver.Name, false)), FriendsIn(await back.ExpectSinceAsync<FinitPacket>(0, null, "her list on coming back")));
        }

        [TestMethod]
        public async Task ADeletedFriendIsGoneFromBothListsAndFromWhatIsStored()
        {
            var deleter = await Harness.AddPlayerAsync("Alice", WorldData.Market, 60, 40);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 40);
            await using (var alice = await EnterAsync(deleter))
            {
                await BefriendAsync(alice, bob);
                var hers = alice.Mark();
                var his = bob.Mark();

                await alice.SendAsync(new FdelPacket { CharacterId = bob.CharacterId });

                Assert.AreEqual(0, FriendsIn(await alice.ExpectSinceAsync<FinitPacket>(hers, null, "her list")).Count);
                Assert.AreEqual(0, FriendsIn(await bob.ExpectSinceAsync<FinitPacket>(his, null, "his list")).Count);
                Same([], await RelationsAsync(alice.CharacterId, bob.CharacterId));
            }

            await using var back = await Harness.ComeBackAsync(deleter);
            Assert.AreEqual(0, FriendsIn(await back.ExpectSinceAsync<FinitPacket>(0, null, "her list on coming back")).Count);
        }

        [TestMethod]
        public async Task DeletingAFriendWhoIsAwayRemovesHerFromHisListToo()
        {
            var away = await Harness.AddPlayerAsync("Bob", WorldData.Market, 61, 42);
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 42);
            await using (var bob = await EnterAsync(away))
            {
                await BefriendAsync(alice, bob);
            }

            await alice.ExpectAsync(Online(away, false), "his going offline");
            var hers = alice.Mark();

            await alice.SendAsync(new FdelPacket { CharacterId = away.CharacterId });

            Assert.AreEqual(0, FriendsIn(await alice.ExpectSinceAsync<FinitPacket>(hers, null, "her list")).Count);
            Same([], await RelationsAsync(alice.CharacterId, away.CharacterId));
            await using var back = await Harness.ComeBackAsync(away);
            Assert.AreEqual(0, FriendsIn(await back.ExpectSinceAsync<FinitPacket>(0, null, "his list on coming back")).Count);
        }

        [TestMethod]
        public async Task ASecondRequestRightAwayIsHeldBack()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 46);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 46);
            var request = bob.Mark();
            await alice.SendAsync(Request(FinsPacketType.Accepted, bob));
            await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
            var again = alice.Mark();
            var second = bob.Mark();

            await alice.SendAsync(Request(FinsPacketType.Accepted, bob));

            await alice.ExpectSinceAsync(again, Told(Game18NConstString.CannotSendInvite), "the notice to wait");
            Assert.IsTrue(await bob.NoneSinceAsync(second, Asked(alice), TimeSpan.FromSeconds(1)), "he is not asked twice");
        }

        [TestMethod]
        public async Task AskingAFriendAgainSaysTheyAreFriendsAlready()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 48);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 48);
            await BefriendAsync(alice, bob);
            var again = alice.Mark();
            var second = bob.Mark();

            await alice.SendAsync(Request(FinsPacketType.Accepted, bob));

            await alice.ExpectSinceAsync(again, Told(Game18NConstString.RegisteredAsFriend), "the notice");
            Assert.IsTrue(await bob.NoneSinceAsync(second, Asked(alice), TimeSpan.FromSeconds(1)), "he is not asked again");
            Same(new[] { "first:Friend", "second:Friend" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task SomeoneWhoRefusesRequestsIsNotAsked()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 52);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 52, c => c.FriendRequestBlocked = true);
            var asking = alice.Mark();
            var request = bob.Mark();

            await alice.SendAsync(Request(FinsPacketType.Accepted, bob));

            await alice.ExpectSinceAsync<Infoi2Packet>(asking, p => p.Message == Game18NConstString.HAsFriendRequestBlocked && p.ArgumentType == 1
                && Equals(p.Game18NArguments[0], NameOf(bob)), "the notice that he refuses requests");
            Assert.IsTrue(await bob.NoneSinceAsync(request, Asked(alice), TimeSpan.FromSeconds(1)), "he is not asked");
        }

        [TestMethod]
        public async Task ABlacklistedPlayerIsListedAndStoredAndStillListedOnComingBack()
        {
            var lister = await Harness.AddPlayerAsync("Alice", WorldData.Market, 60, 54);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 54);
            await using (var alice = await EnterAsync(lister))
            {
                var listed = alice.Mark();

                await alice.SendAsync(new BlInsPacket { CharacterId = bob.CharacterId });

                await alice.ExpectSinceAsync(listed, Told(Game18NConstString.CharacterBlacklisted), "the notice");
                PacketAssert.AreEqual(Blacklisted(bob), BlacklistedIn(await alice.ExpectSinceAsync<BlinitPacket>(listed, null, "her blacklist")));
                Same(new[] { "first:Blocked" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
            }

            await using var back = await Harness.ComeBackAsync(lister);
            PacketAssert.AreEqual(Blacklisted(bob), BlacklistedIn(await back.ExpectSinceAsync<BlinitPacket>(0, null, "her blacklist on coming back")));
            Assert.AreEqual(0, FriendsIn(await back.ExpectSinceAsync<FinitPacket>(0, null, "her friends on coming back")).Count, "the blacklist is no friend list");
        }

        [TestMethod]
        public async Task BlacklistingSomeoneTwiceSaysSoAndStoresOneEntry()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 56);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 56);
            await BlacklistAsync(alice, bob);
            var again = alice.Mark();

            await alice.SendAsync(new BlInsPacket { CharacterId = bob.CharacterId });

            await alice.ExpectSinceAsync(again, Told(Game18NConstString.AlreadyBlacklisted), "the notice");
            Same(new[] { "first:Blocked" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task AFriendCannotBePutOnTheBlacklist()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 58);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 58);
            await BefriendAsync(alice, bob);
            var refused = alice.Mark();

            await alice.SendAsync(new BlInsPacket { CharacterId = bob.CharacterId });

            await alice.ExpectSinceAsync(refused, Told(Game18NConstString.CannotBlackListFriend), "the refusal");
            Assert.IsTrue(await alice.NoneSinceAsync<BlinitPacket>(refused, null, TimeSpan.FromSeconds(1)), "the blacklist does not change");
            Same(new[] { "first:Friend", "second:Friend" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task SomeoneTakenOffTheBlacklistIsGoneFromItForGood()
        {
            var lister = await Harness.AddPlayerAsync("Alice", WorldData.Market, 60, 60);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 60);
            await using (var alice = await EnterAsync(lister))
            {
                await BlacklistAsync(alice, bob);
                var removed = alice.Mark();

                await alice.SendAsync(new BlDelPacket { CharacterId = bob.CharacterId });

                Assert.AreEqual(0, BlacklistedIn(await alice.ExpectSinceAsync<BlinitPacket>(removed, null, "her blacklist")).Count);
                Same([], await RelationsAsync(alice.CharacterId, bob.CharacterId));
            }

            await using var back = await Harness.ComeBackAsync(lister);
            Assert.AreEqual(0, BlacklistedIn(await back.ExpectSinceAsync<BlinitPacket>(0, null, "her blacklist on coming back")).Count);
        }

        [TestMethod]
        public async Task TakingSomeoneOffTheBlacklistWhoIsNotOnItSaysSoInWords()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 62);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 62);
            var asked = alice.Mark();

            await alice.SendAsync(new BlDelPacket { CharacterId = bob.CharacterId });

            var answer = (await alice.ExpectSinceAsync<InfoPacket>(asked, null, "the answer")).Message ?? string.Empty;
            Assert.IsFalse(answer.All(c => char.IsUpper(c) || c == '_'), $"a sentence, not the name of one: {answer}");
        }

        [TestMethod]
        public async Task AWhisperIsRefusedBothWaysBetweenSomeoneAndWhoSheBlacklisted()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 64);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 64);
            await BlacklistAsync(alice, bob);
            var hers = alice.Mark();
            var his = bob.Mark();

            await bob.SendAsync(WhisperTo(NameOf(alice), "can you hear me"));

            await bob.ExpectSinceAsync(his, Told(Game18NConstString.AlreadyBlacklisted), "the refusal to him");
            Assert.IsTrue(await alice.NoneSinceAsync(hers, Whisper(bob, "can you hear me"), TimeSpan.FromSeconds(1)), "she hears nothing");
            hers = alice.Mark();
            his = bob.Mark();

            await alice.SendAsync(WhisperTo(NameOf(bob), "not a word"));

            await alice.ExpectSinceAsync(hers, Told(Game18NConstString.AlreadyBlacklisted), "the refusal to her");
            Assert.IsTrue(await bob.NoneSinceAsync(his, Whisper(alice, "not a word"), TimeSpan.FromSeconds(1)), "he hears nothing");
        }

        [TestMethod]
        public async Task AWhisperPassesAgainOnceTheBlacklistIsLifted()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 66);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 66);
            await BlacklistAsync(alice, bob);
            var his = bob.Mark();
            await bob.SendAsync(WhisperTo(NameOf(alice), "can you hear me"));
            await bob.ExpectSinceAsync(his, Told(Game18NConstString.AlreadyBlacklisted), "the refusal");
            var removed = alice.Mark();
            await alice.SendAsync(new BlDelPacket { CharacterId = bob.CharacterId });
            await alice.ExpectSinceAsync<BlinitPacket>(removed, null, "her blacklist");
            var hers = alice.Mark();

            await bob.SendAsync(WhisperTo(NameOf(alice), "and now"));

            await alice.ExpectSinceAsync(hers, Whisper(bob, "and now"), "his whisper");
        }

        [TestMethod]
        public async Task ABlacklistStillHoldsAfterComingBack()
        {
            var lister = await Harness.AddPlayerAsync("Alice", WorldData.Market, 60, 68);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 68);
            await using (var alice = await EnterAsync(lister))
            {
                await BlacklistAsync(alice, bob);
            }

            await using var back = await Harness.ComeBackAsync(lister);
            var hers = back.Mark();
            var his = bob.Mark();

            await bob.SendAsync(WhisperTo(lister.Name, "are you back"));

            await bob.ExpectSinceAsync(his, Told(Game18NConstString.AlreadyBlacklisted), "the refusal");
            Assert.IsTrue(await back.NoneSinceAsync(hers, Whisper(bob, "are you back"), TimeSpan.FromSeconds(1)), "she hears nothing");
        }

        [TestMethod]
        public async Task NoFriendRequestPassesBetweenSomeoneAndWhoSheBlacklisted()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 70);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 70);
            await BlacklistAsync(alice, bob);
            var hers = alice.Mark();
            var his = bob.Mark();

            await bob.SendAsync(Request(FinsPacketType.Accepted, alice));

            await bob.ExpectSinceAsync(his, Told(Game18NConstString.AlreadyBlacklisted), "the refusal to him");
            Assert.IsTrue(await alice.NoneSinceAsync(hers, Asked(bob), TimeSpan.FromSeconds(1)), "she is not asked");
            hers = alice.Mark();
            his = bob.Mark();

            await alice.SendAsync(Request(FinsPacketType.Accepted, bob));

            await alice.ExpectSinceAsync(hers, Told(Game18NConstString.AlreadyBlacklisted), "the refusal to her");
            Assert.IsTrue(await bob.NoneSinceAsync(his, Asked(alice), TimeSpan.FromSeconds(1)), "he is not asked");
            Same(new[] { "first:Blocked" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task NoGroupInvitationPassesBetweenSomeoneAndWhoSheBlacklisted()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 72);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 72);
            await BlacklistAsync(alice, bob);
            var hers = alice.Mark();
            var his = bob.Mark();

            await bob.SendAsync(new PjoinPacket { RequestType = GroupRequestType.Invited, CharacterId = alice.CharacterId });

            await bob.ExpectSinceAsync(his, Told(Game18NConstString.AlreadyBlacklisted), "the refusal to him");
            Assert.IsTrue(await alice.NoneSinceAsync(hers, InvitedToAGroupBy(bob), TimeSpan.FromSeconds(1)),
                "she is not asked");
            hers = alice.Mark();
            his = bob.Mark();

            await alice.SendAsync(new PjoinPacket { RequestType = GroupRequestType.Invited, CharacterId = bob.CharacterId });

            await alice.ExpectSinceAsync(hers, Told(Game18NConstString.AlreadyBlacklisted), "the refusal to her");
            Assert.IsTrue(await bob.NoneSinceAsync(his, InvitedToAGroupBy(alice), TimeSpan.FromSeconds(1)),
                "he is not asked");
        }

        [TestMethod]
        public async Task NoTradeIsOfferedBetweenSomeoneAndWhoSheBlacklisted()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 74);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 74);
            await BlacklistAsync(alice, bob);
            var hers = alice.Mark();
            var his = bob.Mark();

            await bob.SendAsync(new ExchangeRequestPacket { RequestType = RequestExchangeType.Requested, VisualId = alice.CharacterId });

            await bob.ExpectSinceAsync<IPacket>(his, IsARefusal, "the refusal to him");
            Assert.IsTrue(await alice.NoneSinceAsync(hers, OfferedATradeBy(bob), TimeSpan.FromSeconds(1)),
                "she is not asked");
            hers = alice.Mark();
            his = bob.Mark();

            await alice.SendAsync(new ExchangeRequestPacket { RequestType = RequestExchangeType.Requested, VisualId = bob.CharacterId });

            await alice.ExpectSinceAsync<IPacket>(hers, IsARefusal, "the refusal to her");
            Assert.IsTrue(await bob.NoneSinceAsync(his, OfferedATradeBy(alice), TimeSpan.FromSeconds(1)),
                "he is not asked");
        }

        [TestMethod]
        public async Task NoFamilyInvitationReachesSomeoneWhoBlacklistedTheHead()
        {
            var head = await Harness.AddPlayerAsync("Alice", WorldData.Market, 60, 76);
            await Harness.AddFamilyAsync(head);
            await using var alice = await EnterAsync(head);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 76);
            await BlacklistAsync(bob, alice);
            var hers = alice.Mark();
            var his = bob.Mark();

            await alice.SendAsync(new FamilyInvitePacket { CharacterName = NameOf(bob) });

            await alice.ExpectSinceAsync(hers, Told(Game18NConstString.AlreadyBlacklisted), "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync<Dlgi2Packet>(his, null, TimeSpan.FromSeconds(1)), "he is not asked");
        }

        [TestMethod]
        public async Task SomeoneCanBeBlacklistedByName()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 78);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 78);
            var listed = alice.Mark();

            await alice.SendAsync(new BlPacket { CharacterName = NameOf(bob) });

            PacketAssert.AreEqual(Blacklisted(bob), BlacklistedIn(await alice.ExpectSinceAsync<BlinitPacket>(listed, null, "her blacklist")));
            Same(new[] { "first:Blocked" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task SomeoneCanBeAskedToBeAFriendByName()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 80);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 80);
            var request = bob.Mark();

            await alice.SendAsync(new FlCommandPacket { CharacterName = NameOf(bob) });

            await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
        }
    }
}
