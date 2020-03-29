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
using NosCore.Packets.ClientPackets.Quicklist;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quicklist;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class QsetPacketHandlerTests
    {
        private QSetPacketHandler? _qsetPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public void Setup()
        {
            _session = TestHelpers.Instance.GenerateSession();
            _qsetPacketHandler = new QSetPacketHandler();
        }

        [TestMethod]
        public void Test_Add_Quicklist()
        {
            _qsetPacketHandler!.Execute(new QsetPacket
            {
                Type = QSetType.Set,
                OriginQuickList = 1,
                OriginQuickListSlot = 2,
                FirstData = 3,
                SecondData = 4
            }, _session!);
            var lastpacket = (QsetClientPacket?) _session!.LastPackets.FirstOrDefault(s => s is QsetClientPacket);
            Assert.AreEqual(QSetType.Set, lastpacket?.Data?.Type);
            Assert.AreEqual(1, lastpacket?.OriginQuickList);
            Assert.AreEqual(2, lastpacket?.OriginQuickListSlot);
            Assert.AreEqual(0, (int?) lastpacket?.Data?.Data);
            Assert.AreEqual(3, lastpacket?.Data?.OriginQuickList);
            Assert.AreEqual(4, lastpacket?.Data?.OriginQuickListSlot);
            Assert.AreEqual(1, _session.Character.QuicklistEntries.Count);
        }

        [TestMethod]
        public void Test_Delete_FromQuicklist()
        {
            _qsetPacketHandler!.Execute(new QsetPacket
            {
                Type = QSetType.Remove,
                OriginQuickList = 1,
                OriginQuickListSlot = 2,
                FirstData = 3,
                SecondData = 4
            }, _session!);
            var lastpacket = (QsetClientPacket?) _session!.LastPackets.FirstOrDefault(s => s is QsetClientPacket);
            Assert.AreEqual(QSetType.Reset, lastpacket?.Data?.Type);
            Assert.AreEqual(1, lastpacket?.OriginQuickList);
            Assert.AreEqual(2, lastpacket?.OriginQuickListSlot);
            Assert.AreEqual(0, (int?) lastpacket?.Data?.Data);
            Assert.AreEqual(7, lastpacket?.Data?.OriginQuickList);
            Assert.AreEqual(-1, lastpacket?.Data?.OriginQuickListSlot);
            Assert.AreEqual(0, _session.Character.QuicklistEntries.Count);
        }

        [TestMethod]
        public void Test_Move_Quicklist()
        {
            _qsetPacketHandler!.Execute(new QsetPacket
            {
                Type = QSetType.Set,
                OriginQuickList = 1,
                OriginQuickListSlot = 2,
                FirstData = 3,
                SecondData = 4
            }, _session!);

            _qsetPacketHandler.Execute(new QsetPacket
            {
                Type = QSetType.Set,
                OriginQuickList = 1,
                OriginQuickListSlot = 3,
                FirstData = 4,
                SecondData = 5
            }, _session!);

            _session!.LastPackets.Clear();
            _qsetPacketHandler.Execute(new QsetPacket
            {
                Type = QSetType.Move,
                OriginQuickList = 1,
                OriginQuickListSlot = 3,
                FirstData = 1,
                SecondData = 2
            }, _session);
            var firstpacket = (QsetClientPacket?) _session.LastPackets.FirstOrDefault(s => s is QsetClientPacket);
            var lastpacket = (QsetClientPacket?) _session.LastPackets.Skip(1).FirstOrDefault(s => s is QsetClientPacket);
            Assert.AreEqual(QSetType.Set, lastpacket?.Data?.Type);
            Assert.AreEqual(1, lastpacket?.OriginQuickList);
            Assert.AreEqual(2, lastpacket?.OriginQuickListSlot);
            Assert.AreEqual(0, (int?) lastpacket?.Data?.Data);
            Assert.AreEqual(4, lastpacket?.Data?.OriginQuickList);
            Assert.AreEqual(5, lastpacket?.Data?.OriginQuickListSlot);

            Assert.AreEqual(1, firstpacket?.OriginQuickList);
            Assert.AreEqual(3, firstpacket?.OriginQuickListSlot);
            Assert.AreEqual(0, (int?) firstpacket?.Data?.Data);
            Assert.AreEqual(3, firstpacket?.Data?.OriginQuickList);
            Assert.AreEqual(4, firstpacket?.Data?.OriginQuickListSlot);

            Assert.AreEqual(2, _session.Character.QuicklistEntries.Count);
        }

        [TestMethod]
        public void Test_Move_ToEmpty()
        {
            _qsetPacketHandler!.Execute(new QsetPacket
            {
                Type = QSetType.Set,
                OriginQuickList = 1,
                OriginQuickListSlot = 2,
                FirstData = 3,
                SecondData = 4
            }, _session!);
            _session!.LastPackets.Clear();
            _qsetPacketHandler.Execute(new QsetPacket
            {
                Type = QSetType.Move,
                OriginQuickList = 1,
                OriginQuickListSlot = 3,
                FirstData = 1,
                SecondData = 2
            }, _session);

            Assert.AreEqual(1, _session.Character.QuicklistEntries.Count);

            var firstPacket = (QsetClientPacket?) _session.LastPackets.FirstOrDefault(s => s is QsetClientPacket);
            var lastpacket = (QsetClientPacket?) _session.LastPackets.Skip(1).FirstOrDefault(s => s is QsetClientPacket);

            Assert.AreEqual(QSetType.Set, firstPacket?.Data?.Type);
            Assert.AreEqual(1, firstPacket?.OriginQuickList);
            Assert.AreEqual(3, firstPacket?.OriginQuickListSlot);
            Assert.AreEqual(0, (int?)lastpacket?.Data?.Data);
            Assert.AreEqual(3, firstPacket?.Data?.OriginQuickList);
            Assert.AreEqual(4, firstPacket?.Data?.OriginQuickListSlot);

            Assert.AreEqual(1, lastpacket?.OriginQuickList);
            Assert.AreEqual(2, lastpacket?.OriginQuickListSlot);
            Assert.AreEqual(0, (int?) lastpacket?.Data?.Data);
            Assert.AreEqual(7, lastpacket?.Data?.OriginQuickList);
            Assert.AreEqual(-1, lastpacket?.Data?.OriginQuickListSlot);
        }
    }
}