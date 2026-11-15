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
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Algorithm.FamilyUpgradeService;
using NosCore.Data.Enumerations.Family;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Authority = NosCore.Packets.Enumerations.FamilyAuthority;
using Game18NConstString = NosCore.Packets.Enumerations.Game18NConstString;
using MessageType = NosCore.Packets.Enumerations.MessageType;
using PacketLogType = NosCore.Packets.Enumerations.FamilyLogType;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FamilyExperienceTests
    {
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

        private static Func<GInfoPacket, bool> FamilyInfo(string family, int level, long experience, Authority authority) =>
            packet => packet.FamilyName == family && packet.FamilyLevel == level && packet.FamilyXp == experience
                && packet.MaxFamilyXp == FamilyLevels.GetFamilyExperience((byte)level) && packet.CharacterFamilyAuthority == authority;

        private static Func<GexpPacket, bool> Contribution(long characterId, long experience) =>
            packet => packet.Entries?.Any(entry => entry?.CharacterId == characterId && entry.Experience == experience) == true;

        private static Func<MsgiPacket, bool> FamilyLevelReached(int level) =>
            packet => packet.Type == MessageType.Default && packet.Message == Game18NConstString.FamilyLevelIncreased
                && packet.ArgumentType == 4 && Equals(packet.Game18NArguments[0], (long)level);

        private static Func<NInvPacket, bool> Shelf =>
            packet => packet.VisualType == VisualType.Npc && packet.VisualId == WorldData.FamilyManager
                && packet.Items?.Any(item => item?.VNum == WorldData.FamilyWarehouse) == true;

        private static Func<InPacket, bool> PreyOnTheMap =>
            packet => packet.VisualType == VisualType.Monster && packet.VisualId == WorldData.Prey;

        private static readonly RequestNpcPacket TalkToTheManager = new() { Type = VisualType.Npc, TargetId = WorldData.FamilyManager };

        private static readonly BuyPacket BuyTheWarehouse = new() { VisualType = VisualType.Npc, VisualId = WorldData.FamilyManager, Slot = 0, Amount = 1 };

        private static GhisSubPacket Entry(PacketLogType type, params object[] arguments) => new()
        {
            Type = type,
            FirstArgument = arguments.ElementAtOrDefault(0)?.ToString(),
            SecondArgument = arguments.ElementAtOrDefault(1)?.ToString(),
            HoursAgo = 0,
        };

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
            var (_, cast) = await Steps.KillAsync(killer, WorldData.Prey);
            await killer.ExpectSinceAsync<LevelUpPacket>(cast, p => p.CharacterId == killer.CharacterId, "the level-up");
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

            await bob.ExpectSinceAsync(levelled, FamilyInfo(family.Name, 1, RewardedLevelExperience, Authority.Member), "the family with what Bob earned, for Bob");
            await alice.ExpectSinceAsync(earned, FamilyInfo(family.Name, 1, RewardedLevelExperience, Authority.Head), "the family with what Bob earned, for Alice");
            await alice.ExpectSinceAsync(earned, Contribution(member.CharacterId, RewardedLevelExperience), "what Bob earned next to his name");
            await alice.ExpectSinceAsync<FhisStcPacket>(earned, null, "the history to read again");
            Assert.IsFalse(alice.Since<MsgiPacket>(earned).Any(p => p.Message == Game18NConstString.FamilyLevelIncreased),
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
            await alice.SendAsync(new FhistCtsPacket());

            // The newest first.
            PacketAssert.AreEqual(new GhisPacket
            {
                Unknown = 0,
                Entries =
                [
                    Entry(PacketLogType.FamilyXp, member.Name, RewardedLevelExperience),
                    Entry(PacketLogType.LevelUp, member.Name, RewardedLevel),
                ],
            }, await alice.ExpectSinceAsync<GhisPacket>(asked, null, "the history"));
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
            await bob.ExpectSinceAsync(levelled, FamilyInfo(family.Name, 2, left, Authority.Member), "the family at level 2, for Bob");
            await alice.ExpectSinceAsync(earned, FamilyLevelReached(2), "the family level, for Alice");
            await alice.ExpectSinceAsync(earned, FamilyInfo(family.Name, 2, left, Authority.Head), "the family at level 2, for Alice");

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
            await alice.SendAsync(new FhistCtsPacket());

            PacketAssert.AreEqual(new GhisPacket
            {
                Unknown = 0,
                Entries =
                [
                    Entry(PacketLogType.FamilyLevelUp, 2),
                    Entry(PacketLogType.FamilyXp, member.Name, RewardedLevelExperience),
                    Entry(PacketLogType.LevelUp, member.Name, RewardedLevel),
                ],
            }, await alice.ExpectSinceAsync<GhisPacket>(asked, null, "the history"));
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
            await alice.ExpectSinceAsync(earned, FamilyInfo(family.Name, 2, RewardedLevelExperience - 200, Authority.Head), "the family at level 2");
            await alice.ExpectSinceAsync(earned, Contribution(member.CharacterId, RewardedLevelExperience), "what Bob earned next to his name");
            await alice.ExpectSinceAsync<FhisStcPacket>(earned, null, "the history to read again");
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

            Assert.IsTrue(await alice.NoneSinceAsync<GInfoPacket>(earned, null, TimeSpan.FromSeconds(1)), "the family window stays as it was");
            Assert.IsTrue(await alice.NoneSinceAsync<FhisStcPacket>(earned, null, TimeSpan.Zero), "and so does its history");
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

            await alice.SendAsync(TalkToTheManager);
            await alice.ExpectSinceAsync(refused, Shelf, "what the family manager sells");
            await alice.SendAsync(BuyTheWarehouse);

            await alice.ExpectSinceAsync<MsgiPacket>(refused, p => p.Type == MessageType.Default
                && p.Message == Game18NConstString.FamilyLevelInsufficient, "the family level refusal");
            Assert.AreEqual(0, (await StoredFamilyAsync(family.FamilyId)).WarehouseSize);
            var levelled = alice.Mark();

            await LevelUpAsync(bob);
            await alice.ExpectSinceAsync(levelled, FamilyLevelReached(WorldData.FamilyWarehouseLevel), "the family level");
            var bought = alice.Mark();
            await alice.SendAsync(TalkToTheManager);
            await alice.ExpectSinceAsync(bought, Shelf, "what the family manager sells");
            await alice.SendAsync(BuyTheWarehouse);

            await alice.ExpectSinceAsync<Msgi2Packet>(bought, p => p.Type == MessageType.Default && p.Message == Game18NConstString.PurchasedItemX
                && p.ArgumentType == 2 && Equals(p.Game18NArguments[0], (long)WorldData.FamilyWarehouse), "the purchase");
            await alice.ExpectSinceAsync<GoldPacket>(bought, p => p is { Gold: 0, Unknown: 0 }, "the price paid");
            Assert.AreEqual(new FamilyUpgradeService().GetWarehouseSize(1), (await StoredFamilyAsync(family.FamilyId)).WarehouseSize);
        }
    }
}
