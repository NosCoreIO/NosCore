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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Core.MessageQueue;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Miniland
{
    [TestClass]
    public class MJoinPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly Mock<IPubSubHub> _connectedAccountHttpClient = TestHelpers.Instance.PubSubHubClient;
        private readonly Mock<IFriendHttpClient> _friendHttpClient = TestHelpers.Instance.FriendHttpClient;
        private Mock<IMinilandService>? _minilandProvider;
        private MJoinPacketHandler? _mjoinPacketHandler;

        private ClientSession? _session;
        private ClientSession? _targetSession;

        [TestInitialize]
        public async Task SetupAsync()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc(null, Logger, TestHelpers.Instance.DistanceCalculator));
            Broadcaster.Reset();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _targetSession = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _minilandProvider = new Mock<IMinilandService>();
            _mjoinPacketHandler = new MJoinPacketHandler(_friendHttpClient.Object, _minilandProvider.Object);
        }

        [TestMethod]
        public async Task JoinNonConnectedAsync()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = 50,
                Type = VisualType.Player
            };
            await _mjoinPacketHandler!.ExecuteAsync(mjoinPacket, _session!).ConfigureAwait(false);

            Assert.IsFalse(_session!.Character.MapInstance.Map.MapId == 20001);
        }

        [TestMethod]
        public async Task JoinNonFriendAsync()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
            await _mjoinPacketHandler!.ExecuteAsync(mjoinPacket, _session!).ConfigureAwait(false);

            Assert.IsFalse(_session!.Character.MapInstance.Map.MapId == 20001);
        }


        [TestMethod]
        public async Task JoinClosedAsync()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
            _connectedAccountHttpClient.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<ConnectedAccount>() { 
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = _targetSession.Character.CharacterId }
                    },
                    new ConnectedAccount
                        { ChannelId = 1, ConnectedCharacter = new Character { Id = _session!.Character.CharacterId } }
                });
            _friendHttpClient.Setup(s => s.GetListFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus
                {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Friend
                }
            });
            _minilandProvider!.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new GameObject.Services.MinilandService.Miniland
            { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Lock });
            await _mjoinPacketHandler!.ExecuteAsync(mjoinPacket, _session).ConfigureAwait(false);

            var lastpacket = (InfoPacket?)_session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(lastpacket?.Message,
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.MINILAND_CLOSED_BY_FRIEND, _session.Account.Language));
            Assert.IsFalse(_session.Character.MapInstance.Map.MapId == 20001);
        }

        [TestMethod]
        public async Task JoinAsync()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
            _minilandProvider!.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new GameObject.Services.MinilandService.Miniland
            { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Open });
            _friendHttpClient.Setup(s => s.GetListFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus
                {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Friend
                }
            });
            await _mjoinPacketHandler!.ExecuteAsync(mjoinPacket, _session!).ConfigureAwait(false);
            _connectedAccountHttpClient.Setup(s => s.GetCharacterAsync(_targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = _targetSession.Character.CharacterId }
                    }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacterAsync(_session!.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    { ChannelId = 1, ConnectedCharacter = new Character { Id = _session!.Character.CharacterId } }));

            Assert.IsTrue(_session.Character.MapInstance.Map.MapId == 20001);
        }

        [TestMethod]
        public async Task JoinPrivateAsync()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
            _connectedAccountHttpClient.Setup(s => s.GetCharacterAsync(_targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = _targetSession.Character.CharacterId }
                    }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacterAsync(_session!.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    { ChannelId = 1, ConnectedCharacter = new Character { Id = _session!.Character.CharacterId } }));
            _friendHttpClient.Setup(s => s.GetListFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus
                {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Friend
                }
            });
            _minilandProvider!.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new GameObject.Services.MinilandService.Miniland
            { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Private });
            await _mjoinPacketHandler!.ExecuteAsync(mjoinPacket, _session).ConfigureAwait(false);

            Assert.IsTrue(_session.Character.MapInstance.Map.MapId == 20001);
        }

        [TestMethod]
        public async Task JoinPrivateBlockedAsync()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
            _connectedAccountHttpClient.Setup(s => s.GetCharacterAsync(_targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = _targetSession.Character.CharacterId }
                    }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacterAsync(_session!.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    { ChannelId = 1, ConnectedCharacter = new Character { Id = _session!.Character.CharacterId } }));
            _friendHttpClient.Setup(s => s.GetListFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus
                {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Blocked
                }
            });
            _minilandProvider!.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new GameObject.Services.MinilandService.Miniland
            { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Private });
            await _mjoinPacketHandler!.ExecuteAsync(mjoinPacket, _session).ConfigureAwait(false);

            var lastpacket = (InfoPacket?)_session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(lastpacket!.Message,
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.MINILAND_CLOSED_BY_FRIEND, _session.Account.Language));
            Assert.IsFalse(_session.Character.MapInstance.Map.MapId == 20001);
        }
    }
}