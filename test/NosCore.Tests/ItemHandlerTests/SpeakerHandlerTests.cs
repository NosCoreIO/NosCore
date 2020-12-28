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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class SpeakerHandlerTests : UseItemEventHandlerTestsBase
    {
        private ItemGenerationService? _itemProvider;
        private readonly ILogger _logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            Session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            Handler = new SpeakerHandler();
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Magical, Effect = ItemEffectType.Speaker, EffectValue = 0},
            };
            _itemProvider = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), _logger);
        }

        [TestMethod]
        public async Task Test_SpeakerItemHandlerAsync()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (GuriPacket?)Session.LastPackets.FirstOrDefault(s => s is GuriPacket);
            Assert.IsNotNull(lastpacket);
        }
    }
}