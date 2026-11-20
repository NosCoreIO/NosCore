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
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class MailDataMessageHandlerTests
    {
        private TestWorld World;
        private MailDataMessageChannelCommunicationMessageHandler Handler;
        private ClientSession Session;
        private Mock<ISessionRegistry> SessionRegistry;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            SessionRegistry = World.Freeze<Mock<ISessionRegistry>>();
            Handler = World.Create<MailDataMessageChannelCommunicationMessageHandler>();
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

        [TestMethod]
        public async Task SenderCopyShouldNotAnnounceTheParcelAgain()
        {
            await new Spec("Sender copy should not announce the parcel again")
                .Given(CharacterIsRegistered)
                .WhenAsync(HandlingSenderCopyWithItem)
                .Then(ShouldNotSendSayPacket)
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

        private async Task HandlingSenderCopyWithItem()
        {
            await Handler.Handle(new MailData
            {
                ReceiverName = Session.Character.Name,
                MailId = 1,
                MailDto = new MailDto { Title = "Test Mail with Item", IsSenderCopy = true },
                SenderName = "Sender",
                ItemInstance = new ItemInstanceDto
                {
                    Amount = 5,
                    ItemVNum = 1012
                }
            });
        }

        private void ShouldNotSendSayPacket()
        {
            Assert.IsFalse(Session.LastPackets.OfType<SayiPacket>().Any());
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
            var sent = Session.LastPackets.ToList();
            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.NewParcelArrived2,
            }, sent.OfType<SayiPacket>().Single());
            Assert.IsTrue(sent.FindIndex(p => p is ParcelPacket) < sent.FindIndex(p => p is SayiPacket));
        }
    }
}
