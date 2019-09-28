using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.DAL;
using NosCore.Database.Entities;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.MasterServer.Controllers;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.FriendAndBlacklistsTests
{
    [TestClass]
    public class BDelPacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private BlacklistController _blackListController;
        private Mock<IBlacklistHttpClient> _blackListHttpClient;
        private BlDelPacketHandler _BlDelPacketHandler;
        private Mock<IGenericDao<CharacterDto>> _characterDao;
        private IGenericDao<CharacterRelationDto> _characterRelationDao;
        private Mock<IConnectedAccountHttpClient> _connectedAccountHttpClient;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            _characterRelationDao = new GenericDao<CharacterRelation, CharacterRelationDto>(_logger);
            Broadcaster.Reset();
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
            _blackListHttpClient = TestHelpers.Instance.BlacklistHttpClient;
            _BlDelPacketHandler = new BlDelPacketHandler(_blackListHttpClient.Object);
            _characterDao = new Mock<IGenericDao<CharacterDto>>();
            _blackListController = new BlacklistController(_connectedAccountHttpClient.Object, _characterRelationDao,
                _characterDao.Object);
            _blackListHttpClient.Setup(s => s.GetBlackLists(It.IsAny<long>()))
                .Returns((long id) => _blackListController.GetBlacklisted(id));
            _blackListHttpClient.Setup(s => s.DeleteFromBlacklist(It.IsAny<Guid>()))
                .Callback((Guid id) => _blackListController.Delete(id));
        }

        [TestMethod]
        public void Test_Delete_Friend_When_Disconnected()
        {
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                _session.Character,
                new CharacterDto {CharacterId = 2, Name = "test"}
            };
            _characterDao.Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => list.FirstOrDefault(exp.Compile()));
            _characterRelationDao.InsertOrUpdate(new[]
            {
                new CharacterRelationDto
                {
                    RelatedCharacterId = 2,
                    CharacterRelationId = targetGuid,
                    CharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Blocked
                }
            });
            var blDelPacket = new BlDelPacket
            {
                CharacterId = 2
            };

            _BlDelPacketHandler.Execute(blDelPacket, _session);

            Assert.IsTrue(_characterRelationDao.LoadAll().Count() == 0);
        }

        [TestMethod]
        public void Test_Delete_Friend()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                _session.Character,
                targetSession.Character
            };
            _characterDao.Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => list.FirstOrDefault(exp.Compile()));
            _characterRelationDao.InsertOrUpdate(new[]
            {
                new CharacterRelationDto
                {
                    RelatedCharacterId = targetSession.Character.CharacterId,
                    CharacterRelationId = targetGuid,
                    CharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Blocked
                }
            });
            var blDelPacket = new BlDelPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

            _BlDelPacketHandler.Execute(blDelPacket, _session);

            Assert.IsTrue(_characterRelationDao.LoadAll().Count() == 0);
        }

        [TestMethod]
        public void Test_Delete_Friend_No_Friend()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                _session.Character,
                targetSession.Character
            };
            _characterDao.Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => list.FirstOrDefault(exp.Compile()));

            var blDelPacket = new BlDelPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

            _BlDelPacketHandler.Execute(blDelPacket, _session);
            var lastpacket = (InfoPacket) _session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(Language.Instance.GetMessageFromKey(LanguageKey.NOT_IN_BLACKLIST,
                _session.Account.Language), lastpacket.Message);
        }
    }
}