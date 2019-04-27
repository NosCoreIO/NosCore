using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
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
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class FDelPacketHandlerTests
    {
        private FdelPacketHandler _fDelPacketHandler;
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

            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _fDelPacketHandler = new FdelPacketHandler();
        }


        [TestMethod]
        public void Test_Delete_Friend_When_Disconnected()
        {
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            _session.Character.CharacterRelations.TryAdd(guid,
                new CharacterRelation
                {
                    CharacterId = _session.Character.CharacterId,
                    CharacterRelationId = guid,
                    RelatedCharacterId = 2,
                    RelationType = CharacterRelationType.Friend
                });
            _session.Character.RelationWithCharacter.TryAdd(targetGuid,
                new CharacterRelation
                {
                    CharacterId = 2,
                    CharacterRelationId = targetGuid,
                    RelatedCharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                });

            Assert.IsTrue(_session.Character.CharacterRelations.Count == 1 &&
                _session.Character.RelationWithCharacter.Count == 1);

            var fdelPacket = new FdelPacket
            {
                CharacterId = 2
            };

            _fDelPacketHandler.Execute(fdelPacket, _session);

            Assert.IsTrue(_session.Character.CharacterRelations.IsEmpty);
        }


        [TestMethod]
        public void Test_Delete_Friend()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var fdelPacket = new FdelPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

            targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
            var finsPacket = new FinsPacket
            {
                CharacterId = targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            new FinsPacketHandler(new WorldConfiguration(), _logger).Execute(finsPacket, _session);
            _fDelPacketHandler.Execute(fdelPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.All(s =>
                    s.Value.RelatedCharacterId != targetSession.Character.CharacterId)
                && targetSession.Character.CharacterRelations.All(s =>
                    s.Value.RelatedCharacterId != _session.Character.CharacterId));
        }
    }
}