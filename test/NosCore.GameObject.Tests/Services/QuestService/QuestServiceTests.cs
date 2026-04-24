//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.QuestService.Handlers;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.QuestService
{
    [TestClass]
    public class QuestServiceTests
    {
        private GameObject.Services.QuestService.QuestService Service = null!;
        private ClientSession Session = null!;
        private List<ScriptDto> Scripts = null!;
        private List<QuestDto> Quests = null!;
        private List<QuestObjectiveDto> QuestObjectives = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            Session.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>();
            Session.Character.Level = 50;

            Scripts = new List<ScriptDto>();
            Quests = new List<QuestDto>
            {
                new QuestDto
                {
                    QuestId = 1,
                    QuestType = QuestType.GoTo,
                    LevelMin = 1,
                    LevelMax = 99,
                    IsDaily = false,
                    IsSecondary = true,
                    TargetMap = 0,
                    TargetX = 5,
                    TargetY = 5
                },
                new QuestDto
                {
                    QuestId = 2,
                    QuestType = QuestType.Hunt,
                    LevelMin = 1,
                    LevelMax = 99,
                    IsDaily = true,
                    IsSecondary = true
                },
                new QuestDto
                {
                    QuestId = 3,
                    QuestType = QuestType.Hunt,
                    LevelMin = 60,
                    LevelMax = 99,
                    IsDaily = false,
                    IsSecondary = true
                },
                new QuestDto
                {
                    QuestId = 4,
                    QuestType = QuestType.Hunt,
                    LevelMin = 1,
                    LevelMax = 30,
                    IsDaily = false,
                    IsSecondary = true
                },
                new QuestDto
                {
                    QuestId = 5,
                    QuestType = QuestType.Hunt,
                    LevelMin = 1,
                    LevelMax = 99,
                    IsDaily = false,
                    IsSecondary = false
                }
            };

            QuestObjectives = new List<QuestObjectiveDto>
            {
                new QuestObjectiveDto { QuestId = 1, FirstData = 1 },
                new QuestObjectiveDto { QuestId = 2, FirstData = 1 },
                new QuestObjectiveDto { QuestId = 3, FirstData = 1 },
                new QuestObjectiveDto { QuestId = 4, FirstData = 1 },
                new QuestObjectiveDto { QuestId = 5, FirstData = 1 }
            };

            Service = new GameObject.Services.QuestService.QuestService(
                Scripts,
                TestHelpers.Instance.WorldConfiguration,
                Quests,
                QuestObjectives,
                NullLogger<NosCore.GameObject.Services.QuestService.QuestService>.Instance,
                TestHelpers.Instance.Clock,
                TestHelpers.Instance.LogLanguageLocalizer,
                new IQuestTypeHandler[]
                {
                    new HuntQuestHandler(NullLogger<HuntQuestHandler>.Instance),
                    new NumberOfKillQuestHandler(NullLogger<NumberOfKillQuestHandler>.Instance),
                    new GoToQuestHandler(),
                },
                new Mock<Wolverine.IMessageBus>().Object,
                new List<QuestRewardDto>(),
                new List<QuestQuestRewardDto>(),
                new Mock<GameObject.Services.ItemGenerationService.IItemGenerationService>().Object,
                new Mock<GameObject.Services.ExperienceService.IExperienceProgressionService>().Object);
        }

        [TestMethod]
        public async Task AddingQuestShouldAddToCharacterQuests()
        {
            await new Spec("Adding quest should add to character quests")
                .WhenAsync(AddingValidQuest)
                .Then(QuestShouldBeAdded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingQuestWhenLevelTooLowShouldFail()
        {
            await new Spec("Adding quest when level too low should fail")
                .Given(CharacterLevelIsTooLow)
                .WhenAsync(AddingHighLevelQuest)
                .Then(QuestShouldNotBeAdded)
                .And(AddResultShouldBeFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingQuestWhenLevelTooHighShouldFail()
        {
            await new Spec("Adding quest when level too high should fail")
                .WhenAsync(AddingLowLevelQuest)
                .Then(QuestShouldNotBeAdded)
                .And(AddResultShouldBeFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingAlreadyCompletedQuestShouldFail()
        {
            await new Spec("Adding already completed quest should fail")
                .Given(QuestAlreadyCompleted)
                .WhenAsync(AddingSameQuest)
                .Then(QuestShouldStillBeOne)
                .And(AddResultShouldBeFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingDailyQuestAfterCompletionShouldSucceed()
        {
            await new Spec("Adding daily quest after completion should succeed")
                .Given(DailyQuestCompletedYesterday)
                .WhenAsync(AddingSameDailyQuest)
                .Then(DailyQuestShouldBeAddedAgain)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingNonSecondaryQuestWhenOneExistsShouldFail()
        {
            await new Spec("Adding non-secondary quest when one exists should fail")
                .Given(NonSecondaryQuestAlreadyActive)
                .WhenAsync(AddingAnotherNonSecondaryQuest)
                .Then(QuestShouldStillBeOne)
                .And(AddResultShouldBeFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateGoToQuestShouldSucceedWhenAtLocation()
        {
            await new Spec("Validate GoTo quest should succeed when at location")
                .Given(CharacterHasGoToQuest)
                .And(CharacterIsAtTargetLocation)
                .WhenAsync(ValidatingQuest)
                .Then(ValidationShouldSucceed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateGoToQuestShouldFailWhenNotAtLocation()
        {
            await new Spec("Validate GoTo quest should fail when not at location")
                .Given(CharacterHasGoToQuest)
                .And(CharacterIsNotAtTargetLocation)
                .WhenAsync(ValidatingQuest)
                .Then(ValidationShouldFail)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateNonExistentQuestShouldFail()
        {
            await new Spec("Validate non-existent quest should fail")
                .WhenAsync(ValidatingNonExistentQuest)
                .Then(ValidationShouldFail)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KillingRequiredMobsShouldCompleteHuntObjectives()
        {
            await new Spec("Killing the last required mob should complete hunt objectives")
                .Given(CharacterHasHuntQuestWithOneKillRemaining)
                .WhenAsync(KillingTheRequiredMob)
                .Then(ObjectivesShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KillingEarlyMobsShouldNotCompleteHuntObjectives()
        {
            await new Spec("Killing a mob before target count should not complete hunt objectives")
                .Given(CharacterHasHuntQuestNeedingFiveKills)
                .WhenAsync(KillingOneMob)
                .Then(ObjectivesShouldBeIncomplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KillingMoreThanRequiredClampsObjectiveProgress()
        {
            await new Spec("Progress clamps at required count, never exceeds it on extra kills")
                .Given(CharacterHasHuntQuestNeedingTwoKills)
                .WhenAsync(KillingTheRequiredMobThreeTimes)
                .Then(ObjectiveProgressShouldBe_, 2)
                .And(ObjectivesShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KillingUnrelatedMobDoesNotAdvanceObjective()
        {
            await new Spec("Killing a mob that does not match any objective FirstData leaves progress at zero")
                .Given(CharacterHasHuntQuestNeedingFiveKills)
                .WhenAsync(KillingADifferentMob)
                .Then(ObjectiveProgressShouldBe_, 0)
                .ExecuteAsync();
        }

        private void CharacterHasHuntQuestNeedingTwoKills()
        {
            var objective = new QuestObjectiveDto
            {
                QuestObjectiveId = Guid.NewGuid(),
                FirstData = TargetMobVNum,
                SecondData = 2,
            };
            var quest = new Quest
            {
                QuestId = 12,
                QuestType = QuestType.Hunt,
                QuestObjectives = new List<QuestObjectiveDto> { objective },
            };
            _trackedQuest = new CharacterQuest
            {
                QuestId = 12,
                Quest = quest,
                CompletedOn = null,
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), _trackedQuest);
        }

        private async Task KillingTheRequiredMobThreeTimes()
        {
            for (var i = 0; i < 3; i++)
            {
                await Service.OnMonsterKilledAsync(Session.Character, new NpcMonsterDto { NpcMonsterVNum = TargetMobVNum });
            }
        }

        private async Task KillingADifferentMob()
        {
            await Service.OnMonsterKilledAsync(Session.Character, new NpcMonsterDto { NpcMonsterVNum = 999 });
        }

        private void ObjectiveProgressShouldBe_(int expected)
        {
            var objectiveId = _trackedQuest.Quest.QuestObjectives.First().QuestObjectiveId;
            _trackedQuest.ObjectiveProgress.TryGetValue(objectiveId, out var progress);
            Assert.AreEqual(expected, progress);
        }

        private bool AddQuestResult;
        private bool ValidateResult;

        private async Task AddingValidQuest()
        {
            AddQuestResult = await Service.AddQuestAsync(Session.Character, QuestActionType.Validate, 1);
        }

        private void CharacterLevelIsTooLow()
        {
            Session.Character.Level = 10;
        }

        private async Task AddingHighLevelQuest()
        {
            AddQuestResult = await Service.AddQuestAsync(Session.Character, QuestActionType.Validate, 3);
        }

        private async Task AddingLowLevelQuest()
        {
            AddQuestResult = await Service.AddQuestAsync(Session.Character, QuestActionType.Validate, 4);
        }

        private void QuestAlreadyCompleted()
        {
            var quest = new Quest
            {
                QuestId = 1,
                QuestType = QuestType.Hunt,
                IsDaily = false,
                QuestObjectives = new List<QuestObjectiveDto>()
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
            {
                QuestId = 1,
                Quest = quest,
                CompletedOn = TestHelpers.Instance.Clock.GetCurrentInstant()
            });
        }

        private async Task AddingSameQuest()
        {
            AddQuestResult = await Service.AddQuestAsync(Session.Character, QuestActionType.Validate, 1);
        }

        private void DailyQuestCompletedYesterday()
        {
            var quest = new Quest
            {
                QuestId = 2,
                QuestType = QuestType.Hunt,
                IsDaily = true,
                QuestObjectives = new List<QuestObjectiveDto>()
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
            {
                QuestId = 2,
                Quest = quest,
                CompletedOn = TestHelpers.Instance.Clock.GetCurrentInstant().Minus(NodaTime.Duration.FromDays(2))
            });
        }

        private async Task AddingSameDailyQuest()
        {
            AddQuestResult = await Service.AddQuestAsync(Session.Character, QuestActionType.Validate, 2);
        }

        private void NonSecondaryQuestAlreadyActive()
        {
            var quest = new Quest
            {
                QuestId = 5,
                QuestType = QuestType.Hunt,
                IsSecondary = false,
                QuestObjectives = new List<QuestObjectiveDto>()
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
            {
                QuestId = 5,
                Quest = quest,
                CompletedOn = null
            });
        }

        private async Task AddingAnotherNonSecondaryQuest()
        {
            AddQuestResult = await Service.AddQuestAsync(Session.Character, QuestActionType.Validate, 5);
        }

        private void CharacterHasGoToQuest()
        {
            var quest = new Quest
            {
                QuestId = 1,
                QuestType = QuestType.GoTo,
                TargetMap = 0,
                TargetX = 5,
                TargetY = 5,
                QuestObjectives = new List<QuestObjectiveDto>()
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
            {
                QuestId = 1,
                Quest = quest,
                CompletedOn = null
            });
        }

        private void CharacterIsAtTargetLocation()
        {
            Session.Character.MapId = 0;
            Session.Character.MapX = 5;
            Session.Character.MapY = 5;
        }

        private void CharacterIsNotAtTargetLocation()
        {
            Session.Character.MapId = 0;
            Session.Character.MapX = 50;
            Session.Character.MapY = 50;
        }

        private async Task ValidatingQuest()
        {
            ValidateResult = await Service.ValidateQuestAsync(Session.Character, 1);
        }

        private async Task ValidatingNonExistentQuest()
        {
            ValidateResult = await Service.ValidateQuestAsync(Session.Character, 999);
        }

        private void QuestShouldBeAdded()
        {
            Assert.IsTrue(Session.Character.Quests.Values.Any(q => q.QuestId == 1));
            Assert.IsTrue(AddQuestResult);
        }

        private void QuestShouldNotBeAdded()
        {
            Assert.IsFalse(Session.Character.Quests.Values.Any(q => q.QuestId == 3 || q.QuestId == 4));
        }

        private void QuestShouldStillBeOne()
        {
            Assert.AreEqual(1, Session.Character.Quests.Count);
        }

        private void DailyQuestShouldBeAddedAgain()
        {
            Assert.AreEqual(2, Session.Character.Quests.Values.Count(q => q.QuestId == 2));
        }

        private void AddResultShouldBeFalse()
        {
            Assert.IsFalse(AddQuestResult);
        }

        private void ValidationShouldSucceed()
        {
            Assert.IsTrue(ValidateResult);
        }

        private void ValidationShouldFail()
        {
            Assert.IsFalse(ValidateResult);
        }

        private CharacterQuest _trackedQuest = null!;
        private const short TargetMobVNum = 42;

        private void CharacterHasHuntQuestWithOneKillRemaining()
        {
            var objective = new QuestObjectiveDto
            {
                QuestObjectiveId = Guid.NewGuid(),
                FirstData = TargetMobVNum,
                SecondData = 5
            };
            var quest = new Quest
            {
                QuestId = 10,
                QuestType = QuestType.Hunt,
                QuestObjectives = new List<QuestObjectiveDto> { objective }
            };
            _trackedQuest = new CharacterQuest
            {
                QuestId = 10,
                Quest = quest,
                CompletedOn = null
            };
            _trackedQuest.ObjectiveProgress[objective.QuestObjectiveId] = 4;
            Session.Character.Quests.TryAdd(Guid.NewGuid(), _trackedQuest);
        }

        private void CharacterHasHuntQuestNeedingFiveKills()
        {
            var objective = new QuestObjectiveDto
            {
                QuestObjectiveId = Guid.NewGuid(),
                FirstData = TargetMobVNum,
                SecondData = 5
            };
            var quest = new Quest
            {
                QuestId = 11,
                QuestType = QuestType.Hunt,
                QuestObjectives = new List<QuestObjectiveDto> { objective }
            };
            _trackedQuest = new CharacterQuest
            {
                QuestId = 11,
                Quest = quest,
                CompletedOn = null
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), _trackedQuest);
        }

        private async Task KillingTheRequiredMob()
        {
            await Service.OnMonsterKilledAsync(Session.Character, new NpcMonsterDto { NpcMonsterVNum = TargetMobVNum });
        }

        private async Task KillingOneMob()
        {
            await Service.OnMonsterKilledAsync(Session.Character, new NpcMonsterDto { NpcMonsterVNum = TargetMobVNum });
        }

        private void ObjectivesShouldBeComplete()
        {
            Assert.IsTrue(_trackedQuest.AreObjectivesComplete());
        }

        private void ObjectivesShouldBeIncomplete()
        {
            Assert.IsFalse(_trackedQuest.AreObjectivesComplete());
            Assert.IsNull(_trackedQuest.CompletedOn);
        }
    }
}
