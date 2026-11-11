//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FriendService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
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
        private TestWorld World;
        private static readonly ILogger<NosCore.GameObject.Services.BroadcastService.SessionRegistry> Logger = new Mock<ILogger<NosCore.GameObject.Services.BroadcastService.SessionRegistry>>().Object;
        private Mock<IChannelHub> ChannelHttpClient => World.ChannelHub;
        private IDao<CharacterRelationDto, Guid> CharacterRelationDao;
        private Mock<IPubSubHub> ConnectedAccountHttpClient => World.PubSubHub;
        private Mock<IChannelHub> ChannelHub;
        private FinsPacketHandler FinsPacketHandler;
        private Mock<IFriendHub> FriendHttpClient => World.FriendHub;
        private FriendRequestRegistry FriendRequestHolder;
        private ClientSession Session;
        private ClientSession TargetSession;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            TargetSession = await World.GenerateSessionAsync();
            CharacterRelationDao = World.CharacterRelationDao;
            ChannelHub = new Mock<IChannelHub>();
            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>
                {
                    new ChannelInfo { Host = "127.0.0.1", Type = ServerType.WorldServer, Id = 1 }
                });
            FriendRequestHolder = World.Create<FriendRequestRegistry>();
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
                World.Create<NosCore.GameObject.Services.BroadcastService.SessionRegistry>(),
                World.Clock);
        }

        private FriendService CreateFriendService()
        {
            return new FriendService(
                new Mock<ILogger<FriendService>>().Object,
                CharacterRelationDao,
                World.CharacterDao,
                FriendRequestHolder,
                ConnectedAccountHttpClient.Object,
                ChannelHub.Object,
                World.LogLanguageLocalizer);
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

        [TestMethod]
        public async Task SendingTheSameFriendRequestTwiceShouldKeepOnePendingRequest()
        {
            await new Spec("Sending the same friend request twice should keep one pending request")
                .WhenAsync(TheServiceReceivesAFriendRequestAsync)
                .AndAsync(TheServiceReceivesAFriendRequestAsync)
                .Then(OnePendingRequestShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestingAFriendWhoBlacklistedTheSenderShouldBeRefused()
        {
            await new Spec("Requesting a friend who blacklisted the sender should be refused")
                .GivenAsync(TheTargetBlacklistedTheSenderAsync)
                .WhenAsync(TheServiceReceivesAFriendRequestAsync)
                .Then(TheResultShouldBe_, LanguageKey.BLACKLIST_BLOCKED)
                .And(NoPendingRequestShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestingAFriendWhoBlacklistedSomeoneElseShouldBeSent()
        {
            await new Spec("Requesting a friend who blacklisted someone else should be sent")
                .GivenAsync(TheTargetBlacklistedSomeoneElseAsync)
                .WhenAsync(TheServiceReceivesAFriendRequestAsync)
                .Then(TheResultShouldBe_, LanguageKey.FRIEND_REQUEST_SENT)
                .And(OnePendingRequestShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SendingAFriendRequestTwiceWithinTheCooldownShouldAskTheTargetOnce()
        {
            await new Spec("Sending a friend request twice within the cooldown should ask the target once")
                .Given(TheHubAcceptsFriendRequests)
                .WhenAsync(TheSenderSendsAFriendRequestAsync)
                .AndAsync(TheSenderSendsAFriendRequestAsync)
                .Then(TheTargetShouldBeAsked_Times, 1)
                .And(TheSenderShouldBeToldToWait)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SendingAFriendRequestAgainAfterTheCooldownShouldAskTheTargetAgain()
        {
            await new Spec("Sending a friend request again after the cooldown should ask the target again")
                .Given(TheHubAcceptsFriendRequests)
                .WhenAsync(TheSenderSendsAFriendRequestAsync)
                .And(TheCooldownPasses)
                .AndAsync(TheSenderSendsAFriendRequestAsync)
                .Then(TheTargetShouldBeAsked_Times, 2)
                .ExecuteAsync();
        }

        private LanguageKey _result;

        private async Task TheServiceReceivesAFriendRequestAsync() =>
            _result = await CreateFriendService().AddFriendAsync(Session.Character.CharacterId,
                TargetSession.Character.CharacterId, FinsPacketType.Accepted);

        private Task TheTargetBlacklistedTheSenderAsync() => TheTargetBlacklistsAsync(Session.Character.CharacterId);

        private async Task TheTargetBlacklistedSomeoneElseAsync()
        {
            var someoneElse = await World.GenerateSessionAsync();
            await TheTargetBlacklistsAsync(someoneElse.Character.CharacterId);
        }

        private Task TheTargetBlacklistsAsync(long characterId) =>
            CharacterRelationDao.TryInsertOrUpdateAsync(new CharacterRelationDto
            {
                CharacterRelationId = Guid.NewGuid(),
                CharacterId = TargetSession.Character.CharacterId,
                RelatedCharacterId = characterId,
                RelationType = CharacterRelationType.Blocked
            });

        private void TheResultShouldBe_(LanguageKey expected) => Assert.AreEqual(expected, _result);

        private void OnePendingRequestShouldExist() =>
            Assert.AreEqual(1, FriendRequestHolder.GetRequestsForCharacter(Session.Character.CharacterId).Count());

        private void NoPendingRequestShouldExist() =>
            Assert.IsFalse(FriendRequestHolder.GetRequestsForCharacter(Session.Character.CharacterId).Any());

        private void TheHubAcceptsFriendRequests()
        {
            FriendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .ReturnsAsync(LanguageKey.FRIEND_REQUEST_SENT);
            FinsPacketHandler = new FinsPacketHandler(FriendHttpClient.Object, ChannelHttpClient.Object,
                ConnectedAccountHttpClient.Object, World.SessionRegistry, World.Clock);
        }

        private Task TheSenderSendsAFriendRequestAsync() =>
            FinsPacketHandler.ExecuteAsync(new FinsPacket
            {
                CharacterId = TargetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            }, Session);

        private void TheCooldownPasses() => World.Clock.Advance(Duration.FromSeconds(6));

        private void TheTargetShouldBeAsked_Times(int times) =>
            Assert.AreEqual(times, TargetSession.LastPackets.OfType<Dlgi2Packet>().Count());

        private void TheSenderShouldBeToldToWait() =>
            Assert.AreEqual(Game18NConstString.CannotSendInvite, Session.LastPackets.OfType<InfoiPacket>().Last().Message);

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
