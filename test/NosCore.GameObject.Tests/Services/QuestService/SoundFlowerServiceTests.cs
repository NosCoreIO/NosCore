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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.QuestService
{
    [TestClass]
    public class SoundFlowerServiceTests
    {
        private const short FlowerQuestId = 5981;

        private TestWorld World;
        private ClientSession Session;
        private Mock<IQuestService> Quests;
        private readonly List<QuestDto> QuestList = new()
        {
            new QuestDto { QuestId = 1, QuestType = QuestType.Hunt },
        };
        private bool Started;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Quests = World.Freeze<Mock<IQuestService>>();
            World.Inject(QuestList);
        }

        [TestMethod]
        public async Task AFlowerQuestIsStarted()
        {
            await new Spec("A Sound Flower starts the flower quest")
                .Given(TheQuestDataHasAFlowerQuest)
                .And(TheQuestCanBeTaken_, true)
                .WhenAsync(StartingTheFlowerQuest)
                .Then(TheFlowerQuestIsStarted)
                .And(TheFlowerIsSpent_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AQuestTheCharacterCannotTakeKeepsTheFlower()
        {
            await new Spec("A flower quest the character cannot take, for want of level or because it is held, leaves the flower")
                .Given(TheQuestDataHasAFlowerQuest)
                .And(TheQuestCanBeTaken_, false)
                .WhenAsync(StartingTheFlowerQuest)
                .Then(TheFlowerIsSpent_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutAFlowerQuestNothingStarts()
        {
            await new Spec("Without a flower quest in the quest data no quest starts")
                .Given(TheQuestCanBeTaken_, true)
                .WhenAsync(StartingTheFlowerQuest)
                .Then(NoQuestIsStarted)
                .And(TheFlowerIsSpent_, false)
                .ExecuteAsync();
        }

        private void TheQuestDataHasAFlowerQuest() =>
            QuestList.Add(new QuestDto { QuestId = FlowerQuestId, QuestType = QuestType.FlowerQuest });

        private void TheQuestCanBeTaken_(bool taken) =>
            Quests.Setup(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), QuestActionType.Achieve, FlowerQuestId))
                .ReturnsAsync(taken);

        private async Task StartingTheFlowerQuest() =>
            Started = await World.Create<SoundFlowerService>().StartFlowerQuestAsync(Session.Character);

        private void TheFlowerQuestIsStarted() =>
            Quests.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), QuestActionType.Achieve, FlowerQuestId), Times.Once);

        private void NoQuestIsStarted() =>
            Quests.Verify(q => q.AddQuestAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>(), It.IsAny<short>()), Times.Never);

        private void TheFlowerIsSpent_(bool spent) => Assert.AreEqual(spent, Started);
    }
}
