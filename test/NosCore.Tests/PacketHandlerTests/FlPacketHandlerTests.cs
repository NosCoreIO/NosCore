using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
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
    public class FlPacketHandlerTests
    {
        private FlPacketHandler _flPacketHandler;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {

            TestHelpers.Reset();
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
            _flPacketHandler = new FlPacketHandler();
        }

        [TestMethod]
        public void Test_Add_Distant_Friend()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
            var flPacket = new FlPacket
            {
                CharacterName = targetSession.Character.Name
            };

            _flPacketHandler.Execute(flPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s =>
                    s.Value.RelatedCharacterId == targetSession.Character.CharacterId)
                && targetSession.Character.CharacterRelations.Any(s =>
                    s.Value.RelatedCharacterId == _session.Character.CharacterId));
        }

    }
}
