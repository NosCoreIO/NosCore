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
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class DialogQuestOfferHandlerTests
    {
        private const NrunRunnerType Engineer = NrunRunnerType.GetDailyQuestAkamurMerchantMilitaryEngineerEliminateMonsters;
        private const short EngineerDialog = 300;
        private const short EngineerQuestDialog = 301;
        private const short DiggerDialog = 9700;
        private const short OtherDialog = 1;

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
            new() { QuestId = 6036, LevelMin = 20, LevelMax = 99, OfferedBy = NrunRunnerType.GetQuestGingsengDagger },
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
            World.Inject(new List<NpcTalkButtonDto>
            {
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = EngineerDialog, NextDialogId = EngineerQuestDialog },
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = EngineerQuestDialog, Runner = Engineer },
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = DiggerDialog, Runner = NrunRunnerType.GetQuestGingsengDagger },
            });
            World.UseReal<INpcTalkButtonCatalog, NpcTalkButtonCatalog>();
            Session.Character.Level = 90;
        }

        private NpcComponentBundle NpcWithDialog(int npcId, short dialog)
        {
            var map = World.GetBaseMap(TestWorld.TestMapId);
            return new NpcComponentBundle(map.EcsWorld.CreateNpc(npcId, new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 10 }, map,
                1, 1, 0, 1, 1, false, false, dialog, 0, 0, null), map.EcsWorld);
        }

        private Task AskAsync() => AskAsync(NpcWithDialog(1, EngineerDialog));

        private Task AskAsync(NpcComponentBundle npc) => World.Create<BurningSwordMonstersDailyQuestHandler>()
            .HandleAsync(Session, npc, new NrunPacket { Runner = Engineer, VisualType = VisualType.Npc, VisualId = npc.VisualId });

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
        public async Task AStoryButtonHandsOutItsOneQuest()
        {
            await World.Create<GinsengDiggerQuestHandler>()
                .HandleAsync(Session, NpcWithDialog(2, DiggerDialog), new NrunPacket { Runner = NrunRunnerType.GetQuestGingsengDagger });

            Offered(6036);
        }

        [TestMethod]
        public async Task AnNpcWhoseDialogDoesNotOfferTheButtonHandsOutNothing()
        {
            await World.Create<GinsengDiggerQuestHandler>()
                .HandleAsync(Session, NpcWithDialog(3, OtherDialog), new NrunPacket { Runner = NrunRunnerType.GetQuestGingsengDagger });

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task AnotherNpcsDailyButtonHandsOutNothing()
        {
            await AskAsync(NpcWithDialog(4, DiggerDialog));

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
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
