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

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SpeedPacketHandlerTests
    {
        private SpeedPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new SpeedPacketHandler();
        }

        [TestMethod]
        public async Task SpeedWithValidValueShouldSetVehicleSpeed()
        {
            await new Spec("Speed with valid value should set vehicle speed")
                .WhenAsync(SettingSpeed_, 30)
                .Then(VehicleSpeedShouldBe_, 30)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpeedWithZeroShouldShowHelp()
        {
            await new Spec("Speed with zero should show help")
                .WhenAsync(SettingSpeed_, 0)
                .Then(ShouldShowHelpMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpeedWithNegativeShouldShowHelp()
        {
            await new Spec("Speed with value 60 or more should show help")
                .WhenAsync(SettingSpeed_, 60)
                .Then(ShouldShowHelpMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpeedWith59ShouldSetSpeed()
        {
            await new Spec("Speed with 59 should set speed")
                .WhenAsync(SettingSpeed_, 59)
                .Then(VehicleSpeedShouldBe_, 59)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpeedShouldSendPacketResponse()
        {
            await new Spec("Speed should send packet response")
                .WhenAsync(SettingSpeed_, 30)
                .Then(ShouldSendPacketResponse)
                .ExecuteAsync();
        }

        private async Task SettingSpeed_(int speed)
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new SpeedPacket { Speed = (byte)speed }, Session);
        }

        private void VehicleSpeedShouldBe_(int expectedSpeed)
        {
            Assert.AreEqual((byte)expectedSpeed, Session.Character.VehicleSpeed);
        }

        private void ShouldShowHelpMessage()
        {
            Assert.IsTrue(Session.LastPackets.Any(p => p is SayPacket));
        }

        private void ShouldSendPacketResponse()
        {
            Assert.IsTrue(Session.LastPackets.Count > 0);
        }
    }
}
