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
using NosCore.Packets.ClientPackets.Quest;
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
        private TestWorld World;
        private GameObject.Services.QuestService.QuestService Service;
        private ClientSession Session;
        private List<ScriptDto> Scripts;
        private List<ScriptOfferDto> ScriptOffers;
        private List<QuestDto> Quests;
        private List<QuestObjectiveDto> QuestObjectives;
        private readonly Mock<Wolverine.IMessageBus> Bus = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(0);
            Session.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>();
            Session.Character.Level = 50;

            Scripts = new List<ScriptDto>();
            ScriptOffers = new List<ScriptOfferDto>();
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
                },
                new QuestDto
                {
                    QuestId = 10,
                    QuestType = QuestType.Hunt,
                    LevelMin = 1,
                    LevelMax = 99,
                    IsSecondary = false
                },
                new QuestDto
                {
                    QuestId = 6,
                    QuestType = QuestType.UnKnow,
                    LevelMin = 1,
                    LevelMax = 99,
                    IsSecondary = true,
                    NextQuestId = 8
                },
                new QuestDto
                {
                    QuestId = 8,
                    QuestType = QuestType.Dialog2,
                    LevelMin = 1,
                    LevelMax = 99,
                    IsSecondary = true,
                    NextQuestId = 6
                },
                new QuestDto
                {
                    QuestId = 9,
                    QuestType = QuestType.UnKnow,
                    LevelMin = 1,
                    LevelMax = 99,
                    IsSecondary = true,
                    NextQuestId = 1
                },
                new QuestDto
                {
                    QuestId = 7,
                    QuestType = QuestType.UnKnow,
                    LevelMin = 1,
                    LevelMax = 99,
                    IsSecondary = true
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
                ScriptOffers,
                World.WorldConfiguration,
                Quests,
                QuestObjectives,
                NullLogger<NosCore.GameObject.Services.QuestService.QuestService>.Instance,
                World.Clock,
                World.LogLanguageLocalizer,
                new IQuestTypeHandler[]
                {
                    World.Create<HuntQuestHandler>(),
                    World.Create<NumberOfKillQuestHandler>(),
                    World.Create<DefeatEnemiesQuestHandler>(),
                    World.Create<GoToQuestHandler>(),
                    new NpcButtonQuestHandler(Quests),
                },
                Bus.Object,
                new Mock<IQuestRewardService>().Object);
        }

        [TestMethod]
        public async Task AQuestWithoutObjectiveThatLinksOnIsDoneWhenTaken()
        {
            await new Spec("A quest with no objective, paired with the quest it links to, ends when taken so that quest follows")
                .WhenAsync(TakingQuest_, (short)6)
                .Then(Quest_ShouldBeDone_, (short)6, true)
                .And(QuestCompletedShouldBePublishedFor_, (short)6)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AQuestWithoutObjectiveThatOnlyLinksForwardWaitsForItsButton()
        {
            await new Spec("A quest with no objective whose next quest does not link back stays open")
                .WhenAsync(TakingQuest_, (short)9)
                .Then(Quest_ShouldBeDone_, (short)9, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AQuestWithoutObjectiveOrLinkWaitsForItsButton()
        {
            await new Spec("A quest with no objective and no link stays open for the NPC hand-in button")
                .WhenAsync(TakingQuest_, (short)7)
                .Then(Quest_ShouldBeDone_, (short)7, false)
                .ExecuteAsync();
        }

        private async Task TakingQuest_(short questId) =>
            AddQuestResult = await Service.AddQuestAsync(Session.Character, QuestActionType.Achieve, questId);

        private void Quest_ShouldBeDone_(short questId, bool done) =>
            Assert.AreEqual(done, Session.Character.Quests.Values.Single(q => q.QuestId == questId).CompletedOn is not null);

        private void QuestCompletedShouldBePublishedFor_(short questId) =>
            Assert.IsTrue(Bus.Invocations.Select(i => i.Arguments[0]).OfType<GameObject.Messaging.Events.QuestCompletedEvent>()
                .Any(e => e.Quest.QuestId == questId));

        [TestMethod]
        public async Task AChapterAnNpcOffersDoesNotStartWhenThePreviousOneEnds()
        {
            await new Spec("Finishing a chapter waits for the NPC that offers the next one")
                .Given(Npc_OffersChapter_FromLevel_, (short)331, (byte)2, (byte)19)
                .And(CharacterIsOnTheLastStepOfChapterOne)
                .WhenAsync(TheClientAcknowledgesStep_OfScript_, (short)2, (byte)1)
                .Then(CharacterShouldBeOnStep_OfScript_, (short)2, (byte)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TakingAnOfferedChapterStartsItsFirstStep()
        {
            await new Spec("Picking a chapter from the NPC's list starts it")
                .Given(Npc_OffersChapter_FromLevel_, (short)331, (byte)2, (byte)19)
                .And(CharacterIsOnTheLastStepOfChapterOne)
                .WhenAsync(TakingChapter_FromNpc_, (byte)2, (short)331)
                .Then(CharacterShouldBeOnStep_OfScript_, (short)10, (byte)2)
                .And(TheOfferResultShouldBe_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnOfferedChapterWaitsForItsLevel()
        {
            await new Spec("A chapter is not handed out below its level")
                .Given(Npc_OffersChapter_FromLevel_, (short)331, (byte)2, (byte)60)
                .And(CharacterIsOnTheLastStepOfChapterOne)
                .WhenAsync(TakingChapter_FromNpc_, (byte)2, (short)331)
                .Then(CharacterShouldBeOnStep_OfScript_, (short)2, (byte)1)
                .And(TheOfferResultShouldBe_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnOfferedChapterWaitsForThePreviousOneToEnd()
        {
            await new Spec("A chapter is not handed out while the one before it is still running")
                .Given(Npc_OffersChapter_FromLevel_, (short)331, (byte)2, (byte)19)
                .And(CharacterIsOnTheLastStepOfChapterOne)
                .And(CharacterIsBackOnStepOneOfChapterOne)
                .WhenAsync(TakingChapter_FromNpc_, (byte)2, (short)331)
                .Then(CharacterShouldBeOnStep_OfScript_, (short)1, (byte)1)
                .And(TheOfferResultShouldBe_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyTheNpcThatOffersAChapterHandsItOut()
        {
            await new Spec("Another NPC cannot hand out the chapter")
                .Given(Npc_OffersChapter_FromLevel_, (short)331, (byte)2, (byte)19)
                .And(CharacterIsOnTheLastStepOfChapterOne)
                .WhenAsync(TakingChapter_FromNpc_, (byte)2, (short)300)
                .Then(CharacterShouldBeOnStep_OfScript_, (short)2, (byte)1)
                .And(TheOfferResultShouldBe_, false)
                .ExecuteAsync();
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
        public async Task ADailyQuestDoneEarlierTodayCannotBeTakenAgain()
        {
            await new Spec("A daily quest done a few hours ago waits for the next day")
                .Given(DailyQuestCompletedHoursAgo)
                .WhenAsync(AddingSameDailyQuest)
                .Then(DailyQuestShouldBeHeldOnce)
                .And(AddResultShouldBeFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMainQuestCanBeTakenWhileAnotherIsOpen()
        {
            await new Spec("A main quest can be taken while another main quest is open")
                .Given(NonSecondaryQuestAlreadyActive)
                .WhenAsync(AddingAnotherNonSecondaryQuest)
                .Then(TwoMainQuestsShouldBeOpenInTheirOwnSlots)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyTheQuestJustTakenOpensItsDialog()
        {
            await new Spec("Only the quest just taken opens its dialog")
                .Given(NonSecondaryQuestAlreadyActive)
                .WhenAsync(AddingAnotherNonSecondaryQuest)
                .Then(OnlyTheNewQuestShouldShowItsDialog)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMainQuestIsRefusedWhenTheMainSlotsAreFull()
        {
            await new Spec("A main quest is refused when the five main slots are taken")
                .Given(FiveMainQuestsAreOpen)
                .WhenAsync(AddingAnotherNonSecondaryQuest)
                .Then(AddResultShouldBeFalse)
                .And(TheMainQuestShouldNotBeAdded)
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

        [TestMethod]
        public async Task AnyMonsterKilledInATimeSpaceCountsTowardsADefeatQuest()
        {
            await new Spec("Any monster killed inside a time-space counts towards a quest to defeat enemies in a time-space")
                .Given(CharacterHasADefeatQuestNeedingTwoKillsInATimeSpace)
                .And(TheCharacterIsInATimeSpace)
                .WhenAsync(KillingADifferentMob)
                .Then(ObjectiveProgressShouldBe_, 1)
                .And(ObjectivesShouldBeIncomplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheLastTimeSpaceKillCompletesADefeatQuest()
        {
            await new Spec("The second kill inside a time-space completes a quest to defeat two enemies there")
                .Given(CharacterHasADefeatQuestNeedingTwoKillsInATimeSpace)
                .And(TheCharacterIsInATimeSpace)
                .WhenAsync(KillingADifferentMob)
                .AndAsync(KillingTheRequiredMob)
                .Then(ObjectivesShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KillsOutsideATimeSpaceDoNotCountTowardsADefeatQuest()
        {
            await new Spec("A monster killed on an ordinary map does not count towards a quest to defeat enemies in a time-space")
                .Given(CharacterHasADefeatQuestNeedingTwoKillsInATimeSpace)
                .WhenAsync(KillingADifferentMob)
                .Then(ObjectiveProgressShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DefeatingAnEnemyPlayerCountsTowardsAnAct4DefeatQuest()
        {
            await new Spec("Defeating a player of the other faction counts towards a quest to defeat enemies in Glacernon")
                .Given(CharacterHasADefeatQuestNeedingTwoEnemiesInGlacernon)
                .WhenAsync(DefeatingAnEnemyPlayer)
                .Then(ObjectiveProgressShouldBe_, 1)
                .And(ObjectivesShouldBeIncomplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheLastEnemyPlayerCompletesAnAct4DefeatQuest()
        {
            await new Spec("The second enemy player defeated completes a quest to defeat two enemies in Glacernon")
                .Given(CharacterHasADefeatQuestNeedingTwoEnemiesInGlacernon)
                .WhenAsync(DefeatingAnEnemyPlayer)
                .AndAsync(DefeatingAnEnemyPlayer)
                .Then(ObjectivesShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEnemyPlayerDoesNotCountTowardsATimeSpaceDefeatQuest()
        {
            await new Spec("Defeating an enemy player does not count towards a quest to defeat enemies in a time-space")
                .Given(CharacterHasADefeatQuestNeedingTwoKillsInATimeSpace)
                .WhenAsync(DefeatingAnEnemyPlayer)
                .Then(ObjectiveProgressShouldBe_, 0)
                .ExecuteAsync();
        }

        private void CharacterHasADefeatQuestNeedingTwoEnemiesInGlacernon()
        {
            var objective = new QuestObjectiveDto { QuestObjectiveId = Guid.NewGuid(), FirstData = 9998, SecondData = 2 };
            _trackedQuest = new CharacterQuest
            {
                QuestId = 7525,
                Quest = new Quest { QuestId = 7525, QuestType = QuestType.DefeatEnemies, QuestObjectives = [objective] },
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), _trackedQuest);
        }

        private Task DefeatingAnEnemyPlayer() => Service.OnEnemyPlayerDefeatedAsync(Session.Character);

        private void CharacterHasADefeatQuestNeedingTwoKillsInATimeSpace()
        {
            var objective = new QuestObjectiveDto { QuestObjectiveId = Guid.NewGuid(), FirstData = 9999, SecondData = 2 };
            _trackedQuest = new CharacterQuest
            {
                QuestId = 7500,
                Quest = new Quest { QuestId = 7500, QuestType = QuestType.DefeatEnemies, QuestObjectives = [objective] },
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), _trackedQuest);
        }

        private void TheCharacterIsInATimeSpace() =>
            Session.Character.MapInstance.MapInstanceType = NosCore.Data.Enumerations.Map.MapInstanceType.TimeSpaceInstance;

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
                CompletedOn = World.Clock.GetCurrentInstant()
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
                CompletedOn = World.Clock.GetCurrentInstant().Minus(NodaTime.Duration.FromDays(2))
            });
        }

        private void DailyQuestCompletedHoursAgo()
        {
            DailyQuestCompletedYesterday();
            Session.Character.Quests.Values.Single(q => q.QuestId == 2).CompletedOn =
                World.Clock.GetCurrentInstant().Minus(NodaTime.Duration.FromHours(3));
        }

        private void DailyQuestShouldBeHeldOnce()
        {
            Assert.AreEqual(1, Session.Character.Quests.Values.Count(q => q.QuestId == 2));
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
            AddQuestResult = await Service.AddQuestAsync(Session.Character, QuestActionType.Validate, 10);
        }

        private void FiveMainQuestsAreOpen()
        {
            for (byte slot = 0; slot < 5; slot++)
            {
                Session.Character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
                {
                    QuestId = (short)(100 + slot),
                    Slot = slot,
                    Quest = new Quest { QuestId = (short)(100 + slot), QuestType = QuestType.Hunt, QuestObjectives = new List<QuestObjectiveDto>() },
                });
            }
        }

        private void TwoMainQuestsShouldBeOpenInTheirOwnSlots()
        {
            Assert.IsTrue(AddQuestResult);
            CollectionAssert.AreEquivalent(new byte[] { 0, 1 }, Session.Character.Quests.Values.Select(q => q.Slot).ToArray());
        }

        private void OnlyTheNewQuestShouldShowItsDialog()
        {
            var list = Session.LastPackets.OfType<NosCore.Packets.ServerPackets.Quest.QstlistPacket>().Last().QuestSubPackets;
            Assert.IsTrue(list.Single(q => q?.QuestId == 10)?.QuestToShowInfo);
            Assert.IsFalse(list.Single(q => q?.QuestId == 5)?.QuestToShowInfo);
        }

        private void TheMainQuestShouldNotBeAdded()
        {
            Assert.IsFalse(Session.Character.Quests.Values.Any(q => q.QuestId == 10));
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

        private CharacterQuest _trackedQuest;
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

        [TestMethod]
        public async Task GivingUpAQuestRemovesItAndClosesItsWindow()
        {
            await new Spec("Giving up a quest removes it and sends its closed qsti")
                .Given(NonSecondaryQuestAlreadyActive)
                .WhenAsync(GivingUpQuest_, 5)
                .Then(CharacterShouldHaveNoQuest)
                .And(AClosedQstiShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GivingUpAQuestTheCharacterDoesNotHaveDoesNothing()
        {
            await new Spec("Giving up a quest the character does not have changes nothing")
                .Given(NonSecondaryQuestAlreadyActive)
                .WhenAsync(GivingUpQuest_, 999)
                .Then(QuestShouldStillBeOne)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidatingTheCurrentStepMovesToTheNextOne()
        {
            await new Spec("Validating the current script step moves the character to the next step")
                .Given(CharacterIsOnTalkStepOneOfScript_, (byte)100)
                .WhenAsync(RunningTheCurrentStepAs_, QuestActionType.Validate)
                .Then(CharacterShouldBeOnStep_OfScript_, (short)2, (byte)100)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheClientAcknowledgingATargetStepMovesToTheNextOne()
        {
            await new Spec("The client acknowledging a target step moves the character to the next step")
                .Given(CharacterIsOn_StepOneOfScript_, "target", (byte)100)
                .WhenAsync(TheClientAcknowledgesStep_OfScript_, (short)1, (byte)100)
                .Then(CharacterShouldBeOnStep_OfScript_, (short)2, (byte)100)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheClientAcknowledgingAMoveStepMovesToTheNextOne()
        {
            await new Spec("The client acknowledging a move step moves the character to the next step")
                .Given(CharacterIsOn_StepOneOfScript_, "move", (byte)100)
                .WhenAsync(TheClientAcknowledgesStep_OfScript_, (short)1, (byte)100)
                .Then(CharacterShouldBeOnStep_OfScript_, (short)2, (byte)100)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RunningTheCurrentStepWithoutAScriptDoesNothing()
        {
            await new Spec("Running the current step of a character with no script sends nothing")
                .Given(CharacterHasNoScript)
                .WhenAsync(RunningTheCurrentStepAs_, QuestActionType.Achieve)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private Task GivingUpQuest_(int questId)
        {
            Session.LastPackets.Clear();
            return Service.GiveUpQuestAsync(Session.Character, questId);
        }

        private void CharacterShouldHaveNoQuest()
        {
            Assert.AreEqual(0, Session.Character.Quests.Count);
        }

        private void AClosedQstiShouldBeSent()
        {
            Assert.AreEqual(1, Session.LastPackets.OfType<NosCore.Packets.ServerPackets.Quest.QstiPacket>().Count());
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void CharacterIsOnTalkStepOneOfScript_(byte scriptId)
        {
            var current = new ScriptDto { Id = Guid.NewGuid(), ScriptId = scriptId, ScriptStepId = 1, StepType = "talk" };
            Scripts.Add(current);
            Scripts.Add(new ScriptDto { ScriptId = scriptId, ScriptStepId = 2, StepType = "talk" });
            Session.Character.Script = current;
        }

        private void CharacterIsOn_StepOneOfScript_(string stepType, byte scriptId)
        {
            var current = new ScriptDto { Id = Guid.NewGuid(), ScriptId = scriptId, ScriptStepId = 1, StepType = stepType };
            Scripts.Add(current);
            Scripts.Add(new ScriptDto { ScriptId = scriptId, ScriptStepId = 2, StepType = "talk" });
            Session.Character.Script = current;
        }

        private Task TheClientAcknowledgesStep_OfScript_(short stepId, byte scriptId)
        {
            return Service.RunScriptAsync(Session.Character, new ScriptClientPacket
            {
                Type = QuestActionType.Dialog,
                FirstArgument = scriptId,
                SecondArgument = stepId,
            });
        }

        private void CharacterHasNoScript()
        {
            Session.Character.Script = null;
            Session.LastPackets.Clear();
        }

        private Task RunningTheCurrentStepAs_(QuestActionType type)
        {
            return Service.RunCurrentScriptStepAsync(Session.Character, type);
        }

        private bool _offerResult;

        private void Npc_OffersChapter_FromLevel_(short npcVNum, byte scriptId, byte levelMinimum)
        {
            ScriptOffers.Add(new ScriptOfferDto
            {
                ScriptId = scriptId, NpcVNum = npcVNum, RequiredScriptId = (byte)(scriptId - 1), LevelMinimum = levelMinimum, DialogId = 42
            });
            Scripts.Add(new ScriptDto { Id = Guid.NewGuid(), ScriptId = scriptId, ScriptStepId = 10, StepType = "talk" });
        }

        private void CharacterIsOnTheLastStepOfChapterOne()
        {
            Scripts.Add(new ScriptDto { Id = Guid.NewGuid(), ScriptId = 1, ScriptStepId = 1, StepType = "talk" });
            var last = new ScriptDto { Id = Guid.NewGuid(), ScriptId = 1, ScriptStepId = 2, StepType = "talk" };
            Scripts.Add(last);
            Session.Character.Script = last;
        }

        private void CharacterIsBackOnStepOneOfChapterOne()
        {
            Session.Character.Script = Scripts.First(s => s.ScriptId == 1 && s.ScriptStepId == 1);
        }

        private async Task TakingChapter_FromNpc_(byte scriptId, short npcVNum)
        {
            _offerResult = await Service.TakeScriptOfferAsync(Session.Character, npcVNum, scriptId);
        }

        private void TheOfferResultShouldBe_(bool expected)
        {
            Assert.AreEqual(expected, _offerResult);
        }

        private void CharacterShouldBeOnStep_OfScript_(short stepId, byte scriptId)
        {
            Assert.AreEqual<byte?>(scriptId, Session.Character.Script?.ScriptId);
            Assert.AreEqual<short?>(stepId, Session.Character.Script?.ScriptStepId);
        }
    }
}
