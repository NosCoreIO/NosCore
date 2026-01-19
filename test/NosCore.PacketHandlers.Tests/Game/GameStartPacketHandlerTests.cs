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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.SkillService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class GameStartPacketHandlerTests
    {
        private GameStartPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IMapChangeService> MapChangeService = null!;
        private Mock<ISkillService> SkillService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            var friendHub = new Mock<IFriendHub>();
            var channelHub = new Mock<IChannelHub>();
            var pubSubHub = new Mock<IPubSubHub>();
            var blacklistHub = new Mock<IBlacklistHub>();
            var mailHub = new Mock<IMailHub>();
            var questService = new Mock<IQuestService>();
            MapChangeService = new Mock<IMapChangeService>();
            SkillService = new Mock<ISkillService>();
            var serializer = new Mock<ISerializer>();

            friendHub.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            blacklistHub.Setup(s => s.GetBlacklistedAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            mailHub.Setup(s => s.GetMails(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<MailData>());

            Handler = new GameStartPacketHandler(
                TestHelpers.Instance.WorldConfiguration,
                friendHub.Object,
                channelHub.Object,
                pubSubHub.Object,
                blacklistHub.Object,
                serializer.Object,
                mailHub.Object,
                questService.Object,
                MapChangeService.Object,
                SkillService.Object);
        }

        [TestMethod]
        public async Task GameStartWhenAlreadyStartedShouldDoNothing()
        {
            await new Spec("Game start when already started should do nothing")
                .Given(GameAlreadyStarted)
                .WhenAsync(ExecutingGameStart)
                .Then(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWithoutSelectedCharacterShouldDoNothing()
        {
            await new Spec("Game start without selected character should do nothing")
                .Given(NoCharacterSelected)
                .WhenAsync(ExecutingGameStart)
                .Then(GameStartedShouldBeFalse)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWithValidCharacterShouldSetGameStartedFlag()
        {
            await new Spec("Game start with valid character should set game started flag")
                .Given(CharacterIsReadyToStart)
                .WhenAsync(ExecutingGameStart)
                .Then(GameStartedShouldBeTrue)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWithPositiveHpShouldCallMapChange()
        {
            await new Spec("Game start with positive HP should call map change")
                .Given(CharacterIsReadyToStart)
                .And(CharacterHasPositiveHp)
                .WhenAsync(ExecutingGameStart)
                .Then(MapChangeShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartShouldLoadSkills()
        {
            await new Spec("Game start should load skills")
                .Given(CharacterIsReadyToStart)
                .WhenAsync(ExecutingGameStart)
                .Then(SkillServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameStartWithZeroHpShouldNotCallMapChange()
        {
            await new Spec("Game start with zero HP should not call map change")
                .Given(CharacterIsReadyToStart)
                .And(CharacterHasZeroHp)
                .WhenAsync(ExecutingGameStart)
                .Then(MapChangeShouldNotBeCalled)
                .ExecuteAsync();
        }

        private void GameAlreadyStarted()
        {
            Session.GameStarted = true;
        }

        private void NoCharacterSelected()
        {
            Session.GameStarted = false;
            Session.HasSelectedCharacter = false;
        }

        private void CharacterIsReadyToStart()
        {
            Session.GameStarted = false;
            Session.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>();
        }

        private void CharacterHasPositiveHp()
        {
            Session.Character.Hp = 100;
        }

        private void CharacterHasZeroHp()
        {
            Session.Character.Hp = 0;
        }

        private async Task ExecutingGameStart()
        {
            await Handler.ExecuteAsync(new GameStartPacket(), Session);
        }

        private void GameStartedShouldBeFalse()
        {
            Assert.IsFalse(Session.GameStarted);
        }

        private void GameStartedShouldBeTrue()
        {
            Assert.IsTrue(Session.GameStarted);
        }

        private void MapChangeShouldNotBeCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapAsync(It.IsAny<ClientSession>()), Times.Never);
        }

        private void MapChangeShouldBeCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapAsync(It.IsAny<ClientSession>()), Times.Once);
        }

        private void SkillServiceShouldBeCalled()
        {
            SkillService.Verify(x => x.LoadSkill(It.IsAny<ICharacterEntity>()), Times.Once);
        }
    }
}
