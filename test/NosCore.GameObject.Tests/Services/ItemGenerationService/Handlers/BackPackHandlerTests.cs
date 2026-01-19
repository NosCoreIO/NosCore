//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ItemGenerationService.Handlers
{
    [TestClass]
    public class BackPackHandlerTests : UseItemEventHandlerTestsBase
    {
        private GameObject.Services.ItemGenerationService.ItemGenerationService? ItemProvider;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new BackPackHandler(Options.Create(new WorldConfiguration { MaxAdditionalSpPoints = 1 }), TestHelpers.Instance.Clock);
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Special, Effect = ItemEffectType.InventoryTicketUpgrade, EffectValue = 0},
                new Item {VNum = 2, ItemType = ItemType.Special, Effect = ItemEffectType.InventoryUpgrade, EffectValue = 0},
            };
            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
        }
        [TestMethod]
        public async Task TestCanNotStackAsync()
        {
            Session!.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                DateEnd = null,
                StaticBonusType = StaticBonusType.BackPack
            });
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(2), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);

            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task TestBackPackAsync()
        {
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(2), Session!.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            var lastpacket = (ExtsPacket?)Session.LastPackets.FirstOrDefault(s => s is ExtsPacket);
            Assert.IsNotNull(lastpacket);
            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
            Assert.AreEqual(12, Session.Character.InventoryService.Expensions[NoscorePocketType.Etc]);
            Assert.AreEqual(12, Session.Character.InventoryService.Expensions[NoscorePocketType.Equipment]);
            Assert.AreEqual(12, Session.Character.InventoryService.Expensions[NoscorePocketType.Main]);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task TestCanNotStackTicketAsync()
        {
            Session!.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                DateEnd = null,
                StaticBonusType = StaticBonusType.InventoryTicketUpgrade
            });
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);

            Assert.AreEqual(1, Session.Character.StaticBonusList.Count);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task TestBackPackTicketAsync()
        {
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
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
