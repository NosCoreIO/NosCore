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
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Algorithm.FamilyUpgradeService;
using NosCore.Data.Enumerations.Family;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FamilyExperienceTests
    {
        private const int PlayerType = 1;
        private const int Npc = 2;
        private const int Monster = 3;
        private const int Head = 0;
        private const int Member = 3;

        // 1965 is the client's "Family level increased to %d!".
        private const int FamilyLevelIncreased = 1965;

        // 1975 is the client's "Your family level is insufficient." and 2386 its "Purchased: %s".
        private const int FamilyLevelInsufficient = 1975;
        private const int Purchased = 2386;
        private const int RewardedLevel = 30;
        private const int RewardedLevelExperience = 600;

        private static readonly ExperienceService Levels = new();
        private static readonly FamilyExperienceService FamilyLevels = new();

        // One kill away from the level after this one.
        private static Action<Character> AboutToLeave(byte level) => c =>
        {
            c.Level = level;
            c.LevelXp = Levels.GetExperience(level) - 1;
        };

        // ginfo <family> <head> <head gender> <level> <xp> <xp to level> <members> <capacity> <own authority> ...
        private static Func<string, bool> FamilyInfo(string family, int level, long experience, int authority) =>
            packet => packet.Split(' ') is ["ginfo", var name, _, _, var familyLevel, var xp, var bar, _, _, var own, ..]
                && name == family && familyLevel == level.ToString() && xp == experience.ToString()
                && bar == FamilyLevels.GetFamilyExperience((byte)level).ToString() && own == authority.ToString();

        private static bool AnyFamilyInfo(string packet) => TestClient.HeaderOf(packet) == "ginfo";

        // gexp <member>|<what the member earned for the family> ...
        private static Func<string, bool> Contribution(long characterId, long experience) =>
            packet => TestClient.HeaderOf(packet) == "gexp" && packet.Split(' ').Contains($"{characterId}|{experience}");

        private static bool HistoryChanged(string packet) => packet == "fhis_stc";

        private static Func<string, bool> FamilyLevelReached(int level) =>
            packet => packet.StartsWith($"msgi 0 {FamilyLevelIncreased} 4 {level} ", StringComparison.Ordinal);

        // n_inv <npc type> <npc> 0 100 <pocket>.<slot>.<vnum>.<amount>.<price> ...
        private static bool Shelf(string packet) =>
            packet.StartsWith($"n_inv {Npc} {WorldData.FamilyManager} ", StringComparison.Ordinal)
            && packet.Contains($".{WorldData.FamilyWarehouse}.", StringComparison.Ordinal);

        private static bool PreyOnTheMap(string packet) =>
            packet.StartsWith($"in {Monster} {WorldData.PreyVNum} {WorldData.Prey} ", StringComparison.Ordinal);

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

        // Kills the prey once it is on the map, however many hits that takes: its experience is what
        // takes the killer to the next level.
        private static async Task LevelUpAsync(TestClient killer)
        {
            await killer.ExpectSinceAsync(0, PreyOnTheMap, "the prey on the map");
            var tried = Stopwatch.StartNew();
            while (true)
            {
                var cast = killer.Mark();
                await killer.SendAsync($"u_s 0 {Monster} {WorldData.Prey}");
                var hit = (await killer.ExpectSinceAsync(cast,
                    p => p.StartsWith($"su {PlayerType} {killer.CharacterId} {Monster} {WorldData.Prey} ", StringComparison.Ordinal), "the hit")).Split(' ');
                if (hit[11] == "0")
                {
                    await killer.ExpectSinceAsync(cast, p => p == $"levelup {killer.CharacterId}", "the level-up");
                    return;
                }

                Assert.IsTrue(tried.Elapsed < TimeSpan.FromSeconds(30), "no hit landed in thirty seconds");
                await killer.ExpectSinceAsync(cast, p => p == "sr 0", "the skill ready again");
            }
        }

        private static async Task<Family> StoredFamilyAsync(long familyId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<Family>().AsNoTracking().SingleAsync(f => f.FamilyId == familyId);
        }

        private static async Task<FamilyCharacter> StoredMembershipAsync(long characterId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<FamilyCharacter>().AsNoTracking().SingleAsync(m => m.CharacterId == characterId);
        }

        // The family's history rows, oldest first, each as its type and the arguments it carries.
        private static async Task<string[]> StoredHistoryAsync(long familyId)
        {
            await using var context = Harness.Database.CreateContext();
            var rows = await context.Set<FamilyLog>().AsNoTracking().Where(l => l.FamilyId == familyId)
                .OrderBy(l => l.Timestamp).ThenBy(l => l.FamilyLogId).ToListAsync();
            return rows.Select(l => string.Join('|', new[] { ((int)l.FamilyLogType).ToString(), l.FirstArgument, l.SecondArgument }
                .Where(s => s != null))).ToArray();
        }

        private static string Line(FamilyLogType type, params object[] arguments) =>
            string.Join('|', arguments.Prepend((int)type));

        [TestMethod]
        public async Task AMemberReachingLevelThirtyEarnsTheFamilyTwentyTimesTheLevel()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", WorldData.Meadow, 30, 71, AboutToLeave(RewardedLevel - 1));
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            var earned = alice.Mark();
            var levelled = bob.Mark();

            await LevelUpAsync(bob);

            await bob.ExpectSinceAsync(levelled, FamilyInfo(family.Name, 1, RewardedLevelExperience, Member), "the family with what Bob earned, for Bob");
            await alice.ExpectSinceAsync(earned, FamilyInfo(family.Name, 1, RewardedLevelExperience, Head), "the family with what Bob earned, for Alice");
            await alice.ExpectSinceAsync(earned, Contribution(member.CharacterId, RewardedLevelExperience), "what Bob earned next to his name");
            await alice.ExpectSinceAsync(earned, HistoryChanged, "the history to read again");
            Assert.IsFalse(alice.Since(earned).Any(p => p.StartsWith($"msgi 0 {FamilyLevelIncreased} ", StringComparison.Ordinal)),
                "the family has not levelled up");

            var stored = await StoredFamilyAsync(family.FamilyId);
            Assert.AreEqual(1, stored.FamilyLevel);
            Assert.AreEqual(RewardedLevelExperience, stored.FamilyExperience);
            Assert.AreEqual(RewardedLevelExperience, (await StoredMembershipAsync(member.CharacterId)).Experience);
            Assert.AreEqual(0, (await StoredMembershipAsync(head.CharacterId)).Experience);
            CollectionAssert.AreEqual(new[]
            {
                Line(FamilyLogType.LevelUp, member.Name, RewardedLevel),
                Line(FamilyLogType.FamilyXp, member.Name, RewardedLevelExperience)
            }, await StoredHistoryAsync(family.FamilyId));

            var asked = alice.Mark();
            await alice.SendAsync("fhis_cts");

            // ghis 0 <type>|<arguments>|<hours ago> ..., the newest first.
            Assert.AreEqual($"ghis 0 4|{member.Name}|{RewardedLevelExperience}|0 6|{member.Name}|{RewardedLevel}|0",
                await alice.ExpectSinceAsync(asked, p => TestClient.HeaderOf(p) == "ghis", "the history"));
        }

        [TestMethod]
        public async Task AFamilyWhoseBarFillsLevelsUpAndEveryMemberIsTold()
        {
            var bar = (int)FamilyLevels.GetFamilyExperience(1);
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", WorldData.Meadow, 30, 71, AboutToLeave(RewardedLevel - 1));
            var family = await Harness.AddFamilyAsync(f => f.FamilyExperience = bar - 200, head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            var earned = alice.Mark();
            var levelled = bob.Mark();

            await LevelUpAsync(bob);

            var left = RewardedLevelExperience - 200;
            await bob.ExpectSinceAsync(levelled, FamilyLevelReached(2), "the family level, for Bob");
            await bob.ExpectSinceAsync(levelled, FamilyInfo(family.Name, 2, left, Member), "the family at level 2, for Bob");
            await alice.ExpectSinceAsync(earned, FamilyLevelReached(2), "the family level, for Alice");
            await alice.ExpectSinceAsync(earned, FamilyInfo(family.Name, 2, left, Head), "the family at level 2, for Alice");

            var stored = await StoredFamilyAsync(family.FamilyId);
            Assert.AreEqual(2, stored.FamilyLevel);
            Assert.AreEqual(left, stored.FamilyExperience);
            CollectionAssert.AreEqual(new[]
            {
                Line(FamilyLogType.LevelUp, member.Name, RewardedLevel),
                Line(FamilyLogType.FamilyXp, member.Name, RewardedLevelExperience),
                Line(FamilyLogType.FamilyLevelUp, 2)
            }, await StoredHistoryAsync(family.FamilyId));

            var asked = alice.Mark();
            await alice.SendAsync("fhis_cts");

            Assert.AreEqual($"ghis 0 5|2|0 4|{member.Name}|{RewardedLevelExperience}|0 6|{member.Name}|{RewardedLevel}|0",
                await alice.ExpectSinceAsync(asked, p => TestClient.HeaderOf(p) == "ghis", "the history"));
        }

        [TestMethod]
        public async Task AMemberOnAnotherChannelIsShownTheGainAndTheNewLevel()
        {
            var bar = (int)FamilyLevels.GetFamilyExperience(1);
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", WorldData.Meadow, 30, 71, AboutToLeave(RewardedLevel - 1));
            var family = await Harness.AddFamilyAsync(f => f.FamilyExperience = bar - 200, head, member);
            await using var alice = await EnterAsync(head, channel: 1);
            await using var bob = await EnterAsync(member);
            var earned = alice.Mark();

            await LevelUpAsync(bob);

            await alice.ExpectSinceAsync(earned, FamilyLevelReached(2), "the family level");
            await alice.ExpectSinceAsync(earned, FamilyInfo(family.Name, 2, RewardedLevelExperience - 200, Head), "the family at level 2");
            await alice.ExpectSinceAsync(earned, Contribution(member.CharacterId, RewardedLevelExperience), "what Bob earned next to his name");
            await alice.ExpectSinceAsync(earned, HistoryChanged, "the history to read again");
        }

        [TestMethod]
        public async Task ALevelThatIsNotATenthEarnsTheFamilyNothing()
        {
            var head = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            var member = await Harness.AddPlayerAsync("Bob", WorldData.Meadow, 30, 71, AboutToLeave(RewardedLevel));
            var family = await Harness.AddFamilyAsync(head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            var earned = alice.Mark();

            await LevelUpAsync(bob);

            Assert.IsTrue(await alice.NoneSinceAsync(earned, p => AnyFamilyInfo(p) || HistoryChanged(p), TimeSpan.FromSeconds(1)),
                "the family window and its history stay as they were");
            Assert.AreEqual(0, (await StoredFamilyAsync(family.FamilyId)).FamilyExperience);
            Assert.AreEqual(0, (await StoredMembershipAsync(member.CharacterId)).Experience);
            Assert.AreEqual(0, (await StoredHistoryAsync(family.FamilyId)).Length);
        }

        [TestMethod]
        public async Task WhatTheFamilyMayBuyFollowsItsNewLevel()
        {
            var bar = (int)FamilyLevels.GetFamilyExperience(1);
            var head = await Harness.AddPlayerAsync("Alice", x: 76, y: 109, configure: c => c.Gold = WorldData.FamilyWarehousePrice);
            var member = await Harness.AddPlayerAsync("Bob", WorldData.Meadow, 30, 71, AboutToLeave(RewardedLevel - 1));
            var family = await Harness.AddFamilyAsync(f => f.FamilyExperience = bar - 200, head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            var refused = alice.Mark();

            await alice.SendAsync($"npc_req {Npc} {WorldData.FamilyManager}");
            await alice.ExpectSinceAsync(refused, Shelf, "what the family manager sells");
            await alice.SendAsync($"buy {Npc} {WorldData.FamilyManager} 0 1");

            await alice.ExpectSinceAsync(refused, p => p.StartsWith($"msgi 0 {FamilyLevelInsufficient} ", StringComparison.Ordinal),
                "the family level refusal");
            Assert.AreEqual(0, (await StoredFamilyAsync(family.FamilyId)).WarehouseSize);
            var levelled = alice.Mark();

            await LevelUpAsync(bob);
            await alice.ExpectSinceAsync(levelled, FamilyLevelReached(WorldData.FamilyWarehouseLevel), "the family level");
            var bought = alice.Mark();
            await alice.SendAsync($"npc_req {Npc} {WorldData.FamilyManager}");
            await alice.ExpectSinceAsync(bought, Shelf, "what the family manager sells");
            await alice.SendAsync($"buy {Npc} {WorldData.FamilyManager} 0 1");

            await alice.ExpectSinceAsync(bought, p => p.StartsWith($"msgi2 0 {Purchased} 2 {WorldData.FamilyWarehouse} ", StringComparison.Ordinal),
                "the purchase");
            await alice.ExpectSinceAsync(bought, p => p == "gold 0 0", "the price paid");
            Assert.AreEqual(new FamilyUpgradeService().GetWarehouseSize(1), (await StoredFamilyAsync(family.FamilyId)).WarehouseSize);
        }
    }
}
