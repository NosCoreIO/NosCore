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
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class MinilandBellHandlerTests : UseItemEventHandlerTestsBase
    {
        private ItemGenerationService? _itemProvider;
        private Mock<IMinilandService>? _minilandProvider;
        private readonly ILogger _logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _minilandProvider = new Mock<IMinilandService>();
            Session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _minilandProvider.Setup(s => s.GetMiniland(Session.Character.CharacterId))
                .Returns(new Miniland { MapInstanceId = TestHelpers.Instance.MinilandId });
            Handler = new MinilandBellHandler(_minilandProvider.Object);
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, Effect = ItemEffectType.Teleport, EffectValue = 2},
            };
            _itemProvider = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), _logger);
        }

        [TestMethod]
        public async Task Test_Miniland_On_InstanceAsync()
        {
            Session!.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetMapInstance(TestHelpers.Instance.MinilandId)!;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (SayPacket?)Session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.AreEqual(GameLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_USE, Session.Account.Language), lastpacket?.Message);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task Test_Miniland_On_VehicleAsync()
        {
            Session!.Character.IsVehicled = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (SayPacket?)Session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.AreEqual(GameLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_USE_IN_VEHICLE, Session.Account.Language), lastpacket?.Message);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task Test_Miniland_DelayAsync()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (DelayPacket?)Session.LastPackets.FirstOrDefault(s => s is DelayPacket);
            Assert.IsNotNull(lastpacket);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task Test_MinilandAsync()
        {
            UseItem.Mode = 2;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            Assert.AreEqual(MapInstanceType.NormalInstance, Session!.Character.MapInstance.MapInstanceType);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }
    }
}