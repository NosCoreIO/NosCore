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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Providers.GuriProvider.Handlers;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Tests.Helpers;
using Serilog;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.Tests.GuriHandlerTests
{
    [TestClass]
    public class SpeakerGuriHandlerTests : GuriEventHandlerTestsBase
    {
        private IItemProvider _itemProvider;
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void Setup()
        {
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Magical, Type =  NoscorePocketType.Etc, Effect = ItemEffectType.Speaker},
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());

            _session = TestHelpers.Instance.GenerateSession();
            _logger = new Mock<ILogger>();
            _handler = new SpeakerGuriHandler(_logger.Object);
            Broadcaster.Instance.LastPackets.Clear();
        }

        [TestMethod]
        public void Test_SpeakerWithItem()
        {
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1, 1), 0));
            ExecuteGuriEventHandler(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                VisualId = 0,
                Data = 999,
                Value = "2 0 {test}"
            });
            var sayitempacket = (SayItemPacket)Broadcaster.Instance.LastPackets.FirstOrDefault(s => s is SayItemPacket);
            Assert.IsNotNull(sayitempacket);
            var saypacket = (SayPacket)Broadcaster.Instance.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsNull(saypacket);
        }


        [TestMethod]
        public void Test_SpeakerWithItemDoesNotExist()
        {
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1, 1), 0));
            ExecuteGuriEventHandler(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                VisualId = 0,
                Data = 999,
                Value = "2 1 {test}"
            });
            var sayitempacket = (SayItemPacket)Broadcaster.Instance.LastPackets.FirstOrDefault(s => s is SayItemPacket);
            Assert.IsNull(sayitempacket);
            var saypacket = (SayPacket)Broadcaster.Instance.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsNull(saypacket);
        }


        [TestMethod]
        public void Test_SpeakerWithoutItem()
        {
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_itemProvider.Create(1, 1), 0));
            ExecuteGuriEventHandler(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                VisualId = 0,
            });
            var sayitempacket = (SayItemPacket)Broadcaster.Instance.LastPackets.FirstOrDefault(s => s is SayItemPacket);
            Assert.IsNull(sayitempacket);
            var saypacket = (SayPacket)Broadcaster.Instance.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsNotNull(saypacket);
        }

        [TestMethod]
        public void Test_SpeakerWithNoSpeaker()
        {
            ExecuteGuriEventHandler(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                VisualId = 0
            });
            var sayitempacket = (SayItemPacket)Broadcaster.Instance.LastPackets.FirstOrDefault(s => s is SayItemPacket);
            Assert.IsNull(sayitempacket);
            var saypacket = (SayPacket)Broadcaster.Instance.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsNull(saypacket);
        }
    }
}