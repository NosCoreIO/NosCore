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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Tests.Services.ItemGenerationService.Handlers
{
    [TestClass]
    public class VehicleEventHandlerTests : UseItemEventHandlerTestsBase
    {
        private Mock<ILogger>? Logger;
        private GameObject.Services.ItemGenerationService.ItemGenerationService? ItemProvider;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Logger = new Mock<ILogger>();
            Handler = new VehicleEventHandler(Logger.Object, TestHelpers.Instance.LogLanguageLocalizer, new GameObject.Services.TransformationService.TransformationService(TestHelpers.Instance.Clock, new Mock<IExperienceService>().Object, new Mock<IJobExperienceService>().Object, new Mock<IHeroExperienceService>().Object, Logger.Object, TestHelpers.Instance.LogLanguageLocalizer));
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon}
            };
            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger.Object, TestHelpers.Instance.LogLanguageLocalizer);
        }


        [TestMethod]
        public async Task TestCanNotVehicleInShopAsync()
        {
            Session!.Character.InShop = true;
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session.Character.CharacterId);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            Logger!.Verify(s => s.Error(TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.CANT_USE_ITEM_IN_SHOP]), Times.Exactly(1));
        }

        [TestMethod]
        public async Task TestVehicleGetDelayedAsync()
        {
            UseItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session!.Character.CharacterId);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            var lastpacket = (DelayPacket?)Session.LastPackets.FirstOrDefault(s => s is DelayPacket);
            Assert.IsNotNull(lastpacket);
        }

        [TestMethod]
        public async Task TestVehicleAsync()
        {
            UseItem.Mode = 2;
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session!.Character.CharacterId);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            Assert.IsTrue(Session.Character.IsVehicled);
        }

        [TestMethod]
        public async Task TestVehicleRemoveAsync()
        {
            Session!.Character.IsVehicled = true;
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session.Character.CharacterId);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            Assert.IsFalse(Session.Character.IsVehicled);
        }
    }
}