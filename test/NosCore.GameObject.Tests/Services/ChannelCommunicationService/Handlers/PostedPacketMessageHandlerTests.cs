//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class PostedPacketMessageHandlerTests
    {
        private PostedPacketMessageChannelCommunicationMessageHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<ISessionRegistry> SessionRegistry = null!;
        private Mock<IDeserializer> Deserializer = null!;
        private Mock<ILogger> Logger = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> LogLanguage = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            SessionRegistry = new Mock<ISessionRegistry>();
            Deserializer = new Mock<IDeserializer>();
            Logger = new Mock<ILogger>();
            LogLanguage = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            Handler = new PostedPacketMessageChannelCommunicationMessageHandler(Logger.Object, Deserializer.Object, LogLanguage.Object, SessionRegistry.Object);
        }

        [TestMethod]
        public async Task HandleShouldBroadcastToAllWhenReceiverTypeIsAll()
        {
            await new Spec("Handle should broadcast to all when receiver type is all")
                .Given(DeserializerReturnsPacket)
                .WhenAsync(HandlingBroadcastToAll)
                .Then(ShouldBroadcastToAll)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldSendToSpecificCharacterByName()
        {
            await new Spec("Handle should send to specific character by name")
                .Given(DeserializerReturnsPacket)
                .And(CharacterIsRegisteredByName)
                .WhenAsync(HandlingPostToCharacterByName)
                .Then(ShouldSendToCharacter)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldSendToSpecificCharacterById()
        {
            await new Spec("Handle should send to specific character by id")
                .Given(DeserializerReturnsPacket)
                .And(CharacterIsRegisteredById)
                .WhenAsync(HandlingPostToCharacterById)
                .Then(ShouldSendToCharacter)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldDoNothingWhenReceiverNotFound()
        {
            await new Spec("Handle should do nothing when receiver not found")
                .Given(DeserializerReturnsPacket)
                .And(CharacterIsNotRegistered)
                .WhenAsync(HandlingPostToCharacterByName)
                .Then(ShouldNotSendAnyPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldLogErrorWhenUnknownReceiverType()
        {
            await new Spec("Handle should log error when unknown receiver type")
                .Given(DeserializerReturnsPacket)
                .WhenAsync(HandlingPostWithUnknownReceiverType)
                .Then(ShouldLogError)
                .ExecuteAsync();
        }

        private IPacket DeserializedPacket = null!;

        private void DeserializerReturnsPacket()
        {
            DeserializedPacket = new SayPacket { Message = "Test message" };
            Deserializer.Setup(x => x.Deserialize(It.IsAny<string>()))
                .Returns(DeserializedPacket);
        }

        private void CharacterIsRegisteredByName()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<ICharacterEntity, bool>>()))
                .Returns(Session.Character);
            Session.LastPackets.Clear();
        }

        private void CharacterIsRegisteredById()
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

        private async Task HandlingBroadcastToAll()
        {
            await Handler.Handle(new PostedPacket
            {
                Packet = "say Test message",
                ReceiverType = ReceiverType.All
            });
        }

        private async Task HandlingPostToCharacterByName()
        {
            await Handler.Handle(new PostedPacket
            {
                Packet = "say Test message",
                ReceiverType = ReceiverType.OnlySomeone,
                ReceiverCharacter = new Data.WebApi.Character { Name = Session.Character.Name }
            });
        }

        private async Task HandlingPostToCharacterById()
        {
            await Handler.Handle(new PostedPacket
            {
                Packet = "say Test message",
                ReceiverType = ReceiverType.OnlySomeone,
                ReceiverCharacter = new Data.WebApi.Character { Id = Session.Character.CharacterId }
            });
        }

        private async Task HandlingPostWithUnknownReceiverType()
        {
            await Handler.Handle(new PostedPacket
            {
                Packet = "say Test message",
                ReceiverType = unchecked((ReceiverType)999)
            });
        }

        private void ShouldBroadcastToAll()
        {
            SessionRegistry.Verify(x => x.BroadcastPacketAsync(It.IsAny<IPacket>()), Times.Once);
        }

        private void ShouldSendToCharacter()
        {
            Assert.IsTrue(Session.LastPackets.Count > 0);
        }

        private void ShouldNotSendAnyPacket()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void ShouldLogError()
        {
            Logger.Verify(x => x.Error(It.IsAny<string>()), Times.Once);
        }
    }
}
