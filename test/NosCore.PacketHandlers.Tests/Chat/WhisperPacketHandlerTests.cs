//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BlackListService;
using NosCore.GameObject.Services.SubscriberCacheService;
using NosCore.PacketHandlers.Chat;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Chat
{
    [TestClass]
    public class WhisperPacketHandlerTests
    {
        private TestWorld World;
        private WhisperPacketHandler Handler;
        private ClientSession Session;
        private ClientSession TargetSession;
        private Mock<IBlacklistHub> BlacklistHub;
        private Mock<IPubSubHub> PubSubHub;
        private Mock<ISerializer> Serializer;
        private readonly ILogger<WhisperPacketHandler> Logger = new Mock<ILogger<WhisperPacketHandler>>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            TargetSession = await World.GenerateSessionAsync();
            BlacklistHub = new Mock<IBlacklistHub>();
            PubSubHub = new Mock<IPubSubHub>();
            Serializer = new Mock<ISerializer>();

            BlacklistHub.Setup(x => x.GetBlacklistedAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>()));

            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>()));

            Serializer.Setup(x => x.Serialize(It.IsAny<IPacket[]>()))
                .Returns("serialized");

            var channel = new Channel { ChannelId = 1, ClientName = "TestChannel", Host = "localhost" };

            Handler = new WhisperPacketHandler(
                Logger,
                Serializer.Object,
                new BlacklistCacheService(BlacklistHub.Object, World.Clock),
                new SubscriberCacheService(PubSubHub.Object, World.Clock),
                PubSubHub.Object,
                channel,
                World.GameLanguageLocalizer,
                World.SessionRegistry);
        }

        [TestMethod]
        public async Task WhisperingToNonExistentPlayerShouldShowNotPlayingMessage()
        {
            await new Spec("Whispering to nonexistent player should show not playing message")
                .Given(CharacterIsOnMap)
                .WhenAsync(WhisperingToNonExistentPlayer)
                .Then(ShouldReceiveNotPlayingMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WhisperingToBlacklistedPlayerShouldShowBlacklistMessage()
        {
            await new Spec("Whispering to blacklisted player should show blacklist message")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .And(TargetIsBlacklisted)
                .WhenAsync(WhisperingToTarget)
                .Then(ShouldReceiveBlacklistMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WhisperingToPlayerWhoBlacklistedTheSenderShouldBeRefused()
        {
            await new Spec("Whispering to a player who blacklisted the sender should be refused")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .And(TargetBlacklistedTheSender)
                .WhenAsync(WhisperingToTarget)
                .Then(ShouldReceiveBlacklistMessage)
                .And(NoMessageShouldBeSentViaPubSub)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WhisperingToPlayerWhoBlacklistedSomeoneElseShouldSendMessage()
        {
            await new Spec("Whispering to a player who blacklisted someone else should send the message")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .And(TargetBlacklistedSomeoneElse)
                .WhenAsync(WhisperingToTarget)
                .Then(MessageShouldBeSentViaPubSub)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WhisperingToOnlinePlayerShouldSendMessage()
        {
            await new Spec("Whispering to online player should send message")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .WhenAsync(WhisperingToTarget)
                .Then(MessageShouldBeSentViaPubSub)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WhisperingToPlayerWhoBlocksWhispersShouldBeRefused()
        {
            await new Spec("Whispering to a player who blocks whispers should be refused")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnlineBlockingWhispers)
                .WhenAsync(WhisperingToTarget)
                .Then(ShouldReceiveRefuseWhisperMessage)
                .And(NoMessageShouldBeSentViaPubSub)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WhisperMessageShouldBeTruncatedIfTooLong()
        {
            await new Spec("Whisper message should be truncated if too long")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .WhenAsync(WhisperingLongMessageToTarget)
                .Then(MessageShouldBeSentViaPubSub)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RepeatedWhispersWithinTheCacheWindowAskTheHubsOnce()
        {
            await new Spec("Repeated whispers within the cache window ask the hubs once")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .WhenAsync(WhisperingToTarget)
                .AndAsync(WhisperingToTarget)
                .Then(HubsShouldBeAskedOnce)
                .And(BothMessagesShouldBeSentViaPubSub)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANewBlacklistEntryAppliesOnceTheCacheWindowHasPassed()
        {
            await new Spec("A new blacklist entry applies once the cache window has passed")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .AndAsync(WhisperingToTarget)
                .And(TargetIsBlacklisted)
                .And(TheCacheWindowPasses)
                .WhenAsync(WhisperingToTarget)
                .Then(ShouldReceiveBlacklistMessage)
                .And(MessageShouldBeSentViaPubSub)
                .ExecuteAsync();
        }

        private void TheCacheWindowPasses()
        {
            World.Clock.Advance(BlacklistCacheService.CacheDuration);
        }

        private void HubsShouldBeAskedOnce()
        {
            PubSubHub.Verify(x => x.GetSubscribersAsync(), Times.Once);
            var senderId = Session.Character.VisualId;
            var targetId = TargetSession.Character.CharacterId;
            BlacklistHub.Verify(x => x.GetBlacklistedAsync(senderId), Times.Once);
            BlacklistHub.Verify(x => x.GetBlacklistedAsync(targetId), Times.Once);
        }

        private void BothMessagesShouldBeSentViaPubSub()
        {
            PubSubHub.Verify(x => x.SendMessageAsync(It.IsAny<NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket>()), Times.Exactly(2));
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private void TargetIsOnline()
        {
            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>
                {
                    new Subscriber
                    {
                        ConnectedCharacter = new Data.WebApi.Character
                        {
                            Id = TargetSession.Character.CharacterId,
                            Name = TargetSession.Character.Name
                        }
                    }
                }));
        }

        private void TargetIsOnlineBlockingWhispers()
        {
            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>
                {
                    new Subscriber
                    {
                        ConnectedCharacter = new Data.WebApi.Character
                        {
                            Id = TargetSession.Character.CharacterId,
                            Name = TargetSession.Character.Name,
                            WhisperBlocked = true
                        }
                    }
                }));
        }

        private void TargetIsBlacklisted()
        {
            BlacklistHub.Setup(x => x.GetBlacklistedAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>
                {
                    new CharacterRelationStatus
                    {
                        CharacterId = TargetSession.Character.CharacterId
                    }
                }));
        }

        private void TargetBlacklistedTheSender()
        {
            var targetId = TargetSession.Character.CharacterId;
            BlacklistHub.Setup(x => x.GetBlacklistedAsync(targetId))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>
                {
                    new CharacterRelationStatus { CharacterId = Session.Character.CharacterId }
                }));
        }

        private void TargetBlacklistedSomeoneElse()
        {
            var targetId = TargetSession.Character.CharacterId;
            BlacklistHub.Setup(x => x.GetBlacklistedAsync(targetId))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>
                {
                    new CharacterRelationStatus { CharacterId = Session.Character.CharacterId + 1000 }
                }));
        }

        private async Task WhisperingToNonExistentPlayer()
        {
            await Handler.ExecuteAsync(new WhisperPacket
            {
                Message = "NonExistentPlayer Hello there!"
            }, Session);
        }

        private async Task WhisperingToTarget()
        {
            await Handler.ExecuteAsync(new WhisperPacket
            {
                Message = $"{TargetSession.Character.Name} Hello there!"
            }, Session);
        }

        private async Task WhisperingLongMessageToTarget()
        {
            var longMessage = new string('a', 100);
            await Handler.ExecuteAsync(new WhisperPacket
            {
                Message = $"{TargetSession.Character.Name} {longMessage}"
            }, Session);
        }

        private void ShouldReceiveNotPlayingMessage()
        {
            var packet = Session.LastPackets.OfType<Infoi2Packet>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.IsNotPlaying, packet.Message);
        }

        private void ShouldReceiveBlacklistMessage()
        {
            var packet = Session.LastPackets.OfType<InfoiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.AlreadyBlacklisted, packet.Message);
        }

        private void ShouldReceiveRefuseWhisperMessage()
        {
            var packet = Session.LastPackets.OfType<MsgiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.ThisPersonRefuseWhisper, packet.Message);
        }

        private void NoMessageShouldBeSentViaPubSub()
        {
            PubSubHub.Verify(x => x.SendMessageAsync(It.IsAny<NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket>()), Times.Never);
        }

        private void MessageShouldBeSentViaPubSub()
        {
            PubSubHub.Verify(x => x.SendMessageAsync(It.IsAny<NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket>()), Times.Once);
        }
    }
}
