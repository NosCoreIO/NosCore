//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Quest;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.QuestService.Handlers;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.QuestService
{
    [TestClass]
    public class ScriptedInstanceQuestTests
    {
        private const byte TimeSpaceId = 4;
        private const byte OtherId = 5;

        private TestWorld World;
        private ClientSession Session;
        private OnScriptedInstanceSucceededHandler Handler;
        private CharacterQuest TrackedQuest;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(0);
            Session.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>();

            var handlers = new IQuestTypeHandler[]
            {
                World.Create<TimeSpaceQuestHandler>(),
                World.Create<WinRaidQuestHandler>(),
            };
            var questService = new GameObject.Services.QuestService.QuestService(
                new List<ScriptDto>(),
                new List<ScriptOfferDto>(),
                World.WorldConfiguration,
                new List<QuestDto>(),
                new List<QuestObjectiveDto>(),
                NullLogger<GameObject.Services.QuestService.QuestService>.Instance,
                World.Clock,
                World.LogLanguageLocalizer,
                handlers,
                new Mock<Wolverine.IMessageBus>().Object,
                new Mock<IQuestRewardService>().Object);
            Handler = new OnScriptedInstanceSucceededHandler(questService, handlers);
        }

        [TestMethod]
        public async Task WinningTheNamedTimeSpaceCompletesTheQuest()
        {
            await new Spec("Winning the named time-space completes a TimesSpace quest")
                .Given(CharacterHasQuest_For_Times, QuestType.TimesSpace, TimeSpaceId, 1)
                .WhenAsync(Winning_, ScriptedInstanceType.TimeSpace, TimeSpaceId)
                .Then(QuestShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WinningAnotherTimeSpaceIsIgnored()
        {
            await new Spec("Winning another time-space leaves a TimesSpace quest open")
                .Given(CharacterHasQuest_For_Times, QuestType.TimesSpace, TimeSpaceId, 1)
                .WhenAsync(Winning_, ScriptedInstanceType.TimeSpace, OtherId)
                .Then(QuestShouldBeOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARaidWithTheSameIdDoesNotCountAsATimeSpace()
        {
            await new Spec("A raid sharing the time-space id leaves a TimesSpace quest open")
                .Given(CharacterHasQuest_For_Times, QuestType.TimesSpace, TimeSpaceId, 1)
                .WhenAsync(Winning_, ScriptedInstanceType.Raid, TimeSpaceId)
                .Then(QuestShouldBeOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATimeSpaceToWinTwiceNeedsTwoWins()
        {
            await new Spec("A time-space to win twice stays open after one win and completes after the second")
                .Given(CharacterHasQuest_For_Times, QuestType.TimesSpace, TimeSpaceId, 2)
                .AndAsync(Winning_, ScriptedInstanceType.TimeSpace, TimeSpaceId)
                .And(QuestShouldBeOpen)
                .WhenAsync(Winning_, ScriptedInstanceType.TimeSpace, TimeSpaceId)
                .Then(QuestShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WinningTheNamedRaidCompletesTheQuest()
        {
            await new Spec("Winning the named raid completes a WinRaid quest")
                .Given(CharacterHasQuest_For_Times, QuestType.WinRaid, TimeSpaceId, 1)
                .WhenAsync(Winning_, ScriptedInstanceType.Raid, TimeSpaceId)
                .Then(QuestShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATimeSpaceWithTheSameIdDoesNotCountAsARaid()
        {
            await new Spec("A time-space sharing the raid id leaves a WinRaid quest open")
                .Given(CharacterHasQuest_For_Times, QuestType.WinRaid, TimeSpaceId, 1)
                .WhenAsync(Winning_, ScriptedInstanceType.TimeSpace, TimeSpaceId)
                .Then(QuestShouldBeOpen)
                .ExecuteAsync();
        }

        private void CharacterHasQuest_For_Times(QuestType type, byte instanceId, int times)
        {
            const short questId = 1500;
            TrackedQuest = new CharacterQuest
            {
                Id = Guid.NewGuid(),
                QuestId = questId,
                Quest = new Quest
                {
                    QuestId = questId,
                    QuestType = type,
                    QuestObjectives =
                    [
                        new QuestObjectiveDto { QuestObjectiveId = Guid.NewGuid(), QuestId = questId, FirstData = instanceId, SecondData = times }
                    ]
                }
            };
            Session.Character.Quests.TryAdd(TrackedQuest.Id, TrackedQuest);
        }

        private Task Winning_(ScriptedInstanceType type, byte instanceId) =>
            Handler.Handle(new ScriptedInstanceSucceededEvent(Session.Character, type, instanceId));

        private void QuestShouldBeComplete() => Assert.IsNotNull(TrackedQuest.ObjectivesCompletedOn);

        private void QuestShouldBeOpen()
        {
            Assert.IsNull(TrackedQuest.ObjectivesCompletedOn);
            Assert.IsNull(TrackedQuest.CompletedOn);
        }
    }
}
