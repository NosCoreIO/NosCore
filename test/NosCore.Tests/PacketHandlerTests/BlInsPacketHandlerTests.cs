using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.Database.DAL;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class BlInsPacketHandlerTests
    {
        private BlInsPackettHandler _blInsPacketHandler;
        private ClientSession _session;
        private Mock<IWebApiAccess> _webApiAccess;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao = new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);

        [TestInitialize]
        public void Setup()
        {
            Broadcaster.Reset();
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _webApiAccess = new Mock<IWebApiAccess>();
            _blInsPacketHandler = new BlInsPackettHandler(_webApiAccess.Object);
        }

        [TestMethod]
        public void Test_Blacklist_When_Disconnected()
        {
            var blinsPacket = new BlInsPacket
            {
                CharacterId = 2
            };

            _blInsPacketHandler.Execute(blinsPacket, _session);
            Assert.IsNull(
                _characterRelationDao.FirstOrDefault(s => _session.Character.CharacterId == s.CharacterId && s.RelationType == CharacterRelationType.Blocked));
        }

        //TODO fix
        //[TestMethod]
        //public void Test_Blacklist_Character()
        //{
        //    var targetSession = TestHelpers.Instance.GenerateSession();
        //    var blinsPacket = new BlInsPacket
        //    {
        //        CharacterId = targetSession.Character.CharacterId
        //    };

        //    _blInsPacketHandler.Execute(blinsPacket, _session);
        //    Assert.IsNotNull(
        //        _characterRelationDao.FirstOrDefault(s => _session.Character.CharacterId == s.CharacterId && targetSession.Character.CharacterId == s.RelatedCharacterId && s.RelationType == CharacterRelationType.Blocked));
        //}
    }
}
