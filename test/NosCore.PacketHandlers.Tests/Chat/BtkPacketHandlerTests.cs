//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
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
    public class BtkPacketHandlerTests
    {
        private BtkPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private ClientSession FriendSession = null!;
        private Mock<IFriendHub> FriendHub = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<ISerializer> Serializer = null!;
        private Mock<ISessionRegistry> SessionRegistry = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;
        private const long OfflineFriendId = 99998;
        private const long DifferentChannelFriendId = 99997;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            FriendSession = await TestHelpers.Instance.GenerateSessionAsync();
            FriendHub = new Mock<IFriendHub>();
            PubSubHub = new Mock<IPubSubHub>();
            Serializer = new Mock<ISerializer>();
            SessionRegistry = new Mock<ISessionRegistry>();

            FriendHub.Setup(x => x.GetFriendsAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>()));

            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>()));

            Serializer.Setup(x => x.Serialize(It.IsAny<IPacket[]>()))
                .Returns("serialized");

            var channel = new Channel { ChannelId = 1, ClientName = "TestChannel", Host = "localhost" };

            Handler = new BtkPacketHandler(
                Logger,
                Serializer.Object,
                FriendHub.Object,
                PubSubHub.Object,
                PubSubHub.Object,
                channel,
                TestHelpers.Instance.GameLanguageLocalizer,
                SessionRegistry.Object);
        }

        [TestMethod]
        public async Task SendingMessageToNonFriendShouldBeIgnored()
        {
            await new Spec("Sending message to non friend should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(SendingMessageToNonFriendAsync)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SendingMessageToOfflineFriendShouldShowOfflineMessage()
        {
            await new Spec("Sending message to offline friend should show offline message")
                .Given(CharacterIsOnMap)
                .And(OfflineTargetIsFriend)
                .WhenAsync(SendingMessageToOfflineFriendAsync)
                .Then(ShouldReceiveFriendOfflineMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SendingMessageToFriendOnSameChannelShouldSendDirectly()
        {
            await new Spec("Sending message to friend on same channel should send directly")
                .Given(CharacterIsOnMap)
                .And(TargetIsFriend)
                .And(FriendIsOnSameChannel)
                .WhenAsync(SendingMessageToFriendAsync)
                .Then(FriendShouldReceiveMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SendingMessageToFriendOnDifferentChannelShouldUsePubSub()
        {
            await new Spec("Sending message to friend on different channel should use pubsub")
                .Given(CharacterIsOnMap)
                .And(DifferentChannelTargetIsFriend)
                .And(FriendIsOnDifferentChannel)
                .WhenAsync(SendingMessageToFriendOnDifferentChannelAsync)
                .Then(MessageShouldBeSentViaPubSub)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LongMessageShouldBeTruncated()
        {
            await new Spec("Long message should be truncated")
                .Given(CharacterIsOnMap)
                .And(TargetIsFriend)
                .And(FriendIsOnSameChannel)
                .WhenAsync(SendingLongMessageToFriendAsync)
                .Then(FriendShouldReceiveMessage)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void TargetIsFriend()
        {
            FriendHub.Setup(x => x.GetFriendsAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>
                {
                    new CharacterRelationStatus
                    {
                        CharacterId = FriendSession.Character.CharacterId
                    }
                }));
        }

        private void OfflineTargetIsFriend()
        {
            FriendHub.Setup(x => x.GetFriendsAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>
                {
                    new CharacterRelationStatus
                    {
                        CharacterId = OfflineFriendId
                    }
                }));
        }

        private void DifferentChannelTargetIsFriend()
        {
            FriendHub.Setup(x => x.GetFriendsAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>
                {
                    new CharacterRelationStatus
                    {
                        CharacterId = DifferentChannelFriendId
                    }
                }));
        }

        private void FriendIsOnSameChannel()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<GameObject.ComponentEntities.Interfaces.ICharacterEntity, bool>>()))
                .Returns(FriendSession.Character);
        }

        private void FriendIsOnDifferentChannel()
        {
            PubSubHub.Setup(x => x.GetSubscribersAsync())
                .Returns(Task.FromResult(new List<Subscriber>
                {
                    new Subscriber
                    {
                        ConnectedCharacter = new Data.WebApi.Character
                        {
                            Id = DifferentChannelFriendId,
                            Name = "DifferentChannelFriend"
                        }
                    }
                }));
        }

        private async Task SendingMessageToNonFriendAsync()
        {
            await Handler.ExecuteAsync(new BtkPacket
            {
                CharacterId = 99999,
                Message = "Hello friend!"
            }, Session);
        }

        private async Task SendingMessageToOfflineFriendAsync()
        {
            await Handler.ExecuteAsync(new BtkPacket
            {
                CharacterId = OfflineFriendId,
                Message = "Hello friend!"
            }, Session);
        }

        private async Task SendingMessageToFriendAsync()
        {
            await Handler.ExecuteAsync(new BtkPacket
            {
                CharacterId = FriendSession.Character.CharacterId,
                Message = "Hello friend!"
            }, Session);
        }

        private async Task SendingMessageToFriendOnDifferentChannelAsync()
        {
            await Handler.ExecuteAsync(new BtkPacket
            {
                CharacterId = DifferentChannelFriendId,
                Message = "Hello friend!"
            }, Session);
        }

        private async Task SendingLongMessageToFriendAsync()
        {
            var longMessage = new string('a', 100);
            await Handler.ExecuteAsync(new BtkPacket
            {
                CharacterId = FriendSession.Character.CharacterId,
                Message = longMessage
            }, Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void ShouldReceiveFriendOfflineMessage()
        {
            var packet = Session.LastPackets.OfType<InfoiPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.FriendOffline, packet.Message);
        }

        private void FriendShouldReceiveMessage()
        {
            Assert.IsTrue(FriendSession.LastPackets.Count > 0);
        }

        private void MessageShouldBeSentViaPubSub()
        {
            PubSubHub.Verify(x => x.SendMessageAsync(It.IsAny<NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket>()), Times.Once);
        }
    }
}
