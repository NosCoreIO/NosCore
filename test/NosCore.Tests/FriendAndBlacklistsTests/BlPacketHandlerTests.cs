using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
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
    public class BlPacketHandlerTests
    {
        private BlPacketHandler _blPacketHandler;
        private ClientSession _session;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private IGenericDao<CharacterRelationDto> _characterRelationDao;

        [TestInitialize]
        public void Setup()
        {
            _characterRelationDao = new GenericDao<NosCore.Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
            Broadcaster.Reset();
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            TestHelpers.Instance.ConnectedAccountHttpClient.Setup(s => s.GetCharacter(_session.Character.CharacterId, null))
             .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _session.Character.CharacterId } }));
            TestHelpers.Instance.ConnectedAccountHttpClient.Setup(s => s.GetCharacter(null, _session.Character.Name))
             .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _session.Character.CharacterId } }));
            _blPacketHandler = new BlPacketHandler();
        }

        [TestMethod]
        public void Test_Distant_Blacklist()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var blPacket = new BlPacket
            {
                CharacterName = targetSession.Character.Name
            };
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
            _blPacketHandler.Execute(blPacket, _session);
            Assert.IsTrue(_characterRelationDao.FirstOrDefault(s =>
            s.CharacterId == _session.Character.CharacterId &&
                s.RelatedCharacterId == targetSession.Character.CharacterId
                && s.RelationType == CharacterRelationType.Blocked) != null);
        }

    }
}
