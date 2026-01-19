//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
using System.Threading.Tasks;

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
