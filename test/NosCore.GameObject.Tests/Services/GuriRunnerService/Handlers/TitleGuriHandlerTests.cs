//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.GuriRunnerService.Handlers;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
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
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Services.GuriRunnerService.Handlers
{
    [TestClass]
    public class TitleGuriHandlerTests : GuriEventHandlerTestsBase
    {
        private IItemGenerationService? ItemProvider;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Title, EffectValue = 0, Type =  NoscorePocketType.Main},
            };
            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new TitleGuriHandler();
        }

        [TestMethod]
        public async Task TestTitleGuriHandlerAsync()
        {
            Session!.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1, 1), 0));
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.Title,
                VisualId = 0
            });
            var lastpacket = (InfoiPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.AreEqual(Game18NConstString.TitleChangedOrHidden, lastpacket?.Message);
            Assert.AreEqual(1, Session.Character.Titles.Count);
        }

        [TestMethod]
        public async Task TestTitleGuriHandlerWhenDuplicateAsync()
        {
            Session!.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1, 1), 0));
            Session.Character.Titles = new List<TitleDto> { new() { TitleType = 1 } };
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.Title,
                VisualId = 0
            });
            var lastpacket = (InfoiPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.IsNull(lastpacket);
            Assert.AreEqual(1, Session.Character.Titles.Count);
        }

        [TestMethod]
        public async Task TestTitleGuriHandlerWhenNoTitleItemAsync()
        {
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.Title,
                VisualId = 0
            });
            var lastpacket = (InfoiPacket?)Session!.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.IsNull(lastpacket);
            Assert.AreEqual(0, Session.Character.Titles.Count);
        }
    }
}
