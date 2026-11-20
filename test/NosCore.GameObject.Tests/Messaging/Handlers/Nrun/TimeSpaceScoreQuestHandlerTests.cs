//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class TimeSpaceScoreQuestHandlerTests
    {
        private const NrunRunnerType Score = NrunRunnerType.GetQuestHigherTimeSpaceScore;

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
        }

        private Task PickAsync(short? option) => World.Create<TimeSpaceScoreQuestHandler>()
            .HandleAsync(Session, null, new NrunPacket { Runner = Score, Type = option });

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
        public async Task AButtonWithoutAnArgumentIsIgnored()
        {
            await PickAsync(null);

            QuestService.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);
        }
    }
}
