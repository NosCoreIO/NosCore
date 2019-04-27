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
    public class BlInsPacketHandlerTests
    {
        private BlInsPackettHandler _blInsPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            Broadcaster.Reset();
            TestHelpers.Reset();
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<WebApiRoute, object>
                {
                    {WebApiRoute.Channel, new List<ChannelInfo> {new ChannelInfo()}},
                    {WebApiRoute.ConnectedAccount, new List<ConnectedAccount>()}
                };

            _session = TestHelpers.Instance.GenerateSession();
            _blInsPacketHandler = new BlInsPackettHandler(_logger);
        }

        [TestMethod]
        public void Test_Blacklist_When_Disconnected()
        {
            var blinsPacket = new BlInsPacket
            {
                CharacterId = 2
            };

            _blInsPacketHandler.Execute(blinsPacket, _session);
            Assert.IsFalse(
                _session.Character.CharacterRelations.Any(s => s.Value.RelationType == CharacterRelationType.Blocked));
        }


        [TestMethod]
        public void Test_Blacklist_Character()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var blinsPacket = new BlInsPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

            _blInsPacketHandler.Execute(blinsPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s =>
                s.Value.RelatedCharacterId == targetSession.Character.CharacterId
                && s.Value.RelationType == CharacterRelationType.Blocked));
        }
    }
}
