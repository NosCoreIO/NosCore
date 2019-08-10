using System;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Providers.MinilandProvider;
using ChickenAPI.Packets.ClientPackets.Miniland;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Character;
using System.Collections.Generic;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class MJoinPacketHandlerTests
    {
        private MJoinPacketHandler _mjoinPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private ClientSession _session;
        private ClientSession _targetSession;
        private Mock<IFriendHttpClient> _friendHttpClient;
        private Mock<IConnectedAccountHttpClient> _connectedAccountHttpClient;
        private Mock<IMinilandProvider> _minilandProvider;

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig().ConstructUsing(src => new MapNpc(null, null, null, null, _logger));
            Broadcaster.Reset();
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _targetSession = TestHelpers.Instance.GenerateSession();
            _minilandProvider = new Mock<IMinilandProvider>();
            _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
            _friendHttpClient = TestHelpers.Instance.FriendHttpClient;
            _mjoinPacketHandler = new MJoinPacketHandler(_friendHttpClient.Object, _minilandProvider.Object);
        }

        [TestMethod]
        public void JoinNonConnected()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = 50,
                Type = VisualType.Player
            };
            _mjoinPacketHandler.Execute(mjoinPacket, _session);

            Assert.IsFalse(_session.Character.MapInstance.Map.MapId == 20001);
        }

        [TestMethod]
        public void JoinNonFriend()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession.Character.CharacterId,
                Type = VisualType.Player
            };
            _mjoinPacketHandler.Execute(mjoinPacket, _session);

            Assert.IsFalse(_session.Character.MapInstance.Map.MapId == 20001);
        }


        [TestMethod]
        public void JoinClosed()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession.Character.CharacterId,
                Type = VisualType.Player
            };
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_targetSession.Character.CharacterId, null))
                .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _targetSession.Character.CharacterId } }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_session.Character.CharacterId, null))
                .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _session.Character.CharacterId } }));
            _friendHttpClient.Setup(s => s.GetListFriends(It.IsAny<long>())).Returns(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Friend
                }
            });
            _minilandProvider.Setup(s => s.GetMinilandInfo(It.IsAny<long>())).Returns(new MinilandInfo { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Lock });
            _mjoinPacketHandler.Execute(mjoinPacket, _session);

            var lastpacket = (InfoPacket)_session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(lastpacket.Message, Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_CLOSED_BY_FRIEND, _session.Account.Language));
            Assert.IsFalse(_session.Character.MapInstance.Map.MapId == 20001);
        }

        [TestMethod]
        public void Join()
        {
            var mjoinPacket = new MJoinPacket
            {
                VisualId = _targetSession.Character.CharacterId,
                Type = VisualType.Player
            };
            _minilandProvider.Setup(s => s.GetMinilandInfo(It.IsAny<long>())).Returns(new MinilandInfo { MapInstanceId = TestHelpers.Instance.MinilandId, State = MinilandState.Open });
            _friendHttpClient.Setup(s => s.GetListFriends(It.IsAny<long>())).Returns(new List<CharacterRelationStatus>
            {
                new CharacterRelationStatus {
                    CharacterId = _targetSession.Character.CharacterId,
                    IsConnected = true,
                    CharacterName = _targetSession.Character.Name,
                    RelationType = CharacterRelationType.Friend
                }
            });
            _mjoinPacketHandler.Execute(mjoinPacket, _session);
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_targetSession.Character.CharacterId, null))
                .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _targetSession.Character.CharacterId } }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_session.Character.CharacterId, null))
                .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _session.Character.CharacterId } }));

            Assert.IsTrue(_session.Character.MapInstance.Map.MapId == 20001);
        }
    }
}
