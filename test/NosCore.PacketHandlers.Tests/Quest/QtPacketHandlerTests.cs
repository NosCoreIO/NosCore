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

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
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
    public class QtPacketHandlerTests
    {
        private QtPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IQuestService> QuestService = null!;
        private Guid QuestGuid;
        private short QuestId = 1001;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            QuestService = new Mock<IQuestService>();
            QuestGuid = Guid.NewGuid();

            Handler = new QtPacketHandler(QuestService.Object);
        }

        [TestMethod]
        public async Task UnknownQuestShouldBeIgnored()
        {
            await new Spec("Unknown quest should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(HandlingUnknownQuest)
                .Then(QuestServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidatingQuestShouldCallQuestService()
        {
            await new Spec("Validating quest should call quest service")
                .Given(CharacterIsOnMap)
                .And(CharacterHasQuest)
                .And(CharacterHasScript)
                .WhenAsync(ValidatingQuest)
                .Then(QuestServiceShouldBeCalledWithValidate)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AchievingQuestShouldCallQuestService()
        {
            await new Spec("Achieving quest should call quest service")
                .Given(CharacterIsOnMap)
                .And(CharacterHasQuest)
                .And(CharacterHasScript)
                .WhenAsync(AchievingQuest)
                .Then(QuestServiceShouldBeCalledWithAchieve)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GivingUpQuestShouldRemoveQuestFromCharacter()
        {
            await new Spec("Giving up quest should remove quest from character")
                .Given(CharacterIsOnMap)
                .And(CharacterHasQuest)
                .WhenAsync(GivingUpQuest)
                .Then(QuestShouldBeRemoved)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Session.Character.Quests = new System.Collections.Concurrent.ConcurrentDictionary<Guid, CharacterQuest>();
        }

        private void CharacterHasQuest()
        {
            var characterQuest = new CharacterQuest
            {
                QuestId = QuestId,
                Quest = new NosCore.GameObject.Services.QuestService.Quest
                {
                    QuestId = QuestId,
                    QuestObjectives = new System.Collections.Generic.List<QuestObjectiveDto>()
                }
            };
            Session.Character.Quests.TryAdd(QuestGuid, characterQuest);
        }

        private void CharacterHasScript()
        {
            Session.Character.Script = new ScriptDto
            {
                Argument1 = 1,
                ScriptId = 100,
                ScriptStepId = 1
            };
        }

        private async Task HandlingUnknownQuest()
        {
            await Handler.ExecuteAsync(new QtPacket
            {
                Type = QuestActionType.Validate,
                Data = 9999
            }, Session);
        }

        private async Task ValidatingQuest()
        {
            await Handler.ExecuteAsync(new QtPacket
            {
                Type = QuestActionType.Validate,
                Data = QuestId
            }, Session);
        }

        private async Task AchievingQuest()
        {
            await Handler.ExecuteAsync(new QtPacket
            {
                Type = QuestActionType.Achieve,
                Data = QuestId
            }, Session);
        }

        private async Task GivingUpQuest()
        {
            await Handler.ExecuteAsync(new QtPacket
            {
                Type = QuestActionType.GiveUp,
                Data = QuestId
            }, Session);
        }

        private void QuestServiceShouldNotBeCalled()
        {
            QuestService.Verify(x => x.RunScriptAsync(
                It.IsAny<NosCore.GameObject.ComponentEntities.Entities.Character>(),
                It.IsAny<ScriptClientPacket>()), Times.Never);
        }

        private void QuestServiceShouldBeCalledWithValidate()
        {
            QuestService.Verify(x => x.RunScriptAsync(
                It.IsAny<NosCore.GameObject.ComponentEntities.Entities.Character>(),
                It.Is<ScriptClientPacket>(p => p != null && p.Type == QuestActionType.Validate)), Times.Once);
        }

        private void QuestServiceShouldBeCalledWithAchieve()
        {
            QuestService.Verify(x => x.RunScriptAsync(
                It.IsAny<NosCore.GameObject.ComponentEntities.Entities.Character>(),
                It.Is<ScriptClientPacket>(p => p != null && p.Type == QuestActionType.Achieve)), Times.Once);
        }

        private void QuestShouldBeRemoved()
        {
            Assert.AreEqual(0, Session.Character.Quests.Count);
        }
    }
}
