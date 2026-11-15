//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class DialogQuestOfferHandlerTests
    {
        private const NrunRunnerType Engineer = NrunRunnerType.GetDailyQuestAkamurMerchantMilitaryEngineerEliminateMonsters;

        private TestWorld World;
        private ClientSession Session;
        private Mock<IQuestService> QuestService;
        private Mock<IRandomProvider> Random;
        private readonly List<QuestDto> Quests =
        [
            new() { QuestId = 5515, IsDaily = true, LevelMin = 81, LevelMax = 99, OfferedBy = Engineer },
            new() { QuestId = 5905, IsDaily = true, LevelMin = 85, LevelMax = 99, OfferedBy = Engineer },
            new() { QuestId = 5906, IsDaily = true, LevelMin = 85, LevelMax = 99, OfferedBy = Engineer },
            new() { QuestId = 5514, IsDaily = true, LevelMin = 81, LevelMax = 99,
                OfferedBy = NrunRunnerType.GetDailyQuestSerizardIceFlowerOil },
        ];

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            QuestService = World.Freeze<Mock<IQuestService>>();
            Random = World.Freeze<Mock<IRandomProvider>>();
            Random.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns((int min, int max) => max - 1);
            World.Inject(Quests);
            Session.Character.Level = 90;
        }

        private Task AskAsync() => World.Create<BurningSwordMonstersDailyQuestHandler>()
            .HandleAsync(Session, null, new NrunPacket { Runner = Engineer });

        private void Holds(short questId, Instant? completedOn) =>
            Session.Character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
            {
                QuestId = questId,
                CompletedOn = completedOn,
                Quest = new GameObject.Services.QuestService.Quest
                {
                    QuestId = questId, IsDaily = true, OfferedBy = Engineer, QuestObjectives = []
                },
            });

        private void Offered(short questId) =>
            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), QuestActionType.Achieve, questId), Times.Once);

        [TestMethod]
        public async Task TheNpcHandsOutOneOfItsOwnQuests()
        {
            await AskAsync();

            Offered(5906);
        }

        [TestMethod]
        public async Task OnlyTheQuestsTheLevelAllowsAreDrawn()
        {
            Session.Character.Level = 82;

            await AskAsync();

            Offered(5515);
        }

        [TestMethod]
        public async Task AQuestDoneTodayStandsForTheWholeNpc()
        {
            Holds(5905, World.Clock.GetCurrentInstant() - Duration.FromHours(1));

            await AskAsync();

            Offered(5905);
        }

        [TestMethod]
        public async Task AQuestStillHeldStandsForTheWholeNpc()
        {
            Holds(5515, null);

            await AskAsync();

            Offered(5515);
        }

        [TestMethod]
        public async Task TheNextDayTheNpcDrawsAgain()
        {
            Holds(5905, World.Clock.GetCurrentInstant() - Duration.FromHours(25));

            await AskAsync();

            Offered(5906);
        }

        [TestMethod]
        public async Task ATooLowPlayerIsStillAnsweredByTheQuestService()
        {
            Session.Character.Level = 50;

            await AskAsync();

            Offered(5515);
        }
    }
}
