//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Data.Enumerations.Map;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Miniland
{
    [TestClass]
    public class MJoinPacketHandlerTests
    {
        private static readonly ILogger<MJoinPacketHandlerTests> Logger = new Mock<ILogger<MJoinPacketHandlerTests>>().Object;
        private readonly Mock<IPubSubHub> ConnectedAccountHttpClient = TestHelpers.Instance.PubSubHub;
        private readonly Mock<IFriendHub> FriendHttpClient = TestHelpers.Instance.FriendHttpClient;
        private Mock<IMinilandService> MinilandProvider;
        private MJoinPacketHandler MjoinPacketHandler;
        private ClientSession Session;
        private ClientSession TargetSession;
        private Mock<IMapChangeService> MapChangeService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            MinilandProvider = new Mock<IMinilandService>();
            MapChangeService = new Mock<IMapChangeService>();
            MjoinPacketHandler = new MJoinPacketHandler(FriendHttpClient.Object, MinilandProvider.Object, MapChangeService.Object, TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task JoinNonConnectedPlayerShouldFail()
        {
            await new Spec("Join non connected player should fail")
                .WhenAsync(JoiningNonConnectedPlayer)
                .Then(MapShouldNotChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoinNonFriendShouldFail()
        {
            await new Spec("Join non friend should fail")
                .WhenAsync(JoiningNonFriend)
                .Then(MapShouldNotChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoinClosedMinilandShouldFail()
        {
            await new Spec("Join closed miniland should fail")
                .Given(TargetIsFriend)
                .And(MinilandIsLocked)
                .WhenAsync(JoiningTargetMiniland)
                .Then(ShouldReceiveMinilandLockedMessage)
                .And(MapShouldNotChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoinOpenMinilandShouldSucceed()
        {
            await new Spec("Join open miniland should succeed")
                .Given(TargetIsFriend)
                .And(MinilandIsOpen)
                .WhenAsync(JoiningTargetMiniland)
                .Then(MapShouldChangeToMiniland)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoinPrivateMinilandAsFriendShouldSucceed()
        {
            await new Spec("Join private miniland as friend should succeed")
                .Given(TargetIsFriend)
                .And(MinilandIsPrivate)
                .WhenAsync(JoiningTargetMiniland)
                .Then(MapShouldChangeToMiniland)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoinPrivateMinilandWhenBlockedShouldFail()
        {
            await new Spec("Join private miniland when blocked should fail")
                .Given(TargetHasBlockedSession)
                .And(MinilandIsPrivate)
                .WhenAsync(JoiningTargetMiniland)
                .Then(ShouldReceiveMinilandLockedMessage)
                .And(MapShouldNotChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoinFromATimeSpaceShouldFail()
        {
            await new Spec("Joining a friend's miniland from inside a time-space is refused")
                .Given(TargetIsFriend)
                .And(MinilandIsOpen)
                .And(CharacterIsInAn_, MapInstanceType.TimeSpaceInstance)
                .WhenAsync(JoiningTargetMiniland)
                .Then(MapShouldNotChange)
                .And(ShouldBeToldItCannotBeUsedHere)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoinFromAnArenaShouldFail()
        {
            await new Spec("Joining a friend's miniland from inside an arena is refused")
                .Given(TargetIsFriend)
                .And(MinilandIsOpen)
                .And(CharacterIsInAn_, MapInstanceType.ArenaInstance)
                .WhenAsync(JoiningTargetMiniland)
                .Then(MapShouldNotChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoinFromAnotherMinilandShouldSucceed()
        {
            await new Spec("Joining a friend's miniland from another miniland still works")
                .Given(TargetIsFriend)
                .And(MinilandIsOpen)
                .And(CharacterIsInAn_, MapInstanceType.NormalInstance)
                .And(CharacterIsInAMiniland)
                .WhenAsync(JoiningTargetMiniland)
                .Then(MapShouldChangeToMiniland)
                .ExecuteAsync();
        }

        private void CharacterIsInAn_(MapInstanceType type)
        {
            Session.Character.MapInstance.MapInstanceType = type;
        }

        private void CharacterIsInAMiniland()
        {
            var mapInstanceId = Session.Character.MapInstance.MapInstanceId;
            MinilandProvider.Setup(s => s.GetMinilandFromMapInstanceId(mapInstanceId))
                .Returns(new GameObject.Services.MinilandService.Miniland { MapInstanceId = mapInstanceId });
        }

        private void ShouldBeToldItCannotBeUsedHere()
        {
            var sayi = Session.LastPackets.OfType<SayiPacket>().LastOrDefault();
            Assert.AreEqual(Game18NConstString.CanNotBeUsedHere, sayi?.Message);
        }

        private void TargetIsFriend()
        {
            ConnectedAccountHttpClient.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>()
                {
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = TargetSession.Character.CharacterId }
                    },
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
            FriendHttpClient.Setup(s => s.GetFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new()
                {
                    CharacterId = TargetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = TargetSession.Character.Name,
                    RelationType = CharacterRelationType.Friend
                }
            });
        }

        private void TargetHasBlockedSession()
        {
            ConnectedAccountHttpClient.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>()
                {
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = TargetSession.Character.CharacterId }
                    },
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
            FriendHttpClient.Setup(s => s.GetFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new()
                {
                    CharacterId = TargetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = TargetSession.Character.Name,
                    RelationType = CharacterRelationType.Blocked
                }
            });
        }

        private void MinilandIsLocked()
        {
            MinilandProvider.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new GameObject.Services.MinilandService.Miniland
            { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Lock });
        }

        private void MinilandIsOpen()
        {
            MinilandProvider.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new GameObject.Services.MinilandService.Miniland
            { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Open });
        }

        private void MinilandIsPrivate()
        {
            MinilandProvider.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new GameObject.Services.MinilandService.Miniland
            { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Private });
        }

        private async Task JoiningNonConnectedPlayer()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = 50,
                Type = VisualType.Player
            };
            await MjoinPacketHandler.ExecuteAsync(mjoinPacket, Session);
        }

        private async Task JoiningNonFriend()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = TargetSession.Character.CharacterId,
                Type = VisualType.Player
            };
            await MjoinPacketHandler.ExecuteAsync(mjoinPacket, Session);
        }

        private async Task JoiningTargetMiniland()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = TargetSession.Character.CharacterId,
                Type = VisualType.Player
            };
            await MjoinPacketHandler.ExecuteAsync(mjoinPacket, Session);
        }

        private void MapShouldNotChange()
        {
            MapChangeService.Verify(x => x.ChangeMapInstanceAsync(Session, TestHelpers.Instance.MinilandId, 5, 8), Times.Never);
        }

        private void MapShouldChangeToMiniland()
        {
            MapChangeService.Verify(x => x.ChangeMapInstanceAsync(Session, TestHelpers.Instance.MinilandId, 5, 8), Times.Once);
        }

        private void ShouldReceiveMinilandLockedMessage()
        {
            var lastpacket = (InfoiPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.AreEqual(lastpacket?.Message, Game18NConstString.MinilandLocked);
        }
    }
}
