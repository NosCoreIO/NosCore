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
using NosCore.Data.WebApi;
using NosCore.Database.DAL;
using NosCore.FriendServer;
using NosCore.FriendServer.Controllers;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
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
        private GenericDao<CharacterRelation, CharacterRelationDto> _characterRelationDao;
        private FriendRequestHolder _friendRequestHolder;

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig().ConstructUsing(src => new MapNpc(null, null, null, null, _logger));
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            var webApiAccess = new Mock<IWebApiAccess>();
            _characterRelationDao = new GenericDao<CharacterRelation, CharacterRelationDto>(_logger);
            var CharacterDao = new GenericDao<Character, CharacterDto>(_logger);
            _friendRequestHolder = new FriendRequestHolder();
            var friend = new FriendController(_logger, _characterRelationDao, CharacterDao, _friendRequestHolder, webApiAccess.Object);
            webApiAccess.Setup(s => s.Post<FriendShipRequest>(WebApiRoute.Friend, It.IsAny<ServerConfiguration>()));
            _finsPacketHandler = new FinsPacketHandler(webApiAccess.Object);
        }

        [TestMethod]
        public void Test_Add_Friend()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            _friendRequestHolder.FriendRequestCharacters.TryAdd(Guid.NewGuid(),
                new Tuple<long, long>(targetSession.Character.CharacterId, _session.Character.CharacterId));
            var finsPacket = new FinsPacket
            {
                CharacterId = targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            _finsPacketHandler.Execute(finsPacket, _session);
            Assert.IsTrue(_characterRelationDao.LoadAll().Count() == 2);
        }

        [TestMethod]
        public void Test_Add_Friend_When_Disconnected()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = 2,
                Type = FinsPacketType.Accepted
            };
            _finsPacketHandler.Execute(finsPacket, _session);

            Assert.IsFalse(_characterRelationDao.LoadAll().Any());
        }

        [TestMethod]
        public void Test_Add_Not_Requested_Friend()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var finsPacket = new FinsPacket
            {
                CharacterId = targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            _finsPacketHandler.Execute(finsPacket, _session);
            Assert.IsFalse(_characterRelationDao.LoadAll().Any());
        }

    }
}
