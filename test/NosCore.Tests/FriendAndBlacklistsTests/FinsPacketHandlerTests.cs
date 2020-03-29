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
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.MasterServer.Controllers;
using NosCore.MasterServer.DataHolders;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.Data.WebApi.Character;
using CharacterRelation = NosCore.Database.Entities.CharacterRelation;

namespace NosCore.Tests.FriendAndBlacklistsTests
{
    [TestClass]
    public class FinsPacketHandlerTests
    {
        private static readonly ILogger Logger = Core.I18N.Logger.GetLoggerConfiguration().CreateLogger();
        private readonly Mock<IChannelHttpClient> _channelHttpClient = TestHelpers.Instance.ChannelHttpClient;
        private IGenericDao<CharacterRelationDto>? _characterRelationDao;
        private readonly Mock<IConnectedAccountHttpClient> _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
        private FinsPacketHandler? _finsPacketHandler;
        private readonly Mock<IFriendHttpClient> _friendHttpClient = TestHelpers.Instance.FriendHttpClient;
        private FriendRequestHolder? _friendRequestHolder;

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
            _characterRelationDao = new GenericDao<CharacterRelation, CharacterRelationDto, Guid>(Logger);
            _friendRequestHolder = new FriendRequestHolder();
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character {Id = _targetSession.Character.CharacterId}
                    }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_session.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                        {ChannelId = 1, ConnectedCharacter = new Character {Id = _session.Character.CharacterId}}));
            _finsPacketHandler = new FinsPacketHandler(_friendHttpClient.Object, _channelHttpClient.Object,
                _connectedAccountHttpClient.Object);
        }

        [TestMethod]
        public async Task Test_Add_Friend()
        {
            _friendRequestHolder!.FriendRequestCharacters.TryAdd(Guid.NewGuid(),
                new Tuple<long, long>(_targetSession!.Character.CharacterId, _session!.Character.CharacterId));
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };

            using var friend = new FriendController(Logger, _characterRelationDao!, TestHelpers.Instance.CharacterDao,
                _friendRequestHolder, _connectedAccountHttpClient.Object);
            _friendHttpClient.Setup(s => s.AddFriend(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriend(new FriendShipRequest
                    {CharacterId = _session.Character.CharacterId, FinsPacket = finsPacket}));
            await _finsPacketHandler!.Execute(finsPacket, _session).ConfigureAwait(false);
            Assert.IsTrue(_characterRelationDao!.LoadAll().Count() == 2);
        }

        [TestMethod]
        public async Task Test_Add_Friend_When_Disconnected()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession!.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            using var friend = new FriendController(Logger, _characterRelationDao!, TestHelpers.Instance.CharacterDao,
                _friendRequestHolder!, _connectedAccountHttpClient.Object);
            _friendHttpClient.Setup(s => s.AddFriend(It.IsAny<FriendShipRequest>())).Returns(
                friend.AddFriend(new FriendShipRequest
                    {CharacterId = _session!.Character.CharacterId, FinsPacket = finsPacket}));
            await _finsPacketHandler!.Execute(finsPacket, _session).ConfigureAwait(false);

            Assert.IsFalse(_characterRelationDao!.LoadAll().Any());
        }

        [TestMethod]
        public async Task Test_Add_Not_Requested_Friend()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession!.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            using var friend = new FriendController(Logger, _characterRelationDao!, TestHelpers.Instance.CharacterDao,
                _friendRequestHolder!, _connectedAccountHttpClient.Object);
            _friendHttpClient.Setup(s => s.AddFriend(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriend(new FriendShipRequest
                    {CharacterId = _session!.Character.CharacterId, FinsPacket = finsPacket}));

            await _finsPacketHandler!.Execute(finsPacket, _session).ConfigureAwait(false);
            Assert.IsFalse(_characterRelationDao!.LoadAll().Any());
        }
    }
}