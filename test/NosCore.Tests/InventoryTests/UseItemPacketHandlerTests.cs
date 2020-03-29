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
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.Inventory;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.InventoryTests
{
    [TestClass]
    public class UseItemPacketHandlerTests
    {
        private IItemProvider? _item;
        private ClientSession? _session;
        private UseItemPacketHandler? _useItemPacketHandler;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            TestHelpers.Reset();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = TestHelpers.Instance.GenerateSession();
            _useItemPacketHandler = new UseItemPacketHandler();
        }

        [TestMethod]
        public void Test_Binding()
        {
            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1, 1), 0));
            _useItemPacketHandler!.Execute(new UseItemPacket {Slot = 0, Type = PocketType.Equipment, Mode = 1},
                _session);

            Assert.IsTrue(_session.Character.InventoryService.Any(s =>
                (s.Value.ItemInstance!.ItemVNum == 1) && (s.Value.Type == NoscorePocketType.Wear) &&
                (s.Value.ItemInstance.BoundCharacterId == _session.Character.VisualId)));
        }

        [TestMethod]
        public void Test_Increment_SpAdditionPoints()
        {
            _session!.Character.SpAdditionPoint = 0;
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1078, 1), 0));
            var item = _session.Character.InventoryService.First();
            _useItemPacketHandler!.Execute(new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = 1,
                Type = (PocketType) item.Value.Type,
                Slot = item.Value.Slot,
                Mode = 0,
                Parameter = 0
            }, _session);
            Assert.IsTrue((_session.Character.SpAdditionPoint != 0) && !_session.LastPackets.Any(s => s is MsgPacket));
        }

        [TestMethod]
        public void Test_Overflow_SpAdditionPoints()
        {
            _session!.Character.SpAdditionPoint = _session.WorldConfiguration.MaxAdditionalSpPoints;
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1078, 1), 0));
            var item = _session.Character.InventoryService.First();
            _useItemPacketHandler!.Execute(new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = 1,
                Type = (PocketType) item.Value.Type,
                Slot = item.Value.Slot,
                Mode = 0,
                Parameter = 0
            }, _session);
            var packet = (MsgPacket?) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue((_session.Character.SpAdditionPoint == _session.WorldConfiguration.MaxAdditionalSpPoints) &&
                (packet?.Message == Language.Instance.GetMessageFromKey(LanguageKey.SP_ADDPOINTS_FULL,
                    _session.Character.Account.Language)));
        }

        [TestMethod]
        public void Test_CloseToLimit_SpAdditionPoints()
        {
            _session!.Character.SpAdditionPoint = _session.WorldConfiguration.MaxAdditionalSpPoints - 1;
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item!.Create(1078, 1), 0));
            var item = _session.Character.InventoryService.First();
            _useItemPacketHandler!.Execute(new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = 1,
                Type = (PocketType) item.Value.Type,
                Slot = item.Value.Slot,
                Mode = 0,
                Parameter = 0
            }, _session);
            Assert.IsTrue((_session.Character.SpAdditionPoint == _session.WorldConfiguration.MaxAdditionalSpPoints) &&
                !_session.LastPackets.Any(s => s is MsgPacket));
        }
    }
}