//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.FamilyExperienceService;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.StaticEntities;
using FamilyQuestKind = NosCore.Packets.Enumerations.FamilyQuestKind;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Tests.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FamilyEntity = NosCore.Database.Entities.Family;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyProgressionServiceTests
    {
        private TestWorld World;
        private const long MemberId = 1;
        private const long OutsiderId = 2;
        private const long OtherMemberId = 3;
        private const long ThirdMemberId = 4;

        private readonly FamilyExperienceService _curve = new();
        private IDao<FamilyDto, long> _familyDao;
        private IDao<FamilyCharacterDto, long> _familyCharacterDao;
        private IDao<FamilyQuestProgressDto, long> _questProgressDao;
        private List<FamilyQuestDto> _quests;
        private FamilyProgressionService _service;
        private long _familyId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            var context = World.DbContextBuilder;
            _familyDao = new Dao<FamilyEntity, FamilyDto, long>(NullLogger<Dao<FamilyEntity, FamilyDto, long>>.Instance, context);
            _familyCharacterDao = new Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>(
                NullLogger<Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>>.Instance, context);
            _familyId = (await _familyDao.TryInsertOrUpdateAsync(new FamilyDto
            {
                Name = "Testers", FamilyLevel = 1, MaxSize = 50, FamilyMessage = string.Empty
            })).NotNull().FamilyId;
            await _familyCharacterDao.TryInsertOrUpdateAsync(new FamilyCharacterDto
            {
                FamilyId = _familyId, CharacterId = MemberId, Authority = FamilyAuthority.Head, DailyMessage = string.Empty
            });
            _questProgressDao = new Dao<Database.Entities.FamilyQuestProgress, FamilyQuestProgressDto, long>(
                NullLogger<Dao<Database.Entities.FamilyQuestProgress, FamilyQuestProgressDto, long>>.Instance, context);
            _quests =
            [
                Achievement(LevelTwo, FamilyQuestObjective.FamilyLevel, target: 1, parameter: 2),
                Achievement(LevelThree, FamilyQuestObjective.FamilyLevel, target: 1, parameter: 3, previous: LevelTwo),
                Achievement(FirstQuotes, FamilyQuestObjective.DailyMessage, target: 2, experience: QuoteReward),
                Achievement(MoreQuotes, FamilyQuestObjective.DailyMessage, target: 4, previous: FirstQuotes),
                new()
                {
                    QuestId = InstantCombats, Kind = FamilyQuestKind.Mission, Objective = FamilyQuestObjective.InstantCombat,
                    FamilyLevelMinimum = 1, Target = 2, Experience = MissionReward, ResetsDaily = true, OncePerMemberPerDay = true
                }
            ];
            _service = new FamilyProgressionService(_familyDao, _familyCharacterDao, _questProgressDao,
                new Dao<Database.Entities.FamilyQuestContribution, FamilyQuestContributionDto, long>(
                    NullLogger<Dao<Database.Entities.FamilyQuestContribution, FamilyQuestContributionDto, long>>.Instance, context),
                _quests, _curve,
                new DaoTransactionScope(context), World.Clock);
        }

        private const short LevelTwo = 9018;
        private const short LevelThree = 9019;
        private const short FirstQuotes = 9037;
        private const short MoreQuotes = 9038;
        private const int QuoteReward = 300;
        private const short InstantCombats = 9017;
        private const int MissionReward = 500;

        private static FamilyQuestDto Achievement(short id, FamilyQuestObjective objective, int target, int? parameter = null,
            short? previous = null, int experience = 0) => new()
        {
            QuestId = id, Kind = FamilyQuestKind.Achievement, Objective = objective, FamilyLevelMinimum = 1, Target = target,
            Parameter = parameter, PreviousQuestId = previous, Experience = experience
        };

        private FamilyQuestProgressDto? Quest(short id) => _questProgressDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId && s.QuestId == id).Result;

        private async Task<FamilyDto?> FamilyAsync() => await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId);

        [TestMethod]
        public async Task TheFamilyAndTheMemberBothGain()
        {
            var gain = await _service.AddExperienceAsync(_familyId, MemberId, 600);

            Assert.IsNotNull(gain);
            Assert.IsFalse(gain.LeveledUp);
            Assert.AreEqual(600, (await FamilyAsync()).NotNull().FamilyExperience);
            Assert.AreEqual(600, (await _familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == MemberId)).NotNull().Experience);
        }

        [TestMethod]
        public async Task FillingTheBarLevelsTheFamilyUp()
        {
            var bar = (int)_curve.GetFamilyExperience(1);

            var gain = (await _service.AddExperienceAsync(_familyId, MemberId, bar + 400)).NotNull();

            Assert.IsTrue(gain.LeveledUp);
            Assert.AreEqual(2, gain.FamilyLevel);
            var family = (await FamilyAsync()).NotNull();
            Assert.AreEqual(2, family.FamilyLevel);
            Assert.AreEqual(400, family.FamilyExperience);
        }

        [TestMethod]
        public async Task TheLastLevelKeepsAFullBar()
        {
            var family = (await FamilyAsync()).NotNull();
            family.FamilyLevel = 30;
            await _familyDao.TryInsertOrUpdateAsync(family);

            var gain = (await _service.AddExperienceAsync(_familyId, MemberId, (int)_curve.GetFamilyExperience(30))).NotNull();

            Assert.IsFalse(gain.LeveledUp);
            Assert.AreEqual(30, gain.FamilyLevel);
        }

        [TestMethod]
        public async Task SomeoneOutsideTheFamilyAddsNothing()
        {
            var gain = await _service.AddExperienceAsync(_familyId, OutsiderId, 600);

            Assert.IsNull(gain);
            Assert.AreEqual(0, (await FamilyAsync()).NotNull().FamilyExperience);
        }

        [TestMethod]
        public async Task LevellingUpUnlocksTheAchievementForThatLevel()
        {
            var gain = (await _service.AddExperienceAsync(_familyId, MemberId, (int)_curve.GetFamilyExperience(1))).NotNull();

            CollectionAssert.AreEqual(new[] { LevelTwo }, gain.CompletedQuestIds.ToArray());
            Assert.IsNotNull(Quest(LevelTwo).NotNull().CompletedAt);
            Assert.IsNull(Quest(LevelThree));
        }

        [TestMethod]
        public async Task SkippingALevelUnlocksEveryAchievementOnTheWay()
        {
            var bar = (int)(_curve.GetFamilyExperience(1) + _curve.GetFamilyExperience(2));

            var gain = (await _service.AddExperienceAsync(_familyId, MemberId, bar)).NotNull();

            CollectionAssert.AreEqual(new[] { LevelTwo, LevelThree }, gain.CompletedQuestIds.ToArray());
        }

        [TestMethod]
        public async Task EachDailyMessageCountsTowardsTheQuoteAchievement()
        {
            var gain = (await _service.AdvanceQuestsAsync(_familyId, null, FamilyQuestObjective.DailyMessage, 1)).NotNull();

            Assert.AreEqual(0, gain.CompletedQuestIds.Count);
            Assert.AreEqual(1, Quest(FirstQuotes).NotNull().Progress);
            Assert.IsNull(Quest(FirstQuotes).NotNull().CompletedAt);
        }

        [TestMethod]
        public async Task ReachingTheTargetUnlocksItGrantsItsExperienceAndCarriesTheCountOn()
        {
            await _service.AdvanceQuestsAsync(_familyId, null, FamilyQuestObjective.DailyMessage, 1);

            var gain = (await _service.AdvanceQuestsAsync(_familyId, null, FamilyQuestObjective.DailyMessage, 1)).NotNull();

            CollectionAssert.AreEqual(new[] { FirstQuotes }, gain.CompletedQuestIds.ToArray());
            Assert.AreEqual(QuoteReward, (await FamilyAsync()).NotNull().FamilyExperience);
            Assert.AreEqual(2, Quest(MoreQuotes).NotNull().Progress);
            Assert.AreEqual(0, (await _familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == MemberId)).NotNull().Experience);
        }

        [TestMethod]
        public async Task AnUnlockedAchievementIsNotUnlockedAgain()
        {
            await _service.AdvanceQuestsAsync(_familyId, null, FamilyQuestObjective.DailyMessage, 2);

            var gain = (await _service.AdvanceQuestsAsync(_familyId, null, FamilyQuestObjective.DailyMessage, 1)).NotNull();

            Assert.AreEqual(0, gain.CompletedQuestIds.Count);
            Assert.AreEqual(1, Quest(FirstQuotes).NotNull().TotalCompletions);
            Assert.AreEqual(3, Quest(MoreQuotes).NotNull().Progress);
        }

        [TestMethod]
        public async Task AnAchievementRewardThatLevelsTheFamilyUnlocksTheLevelAchievementToo()
        {
            _quests[2].Experience = (int)_curve.GetFamilyExperience(1);

            var gain = (await _service.AdvanceQuestsAsync(_familyId, null, FamilyQuestObjective.DailyMessage, 2)).NotNull();

            Assert.IsTrue(gain.LeveledUp);
            CollectionAssert.AreEquivalent(new[] { FirstQuotes, LevelTwo }, gain.CompletedQuestIds.ToArray());
        }

        [TestMethod]
        public async Task AFailedSaveReportsNoGain()
        {
            var familyCharacterDao = new Mock<IDao<FamilyCharacterDto, long>>();
            familyCharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<FamilyCharacterDto, bool>>>()))
                .ReturnsAsync(new FamilyCharacterDto { CharacterId = MemberId, FamilyId = _familyId });
#nullable disable
            familyCharacterDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<FamilyCharacterDto>()))
                .Returns(Task.FromResult<FamilyCharacterDto>(null));
#nullable restore
            var service = new FamilyProgressionService(_familyDao, familyCharacterDao.Object, _questProgressDao,
                new Mock<IDao<FamilyQuestContributionDto, long>>().Object, _quests, _curve,
                new Mock<IDaoTransactionScope> { DefaultValue = DefaultValue.Mock }.Object, World.Clock);

            Assert.IsNull(await service.AddExperienceAsync(_familyId, MemberId, 600));
        }

        private Task<FamilyExperienceGain?> WinAsync(long characterId) =>
            _service.AdvanceQuestsAsync(_familyId, characterId, FamilyQuestObjective.InstantCombat, 1);

        [TestMethod]
        public async Task WinningInstantCombatsCompletesTheDailyMission()
        {
            await WinAsync(MemberId);

            var gain = (await WinAsync(OtherMemberId)).NotNull();

            CollectionAssert.AreEqual(new[] { InstantCombats }, gain.CompletedQuestIds.ToArray());
            Assert.AreEqual(MissionReward, (await FamilyAsync()).NotNull().FamilyExperience);
        }

        [TestMethod]
        public async Task AMemberCountsOnceADayTowardsTheMission()
        {
            await WinAsync(MemberId);

            var gain = (await WinAsync(MemberId)).NotNull();

            Assert.AreEqual(0, gain.CompletedQuestIds.Count);
            Assert.AreEqual(1, Quest(InstantCombats).NotNull().Progress);
        }

        [TestMethod]
        public async Task ADailyMissionStartsOverTheNextDay()
        {
            await WinAsync(MemberId);
            World.Clock.AdvanceDays(1);

            var gain = (await WinAsync(MemberId)).NotNull();

            Assert.AreEqual(0, gain.CompletedQuestIds.Count);
            Assert.AreEqual(1, Quest(InstantCombats).NotNull().Progress);
        }

        [TestMethod]
        public async Task ACompletedDailyMissionIsCompletedAgainTheNextDayOnly()
        {
            await WinAsync(MemberId);
            await WinAsync(OtherMemberId);
            await WinAsync(ThirdMemberId);
            World.Clock.AdvanceDays(1);
            await WinAsync(MemberId);

            var gain = (await WinAsync(OtherMemberId)).NotNull();

            CollectionAssert.AreEqual(new[] { InstantCombats }, gain.CompletedQuestIds.ToArray());
            Assert.AreEqual(2, Quest(InstantCombats).NotNull().TotalCompletions);
            Assert.AreEqual(2 * MissionReward, (await FamilyAsync()).NotNull().FamilyExperience);
        }
    }
}
