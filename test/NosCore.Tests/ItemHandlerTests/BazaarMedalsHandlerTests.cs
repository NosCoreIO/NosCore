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
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Inventory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
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
    public class BazaarMedalsHandlerTests : UseItemEventHandlerTestsBase
    {
        private ItemProvider? _itemProvider;
        private Mock<ILogger>? _logger;

        [TestInitialize]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            Session = TestHelpers.Instance.GenerateSession();
            Handler = new BazaarMedalsHandler(_logger.Object);
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, Effect = ItemEffectType.GoldNosMerchantUpgrade, EffectValue = 1},
                new Item {VNum = 2, Effect = ItemEffectType.SilverNosMerchantUpgrade, EffectValue = 1},
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
        }
        [TestMethod]
        public async Task Test_AddMedal_AlreadyOneDifferent()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(2), Session!.Character.CharacterId);
            Session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                DateEnd = SystemTime.Now().AddDays(1),
                StaticBonusType = StaticBonusType.BazaarMedalGold
            });
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandler(itemInstance);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task Test_AddMedal_AlreadyOne()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                DateEnd = SystemTime.Now().AddDays(1),
                StaticBonusType = StaticBonusType.BazaarMedalGold
            });
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandler(itemInstance);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task Test_AddMedal()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandler(itemInstance);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
        }
    }
}