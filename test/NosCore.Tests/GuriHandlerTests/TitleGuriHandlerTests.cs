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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.GuriProvider.Handlers;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Tests.Helpers;
using Serilog;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.Tests.GuriHandlerTests
{
    [TestClass]
    public class TitleGuriHandlerTests : GuriEventHandlerTestsBase
    {
        private IItemProvider? _itemProvider;
        private readonly ILogger _logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Title, EffectValue = 0, Type =  NoscorePocketType.Main},
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>(), _logger);

            Session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            Handler = new TitleGuriHandler();
        }

        [TestMethod]
        public async Task Test_TitleGuriHandlerAsync()
        {
            Session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1, 1), 0));
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.Title,
                VisualId = 0
            }).ConfigureAwait(false);
            var lastpacket = (InfoPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(GameLanguage.Instance.GetMessageFromKey(LanguageKey.WEAR_NEW_TITLE,
                Session.Account.Language), lastpacket?.Message);
            Assert.AreEqual(1, Session.Character.Titles.Count);
        }

        [TestMethod]
        public async Task Test_TitleGuriHandlerWhenDuplicateAsync()
        {
            Session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1, 1), 0));
            Session.Character.Titles = new List<TitleDto> { new TitleDto { TitleType = 1 } };
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.Title,
                VisualId = 0
            }).ConfigureAwait(false);
            var lastpacket = (InfoPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.IsNull(lastpacket);
            Assert.AreEqual(1, Session.Character.Titles.Count);
        }

        [TestMethod]
        public async Task Test_TitleGuriHandlerWhenNoTitleItemAsync()
        {
            await ExecuteGuriEventHandlerAsync(new GuriPacket
            {
                Type = GuriPacketType.Title,
                VisualId = 0
            }).ConfigureAwait(false);
            var lastpacket = (InfoPacket?)Session!.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.IsNull(lastpacket);
            Assert.AreEqual(0, Session.Character.Titles.Count);
        }
    }
}