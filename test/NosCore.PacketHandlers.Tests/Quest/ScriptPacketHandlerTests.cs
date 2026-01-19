//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Threading.Tasks;
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
