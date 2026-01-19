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
using NosCore.PacketHandlers.Warehouse;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Warehouse
{
    [TestClass]
    public class StashEndPacketHandlerTests
    {
        private ClientSession Session = null!;
        private StashEndPacketHandler Handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new StashEndPacketHandler();
        }

        [TestMethod]
        public async Task StashEndPacketShouldExecuteWithoutError()
        {
            await new Spec("Stash end packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StashEndPacketShouldNotThrowWhenWarehouseNotOpen()
        {
            await new Spec("Stash end packet should not throw when warehouse not open")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingWarehouseWithoutOpening)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StashEndPacketShouldNotThrowMultipleTimes()
        {
            await new Spec("Stash end packet should not throw multiple times")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingWarehouseMultipleTimes)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task ClosingWarehouse()
        {
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
        }

        private async Task ClosingWarehouseWithoutOpening()
        {
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
        }

        private async Task ClosingWarehouseMultipleTimes()
        {
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
