using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class BlDelPacketHandlerTests
    {
        private BlDelPacketHandler _blDelPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
     
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            Broadcaster.Reset();
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<WebApiRoute, object>
                {
                    {WebApiRoute.Channel, new List<ChannelInfo> {new ChannelInfo()}},
                    {WebApiRoute.ConnectedAccount, new List<ConnectedAccount>()}
                };


            _session = TestHelpers.Instance.GenerateSession();
            _blDelPacketHandler = new BlDelPacketHandler();
        }


        [TestMethod]
        public void Test_Delete_Blacklist_When_Disconnected()
        {
            var guid = Guid.NewGuid();

            _session.Character.CharacterRelations.TryAdd(guid,
                new CharacterRelation
                {
                    CharacterId = _session.Character.CharacterId,
                    CharacterRelationId = guid,
                    RelatedCharacterId = 2,
                    RelationType = CharacterRelationType.Blocked
                });

            var bldelPacket = new BlDelPacket
            {
                CharacterId = 2
            };

            Assert.IsTrue(_session.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == 2));

            _blDelPacketHandler.Execute(bldelPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.All(s => s.Value.RelatedCharacterId != 2));
        }


        [TestMethod]
        public void Test_Delete_Blacklist()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var blinsPacket = new BlInsPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };
            new BlInsPackettHandler(_logger).Execute(blinsPacket, _session);

            var bldelPacket = new BlDelPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };
            _blDelPacketHandler.Execute(bldelPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.All(s =>
                s.Value.RelatedCharacterId != targetSession.Character.CharacterId));
        }
    }
}
