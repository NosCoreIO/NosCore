//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        private QtPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IQuestService> QuestService = null!;
        private const short QuestId = 1001;
        private long CharacterId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            CharacterId = Session.Character.VisualId;
            QuestService = new Mock<IQuestService>();
            Handler = new QtPacketHandler(QuestService.Object);
        }

        [TestMethod]
        public async Task ValidatingRunsTheCurrentScriptStep()
        {
            await new Spec("Validating runs the current script step as a validation")
                .WhenAsync(SendingQt_, QuestActionType.Validate)
                .Then(TheCurrentStepShouldRunAs_, QuestActionType.Validate)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AchievingRunsTheCurrentScriptStep()
        {
            await new Spec("Achieving runs the current script step as an achievement")
                .WhenAsync(SendingQt_, QuestActionType.Achieve)
                .Then(TheCurrentStepShouldRunAs_, QuestActionType.Achieve)
                .ExecuteAsync();
        }

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

        private void TheCurrentStepShouldRunAs_(QuestActionType type)
        {
            QuestService.Verify(x => x.RunCurrentScriptStepAsync(It.Is<ICharacterEntity>(c => c.VisualId == CharacterId), type), Times.Once);
            QuestService.Verify(x => x.GiveUpQuestAsync(It.IsAny<ICharacterEntity>(), It.IsAny<int>()), Times.Never);
        }

        private void TheQuestShouldBeGivenUp()
        {
            QuestService.Verify(x => x.GiveUpQuestAsync(It.Is<ICharacterEntity>(c => c.VisualId == CharacterId), QuestId), Times.Once);
        }

        private void NoScriptStepShouldRun()
        {
            QuestService.Verify(x => x.RunCurrentScriptStepAsync(It.IsAny<ICharacterEntity>(), It.IsAny<QuestActionType>()), Times.Never);
        }
    }
}
