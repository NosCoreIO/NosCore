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
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class DisconnectDataMessageHandlerTests
    {
        private DisconnectDataMessageChannelCommunicationMessageHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<ISessionRegistry> SessionRegistry = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            SessionRegistry = new Mock<ISessionRegistry>();
            Handler = new DisconnectDataMessageChannelCommunicationMessageHandler(SessionRegistry.Object);
        }

        [TestMethod]
        public async Task HandleShouldDisconnectWhenCharacterFound()
        {
            await new Spec("Handle should disconnect when character found")
                .Given(CharacterIsRegistered)
                .WhenAsync(HandlingDisconnectData)
                .Then(ShouldDisconnectCharacter)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldDoNothingWhenCharacterNotFound()
        {
            await new Spec("Handle should do nothing when character not found")
                .Given(CharacterIsNotRegistered)
                .WhenAsync(HandlingDisconnectData)
                .Then(ShouldNotAttemptDisconnect)
                .ExecuteAsync();
        }

        private void CharacterIsRegistered()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<ICharacterEntity, bool>>()))
                .Returns(Session.Character);
        }

        private void CharacterIsNotRegistered()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<ICharacterEntity, bool>>()))
                .Returns((ICharacterEntity?)null);
        }

        private async Task HandlingDisconnectData()
        {
            await Handler.Handle(new DisconnectData
            {
                CharacterId = Session.Character.CharacterId
            });
        }

        private void ShouldDisconnectCharacter()
        {
            SessionRegistry.Verify(x => x.DisconnectByCharacterIdAsync(Session.Character.CharacterId), Times.Once);
        }

        private void ShouldNotAttemptDisconnect()
        {
            SessionRegistry.Verify(x => x.DisconnectByCharacterIdAsync(It.IsAny<long>()), Times.Never);
        }
    }
}
