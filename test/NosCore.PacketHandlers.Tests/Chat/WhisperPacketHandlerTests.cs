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
using NosCore.PacketHandlers.Chat;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Chat
{
    [TestClass]
    public class WhisperPacketHandlerTests
    {
        private WhisperPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private ClientSession TargetSession = null!;
        private Mock<IBlacklistHub> BlacklistHub = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<ISerializer> Serializer = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
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
                BlacklistHub.Object,
                PubSubHub.Object,
                channel,
                TestHelpers.Instance.GameLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
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
        public async Task WhisperMessageShouldBeTruncatedIfTooLong()
        {
            await new Spec("Whisper message should be truncated if too long")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .WhenAsync(WhisperingLongMessageToTarget)
                .Then(MessageShouldBeSentViaPubSub)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
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

        private void MessageShouldBeSentViaPubSub()
        {
            PubSubHub.Verify(x => x.SendMessageAsync(It.IsAny<NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket>()), Times.Once);
        }
    }
}
