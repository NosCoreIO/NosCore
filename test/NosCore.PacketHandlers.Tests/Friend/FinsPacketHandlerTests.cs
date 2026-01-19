//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FriendService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.AutoFixture;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class FinsPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private NosCoreFixture Fixture = null!;
        private readonly Mock<IChannelHub> ChannelHttpClient = TestHelpers.Instance.ChannelHttpClient;
        private IDao<CharacterRelationDto, Guid> CharacterRelationDao = null!;
        private readonly Mock<IPubSubHub> ConnectedAccountHttpClient = TestHelpers.Instance.PubSubHub;
        private Mock<IChannelHub> ChannelHub = null!;
        private FinsPacketHandler FinsPacketHandler = null!;
        private readonly Mock<IFriendHub> FriendHttpClient = TestHelpers.Instance.FriendHttpClient;
        private FriendRequestRegistry FriendRequestHolder = null!;
        private ClientSession Session = null!;
        private ClientSession TargetSession = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc(null, new Mock<ILogger>().Object,
                    TestHelpers.Instance.DistanceCalculator, TestHelpers.Instance.Clock));
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            Fixture = new NosCoreFixture();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            CharacterRelationDao = TestHelpers.Instance.CharacterRelationDao;
            ChannelHub = new Mock<IChannelHub>();
            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>
                {
                    new ChannelInfo { Type = ServerType.WorldServer, Id = 1 }
                });
            FriendRequestHolder = new FriendRequestRegistry();
            ConnectedAccountHttpClient.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>
                {
                    new Subscriber
                    {
                        ChannelId = 1,
                        ConnectedCharacter = new Character { Id = TargetSession.Character.CharacterId }
                    },
                    new Subscriber
                    {
                        ChannelId = 1,
                        ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
            FinsPacketHandler = new FinsPacketHandler(
                FriendHttpClient.Object,
                ChannelHttpClient.Object,
                ConnectedAccountHttpClient.Object,
                new NosCore.GameObject.Services.BroadcastService.SessionRegistry(Logger));
        }

        private FriendService CreateFriendService()
        {
            return new FriendService(
                new Mock<ILogger>().Object,
                CharacterRelationDao,
                TestHelpers.Instance.CharacterDao,
                FriendRequestHolder,
                ConnectedAccountHttpClient.Object,
                ChannelHub.Object,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task AcceptingFriendRequestShouldCreateMutualRelation()
        {
            await new Spec("Accepting friend request should create mutual relation")
                .Given(AFriendRequestExists)
                .WhenAsync(AcceptingTheFriendRequest)
                .Then(TwoFriendRelationsShouldBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingFriendWhenTargetDisconnectedShouldFail()
        {
            await new Spec("Adding friend when target disconnected should fail")
                .WhenAsync(AttemptingToAddFriend)
                .Then(NoRelationShouldBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AcceptingUnrequestedFriendShouldFail()
        {
            await new Spec("Accepting unrequested friend should fail")
                .WhenAsync(AttemptingToAcceptNonExistentRequest)
                .Then(NoRelationShouldBeCreated)
                .ExecuteAsync();
        }

        private void AFriendRequestExists()
        {
            FriendRequestHolder.RegisterRequest(Guid.NewGuid(),
                TargetSession.Character.CharacterId, Session.Character.CharacterId);
        }

        private async Task AcceptingTheFriendRequest()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = TargetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            var friend = CreateFriendService();
            FriendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(Session.Character.CharacterId,
                    finsPacket.CharacterId, finsPacket.Type));
            await FinsPacketHandler.ExecuteAsync(finsPacket, Session);
        }

        private void TwoFriendRelationsShouldBeCreated()
        {
            Assert.AreEqual(2, CharacterRelationDao.LoadAll().Count());
        }

        private async Task AttemptingToAddFriend()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = TargetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            var friend = CreateFriendService();
            FriendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(Session.Character.CharacterId,
                    finsPacket.CharacterId, finsPacket.Type));
            await FinsPacketHandler.ExecuteAsync(finsPacket, Session);
        }

        private void NoRelationShouldBeCreated()
        {
            Assert.IsFalse(CharacterRelationDao.LoadAll().Any());
        }

        private async Task AttemptingToAcceptNonExistentRequest()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = TargetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            var friend = CreateFriendService();
            FriendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(Session.Character.CharacterId,
                    finsPacket.CharacterId, finsPacket.Type));
            await FinsPacketHandler.ExecuteAsync(finsPacket, Session);
        }
    }
}
