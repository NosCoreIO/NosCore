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
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.MasterServer.Controllers;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.MasterServer.DataHolders;
using NosCore.GameObject.Providers.MinilandProvider;

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
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_targetSession.Character.CharacterId, null))
                .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _targetSession.Character.CharacterId } }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacter(_session.Character.CharacterId, null))
                .Returns((new ServerConfiguration(), new ConnectedAccount { ChannelId = 1, ConnectedCharacter = new Data.WebApi.Character { Id = _session.Character.CharacterId } }));

            _mjoinPacketHandler = new MJoinPacketHandler(_friendHttpClient.Object, _minilandProvider.Object);
        }

        [TestMethod]
        public void JoinNonConnected()
        {
         
        }

        [TestMethod]
        public void JoinNonFriend()
        {

        }


        [TestMethod]
        public void JoinClosed()
        {

        }

        [TestMethod]
        public void Join()
        {

        }


    }
}
