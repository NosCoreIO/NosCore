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
    public class PositionPacketHandlerTests
    {
        private PositionPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new PositionPacketHandler();
        }

        [TestMethod]
        public async Task PositionShouldSendSayPacket()
        {
            await new Spec("Position should send say packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingPositionCommand)
                .Then(ShouldReceiveSayPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PositionShouldContainMapId()
        {
            await new Spec("Position should contain map id")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingPositionCommand)
                .Then(SayPacketShouldContainMapInfo)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Session.Character.PositionX = 0;
            Session.Character.PositionY = 0;
            Session.LastPackets.Clear();
        }

        private async Task ExecutingPositionCommand()
        {
            await Handler.ExecuteAsync(new PositionPacket(), Session);
        }

        private void ShouldReceiveSayPacket()
        {
            var sayPacket = Session.LastPackets.OfType<SayPacket>().FirstOrDefault();
            Assert.IsNotNull(sayPacket);
        }

        private void SayPacketShouldContainMapInfo()
        {
            var sayPacket = Session.LastPackets.OfType<SayPacket>().FirstOrDefault();
            Assert.IsNotNull(sayPacket);
            Assert.IsTrue(sayPacket.Message?.Contains("Map:") ?? false);
            Assert.IsTrue(sayPacket.Message?.Contains("X:0") ?? false);
            Assert.IsTrue(sayPacket.Message?.Contains("Y:0") ?? false);
        }
    }
}
