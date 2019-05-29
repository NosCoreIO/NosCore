using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.MasterServer;
using NosCore.MasterServer.Controllers;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class FinsPacketHandlerTests
    {
        private FinsPacketHandler _finsPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private ClientSession _session;
        private ClientSession _targetSession;
        private IGenericDao<CharacterRelationDto> _characterRelationDao;
        private FriendRequestHolder _friendRequestHolder;
        private Mock<IWebApiAccess> _webApiAccess;

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig().ConstructUsing(src => new MapNpc(null, null, null, null, _logger));
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _targetSession = TestHelpers.Instance.GenerateSession();
            _webApiAccess = new Mock<IWebApiAccess>();
            _characterRelationDao = new GenericDao<NosCore.Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
            _friendRequestHolder = new FriendRequestHolder();
            _webApiAccess.Setup(s => s.GetCharacter(_targetSession.Character.CharacterId, null))
                .Returns((new ServerConfiguration(), new ConnectedAccount() { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _targetSession.Character.CharacterId } }));
            _webApiAccess.Setup(s => s.GetCharacter(_session.Character.CharacterId, null))
                .Returns((new ServerConfiguration(), new ConnectedAccount() { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _session.Character.CharacterId } }));
            _finsPacketHandler = new FinsPacketHandler(_webApiAccess.Object);
        }

        [TestMethod]
        public void Test_Add_Friend()
        {
            _friendRequestHolder.FriendRequestCharacters.TryAdd(Guid.NewGuid(),
                new Tuple<long, long>(_session.Character.CharacterId, _targetSession.Character.CharacterId));
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };

            var friend = new FriendController(_logger, _characterRelationDao, TestHelpers.Instance.CharacterDao, _friendRequestHolder, _webApiAccess.Object);
            _webApiAccess.Setup(s => s.Post<LanguageKey>(WebApiRoute.Friend, It.IsAny<FriendShipRequest>(), It.IsAny<ServerConfiguration>()))
                .Returns(friend.AddFriend(new FriendShipRequest { CharacterId = _session.Character.CharacterId, FinsPacket = finsPacket }));
            _finsPacketHandler.Execute(finsPacket, _session);
            Assert.IsTrue(_characterRelationDao.LoadAll().Count() == 1);
        }

        [TestMethod]
        public void Test_Add_Friend_When_Disconnected()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            var friend = new FriendController(_logger, _characterRelationDao, TestHelpers.Instance.CharacterDao, _friendRequestHolder, _webApiAccess.Object);
            _webApiAccess.Setup(s => s.Post<LanguageKey>(WebApiRoute.Friend, It.IsAny<FriendShipRequest>(), It.IsAny<ServerConfiguration>())).Returns(friend.AddFriend(new FriendShipRequest { CharacterId = _session.Character.CharacterId, FinsPacket = finsPacket }));
            _finsPacketHandler.Execute(finsPacket, _session);

            Assert.IsFalse(_characterRelationDao.LoadAll().Any());
        }

        [TestMethod]
        public void Test_Add_Not_Requested_Friend()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            var friend = new FriendController(_logger, _characterRelationDao, TestHelpers.Instance.CharacterDao, _friendRequestHolder, _webApiAccess.Object);
            _webApiAccess.Setup(s => s.Post<LanguageKey>(WebApiRoute.Friend, It.IsAny<FriendShipRequest>(), It.IsAny<ServerConfiguration>())).Returns(friend.AddFriend(new FriendShipRequest { CharacterId = _session.Character.CharacterId, FinsPacket = finsPacket }));

            _finsPacketHandler.Execute(finsPacket, _session);
            Assert.IsFalse(_characterRelationDao.LoadAll().Any());
        }

    }
}
