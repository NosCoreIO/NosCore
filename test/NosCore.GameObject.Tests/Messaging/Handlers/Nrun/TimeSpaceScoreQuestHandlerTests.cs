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
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class TimeSpaceScoreQuestHandlerTests
    {
        private const NrunRunnerType Score = NrunRunnerType.GetQuestHigherTimeSpaceScore;
        private const short ScoreDialog = 54;
        private const short OtherDialog = 1;

        private TestWorld World;
        private ClientSession Session;
        private Mock<IQuestService> QuestService;
        private readonly List<QuestDto> Quests =
        [
            new() { QuestId = 900, QuestType = QuestType.TsPoint, LevelMin = 1, LevelMax = 10, OfferedBy = Score, OfferOption = 0 },
            new() { QuestId = 901, QuestType = QuestType.TsPoint, LevelMin = 11, LevelMax = 20, OfferedBy = Score, OfferOption = 1 },
            new() { QuestId = 5514, LevelMin = 81, LevelMax = 99, OfferedBy = NrunRunnerType.GetDailyQuestSerizardIceFlowerOil },
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
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = ScoreDialog, Runner = Score, Argument = 0 },
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = ScoreDialog, Runner = Score, Argument = 1 },
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = ScoreDialog, Runner = Score, Argument = 2 },
                new() { NpcTalkButtonId = Guid.NewGuid(), DialogId = OtherDialog, Runner = NrunRunnerType.OpenProduction, Argument = 0 },
            });
            World.UseReal<INpcTalkButtonCatalog, NpcTalkButtonCatalog>();
            ScoreKeeper = NpcWithDialog(1, ScoreDialog);
        }

        private NpcComponentBundle ScoreKeeper;

        private NpcComponentBundle NpcWithDialog(int npcId, short dialog)
        {
            var map = World.GetBaseMap(TestWorld.TestMapId);
            return new NpcComponentBundle(map.EcsWorld.CreateNpc(npcId, new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 10 }, map,
                1, 1, 0, 1, 1, false, false, dialog, 0, 0, null), map.EcsWorld);
        }

        private Task PickAsync(short? option) => PickFromAsync(ScoreKeeper, option);

        private Task PickFromAsync(NpcComponentBundle npc, short? option) => World.Create<TimeSpaceScoreQuestHandler>()
            .HandleAsync(Session, npc, new NrunPacket { Runner = Score, Type = option, VisualType = VisualType.Npc, VisualId = npc.VisualId });

        [TestMethod]
        public async Task EachButtonHandsOutTheQuestOfItsLevelRange()
        {
            await PickAsync(1);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), QuestActionType.Achieve, 901), Times.Once);
            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), 900), Times.Never);
        }

        [TestMethod]
        public async Task AButtonWithoutAQuestIsIgnored()
        {
            await PickAsync(2);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task AnNpcWhoseDialogHasNoScoreButtonsHandsOutNothing()
        {
            await PickFromAsync(NpcWithDialog(2, OtherDialog), 1);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task AButtonTheDialogDoesNotShowHandsOutNothing()
        {
            Quests.Add(new QuestDto { QuestId = 913, QuestType = QuestType.TsPoint, LevelMin = 1, LevelMax = 99, OfferedBy = Score, OfferOption = 13 });

            await PickAsync(13);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }

        [TestMethod]
        public async Task AButtonWithoutAnArgumentIsIgnored()
        {
            await PickAsync(null);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }
    }
}
