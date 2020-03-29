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
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class MinilandBellHandlerTests : UseItemEventHandlerTestsBase
    {
        private ItemProvider _itemProvider;
        private Mock<IMinilandProvider> _minilandProvider;

        [TestInitialize]
        public void Setup()
        {
            _minilandProvider = new Mock<IMinilandProvider>();
            Session = TestHelpers.Instance.GenerateSession();
            _minilandProvider.Setup(s => s.GetMiniland(Session.Character.CharacterId))
                .Returns(new Miniland { MapInstanceId = TestHelpers.Instance.MinilandId });
            Handler = new MinilandBellHandler(_minilandProvider.Object);
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, Effect = ItemEffectType.Teleport, EffectValue = 2},
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
        }

        [TestMethod]
        public void Test_Miniland_On_Instance()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceProvider.GetMapInstance(TestHelpers.Instance.MinilandId);
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (SayPacket?)Session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.AreEqual(Language.Instance.GetMessageFromKey(LanguageKey.CANT_USE, Session.Character.Account.Language), lastpacket.Message);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public void Test_Miniland_On_Vehicle()
        {
            Session.Character.IsVehicled = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (SayPacket?)Session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.AreEqual(Language.Instance.GetMessageFromKey(LanguageKey.CANT_USE_IN_VEHICLE, Session.Character.Account.Language), lastpacket.Message);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public void Test_Miniland_Delay()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (DelayPacket?)Session.LastPackets.FirstOrDefault(s => s is DelayPacket);
            Assert.IsNotNull(lastpacket);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public void Test_Miniland()
        {
            UseItem.Mode = 2;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            Assert.AreEqual(MapInstanceType.NormalInstance, Session.Character.MapInstance.MapInstanceType);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }
    }
}