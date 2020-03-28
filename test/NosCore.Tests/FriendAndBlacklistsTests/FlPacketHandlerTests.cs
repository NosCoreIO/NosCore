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
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.Database.DAL;
using NosCore.Database.Entities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.MasterServer.Controllers;
using NosCore.MasterServer.DataHolders;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.Tests.FriendAndBlacklistsTests
{
    [TestClass]
    public class FlPacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private IGenericDao<CharacterRelationDto> _characterRelationDao;
        private FlPacketHandler _flPacketHandler;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            _characterRelationDao = new GenericDao<CharacterRelation, CharacterRelationDto, Guid>(_logger);
            TestHelpers.Reset();
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _flPacketHandler = new FlPacketHandler();
        }

        [TestMethod]
        public void Test_Add_Distant_Friend()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var friendRequestHolder = new FriendRequestHolder();
            friendRequestHolder.FriendRequestCharacters.TryAdd(Guid.NewGuid(),
                new Tuple<long, long>(targetSession.Character.CharacterId, _session.Character.CharacterId));
            var flPacket = new FlPacket
            {
                CharacterName = targetSession.Character.Name
            };
            TestHelpers.Instance.ConnectedAccountHttpClient
                .Setup(s => s.GetCharacter(targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = targetSession.Character.CharacterId }
                    }));
            TestHelpers.Instance.ConnectedAccountHttpClient
                .Setup(s => s.GetCharacter(_session.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    { ChannelId = 1, ConnectedCharacter = new Character { Id = _session.Character.CharacterId } }));
            using var friend = new FriendController(_logger, _characterRelationDao, TestHelpers.Instance.CharacterDao,
                friendRequestHolder, TestHelpers.Instance.ConnectedAccountHttpClient.Object);
            TestHelpers.Instance.FriendHttpClient.Setup(s => s.AddFriend(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriend(new FriendShipRequest
                {
                    CharacterId = _session.Character.CharacterId,
                    FinsPacket = new FinsPacket
                    {
                        CharacterId = targetSession.Character.VisualId,
                        Type = FinsPacketType.Accepted
                    }
                }));

            _flPacketHandler.Execute(flPacket, _session);
            Assert.IsTrue(_characterRelationDao.FirstOrDefault(s =>
                (s.CharacterId == _session.Character.CharacterId) &&
                (s.RelatedCharacterId == targetSession.Character.CharacterId)
                && (s.RelationType == CharacterRelationType.Friend)) != null);
        }
    }
}