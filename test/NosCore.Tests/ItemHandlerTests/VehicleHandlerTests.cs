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

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class VehicleEventHandlerTests : UseItemEventHandlerTestsBase
    {
        private Mock<ILogger> _logger;
        private ItemProvider _itemProvider;

        [TestInitialize]
        public void Setup()
        {
            _session = TestHelpers.Instance.GenerateSession();
            _logger = new Mock<ILogger>();
            _handler = new VehicleEventHandler(_logger.Object);
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon}
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
        }


        [TestMethod]
        public void Test_Can_Not_Vehicle_In_Shop()
        {
            _session.Character.InShop = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
           _logger.Verify(s=>s.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_USE_ITEM_IN_SHOP)), Times.Exactly(1));
        }

        [TestMethod]
        public void Test_Vehicle_GetDelayed()
        {
            _useItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (DelayPacket?)_session.LastPackets.FirstOrDefault(s => s is DelayPacket);
            Assert.IsNotNull(lastpacket);
        }

        [TestMethod]
        public void Test_Vehicle()
        {
            _useItem.Mode = 2;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            Assert.IsTrue(_session.Character.IsVehicled);
        }

        [TestMethod]
        public void Test_Vehicle_Remove()
        {
            _session.Character.IsVehicled = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            Assert.IsFalse(_session.Character.IsVehicled);
        }
    }
}