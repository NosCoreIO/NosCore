//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.Interaction;
using Act4RaidType = NosCore.Packets.Enumerations.Act4RaidType;
using NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyContributionServiceTests
    {
        private const long FamilyId = 3;
        private const long CharacterId = 7;
        private const string CharacterName = "Tester";

        private readonly List<(FamilyLogType Type, object[] Arguments)> _logs = new();
        private readonly List<IMessage> _published = new();
        private TestWorld World;
        private Mock<IFamilyHub> _hub;
        private FamilyContributionService _service;
        private bool _earned;
        private bool _boosted;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Freeze<Mock<IFamilyLogService>>()
                .Setup(s => s.AddAsync(FamilyId, It.IsAny<FamilyLogType>(), It.IsAny<object[]>()))
                .Callback<long, FamilyLogType, object[]>((_, type, arguments) => _logs.Add((type, arguments)))
                .Returns(Task.CompletedTask);
            _hub = World.Freeze<Mock<IFamilyHub>>();
            _hub.Setup(s => s.AddExperienceAsync(FamilyId, CharacterId, It.IsAny<int>()))
                .ReturnsAsync(new FamilyExperienceGain(1, false));
            World.PubSubHub.Setup(s => s.SendMessageAsync(It.IsAny<IMessage>())).Callback<IMessage>(_published.Add).ReturnsAsync(true);
            _service = World.Create<FamilyContributionService>();
        }

        [TestMethod]
        public async Task ReachingATenthLevelAbove20GivesTwentyTimesTheLevel()
        {
            await new Spec("A member reaching level 30 earns the family 600 experience")
                .WhenAsync(TheMemberGoesFromLevel_To_, 29, 30)
                .Then(TheFamilyShouldHaveEarned_Once, 600)
                .And(TheHistoryShouldSayTheMemberEarned_, 600)
                .And(TheMembersShouldBeShownTheFamilyAgainWithoutALevelUp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFamilyExperienceBoosterDoublesWhatTheMemberEarns()
        {
            await new Spec("A member under an FXP Booster reaching level 30 earns the family 1 200 experience")
                .Given(TheMemberUsesAnFxpBooster)
                .WhenAsync(TheMemberGoesFromLevel_To_, 29, 30)
                .Then(TheFamilyShouldHaveEarned_Once, 1200)
                .And(TheHistoryShouldSayTheMemberEarned_, 1200)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherLevelsGiveNothing()
        {
            await new Spec("Level 20 and levels between two tenths earn the family nothing")
                .WhenAsync(TheMemberGoesFromLevel_To_, 18, 20)
                .AndAsync(TheMemberGoesFromLevel_To_, 30, 31)
                .Then(TheFamilyShouldHaveEarnedNothing)
                .And(NothingShouldBeWrittenOrShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryTenthLevelPassedInOneGoIsRewarded()
        {
            await new Spec("A member going from level 29 to 41 at once earns the family 600 and 800")
                .WhenAsync(TheMemberGoesFromLevel_To_, 29, 41)
                .Then(TheFamilyShouldHaveEarned_Once, 600)
                .And(TheFamilyShouldHaveEarned_Once, 800)
                .And(TheHistoryShouldHold_ExperienceLines, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFamilyLevelUpIsLoggedAndAnnounced()
        {
            await new Spec("A gain that fills the bar is written to the history and told to the members")
                .Given(AGainOf_TakesTheFamilyToLevel_, 800, 4)
                .WhenAsync(TheMemberGoesFromLevel_To_, 39, 40)
                .Then(TheHistoryShouldSayTheFamilyReachedLevel_, 4)
                .And(TheMembersShouldBeToldTheFamilyReachedLevel_, 4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WhatAMemberEarnsIsStoredWrittenAndShown()
        {
            await new Spec("Experience a member earns is added on the master, written to the history and shown")
                .WhenAsync(TheMemberEarns_, 250)
                .Then(TheFamilyShouldHaveEarned_Once, 250)
                .And(TheHistoryShouldSayTheMemberEarned_, 250)
                .And(TheMembersShouldBeShownTheFamilyAgainWithoutALevelUp)
                .And(TheGainShouldBeReportedAs_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SomeoneTheMasterRefusesEarnsNothing()
        {
            await new Spec("A gain the master does not store is neither written to the history nor shown")
                .Given(TheMasterStoresNothing)
                .WhenAsync(TheMemberEarns_, 250)
                .Then(NothingShouldBeWrittenOrShown)
                .And(TheGainShouldBeReportedAs_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWonRaidCountsForEveryMemberWithoutAHistoryLine()
        {
            await new Spec("Two members winning Mother Cuby advance its missions twice and write no history line")
                .WhenAsync(Members_And_WinRaid_, 7, 8, 0)
                .Then(EachOf_And_ShouldAdvanceRaid_, 7, 8, 0)
                .And(TheHistoryShouldStayEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGlacernonRaidWinIsWrittenToTheHistoryAndCountedOnce()
        {
            await new Spec("Beating Lord Hatus writes a raid line and counts once towards the family raid and Hatus quests")
                .WhenAsync(TheFamilyBeats_, Act4RaidType.Hatus, false, false)
                .Then(TheHistoryShouldSayTheFamilyMastered_, Act4RaidType.Hatus)
                .And(TheFamilyShouldAdvance_With_Once, FamilyQuestObjective.GlacernonRaid, (int?)null)
                .And(TheFamilyShouldAdvance_With_Once, FamilyQuestObjective.GlacernonRaidBoss, (int?)Act4RaidType.Hatus)
                .And(TheFamilyShouldNotAdvance_, FamilyQuestObjective.GlacernonRaidInTenMinutes)
                .And(TheFamilyShouldNotAdvance_, FamilyQuestObjective.GlacernonRaidWithoutLoss)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AQuickGlacernonRaidWinWithoutLossCountsForThoseMissionsToo()
        {
            await new Spec("Beating Lady Calvinas within ten minutes and without anyone falling counts for both daily missions")
                .WhenAsync(TheFamilyBeats_, Act4RaidType.Calvina, true, true)
                .Then(TheFamilyShouldAdvance_With_Once, FamilyQuestObjective.GlacernonRaidInTenMinutes, (int?)null)
                .And(TheFamilyShouldAdvance_With_Once, FamilyQuestObjective.GlacernonRaidWithoutLoss, (int?)null)
                .ExecuteAsync();
        }

        private Task TheFamilyBeats_(Act4RaidType raid, bool inTenMinutes, bool withoutLoss) =>
            _service.GlacernonRaidWonAsync(FamilyId, raid, inTenMinutes, withoutLoss);

        private void TheHistoryShouldSayTheFamilyMastered_(Act4RaidType raid) =>
            CollectionAssert.AreEqual(new object[] { (byte)raid }, _logs.Single(s => s.Type == FamilyLogType.RaidWon).Arguments);

        private void TheFamilyShouldAdvance_With_Once(FamilyQuestObjective objective, int? parameter) =>
            _hub.Verify(s => s.AdvanceQuestsAsync(FamilyId, null, objective, 1, parameter), Times.Once);

        private void TheFamilyShouldNotAdvance_(FamilyQuestObjective objective) =>
            _hub.Verify(s => s.AdvanceQuestsAsync(It.IsAny<long>(), It.IsAny<long?>(), objective, It.IsAny<int>(), It.IsAny<int?>()), Times.Never);

        private Task Members_And_WinRaid_(int first, int second, int raidId) =>
            _service.RaidWonAsync(FamilyId, new long[] { first, second }, (byte)raidId, false);

        private void TheHistoryShouldStayEmpty() => Assert.AreEqual(0, _logs.Count);

        private void EachOf_And_ShouldAdvanceRaid_(int first, int second, int raidId)
        {
            foreach (var characterId in new long[] { first, second })
            {
                _hub.Verify(s => s.AdvanceQuestsAsync(FamilyId, characterId, FamilyQuestObjective.Raid, 1, raidId), Times.Once);
            }
        }

        private void AGainOf_TakesTheFamilyToLevel_(int experience, int level) =>
            _hub.Setup(s => s.AddExperienceAsync(FamilyId, CharacterId, experience))
                .ReturnsAsync(new FamilyExperienceGain((byte)level, true));

        private void TheMemberUsesAnFxpBooster() => _boosted = true;

        private void TheMasterStoresNothing() =>
            _hub.Setup(s => s.AddExperienceAsync(FamilyId, CharacterId, It.IsAny<int>()))
                .ReturnsAsync((FamilyExperienceGain?)null);

        private Task TheMemberGoesFromLevel_To_(int previous, int level) =>
            _service.CharacterLevelledUpAsync(FamilyId, CharacterId, CharacterName, (byte)previous, (byte)level, _boosted);

        private async Task TheMemberEarns_(int experience) =>
            _earned = await _service.ExperienceEarnedAsync(FamilyId, CharacterId, CharacterName, experience, _boosted);

        private void TheFamilyShouldHaveEarned_Once(int experience) =>
            _hub.Verify(s => s.AddExperienceAsync(FamilyId, CharacterId, experience), Times.Once);

        private void TheFamilyShouldHaveEarnedNothing() =>
            _hub.Verify(s => s.AddExperienceAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()), Times.Never);

        private void TheHistoryShouldSayTheMemberEarned_(int experience) =>
            CollectionAssert.AreEqual(new object[] { CharacterName, experience },
                _logs.Single(s => s.Type == FamilyLogType.FamilyXp).Arguments);

        private void TheHistoryShouldHold_ExperienceLines(int count) =>
            Assert.AreEqual(count, _logs.Count(s => s.Type == FamilyLogType.FamilyXp));

        private void TheHistoryShouldSayTheFamilyReachedLevel_(int level) =>
            CollectionAssert.AreEqual(new object[] { (byte)level },
                _logs.Single(s => s.Type == FamilyLogType.FamilyLevelUp).Arguments);

        private void TheMembersShouldBeShownTheFamilyAgainWithoutALevelUp()
        {
            var change = (FamilyChangedData)_published.Single();
            Assert.AreEqual(FamilyId, change.FamilyId);
            Assert.IsNull(change.ReachedFamilyLevel);
        }

        private void TheMembersShouldBeToldTheFamilyReachedLevel_(int level) =>
            Assert.AreEqual((byte)level, ((FamilyChangedData)_published.Single()).ReachedFamilyLevel);

        private void NothingShouldBeWrittenOrShown()
        {
            Assert.AreEqual(0, _logs.Count);
            Assert.AreEqual(0, _published.Count);
        }

        private void TheGainShouldBeReportedAs_(bool earned) => Assert.AreEqual(earned, _earned);
    }
}
