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
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class DeleteMailDataMessageHandlerTests
    {
        private DeleteMailDataMessageChannelCommunicationMessageHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<ISessionRegistry> SessionRegistry = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            SessionRegistry = new Mock<ISessionRegistry>();
            Handler = new DeleteMailDataMessageChannelCommunicationMessageHandler(SessionRegistry.Object);
        }

        [TestMethod]
        public async Task HandleShouldSendPostPacketWhenCharacterFound()
        {
            await new Spec("Handle should send post packet when character found")
                .Given(CharacterIsRegistered)
                .WhenAsync(HandlingDeleteMailData)
                .Then(ShouldSendPostPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldDoNothingWhenCharacterNotFound()
        {
            await new Spec("Handle should do nothing when character not found")
                .Given(CharacterIsNotRegistered)
                .WhenAsync(HandlingDeleteMailData)
                .Then(ShouldNotSendAnyPacket)
                .ExecuteAsync();
        }

        private DeleteMailData MailData = null!;

        private void CharacterIsRegistered()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<ICharacterEntity, bool>>()))
                .Returns(Session.Character);
            Session.LastPackets.Clear();
        }

        private void CharacterIsNotRegistered()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<ICharacterEntity, bool>>()))
                .Returns((ICharacterEntity?)null);
            Session.LastPackets.Clear();
        }

        private async Task HandlingDeleteMailData()
        {
            MailData = new DeleteMailData
            {
                CharacterId = Session.Character.CharacterId,
                MailId = 123,
                PostType = 1
            };
            await Handler.Handle(MailData);
        }

        private void ShouldSendPostPacket()
        {
            var postPacket = Session.LastPackets.OfType<PostPacket>().FirstOrDefault();
            Assert.IsNotNull(postPacket);
            Assert.AreEqual(2, postPacket.Type);
            Assert.AreEqual(MailData.PostType, postPacket.PostType);
            Assert.AreEqual(MailData.MailId, postPacket.Id);
        }

        private void ShouldNotSendAnyPacket()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
