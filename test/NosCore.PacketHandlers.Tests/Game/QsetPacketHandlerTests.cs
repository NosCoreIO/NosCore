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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Quicklist;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class QsetPacketHandlerTests
    {
        private QSetPacketHandler QsetPacketHandler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            QsetPacketHandler = new QSetPacketHandler();
        }

        [TestMethod]
        public async Task AddingToQuicklistShouldSucceed()
        {
            await new Spec("Adding to quicklist should succeed")
                .WhenAsync(AddingToQuicklist)
                .Then(QuicklistEntryShouldBeCreated)
                .And(AddPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingFromQuicklistShouldSucceed()
        {
            await new Spec("Deleting from quicklist should succeed")
                .WhenAsync(DeletingFromQuicklist)
                .Then(QuicklistShouldBeEmpty)
                .And(ResetPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingQuicklistEntryShouldSucceed()
        {
            await new Spec("Moving quicklist entry should succeed")
                .GivenAsync(TwoQuicklistEntriesExist)
                .WhenAsync(MovingQuicklistEntry)
                .Then(BothQuicklistEntriesShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingToEmptySlotShouldSucceed()
        {
            await new Spec("Moving to empty slot should succeed")
                .GivenAsync(OneQuicklistEntryExists)
                .WhenAsync(MovingQuicklistEntry)
                .Then(OneQuicklistEntryShouldExist)
                .ExecuteAsync();
        }

        private async Task AddingToQuicklist()
        {
            await QsetPacketHandler.ExecuteAsync(new QsetPacket
            {
                Type = QSetType.Set,
                OriginQuickList = 1,
                OriginQuickListSlot = 2,
                FirstData = 3,
                SecondData = 4
            }, Session);
        }

        private async Task DeletingFromQuicklist()
        {
            await QsetPacketHandler.ExecuteAsync(new QsetPacket
            {
                Type = QSetType.Remove,
                OriginQuickList = 1,
                OriginQuickListSlot = 2,
                FirstData = 3,
                SecondData = 4
            }, Session);
        }

        private async Task TwoQuicklistEntriesExist()
        {
            await QsetPacketHandler.ExecuteAsync(new QsetPacket
            {
                Type = QSetType.Set,
                OriginQuickList = 1,
                OriginQuickListSlot = 2,
                FirstData = 3,
                SecondData = 4
            }, Session);

            await QsetPacketHandler.ExecuteAsync(new QsetPacket
            {
                Type = QSetType.Set,
                OriginQuickList = 1,
                OriginQuickListSlot = 3,
                FirstData = 4,
                SecondData = 5
            }, Session);

            Session.LastPackets.Clear();
        }

        private async Task OneQuicklistEntryExists()
        {
            await QsetPacketHandler.ExecuteAsync(new QsetPacket
            {
                Type = QSetType.Set,
                OriginQuickList = 1,
                OriginQuickListSlot = 2,
                FirstData = 3,
                SecondData = 4
            }, Session);
            Session.LastPackets.Clear();
        }

        private async Task MovingQuicklistEntry()
        {
            await QsetPacketHandler.ExecuteAsync(new QsetPacket
            {
                Type = QSetType.Move,
                OriginQuickList = 1,
                OriginQuickListSlot = 3,
                FirstData = 1,
                SecondData = 2
            }, Session);
        }

        private void QuicklistEntryShouldBeCreated()
        {
            Assert.AreEqual(1, Session.Character.QuicklistEntries.Count);
        }

        private void AddPacketShouldBeSent()
        {
            var lastpacket = (QsetClientPacket?)Session.LastPackets.FirstOrDefault(s => s is QsetClientPacket);
            Assert.AreEqual(QSetType.Set, lastpacket?.Data?.Type);
            Assert.AreEqual(1, lastpacket?.OriginQuickList ?? 0);
            Assert.AreEqual(2, lastpacket?.OriginQuickListSlot ?? 0);
            Assert.AreEqual(0, lastpacket?.Data?.Data ?? 0);
            Assert.AreEqual(3, lastpacket?.Data?.OriginQuickList ?? 0);
            Assert.AreEqual(4, lastpacket?.Data?.OriginQuickListSlot ?? 0);
        }

        private void QuicklistShouldBeEmpty()
        {
            Assert.AreEqual(0, Session.Character.QuicklistEntries.Count);
        }

        private void ResetPacketShouldBeSent()
        {
            var lastpacket = (QsetClientPacket?)Session.LastPackets.FirstOrDefault(s => s is QsetClientPacket);
            Assert.AreEqual(QSetType.Reset, lastpacket?.Data?.Type ?? 0);
            Assert.AreEqual(1, lastpacket?.OriginQuickList ?? 0);
            Assert.AreEqual(2, lastpacket?.OriginQuickListSlot ?? 0);
            Assert.AreEqual(0, lastpacket?.Data?.Data ?? 0);
            Assert.AreEqual(7, lastpacket?.Data?.OriginQuickList ?? 0);
            Assert.AreEqual(-1, lastpacket?.Data?.OriginQuickListSlot ?? 0);
        }

        private void BothQuicklistEntriesShouldExist()
        {
            Assert.AreEqual(2, Session.Character.QuicklistEntries.Count);
        }

        private void OneQuicklistEntryShouldExist()
        {
            Assert.AreEqual(1, Session.Character.QuicklistEntries.Count);
        }
    }
}
