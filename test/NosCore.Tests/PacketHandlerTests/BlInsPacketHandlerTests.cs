using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Database.DAL;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.MasterServer.Controllers;
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
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao = new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);

        [TestInitialize]
        public void Setup()
        {
            Broadcaster.Reset();
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _blInsPacketHandler = new BlInsPackettHandler(TestHelpers.Instance.BlacklistHttpClient.Object);
            TestHelpers.Instance.ConnectedAccountHttpClient.Setup(s => s.GetCharacter(_session.Character.CharacterId, null))
             .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _session.Character.CharacterId } }));
            TestHelpers.Instance.ConnectedAccountHttpClient.Setup(s => s.GetCharacter(null, _session.Character.Name))
             .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _session.Character.CharacterId } }));
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

        [TestMethod]
        public void Test_Blacklist_Character()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            TestHelpers.Instance.ConnectedAccountHttpClient.Setup(s => s.GetCharacter(targetSession.Character.CharacterId, null))
            .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = targetSession.Character.CharacterId } }));
            var blacklist = new BlacklistController(TestHelpers.Instance.ConnectedAccountHttpClient.Object, _characterRelationDao, TestHelpers.Instance.CharacterDao);
            TestHelpers.Instance.BlacklistHttpClient.Setup(s => s.AddToBlacklist(It.IsAny<BlacklistRequest>()))
                .Returns(blacklist.AddBlacklist(new BlacklistRequest
                {
                    CharacterId = _session.Character.CharacterId,
                    BlInsPacket = new BlInsPacket
                    {
                        CharacterId = targetSession.Character.VisualId
                    }
                }));
            var blinsPacket = new BlInsPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

            _blInsPacketHandler.Execute(blinsPacket, _session);
            Assert.IsNotNull(
                _characterRelationDao.FirstOrDefault(s => _session.Character.CharacterId == s.CharacterId
                    && targetSession.Character.CharacterId == s.RelatedCharacterId && s.RelationType == CharacterRelationType.Blocked));
        }
    }
}
