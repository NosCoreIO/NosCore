//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
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
    public class SpRechargerEventHandlerTests : UseItemEventHandlerTestsBase
    {
        private GameObject.Services.ItemGenerationService.ItemGenerationService? ItemProvider;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new SpRechargerEventHandler(Options.Create(new WorldConfiguration { MaxAdditionalSpPoints = 1 }));
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Special, EffectValue = 1},
            };
            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
        }
        [TestMethod]
        public async Task TestSpRechargerWhenMaxAsync()
        {
            Session!.Character.SpAdditionPoint = 1;
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            var lastpacket = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(lastpacket?.Type == MessageType.Default, lastpacket?.Message == Game18NConstString.CannotBeUsedExceedsCapacity);
            Assert.AreEqual(1, Session.Character.SpAdditionPoint);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task TestSpRechargerAsync()
        {
            Session!.Character.SpAdditionPoint = 0;
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            Assert.AreEqual(1, Session.Character.SpAdditionPoint);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }
    }
}
