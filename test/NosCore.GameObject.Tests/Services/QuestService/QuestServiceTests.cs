//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.QuestService
{
    [TestClass]
    public class QuestServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
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
                Logger,
                TestHelpers.Instance.Clock,
                TestHelpers.Instance.LogLanguageLocalizer);
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
    }
}
