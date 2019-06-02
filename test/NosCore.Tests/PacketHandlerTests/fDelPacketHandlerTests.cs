//using System;
//using System.Collections.Generic;
//using System.Linq;
//using ChickenAPI.Packets.ClientPackets.Relations;
//using ChickenAPI.Packets.Enumerations;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using NosCore.Configuration;
//using NosCore.Core;
//using NosCore.Core.I18N;
//using NosCore.Core.Networking;
//using NosCore.Data;
//using NosCore.Data.Enumerations;
//using NosCore.Data.WebApi;
//using NosCore.Database.DAL;
//using NosCore.GameObject;
//using NosCore.GameObject.Networking;
//using NosCore.GameObject.Networking.ClientSession;
//using NosCore.PacketHandlers.Friend;
//using NosCore.Tests.Helpers;
//using Serilog;
//using Character = NosCore.GameObject.Character;

//namespace NosCore.Tests.PacketHandlerTests
//{
//    [TestClass]
//    public class FDelPacketHandlerTests
//    {
//        private FdelPacketHandler _fDelPacketHandler;
//        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
//        private IGenericDao<CharacterRelationDto> _characterRelationDao;
//        private ClientSession _session;
//        private Mock<IWebApiAccess> _webApiAccess;

//        [TestInitialize]
//        public void Setup()
//        {
//            _characterRelationDao = new GenericDao<NosCore.Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
//            Broadcaster.Reset();
//            TestHelpers.Reset();
//            _session = TestHelpers.Instance.GenerateSession();
//            _webApiAccess = new Mock<IWebApiAccess>();
//            _fDelPacketHandler = new FdelPacketHandler(_logger, _webApiAccess.Object);
//        }

//        [TestMethod]
//        public void Test_Delete_Friend_When_Disconnected()
//        {
//            var guid = Guid.NewGuid();
//            var targetGuid = Guid.NewGuid();
//            _session.Character.CharacterRelations.TryAdd(guid,
//                new CharacterRelation
//                {
//                    CharacterId = _session.Character.CharacterId,
//                    CharacterRelationId = guid,
//                    RelatedCharacterId = 2,
//                    RelationType = CharacterRelationType.Friend
//                });
//            _session.Character.RelationWithCharacter.TryAdd(targetGuid,
//                new CharacterRelation
//                {
//                    CharacterId = 2,
//                    CharacterRelationId = targetGuid,
//                    RelatedCharacterId = _session.Character.CharacterId,
//                    RelationType = CharacterRelationType.Friend
//                });

//            Assert.IsTrue(_session.Character.CharacterRelations.Count == 1 &&
//                _session.Character.RelationWithCharacter.Count == 1);

//            var fdelPacket = new FdelPacket
//            {
//                CharacterId = 2
//            };

//            _fDelPacketHandler.Execute(fdelPacket, _session);

//            Assert.IsTrue(_characterRelationDao.LoadAll().Count() == 0);
//        }

//        [TestMethod]
//        public void Test_Delete_Friend()
//        {
//            var targetSession = TestHelpers.Instance.GenerateSession();
//            var fdelPacket = new FdelPacket
//            {
//                CharacterId = targetSession.Character.CharacterId
//            };

//            targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
//            var finsPacket = new FinsPacket
//            {
//                CharacterId = targetSession.Character.CharacterId,
//                Type = FinsPacketType.Accepted
//            };
//            new FinsPacketHandler(_webApiAccess.Object).Execute(finsPacket, _session);
//            _fDelPacketHandler.Execute(fdelPacket, _session);
//            Assert.IsTrue(_characterRelationDao.LoadAll().Count() == 0);
//        }
//    }
//}