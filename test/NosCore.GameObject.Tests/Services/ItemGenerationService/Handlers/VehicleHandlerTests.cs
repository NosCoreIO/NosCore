//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
