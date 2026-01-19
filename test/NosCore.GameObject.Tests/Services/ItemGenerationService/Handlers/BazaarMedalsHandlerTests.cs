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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Tests.Shared;
using Serilog;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Tests.Services.ItemGenerationService.Handlers
{
    [TestClass]
    public class BazaarMedalsHandlerTests : UseItemEventHandlerTestsBase
    {
        private GameObject.Services.ItemGenerationService.ItemGenerationService? ItemProvider;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new BazaarMedalsHandler(TestHelpers.Instance.Clock);
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, Effect = ItemEffectType.GoldNosMerchantUpgrade, EffectValue = 1},
                new Item {VNum = 2, Effect = ItemEffectType.SilverNosMerchantUpgrade, EffectValue = 1},
            };
            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
        }
        [TestMethod]
        public async Task TestAddMedalAlreadyOneDifferentAsync()
        {
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(2), Session!.Character.CharacterId);
            Session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                DateEnd = TestHelpers.Instance.Clock.GetCurrentInstant().Plus(Duration.FromDays(1)),
                StaticBonusType = StaticBonusType.BazaarMedalGold
            });
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task TestAddMedalAlreadyOneAsync()
        {
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                DateEnd = TestHelpers.Instance.Clock.GetCurrentInstant().Plus(Duration.FromDays(1)),
                StaticBonusType = StaticBonusType.BazaarMedalGold
            });
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task TestAddMedalAsync()
        {
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
        }
    }
}