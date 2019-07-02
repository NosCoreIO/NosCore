using System;
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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Database.DAL;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.MasterServer;
using NosCore.MasterServer.Controllers;
using NosCore.MasterServer.DataHolders;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class FlPacketHandlerTests
    {
        private FlPacketHandler _flPacketHandler;
        private ClientSession _session;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private IGenericDao<CharacterRelationDto> _characterRelationDao;

        [TestInitialize]
        public void Setup()
        {
            _characterRelationDao = new GenericDao<NosCore.Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
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
            friendRequestHolder.FriendRequestCharacters.TryAdd(Guid.NewGuid(), new Tuple<long, long>(targetSession.Character.CharacterId, _session.Character.CharacterId));
            var flPacket = new FlPacket
            {
                CharacterName = targetSession.Character.Name
            };
            TestHelpers.Instance.WebApiMock.Setup(s => s.GetCharacter(targetSession.Character.CharacterId, null))
              .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = targetSession.Character.CharacterId } }));
            TestHelpers.Instance.WebApiMock.Setup(s => s.GetCharacter(_session.Character.CharacterId, null))
                .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _session.Character.CharacterId } }));
            var friend = new FriendController(_logger, _characterRelationDao, TestHelpers.Instance.CharacterDao, friendRequestHolder, TestHelpers.Instance.WebApiMock.Object);
            TestHelpers.Instance.WebApiMock.Setup(s => s.Post<LanguageKey>(WebApiRoute.Friend, It.IsAny<FriendShipRequest>(), It.IsAny<ServerConfiguration>()))
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
               s.CharacterId == _session.Character.CharacterId &&
                   s.RelatedCharacterId == targetSession.Character.CharacterId
                   && s.RelationType == CharacterRelationType.Friend) != null);
        }

    }
}
