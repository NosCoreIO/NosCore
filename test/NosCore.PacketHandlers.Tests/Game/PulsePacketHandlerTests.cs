//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Tests.Shared;
using SpecLight;

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
