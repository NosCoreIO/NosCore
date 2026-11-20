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
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class TakeQuestFromDialogHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IQuestService> QuestService;
        private readonly List<QuestDto> Quests =
        [
            new() { QuestId = 6864, QuestType = QuestType.WinRaid, LevelMin = 20, LevelMax = 99 },
            new() { QuestId = 5007, QuestType = QuestType.Hunt, LevelMin = 80, LevelMax = 99, RequiredQuestId = 5006 },
        ];

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            QuestService = World.Freeze<Mock<IQuestService>>();
            World.Inject(Quests);
            World.Inject(new List<NpcTalkButtonDto>
            {
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = QuestGiverDialog, Runner = NrunRunnerType.GetXQuest, Argument = 6864 },
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = QuestGiverDialog, Runner = NrunRunnerType.GetXQuest, Argument = 5007 },
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = QuestGiverDialog, Runner = NrunRunnerType.GetXQuest, Argument = 1 },
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = QuestGiverDialog, Runner = NrunRunnerType.GetXSPQuest, Argument = 6864 },
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = QuestGiverDialog, Runner = NrunRunnerType.GetXSPQuest, Argument = 5007 },
            });
            World.UseReal<INpcTalkButtonCatalog, NpcTalkButtonCatalog>();
            QuestGiver = NpcWithDialog(1, QuestGiverDialog);
        }

        private const short QuestGiverDialog = 11;

        private NpcComponentBundle QuestGiver;

        private NpcComponentBundle NpcWithDialog(int npcId, short dialog)
        {
            var map = World.GetBaseMap(TestWorld.TestMapId);
            return new NpcComponentBundle(map.EcsWorld.CreateNpc(npcId, new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 10 }, map,
                1, 1, 0, 1, 1, false, false, dialog, 0, 0, null), map.EcsWorld);
        }

        private Task TakeAsync(short questId) => TakeFromAsync(QuestGiver, questId);

        private Task TakeFromAsync(NpcComponentBundle npc, short questId) => World.Create<TakeQuestFromDialogHandler>()
            .HandleAsync(Session, npc, new NrunPacket { Runner = NrunRunnerType.GetXQuest, Type = questId });

        [TestMethod]
        public async Task TheDialogButtonHandsOutItsQuest()
        {
            await TakeAsync(6864);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), QuestActionType.Achieve, 6864), Times.Once);
        }

        [TestMethod]
        public async Task TheSpecialistQuestButtonHandsOutItsQuest()
        {
            await World.Create<TakeSpecialistQuestFromDialogHandler>()
                .HandleAsync(Session, QuestGiver, new NrunPacket { Runner = NrunRunnerType.GetXSPQuest, Type = 6864 });

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), QuestActionType.Achieve, 6864), Times.Once);
        }

        [TestMethod]
        public async Task TheSpecialistQuestButtonWaitsForTheQuestBeforeIt()
        {
            await World.Create<TakeSpecialistQuestFromDialogHandler>()
                .HandleAsync(Session, QuestGiver, new NrunPacket { Runner = NrunRunnerType.GetXSPQuest, Type = 5007 });

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task AnNpcWhoseDialogDoesNotOfferTheQuestHandsOutNothing()
        {
            await TakeFromAsync(NpcWithDialog(2, 12), 6864);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task TheSpecialistQuestButtonOfAnotherNpcHandsOutNothing()
        {
            await World.Create<TakeSpecialistQuestFromDialogHandler>()
                .HandleAsync(Session, NpcWithDialog(2, 12), new NrunPacket { Runner = NrunRunnerType.GetXSPQuest, Type = 6864 });

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task AnUnknownQuestIsIgnored()
        {
            await TakeAsync(1);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task AQuestWaitsForTheOneBeforeIt()
        {
            await TakeAsync(5007);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task AQuestIsHandedOutOnceTheOneBeforeItIsDone()
        {
            Session.Character.Quests.TryAdd(Guid.NewGuid(), new CharacterQuest
            {
                QuestId = 5006,
                CompletedOn = Instant.FromUnixTimeSeconds(1),
                Quest = new GameObject.Services.QuestService.Quest { QuestId = 5006, QuestObjectives = [] },
            });

            await TakeAsync(5007);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), QuestActionType.Achieve, 5007), Times.Once);
        }
    }
}
