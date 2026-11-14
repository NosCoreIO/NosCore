//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FriendTests
    {
        private const int Player = 1;
        private const int Accept = 1;
        private const int Decline = 2;
        private const int Declined = 238;
        private const int Registered = 239;
        private const int RefusesRequests = 242;
        private const int WantsToBeAFriend = 243;
        private const int AlreadyAFriend = 937;
        private const int Blacklisted = 1764;
        private const int AFriendCannotBeBlacklisted = 1769;
        private const int NowBlacklisted = 1770;
        private const int WaitBeforeInvitingAgain = 2359;


        private static Func<string, bool> Told(int message) =>
            packet => packet.Split(' ') is ["infoi", var id, ..] && id == message.ToString(CultureInfo.InvariantCulture);

        private static Func<string, bool> Asked(TestClient by) =>
            packet => packet.StartsWith($"dlgi2 #fins^{Accept}^{by.CharacterId} #fins^{Decline}^{by.CharacterId} {WantsToBeAFriend} ", StringComparison.Ordinal);

        private static Func<string, bool> Friends => packet => TestClient.HeaderOf(packet) == "finit";

        private static Func<string, bool> Blacklist => packet => TestClient.HeaderOf(packet) == "blinit";

        // finit <id|relation|online|name>...
        private static string[] FriendsIn(string finit) => finit.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

        // blinit <id|name>...
        private static string[] BlacklistedIn(string blinit) => blinit.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

        private static string NameOf(TestClient client) => client.Received.First(p => TestClient.HeaderOf(p) == "c_info").Split(' ')[1];

        private static Func<string, bool> Whisper(TestClient from, string words) =>
            packet => packet.StartsWith($"spk {Player} {from.CharacterId} ", StringComparison.Ordinal) && packet.Contains(words, StringComparison.Ordinal);

        // The lists of friends and of the blacklisted are sent a moment after the character is in
        // the world; a scenario starts once they have arrived, so that it reads its own.
        private static async Task<TestClient> EnterAsync(Player player)
        {
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
                await client.ExpectSinceAsync(0, Blacklist, "the blacklist sent on entering");
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
            await asker.SendAsync($"fins {Accept} {asked.CharacterId}");
            await asked.ExpectSinceAsync(request, Asked(asker), "the friend request");
            var hers = asker.Mark();
            var his = asked.Mark();
            await asked.SendAsync($"fins {Accept} {asker.CharacterId}");
            await asker.ExpectSinceAsync(hers, p => Friends(p) && FriendsIn(p).Any(f => f.StartsWith($"{asked.CharacterId}|", StringComparison.Ordinal)),
                "the new friend in the list");
            await asked.ExpectSinceAsync(his, p => Friends(p) && FriendsIn(p).Any(f => f.StartsWith($"{asker.CharacterId}|", StringComparison.Ordinal)),
                "the new friend in the other list");
        }

        private static async Task BlacklistAsync(TestClient who, TestClient whom)
        {
            var listed = who.Mark();
            await who.SendAsync($"blins {whom.CharacterId}");
            await who.ExpectSinceAsync(listed, p => Blacklist(p) && BlacklistedIn(p).Any(b => b.StartsWith($"{whom.CharacterId}|", StringComparison.Ordinal)),
                "the blacklist with the other in it");
        }

        [TestMethod]
        public async Task ARequestIsAQuestionToTheOtherAndStoresNothing()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 30);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 30);
            var asking = alice.Mark();
            var request = bob.Mark();

            await alice.SendAsync($"fins {Accept} {bob.CharacterId}");

            var asked = await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
            Assert.AreEqual($"dlgi2 #fins^{Accept}^{alice.CharacterId} #fins^{Decline}^{alice.CharacterId} {WantsToBeAFriend} 1 {NameOf(alice)} 0 0 0", asked);
            Assert.IsTrue(await alice.NoneSinceAsync(asking, p => Friends(p) || Told(Registered)(p), TimeSpan.FromSeconds(1)),
                "nothing is registered yet");
            Same([], await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task AnAcceptedRequestIsStoredBothWaysAndShownToBoth()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 32);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 32);
            var request = bob.Mark();
            await alice.SendAsync($"fins {Accept} {bob.CharacterId}");
            await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
            var hers = alice.Mark();
            var his = bob.Mark();

            await bob.SendAsync($"fins {Accept} {alice.CharacterId}");

            await alice.ExpectSinceAsync(hers, Told(Registered), "the notice to her");
            await bob.ExpectSinceAsync(his, Told(Registered), "the notice to him");
            Same(new[] { $"{bob.CharacterId}|0|1|{NameOf(bob)}" }, FriendsIn(await alice.ExpectSinceAsync(hers, Friends, "her list")));
            Same(new[] { $"{alice.CharacterId}|0|1|{NameOf(alice)}" }, FriendsIn(await bob.ExpectSinceAsync(his, Friends, "his list")));
            Same(new[] { "first:Friend", "second:Friend" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task ADeclinedRequestTellsTheOneWhoAskedAndStoresNothing()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 34);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 34);
            var request = bob.Mark();
            await alice.SendAsync($"fins {Accept} {bob.CharacterId}");
            await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
            var hers = alice.Mark();
            var his = bob.Mark();

            await bob.SendAsync($"fins {Decline} {alice.CharacterId}");

            await alice.ExpectSinceAsync(hers, Told(Declined), "the notice that he declined");
            Assert.IsTrue(await bob.NoneSinceAsync(his, p => Friends(p) || Told(Registered)(p), TimeSpan.FromSeconds(1)), "nothing is registered");
            Assert.IsFalse(alice.Since(hers).Any(p => Friends(p) || Told(Registered)(p)), "nothing is registered for her either");
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

            // finfo <id.online.name>
            await bob.ExpectSinceAsync(left, p => p == $"finfo {leaver.CharacterId}.0.{leaver.Name}", "her going offline");
            var returned = bob.Mark();
            await using var back = await Harness.ComeBackAsync(leaver);

            await bob.ExpectSinceAsync(returned, p => p == $"finfo {leaver.CharacterId}.1.{leaver.Name}", "her coming online");
            Same(new[] { $"{bob.CharacterId}|0|1|{NameOf(bob)}" }, FriendsIn(await back.ExpectSinceAsync(0, Friends, "her list on coming back")));
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

                await alice.ExpectAsync(p => p == $"finfo {leaver.CharacterId}.0.{leaver.Name}", "his going offline");
            }

            await using var back = await Harness.ComeBackAsync(stayer);
            Same(new[] { $"{leaver.CharacterId}|0|0|{leaver.Name}" }, FriendsIn(await back.ExpectSinceAsync(0, Friends, "her list on coming back")));
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

                await alice.SendAsync($"fdel {bob.CharacterId}");

                Same([], FriendsIn(await alice.ExpectSinceAsync(hers, Friends, "her list")));
                Same([], FriendsIn(await bob.ExpectSinceAsync(his, Friends, "his list")));
                Same([], await RelationsAsync(alice.CharacterId, bob.CharacterId));
            }

            await using var back = await Harness.ComeBackAsync(deleter);
            Same([], FriendsIn(await back.ExpectSinceAsync(0, Friends, "her list on coming back")));
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

            await alice.ExpectAsync(p => p == $"finfo {away.CharacterId}.0.{away.Name}", "his going offline");
            var hers = alice.Mark();

            await alice.SendAsync($"fdel {away.CharacterId}");

            Same([], FriendsIn(await alice.ExpectSinceAsync(hers, Friends, "her list")));
            Same([], await RelationsAsync(alice.CharacterId, away.CharacterId));
            await using var back = await Harness.ComeBackAsync(away);
            Same([], FriendsIn(await back.ExpectSinceAsync(0, Friends, "his list on coming back")));
        }

        [TestMethod]
        public async Task ASecondRequestRightAwayIsHeldBack()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 46);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 46);
            var request = bob.Mark();
            await alice.SendAsync($"fins {Accept} {bob.CharacterId}");
            await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
            var again = alice.Mark();
            var second = bob.Mark();

            await alice.SendAsync($"fins {Accept} {bob.CharacterId}");

            await alice.ExpectSinceAsync(again, Told(WaitBeforeInvitingAgain), "the notice to wait");
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

            await alice.SendAsync($"fins {Accept} {bob.CharacterId}");

            await alice.ExpectSinceAsync(again, Told(AlreadyAFriend), "the notice");
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

            await alice.SendAsync($"fins {Accept} {bob.CharacterId}");

            await alice.ExpectSinceAsync(asking, p => p.StartsWith($"infoi2 {RefusesRequests} 1 {NameOf(bob)}", StringComparison.Ordinal), "the notice that he refuses requests");
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

                await alice.SendAsync($"blins {bob.CharacterId}");

                await alice.ExpectSinceAsync(listed, Told(NowBlacklisted), "the notice");
                Same(new[] { $"{bob.CharacterId}|{NameOf(bob)}" }, BlacklistedIn(await alice.ExpectSinceAsync(listed, Blacklist, "her blacklist")));
                Same(new[] { "first:Blocked" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
            }

            await using var back = await Harness.ComeBackAsync(lister);
            Same(new[] { $"{bob.CharacterId}|{NameOf(bob)}" }, BlacklistedIn(await back.ExpectSinceAsync(0, Blacklist, "her blacklist on coming back")));
            Same([], FriendsIn(await back.ExpectSinceAsync(0, Friends, "her friends on coming back")), "the blacklist is no friend list");
        }

        [TestMethod]
        public async Task BlacklistingSomeoneTwiceSaysSoAndStoresOneEntry()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 56);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 56);
            await BlacklistAsync(alice, bob);
            var again = alice.Mark();

            await alice.SendAsync($"blins {bob.CharacterId}");

            await alice.ExpectSinceAsync(again, Told(Blacklisted), "the notice");
            Same(new[] { "first:Blocked" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task AFriendCannotBePutOnTheBlacklist()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 58);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 58);
            await BefriendAsync(alice, bob);
            var refused = alice.Mark();

            await alice.SendAsync($"blins {bob.CharacterId}");

            await alice.ExpectSinceAsync(refused, Told(AFriendCannotBeBlacklisted), "the refusal");
            Assert.IsTrue(await alice.NoneSinceAsync(refused, Blacklist, TimeSpan.FromSeconds(1)), "the blacklist does not change");
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

                await alice.SendAsync($"bldel {bob.CharacterId}");

                Same([], BlacklistedIn(await alice.ExpectSinceAsync(removed, Blacklist, "her blacklist")));
                Same([], await RelationsAsync(alice.CharacterId, bob.CharacterId));
            }

            await using var back = await Harness.ComeBackAsync(lister);
            Same([], BlacklistedIn(await back.ExpectSinceAsync(0, Blacklist, "her blacklist on coming back")));
        }

        [TestMethod]
        public async Task TakingSomeoneOffTheBlacklistWhoIsNotOnItSaysSoInWords()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 62);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 62);
            var asked = alice.Mark();

            await alice.SendAsync($"bldel {bob.CharacterId}");

            var answer = await alice.ExpectSinceAsync(asked, p => TestClient.HeaderOf(p) == "info", "the answer");
            Assert.IsFalse(answer.Split(' ', 2)[1].All(c => char.IsUpper(c) || c == '_'), $"a sentence, not the name of one: {answer}");
        }

        [TestMethod]
        public async Task AWhisperIsRefusedBothWaysBetweenSomeoneAndWhoSheBlacklisted()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 64);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 64);
            await BlacklistAsync(alice, bob);
            var hers = alice.Mark();
            var his = bob.Mark();

            await bob.SendAsync($"/{NameOf(alice)} can you hear me");

            await bob.ExpectSinceAsync(his, Told(Blacklisted), "the refusal to him");
            Assert.IsTrue(await alice.NoneSinceAsync(hers, Whisper(bob, "can you hear me"), TimeSpan.FromSeconds(1)), "she hears nothing");
            hers = alice.Mark();
            his = bob.Mark();

            await alice.SendAsync($"/{NameOf(bob)} not a word");

            await alice.ExpectSinceAsync(hers, Told(Blacklisted), "the refusal to her");
            Assert.IsTrue(await bob.NoneSinceAsync(his, Whisper(alice, "not a word"), TimeSpan.FromSeconds(1)), "he hears nothing");
        }

        [TestMethod]
        public async Task AWhisperPassesAgainOnceTheBlacklistIsLifted()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 66);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 66);
            await BlacklistAsync(alice, bob);
            var his = bob.Mark();
            await bob.SendAsync($"/{NameOf(alice)} can you hear me");
            await bob.ExpectSinceAsync(his, Told(Blacklisted), "the refusal");
            var removed = alice.Mark();
            await alice.SendAsync($"bldel {bob.CharacterId}");
            await alice.ExpectSinceAsync(removed, Blacklist, "her blacklist");
            var hers = alice.Mark();

            await bob.SendAsync($"/{NameOf(alice)} and now");

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

            await bob.SendAsync($"/{lister.Name} are you back");

            await bob.ExpectSinceAsync(his, Told(Blacklisted), "the refusal");
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

            await bob.SendAsync($"fins {Accept} {alice.CharacterId}");

            await bob.ExpectSinceAsync(his, Told(Blacklisted), "the refusal to him");
            Assert.IsTrue(await alice.NoneSinceAsync(hers, Asked(bob), TimeSpan.FromSeconds(1)), "she is not asked");
            hers = alice.Mark();
            his = bob.Mark();

            await alice.SendAsync($"fins {Accept} {bob.CharacterId}");

            await alice.ExpectSinceAsync(hers, Told(Blacklisted), "the refusal to her");
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

            await bob.SendAsync($"pjoin 1 {alice.CharacterId}");

            await bob.ExpectSinceAsync(his, Told(Blacklisted), "the refusal to him");
            Assert.IsTrue(await alice.NoneSinceAsync(hers, p => p.Contains($"#pjoin^3^{bob.CharacterId}", StringComparison.Ordinal), TimeSpan.FromSeconds(1)),
                "she is not asked");
            hers = alice.Mark();
            his = bob.Mark();

            await alice.SendAsync($"pjoin 1 {bob.CharacterId}");

            await alice.ExpectSinceAsync(hers, Told(Blacklisted), "the refusal to her");
            Assert.IsTrue(await bob.NoneSinceAsync(his, p => p.Contains($"#pjoin^3^{alice.CharacterId}", StringComparison.Ordinal), TimeSpan.FromSeconds(1)),
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

            await bob.SendAsync($"req_exc 1 {alice.CharacterId}");

            await bob.ExpectSinceAsync(his, p => TestClient.HeaderOf(p) is "infoi" or "infoi2" or "sayi", "the refusal to him");
            Assert.IsTrue(await alice.NoneSinceAsync(hers, p => p.StartsWith($"dlgi2 #req_exc^2^{bob.CharacterId} ", StringComparison.Ordinal), TimeSpan.FromSeconds(1)),
                "she is not asked");
            hers = alice.Mark();
            his = bob.Mark();

            await alice.SendAsync($"req_exc 1 {bob.CharacterId}");

            await alice.ExpectSinceAsync(hers, p => TestClient.HeaderOf(p) is "infoi" or "infoi2" or "sayi", "the refusal to her");
            Assert.IsTrue(await bob.NoneSinceAsync(his, p => p.StartsWith($"dlgi2 #req_exc^2^{alice.CharacterId} ", StringComparison.Ordinal), TimeSpan.FromSeconds(1)),
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

            await alice.SendAsync($"%Familyinvite {NameOf(bob)}");

            await alice.ExpectSinceAsync(hers, Told(Blacklisted), "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync(his, p => TestClient.HeaderOf(p) == "dlgi2", TimeSpan.FromSeconds(1)), "he is not asked");
        }

        [TestMethod]
        public async Task SomeoneCanBeBlacklistedByName()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 78);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 78);
            var listed = alice.Mark();

            await alice.SendAsync($"$bl {NameOf(bob)}");

            Same(new[] { $"{bob.CharacterId}|{NameOf(bob)}" }, BlacklistedIn(await alice.ExpectSinceAsync(listed, Blacklist, "her blacklist")));
            Same(new[] { "first:Blocked" }, await RelationsAsync(alice.CharacterId, bob.CharacterId));
        }

        [TestMethod]
        public async Task SomeoneCanBeAskedToBeAFriendByName()
        {
            await using var alice = await ArriveAsync("Alice", WorldData.Market, 60, 80);
            await using var bob = await ArriveAsync("Bob", WorldData.Market, 61, 80);
            var request = bob.Mark();

            await alice.SendAsync($"$fl {NameOf(bob)}");

            await bob.ExpectSinceAsync(request, Asked(alice), "the friend request");
        }
    }
}
