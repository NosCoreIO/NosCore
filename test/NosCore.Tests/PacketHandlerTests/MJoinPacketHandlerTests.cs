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
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class MJoinPacketHandlerTests
    {
        private static readonly ILogger Logger = Core.I18N.Logger.GetLoggerConfiguration().CreateLogger();
        private readonly Mock<IConnectedAccountHttpClient> _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
        private readonly Mock<IFriendHttpClient> _friendHttpClient = TestHelpers.Instance.FriendHttpClient;
        private Mock<IMinilandProvider>? _minilandProvider;
        private MJoinPacketHandler? _mjoinPacketHandler;

        private ClientSession? _session;
        private ClientSession? _targetSession;

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc(null, null, null, null, Logger));
            Broadcaster.Reset();
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _targetSession = TestHelpers.Instance.GenerateSession();
            _minilandProvider = new Mock<IMinilandProvider>();
            _mjoinPacketHandler = new MJoinPacketHandler(_friendHttpClient.Object, _minilandProvider.Object);
        }

        [TestMethod]
        public async Task JoinNonConnected()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = 50,
                Type = VisualType.Player
            };
            await _mjoinPacketHandler!.Execute(mjoinPacket, _session!).ConfigureAwait(false);

            Assert.IsFalse(_session!.Character.MapInstance!.Map.MapId == 20001);
        }

        [TestMethod]
        public async Task JoinNonFriend()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
           await _mjoinPacketHandler!.Execute(mjoinPacket, _session!).ConfigureAwait(false);

            Assert.IsFalse(_session!.Character.MapInstance!.Map.MapId == 20001);
        }


        [TestMethod]
        public async Task JoinClosed()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(
                    new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character {Id = _targetSession.Character.CharacterId}
                    }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_session!.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                        {ChannelId = 1, ConnectedCharacter = new Character {Id = _session!.Character.CharacterId}}));
            _friendHttpClient.Setup(s => s.GetListFriends(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus
                {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Friend
                }
            });
            _minilandProvider!.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new Miniland
                {MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Lock});
           await _mjoinPacketHandler!.Execute(mjoinPacket, _session).ConfigureAwait(false);

            var lastpacket = (InfoPacket?) _session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(lastpacket?.Message,
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.MINILAND_CLOSED_BY_FRIEND, _session.Account.Language));
            Assert.IsFalse(_session.Character.MapInstance!.Map.MapId == 20001);
        }

        [TestMethod]
        public async Task Join()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
            _minilandProvider!.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new Miniland
                {MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Open});
            _friendHttpClient.Setup(s => s.GetListFriends(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus
                {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Friend
                }
            });
            await _mjoinPacketHandler!.Execute(mjoinPacket, _session!).ConfigureAwait(false);
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character {Id = _targetSession.Character.CharacterId}
                    }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_session!.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                        {ChannelId = 1, ConnectedCharacter = new Character {Id = _session!.Character.CharacterId}}));

            Assert.IsTrue(_session.Character.MapInstance!.Map.MapId == 20001);
        }

        [TestMethod]
        public async Task JoinPrivate()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = _targetSession.Character.CharacterId }
                    }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_session!.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    { ChannelId = 1, ConnectedCharacter = new Character { Id = _session!.Character.CharacterId } }));
            _friendHttpClient.Setup(s => s.GetListFriends(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus
                {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Friend
                }
            });
            _minilandProvider!.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new Miniland
            { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Private });
            await _mjoinPacketHandler!.Execute(mjoinPacket, _session).ConfigureAwait(false);

            Assert.IsTrue(_session.Character.MapInstance!.Map.MapId == 20001);
        }

        [TestMethod]
        public async Task JoinPrivateBlocked()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession!.Character.CharacterId,
                Type = VisualType.Player
            };
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = _targetSession.Character.CharacterId }
                    }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_session!.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    { ChannelId = 1, ConnectedCharacter = new Character { Id = _session!.Character.CharacterId } }));
            _friendHttpClient.Setup(s => s.GetListFriends(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus
                {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Blocked
                }
            });
            _minilandProvider!.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(new Miniland
            { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Private });
            await _mjoinPacketHandler!.Execute(mjoinPacket, _session).ConfigureAwait(false);

            var lastpacket = (InfoPacket?)_session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(lastpacket!.Message,
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.MINILAND_CLOSED_BY_FRIEND, _session.Account.Language));
            Assert.IsFalse(_session.Character.MapInstance!.Map.MapId == 20001);
        }
    }
}