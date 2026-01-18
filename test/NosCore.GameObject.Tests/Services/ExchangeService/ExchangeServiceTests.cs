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
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Tests.Shared;
using Serilog;

namespace NosCore.GameObject.Tests.Services.ExchangeService
{
    [TestClass]
    public class ExchangeServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private GameObject.Services.ExchangeService.ExchangeService? _exchangeService;
        private IExchangeRegistry? _exchangeRegistry;
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

            _itemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                    new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
            _exchangeRegistry = new ExchangeRegistry();
            _exchangeService = new GameObject.Services.ExchangeService.ExchangeService(_itemProvider,
                _worldConfiguration, Logger, _exchangeRegistry, TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.GameLanguageLocalizer, new InventoryPacketSystem());
        }

        [TestMethod]
        public void Test_Set_Gold()
        {
            var exchange = _exchangeService!.OpenExchange(1, 2);
            Assert.IsNotNull(exchange);

            exchange.SetGold(1, 1000, 1000);
            exchange.SetGold(2, 2000, 2000);

            var data1 = exchange.GetPlayerData(1);
            var data2 = exchange.GetPlayerData(2);

            Assert.IsTrue((data1.Gold == 1000) && (data1.BankGold == 1000) && (data2.Gold == 2000) &&
                (data2.BankGold == 2000));
        }

        [TestMethod]
        public void Test_Confirm_Exchange()
        {
            var exchange = _exchangeService!.OpenExchange(1, 2);
            Assert.IsNotNull(exchange);

            exchange.Confirm(1);
            exchange.Confirm(2);

            Assert.IsTrue(exchange.IsConfirmed(1) && exchange.IsConfirmed(2));
            Assert.IsTrue(exchange.BothConfirmed);
        }

        [TestMethod]
        public void Test_Add_Items()
        {
            var exchange = _exchangeService!.OpenExchange(1, 2);
            Assert.IsNotNull(exchange);

            var item = new InventoryItemInstance(new ItemInstance(new Item { VNum = 1012 })
            {
                Amount = 1
            });

            exchange.AddItem(1, item, item.ItemInstance.Amount);

            var data1 = exchange.GetPlayerData(1);

            Assert.IsTrue(data1.ExchangeItems.Any(s =>
                (s.Key.ItemInstance?.ItemVNum == 1012) && (s.Key.ItemInstance.Amount == 1)));
        }

        [TestMethod]
        public void Test_Check_Exchange()
        {
            var wrongExchange = _exchangeService!.GetExchange(1);
            Assert.IsNull(wrongExchange);

            var exchange = _exchangeService.OpenExchange(1, 2);
            Assert.IsNotNull(exchange);

            var goodExchange = _exchangeService.GetExchange(1);
            Assert.IsNotNull(goodExchange);
        }

        [TestMethod]
        public void Test_Close_Exchange()
        {
            var wrongClose = _exchangeService!.CloseExchange(1, ExchangeResultType.Failure);
            Assert.IsNull(wrongClose);

            _exchangeService.OpenExchange(1, 2);
            var goodClose = _exchangeService.CloseExchange(1, ExchangeResultType.Failure);
            Assert.IsTrue((goodClose != null) && (goodClose.Type == ExchangeResultType.Failure));
        }

        [TestMethod]
        public void Test_Open_Exchange()
        {
            var exchange = _exchangeService!.OpenExchange(1, 2);
            Assert.IsNotNull(exchange);
            Assert.AreEqual(1, exchange.Player1Id);
            Assert.AreEqual(2, exchange.Player2Id);
        }

        [TestMethod]
        public void Test_Open_Second_Exchange()
        {
            var exchange = _exchangeService!.OpenExchange(1, 2);
            Assert.IsNotNull(exchange);

            var wrongExchange = _exchangeService.OpenExchange(1, 3);
            Assert.IsNull(wrongExchange);
        }

        [TestMethod]
        public void Test_Process_Exchange()
        {
            IInventoryService inventory1 =
                new GameObject.Services.InventoryService.InventoryService(
                    new List<ItemDto> { new Item { VNum = 1012, Type = NoscorePocketType.Main } },
                    _worldConfiguration!, Logger);
            IInventoryService inventory2 =
                new GameObject.Services.InventoryService.InventoryService(
                    new List<ItemDto> { new Item { VNum = 1013, Type = NoscorePocketType.Main } },
                    _worldConfiguration!, Logger);
            var item1 = inventory1.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 1), 0))!
                .First();
            var item2 = inventory2.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 1), 0))!
                .First();

            var exchange = _exchangeService!.OpenExchange(1, 2);
            Assert.IsNotNull(exchange);

            exchange.AddItem(1, item1, 1);
            exchange.AddItem(2, item2, 1);
            var itemList = _exchangeService.ProcessExchange(1, 2, inventory1, inventory2, exchange);
            Assert.IsTrue((itemList.Count(s => s.Key == 1) == 2) && (itemList.Count(s => s.Key == 2) == 2));
        }

        [TestMethod]
        public void Test_Exchange_Partner_Id()
        {
            var exchange = _exchangeService!.OpenExchange(1, 2);
            Assert.IsNotNull(exchange);

            Assert.AreEqual(2, exchange.GetPartnerId(1));
            Assert.AreEqual(1, exchange.GetPartnerId(2));
        }

        [TestMethod]
        public void Test_Exchange_Is_Participant()
        {
            var exchange = _exchangeService!.OpenExchange(1, 2);
            Assert.IsNotNull(exchange);

            Assert.IsTrue(exchange.IsParticipant(1));
            Assert.IsTrue(exchange.IsParticipant(2));
            Assert.IsFalse(exchange.IsParticipant(3));
        }
    }
}
