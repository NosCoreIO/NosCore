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
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Holders;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using Serilog;

namespace NosCore.GameObject.Tests.Services.ExchangeService
{
    [TestClass]
    public class ExchangeServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private GameObject.Services.ExchangeService.ExchangeService? _exchangeProvider;

        private GameObject.Services.ItemGenerationService.ItemGenerationService? _itemProvider;

        private IOptions<WorldConfiguration>? _worldConfiguration;

        [TestInitialize]
        public void Setup()
        {

            _worldConfiguration = Options.Create(new WorldConfiguration
            {
                MaxItemAmount = 999,
                BackpackSize = 48,
                MaxGoldAmount = 1000000000,
                MaxBankGoldAmount = 100000000000
            });

            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Main, VNum = 1012},
                new Item {Type = NoscorePocketType.Main, VNum = 1013}
            };

            _itemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _exchangeProvider = new GameObject.Services.ExchangeService.ExchangeService(_itemProvider, _worldConfiguration, Logger, new ExchangeRequestHolder(), TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.GameLanguageLocalizer);
        }

        [TestMethod]
        public void Test_Set_Gold()
        {
            _exchangeProvider!.OpenExchange(1, 2);
            _exchangeProvider.SetGold(1, 1000, 1000);
            _exchangeProvider.SetGold(2, 2000, 2000);

            var data1 = _exchangeProvider.GetData(1);
            var data2 = _exchangeProvider.GetData(2);

            Assert.AreEqual(1000, data1.Gold);
            Assert.AreEqual(1000, data1.BankGold);
            Assert.AreEqual(2000, data2.Gold);
            Assert.AreEqual(2000, data2.BankGold);
        }

        [TestMethod]
        public void Test_Confirm_Exchange()
        {
            _exchangeProvider!.OpenExchange(1, 2);
            _exchangeProvider.ConfirmExchange(1);
            _exchangeProvider.ConfirmExchange(2);

            var data1 = _exchangeProvider.GetData(1);
            var data2 = _exchangeProvider.GetData(2);

            Assert.IsTrue(data1.ExchangeConfirmed && data2.ExchangeConfirmed);
        }

        [TestMethod]
        public void Test_Add_Items()
        {
            _exchangeProvider!.OpenExchange(1, 2);

            var item = new InventoryItemInstance(new ItemInstance(new Item { VNum = 1012 })
            {
                Amount = 1
            });

            _exchangeProvider.AddItems(1, item, item.ItemInstance.Amount);

            var data1 = _exchangeProvider.GetData(1);

            Assert.IsTrue(data1.ExchangeItems.Any(s =>
                (s.Key.ItemInstance?.ItemVNum == 1012) && (s.Key.ItemInstance.Amount == 1)));
        }

        [TestMethod]
        public void Test_Check_Exchange()
        {
            var wrongExchange = _exchangeProvider!.CheckExchange(1);
            _exchangeProvider.OpenExchange(1, 2);
            var goodExchange = _exchangeProvider.CheckExchange(1);

            Assert.IsTrue(!wrongExchange && goodExchange);
        }

        [TestMethod]
        public void Test_Close_Exchange()
        {
            var wrongClose = _exchangeProvider!.CloseExchange(1, ExchangeResultType.Failure);

            Assert.IsNull(wrongClose);

            _exchangeProvider.OpenExchange(1, 2);
            var goodClose = _exchangeProvider.CloseExchange(1, ExchangeResultType.Failure);
            Assert.IsNotNull(goodClose);
            Assert.AreEqual(ExchangeResultType.Failure, goodClose.Type);
        }

        [TestMethod]
        public void Test_Open_Exchange()
        {
            var exchange = _exchangeProvider!.OpenExchange(1, 2);
            Assert.IsTrue(exchange);
        }

        [TestMethod]
        public void Test_Open_Second_Exchange()
        {
            var exchange = _exchangeProvider!.OpenExchange(1, 2);
            Assert.IsTrue(exchange);

            var wrongExchange = _exchangeProvider.OpenExchange(1, 3);
            Assert.IsFalse(wrongExchange);
        }

        [TestMethod]
        public void Test_Process_Exchange()
        {
            IInventoryService inventory1 =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1012, Type = NoscorePocketType.Main } },
                    _worldConfiguration!, Logger);
            IInventoryService inventory2 =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1013, Type = NoscorePocketType.Main } },
                    _worldConfiguration!, Logger);
            var item1 = inventory1.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 1), 0))!
                .First();
            var item2 = inventory2.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 1), 0))!
                .First();

            _exchangeProvider!.OpenExchange(1, 2);
            _exchangeProvider.AddItems(1, item1, 1);
            _exchangeProvider.AddItems(2, item2, 1);
            var itemList = _exchangeProvider.ProcessExchange(1, 2, inventory1, inventory2);
            Assert.AreEqual(2, itemList.Count(s => s.Key == 1));
            Assert.AreEqual(2, itemList.Count(s => s.Key == 2));
        }
    }
}