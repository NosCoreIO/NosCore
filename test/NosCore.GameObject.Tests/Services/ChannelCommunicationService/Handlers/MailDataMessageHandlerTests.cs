//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class MailDataMessageHandlerTests
    {
        private MailDataMessageChannelCommunicationMessageHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<ISessionRegistry> SessionRegistry = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            SessionRegistry = new Mock<ISessionRegistry>();
            Handler = new MailDataMessageChannelCommunicationMessageHandler(SessionRegistry.Object);
        }

        [TestMethod]
        public async Task HandleShouldSendMailWhenCharacterFound()
        {
            await new Spec("Handle should send mail when character found")
                .Given(CharacterIsRegistered)
                .WhenAsync(HandlingMailData)
                .Then(ShouldSendMailPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldDoNothingWhenCharacterNotFound()
        {
            await new Spec("Handle should do nothing when character not found")
                .Given(CharacterIsNotRegistered)
                .WhenAsync(HandlingMailData)
                .Then(ShouldNotSendAnyPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleWithItemShouldAnnounceTheParcel()
        {
            await new Spec("Handle with item should announce the parcel")
                .Given(CharacterIsRegistered)
                .WhenAsync(HandlingMailDataWithItem)
                .Then(ShouldSendSayPacket)
                .ExecuteAsync();
        }

        private void CharacterIsRegistered()
        {
            SessionRegistry.Setup(x => x.GetSession(It.IsAny<System.Func<ClientSession, bool>>()))
                .Returns(Session);
            Session.LastPackets.Clear();
        }

        private void CharacterIsNotRegistered()
        {
            SessionRegistry.Setup(x => x.GetSession(It.IsAny<System.Func<ClientSession, bool>>()))
                .Returns((ClientSession?)null);
            Session.LastPackets.Clear();
        }

        private async Task HandlingMailData()
        {
            await Handler.Handle(new MailData
            {
                ReceiverName = Session.Character.Name,
                MailId = 1,
                MailDto = new MailDto { Title = "Test Mail" },
                SenderName = "Sender"
            });
        }

        private async Task HandlingMailDataWithItem()
        {
            await Handler.Handle(new MailData
            {
                ReceiverName = Session.Character.Name,
                MailId = 1,
                MailDto = new MailDto { Title = "Test Mail with Item" },
                SenderName = "Sender",
                ItemInstance = new ItemInstanceDto
                {
                    Amount = 5,
                    ItemVNum = 1012
                }
            });
        }

        private void ShouldSendMailPacket()
        {
            Assert.IsTrue(Session.LastPackets.Count > 0);
        }

        private void ShouldNotSendAnyPacket()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void ShouldSendSayPacket()
        {
            var sayPacket = Session.LastPackets.OfType<SayiPacket>().FirstOrDefault();
            Assert.IsNotNull(sayPacket);
            Assert.AreEqual(Game18NConstString.ParcelReceived, sayPacket.Message);
            Assert.AreEqual("1012", sayPacket.Game18NArguments[0]);
            Assert.AreEqual(5, System.Convert.ToInt32(sayPacket.Game18NArguments[1]));
        }
    }
}
