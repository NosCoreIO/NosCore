using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PulsePacketHandlerTests
    {
        private PulsePacketHandler _pulsePacketHandler;


        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            _pulsePacketHandler = new PulsePacketHandler();
            Broadcaster.Reset();
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
        }

        [TestMethod]
        public void Test_Pulse_Packet()
        {
            var pulsePacket = new PulsePacket
            {
                Tick = 0
            };

            for (var i = 60; i < 600; i += 60)
            {
                pulsePacket = new PulsePacket
                {
                    Tick = i
                };
                _pulsePacketHandler.Execute(pulsePacket, _session);
            }

            Assert.IsTrue(_session.LastPulse == pulsePacket.Tick);
        }
    }
}
