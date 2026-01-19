//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class PulsePacketHandlerTests
    {
        private PulsePacketHandler PulsePacketHandler = null!;
        private ClientSession Session = null!;
        private PulsePacket LastPulsePacket = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            PulsePacketHandler = new PulsePacketHandler();
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        [TestMethod]
        public async Task PulsePacketShouldUpdateLastPulse()
        {
            await new Spec("Pulse packet should update last pulse")
                .WhenAsync(SendingMultiplePulsePackets)
                .Then(LastPulseShouldBeUpdated)
                .ExecuteAsync();
        }

        private async Task SendingMultiplePulsePackets()
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
                await PulsePacketHandler.ExecuteAsync(pulsePacket, Session);
            }

            LastPulsePacket = pulsePacket;
        }

        private void LastPulseShouldBeUpdated()
        {
            Assert.IsTrue(Session.LastPulse == LastPulsePacket.Tick);
        }
    }
}
