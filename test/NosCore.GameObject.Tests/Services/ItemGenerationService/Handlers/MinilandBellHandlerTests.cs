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
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Tests.Services.ItemGenerationService.Handlers
{
    [TestClass]
    public class MinilandBellHandlerTests : UseItemEventHandlerTestsBase
    {
        private GameObject.Services.ItemGenerationService.ItemGenerationService? ItemProvider;
        private Mock<IMinilandService>? MinilandProvider;
        private Mock<IMapChangeService> mapChangeService = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            MinilandProvider = new Mock<IMinilandService>();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            MinilandProvider.Setup(s => s.GetMiniland(Session.Character.CharacterId))
                .Returns(new Miniland { MapInstanceId = TestHelpers.Instance.MinilandId });
            mapChangeService = new Mock<IMapChangeService>();
            Handler = new MinilandBellHandler(MinilandProvider.Object, mapChangeService.Object);
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, Effect = ItemEffectType.Teleport, EffectValue = 2},
            };
            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task TestMinilandOnInstanceAsync()
        {
            Session!.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetMapInstance(TestHelpers.Instance.MinilandId)!;
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            var lastpacket = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.AreEqual(lastpacket?.VisualType == VisualType.Player, lastpacket?.VisualId == Session.Character.CharacterId);
            Assert.AreEqual(lastpacket?.Type == SayColorType.Yellow, lastpacket?.Message == Game18NConstString.CanNotBeUsedHere);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task TestMinilandOnVehicleAsync()
        {
            Session!.Character.IsVehicled = true;
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            var lastpacket = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.AreEqual(lastpacket?.VisualType == VisualType.Player, lastpacket?.VisualId == Session.Character.CharacterId);
            Assert.AreEqual(lastpacket?.Type == SayColorType.Yellow, lastpacket?.Message == Game18NConstString.OnlyPotionInVehicle);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task TestMinilandDelayAsync()
        {
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            var lastpacket = (DelayPacket?)Session.LastPackets.FirstOrDefault(s => s is DelayPacket);
            Assert.IsNotNull(lastpacket);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task TestMinilandAsync()
        {
            UseItem.Mode = 2;
            var itemInstance = InventoryItemInstance.Create(ItemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance);
            mapChangeService.Verify(x=>x.ChangeMapInstanceAsync(Session, TestHelpers.Instance.MinilandId, 5, 8), Times.Once);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }
    }
}