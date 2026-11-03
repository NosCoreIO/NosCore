//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Linq;
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
        private Mock<ILogger<PostedPacketMessageChannelCommunicationMessageHandler>> Logger = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> LogLanguage = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            SessionRegistry = new Mock<ISessionRegistry>();
            Deserializer = new Mock<IDeserializer>();
            Logger = new Mock<ILogger<PostedPacketMessageChannelCommunicationMessageHandler>>();
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
        public async Task HeroMessageSkipsPlayersBlockingHeroChat()
        {
            await new Spec("A hero message is not delivered to a player who blocks hero chat")
                .Given(DeserializerReturnsPacket)
                .AndAsync(TwoPlayersAreOnline)
                .And(ListenerBlocksHeroChat)
                .WhenAsync(HandlingHeroMessage)
                .Then(ListenerShouldNotReceiveIt)
                .And(OtherPlayerShouldReceiveIt)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HeroMessageReachesPlayersAllowingHeroChat()
        {
            await new Spec("A hero message is delivered to a player who allows hero chat")
                .Given(DeserializerReturnsPacket)
                .AndAsync(TwoPlayersAreOnline)
                .And(ListenerAllowsHeroChat)
                .WhenAsync(HandlingHeroMessage)
                .Then(ListenerShouldReceiveIt)
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

        private ClientSession Listener = null!;

        private async Task TwoPlayersAreOnline()
        {
            Listener = await TestHelpers.Instance.GenerateSessionAsync();
            var characters = new[] { Session.Character, Listener.Character };
            SessionRegistry.Setup(x => x.GetCharacters(It.IsAny<System.Func<PlayerComponentBundle, bool>?>()))
                .Returns((System.Func<PlayerComponentBundle, bool>? predicate) =>
                    predicate == null ? characters : characters.Where(predicate));
            Session.LastPackets.Clear();
            Listener.LastPackets.Clear();
        }

        private void ListenerBlocksHeroChat() => Listener.Character.HeroChatBlocked = true;

        private void ListenerAllowsHeroChat() => Listener.Character.HeroChatBlocked = false;

        private async Task HandlingHeroMessage()
        {
            await Handler.Handle(new PostedPacket
            {
                Packet = "say Test message",
                ReceiverType = ReceiverType.AllNoHeroBlocked
            });
        }

        private void ListenerShouldNotReceiveIt() => Assert.AreEqual(0, Listener.LastPackets.Count);

        private void ListenerShouldReceiveIt() => Assert.IsTrue(Listener.LastPackets.Count > 0);

        private void OtherPlayerShouldReceiveIt() => Assert.IsTrue(Session.LastPackets.Count > 0);

        private void CharacterIsRegisteredByName()
        {
            SessionRegistry.Setup(x => x.GetSession(It.IsAny<System.Func<ClientSession, bool>>()))
                .Returns(Session);
            Session.LastPackets.Clear();
        }

        private void CharacterIsRegisteredById()
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
            Logger.Verify(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => true),
                    It.IsAny<System.Exception?>(),
                    It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
                Times.Once);
        }
    }
}
