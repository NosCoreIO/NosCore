using System.Collections.Concurrent;
using System.Collections.Generic;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Groups;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.Group;
using NosCore.PacketHandlers.Group;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PJoinPacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly Dictionary<int, Character> _characters = new Dictionary<int, Character>();
        private PjoinPacketHandler _pJoinPacketHandler;

        [TestInitialize]
        public void Setup()
        {
            Broadcaster.Reset();
            GroupAccess.Instance.Groups = new ConcurrentDictionary<long, Group>();
            for (byte i = 0; i < (byte)(GroupType.Group + 1); i++)
            {
                var session = TestHelpers.Instance.GenerateSession();
                session.RegisterChannel(null);
                _characters.Add(i, session.Character);
                session.Character.Group.JoinGroup(session.Character);
            }

            var mock = new Mock<IBlacklistHttpClient>();
            _pJoinPacketHandler = new PjoinPacketHandler(_logger, mock.Object);
        }

        [TestMethod]
        public void Test_Accept_Group_Join_Requested()
        {
            _characters[1].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            Assert.IsTrue(_characters[0].Group.Count > 1
                && _characters[1].Group.Count > 1
                && _characters[0].Group.GroupId
                == _characters[1].Group.GroupId);
        }

        [TestMethod]
        public void Test_Join_Full_Group()
        {
            PjoinPacket pjoinPacket;

            for (var i = 1; i < 3; i++)
            {
                _characters[i].GroupRequestCharacterIds
                    .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

                pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _characters[i].CharacterId
                };

                _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull);

            _characters[3].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[3].CharacterId
            };

            _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            Assert.IsTrue(_characters[3].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Accept_Not_Requested_Group()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            Assert.IsTrue(_characters[0].Group.Count == 1
                && _characters[1].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Decline_Not_Requested_Group()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = _characters[1].CharacterId
            };

            _pJoinPacketHandler.Execute(pjoinPacket, _characters[0].Session);
            Assert.IsTrue(_characters[0].Group.Count == 1
                && _characters[1].Group.Count == 1);
        }
    }
}
