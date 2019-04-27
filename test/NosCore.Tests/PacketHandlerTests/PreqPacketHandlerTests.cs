using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Movement;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PreqPacketHandlerTests
    {
        private ClientSession _session;
        private PreqPacketHandler _preqPacketHandler;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues = new Dictionary<WebApiRoute, object>();
            _preqPacketHandler = new PreqPacketHandler(TestHelpers.Instance.MapInstanceProvider);
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceProvider.GetBaseMapById(0);

            _session.Character.MapInstance.Portals = new List<Portal> { new Portal { DestinationMapId = 1,
                DestinationMapInstanceId = TestHelpers.Instance.MapInstanceProvider.GetBaseMapInstanceIdByMapId(1),
                DestinationX = 5, DestinationY = 5, SourceMapId = 0, SourceMapInstanceId = TestHelpers.Instance.MapInstanceProvider.GetBaseMapInstanceIdByMapId(0), SourceX = 0, SourceY = 0 } };
        }

        [TestMethod]
        public void UserCanUsePortal()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _preqPacketHandler.Execute(new PreqPacket(), _session);
            Assert.IsTrue(_session.Character.PositionY == 5 && _session.Character.PositionX == 5 &&
                _session.Character.MapInstance.Map.MapId == 1);
        }

        [TestMethod]
        public void UserCanTUsePortalIfTooFar()
        {
            _session.Character.PositionX = 8;
            _session.Character.PositionY = 8;
            _preqPacketHandler.Execute(new PreqPacket(), _session);
            Assert.IsTrue(_session.Character.PositionY == 8 && _session.Character.PositionX == 8 &&
                _session.Character.MapInstance.Map.MapId == 0);
        }
    }
}
