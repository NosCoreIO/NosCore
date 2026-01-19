//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
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
    public class ScriptPacketHandlerTests
    {
        private ScriptPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IQuestService> QuestService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            QuestService = new Mock<IQuestService>();

            Handler = new ScriptPacketHandler(QuestService.Object);
        }

        [TestMethod]
        public async Task RunningScriptShouldCallQuestService()
        {
            await new Spec("Running script should call quest service")
                .Given(CharacterIsOnMap)
                .WhenAsync(RunningScript)
                .Then(QuestServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RunningScriptWithValidateActionShouldCallQuestService()
        {
            await new Spec("Running script with validate action should call quest service")
                .Given(CharacterIsOnMap)
                .WhenAsync(RunningScriptWithValidateAction)
                .Then(QuestServiceShouldBeCalled)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task RunningScript()
        {
            await Handler.ExecuteAsync(new ScriptClientPacket
            {
                Type = QuestActionType.Achieve,
                FirstArgument = 1,
                SecondArgument = 100,
                ThirdArgument = 1
            }, Session);
        }

        private async Task RunningScriptWithValidateAction()
        {
            await Handler.ExecuteAsync(new ScriptClientPacket
            {
                Type = QuestActionType.Validate,
                FirstArgument = 1,
                SecondArgument = 100,
                ThirdArgument = 1
            }, Session);
        }

        private void QuestServiceShouldBeCalled()
        {
            QuestService.Verify(x => x.RunScriptAsync(
                Session.Character,
                It.IsAny<ScriptClientPacket>()), Times.Once);
        }
    }
}
