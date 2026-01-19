//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Miniland
{
    [TestClass]
    public class MJoinPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly Mock<IPubSubHub> ConnectedAccountHttpClient = TestHelpers.Instance.PubSubHub;
        private readonly Mock<IFriendHub> FriendHttpClient = TestHelpers.Instance.FriendHttpClient;
        private Mock<IMinilandService> MinilandProvider = null!;
        private MJoinPacketHandler MjoinPacketHandler = null!;
        private ClientSession Session = null!;
        private ClientSession TargetSession = null!;
        private Mock<IMapChangeService> MapChangeService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc(null, Logger, TestHelpers.Instance.DistanceCalculator, TestHelpers.Instance.Clock));
            Broadcaster.Reset();
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
