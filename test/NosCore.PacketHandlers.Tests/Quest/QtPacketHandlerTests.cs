//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs;
using Moq;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.PacketHandlers.Quest;
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Quest
{
    [TestClass]
    public class QtPacketHandlerTests
    {
        private TestWorld World;
        private QtPacketHandler Handler;
        private ClientSession Session;
        private Mock<IQuestService> QuestService;
        private const short QuestId = 1001;
        private long CharacterId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            CharacterId = Session.Character.VisualId;
            QuestService = World.Freeze<Mock<IQuestService>>();
            Handler = World.Create<QtPacketHandler>();
        }

        [TestMethod]
        public async Task ValidatingClaimsTheQuestInThePacketsSlot()
        {
            await new Spec("Validating claims the quest in the quest window slot the packet names")
                .WhenAsync(SendingQt_, QuestActionType.Validate)
                .Then(TheQuestInSlot_ShouldBeClaimed, QuestId)
                .And(NoScriptStepShouldRun)
                .ExecuteAsync();
        }

        private void TheQuestInSlot_ShouldBeClaimed(short slot) =>
            QuestService.Verify(x => x.ClaimQuestAsync(It.Is<PlayerComponentBundle>(c => c.VisualId == CharacterId), slot), Times.Once);

        [TestMethod]
        public async Task AchievingAchievesThePacketsQuest()
        {
            await new Spec("Achieving (qt 1) achieves the quest the packet names")
                .WhenAsync(SendingQt_, QuestActionType.Achieve)
                .Then(TheQuestShouldBeAchieved)
                .And(NoScriptStepShouldRun)
                .ExecuteAsync();
        }

        private void TheQuestShouldBeAchieved() =>
            QuestService.Verify(x => x.AchieveQuestAsync(It.Is<PlayerComponentBundle>(c => c.VisualId == CharacterId), QuestId), Times.Once);

        [TestMethod]
        public async Task GivingUpGivesUpThePacketsQuest()
        {
            await new Spec("Giving up gives up the quest named by the packet")
                .WhenAsync(SendingQt_, QuestActionType.GiveUp)
                .Then(TheQuestShouldBeGivenUp)
                .And(NoScriptStepShouldRun)
                .ExecuteAsync();
        }

        private Task SendingQt_(QuestActionType type)
        {
            return Handler.ExecuteAsync(new QtPacket { Type = type, Data = QuestId }, Session);
        }

        private void TheQuestShouldBeGivenUp()
        {
            QuestService.Verify(x => x.GiveUpQuestAsync(It.Is<PlayerComponentBundle>(c => c.VisualId == CharacterId), QuestId), Times.Once);
        }

        private void NoScriptStepShouldRun()
        {
            QuestService.Verify(x => x.RunCurrentScriptStepAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<QuestActionType>()), Times.Never);
        }
    }
}
