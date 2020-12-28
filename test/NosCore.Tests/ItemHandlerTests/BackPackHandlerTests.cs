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
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.Inventory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class BackPackHandlerTests : UseItemEventHandlerTestsBase
    {
        private ItemProvider? _itemProvider;
        private readonly ILogger _logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            Session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            Handler = new BackPackHandler(Options.Create(new WorldConfiguration { MaxAdditionalSpPoints = 1 }));
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Special, Effect = ItemEffectType.InventoryTicketUpgrade, EffectValue = 0},
                new Item {VNum = 2, ItemType = ItemType.Special, Effect = ItemEffectType.InventoryUpgrade, EffectValue = 0},
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>(), _logger);
        }
        [TestMethod]
        public async Task Test_Can_Not_StackAsync()
        {
            Session!.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                DateEnd = null,
                StaticBonusType = StaticBonusType.BackPack
            });
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(2), Session.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);

            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task Test_BackPackAsync()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(2), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (ExtsPacket?)Session.LastPackets.FirstOrDefault(s => s is ExtsPacket);
            Assert.IsNotNull(lastpacket);
            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
            Assert.AreEqual(12, Session.Character.InventoryService.Expensions[NoscorePocketType.Etc]);
            Assert.AreEqual(12, Session.Character.InventoryService.Expensions[NoscorePocketType.Equipment]);
            Assert.AreEqual(12, Session.Character.InventoryService.Expensions[NoscorePocketType.Main]);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task Test_Can_Not_StackTicketAsync()
        {
            Session!.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                DateEnd = null,
                StaticBonusType = StaticBonusType.InventoryTicketUpgrade
            });
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);

            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task Test_BackPackTicketAsync()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (ExtsPacket?)Session.LastPackets.FirstOrDefault(s => s is ExtsPacket);
            Assert.IsNotNull(lastpacket);
            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
            Assert.AreEqual(60, Session.Character.InventoryService.Expensions[NoscorePocketType.Etc]);
            Assert.AreEqual(60, Session.Character.InventoryService.Expensions[NoscorePocketType.Equipment]);
            Assert.AreEqual(60, Session.Character.InventoryService.Expensions[NoscorePocketType.Main]);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }
    }
}