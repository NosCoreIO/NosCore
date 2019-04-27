using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class BlPacketHandlerTests
    {
        private BlPacketHandler _blPacketHandler;
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

            _blPacketHandler.Execute(blPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s =>
                s.Value.RelatedCharacterId == targetSession.Character.CharacterId
                && s.Value.RelationType == CharacterRelationType.Blocked));
        }

    }
}
