//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using ExchangeRequestRegistry = NosCore.GameObject.Services.ExchangeService.ExchangeRequestRegistry;

namespace NosCore.GameObject.Tests.Services.ExchangeService
{
    [TestClass]
    public class ExchangeServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private GameObject.Services.ExchangeService.ExchangeService? ExchangeProvider;
        private GameObject.Services.ItemGenerationService.ItemGenerationService? ItemProvider;
        private IOptions<WorldConfiguration>? WorldConfiguration;

        [TestInitialize]
        public void Setup()
        {
            WorldConfiguration = Options.Create(new WorldConfiguration
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

            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            ExchangeProvider = new GameObject.Services.ExchangeService.ExchangeService(ItemProvider, WorldConfiguration, Logger, new ExchangeRequestRegistry(), TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.GameLanguageLocalizer);
        }

        [TestMethod]
        public void SettingGoldShouldUpdateExchangeData()
        {
            new Spec("Setting gold should update exchange data")
                .Given(ExchangeIsOpen)
                .When(SettingGoldForBothParties)
                .Then(BothPartiesShouldHaveCorrectGold)
                .Execute();
        }

        [TestMethod]
        public void ConfirmingExchangeShouldSetConfirmedFlag()
        {
            new Spec("Confirming exchange should set confirmed flag")
                .Given(ExchangeIsOpen)
                .When(BothPartiesConfirm)
                .Then(BothShouldBeConfirmed)
                .Execute();
        }

        [TestMethod]
        public void AddingItemsShouldAddToExchange()
        {
            new Spec("Adding items should add to exchange")
                .Given(ExchangeIsOpen)
                .When(AddingItemToExchange)
                .Then(ExchangeShouldContainItem)
                .Execute();
        }

        [TestMethod]
        public void CheckingExchangeShouldReturnCorrectStatus()
        {
            new Spec("Checking exchange should return correct status")
                .Then(CheckBeforeOpenShouldBeFalse)
                .And(CheckAfterOpenShouldBeTrue)
                .Execute();
        }

        [TestMethod]
        public void ClosingExchangeShouldWorkCorrectly()
        {
            new Spec("Closing exchange should work correctly")
                .Then(CloseWithoutExchangeShouldReturnNull)
                .And(CloseWithExchangeShouldReturnResult)
                .Execute();
        }

        [TestMethod]
        public void OpeningExchangeShouldSucceed()
        {
            new Spec("Opening exchange should succeed")
                .Then(FirstExchangeShouldSucceed)
                .Execute();
        }

        [TestMethod]
        public void OpeningSecondExchangeShouldFail()
        {
            new Spec("Opening second exchange should fail")
                .Given(ExchangeIsOpen)
                .Then(SecondExchangeShouldFail)
                .Execute();
        }

        [TestMethod]
        public void ProcessingExchangeShouldSwapItems()
        {
            new Spec("Processing exchange should swap items")
                .Then(ProcessExchangeShouldReturnItemsForBoth)
                .Execute();
        }

        private void ExchangeIsOpen()
        {
            ExchangeProvider!.OpenExchange(1, 2);
        }

        private void SettingGoldForBothParties()
        {
            ExchangeProvider!.SetGold(1, 1000, 1000);
            ExchangeProvider.SetGold(2, 2000, 2000);
        }

        private void BothPartiesShouldHaveCorrectGold()
        {
            var data1 = ExchangeProvider!.GetData(1);
            var data2 = ExchangeProvider.GetData(2);
            Assert.IsTrue((data1.Gold == 1000) && (data1.BankGold == 1000) && (data2.Gold == 2000) &&
                (data2.BankGold == 2000));
        }

        private void BothPartiesConfirm()
        {
            ExchangeProvider!.ConfirmExchange(1);
            ExchangeProvider.ConfirmExchange(2);
        }

        private void BothShouldBeConfirmed()
        {
            var data1 = ExchangeProvider!.GetData(1);
            var data2 = ExchangeProvider.GetData(2);
            Assert.IsTrue(data1.ExchangeConfirmed && data2.ExchangeConfirmed);
        }

        private void AddingItemToExchange()
        {
            var item = new InventoryItemInstance(new ItemInstance(new Item { VNum = 1012 })
            {
                Amount = 1
            });
            ExchangeProvider!.AddItems(1, item, item.ItemInstance.Amount);
        }

        private void ExchangeShouldContainItem()
        {
            var data1 = ExchangeProvider!.GetData(1);
            Assert.IsTrue(data1.ExchangeItems.Any(s =>
                (s.Key.ItemInstance?.ItemVNum == 1012) && (s.Key.ItemInstance.Amount == 1)));
        }

        private void CheckBeforeOpenShouldBeFalse()
        {
            var wrongExchange = ExchangeProvider!.CheckExchange(1);
            Assert.IsFalse(wrongExchange);
        }

        private void CheckAfterOpenShouldBeTrue()
        {
            ExchangeProvider!.OpenExchange(1, 2);
            var goodExchange = ExchangeProvider.CheckExchange(1);
            Assert.IsTrue(goodExchange);
        }

        private void CloseWithoutExchangeShouldReturnNull()
        {
            var wrongClose = ExchangeProvider!.CloseExchange(1, ExchangeResultType.Failure);
            Assert.IsNull(wrongClose);
        }

        private void CloseWithExchangeShouldReturnResult()
        {
            ExchangeProvider!.OpenExchange(1, 2);
            var goodClose = ExchangeProvider.CloseExchange(1, ExchangeResultType.Failure);
            Assert.IsTrue((goodClose != null) && (goodClose.Type == ExchangeResultType.Failure));
        }

        private void FirstExchangeShouldSucceed()
        {
            var exchange = ExchangeProvider!.OpenExchange(1, 2);
            Assert.IsTrue(exchange);
        }

        private void SecondExchangeShouldFail()
        {
            var wrongExchange = ExchangeProvider!.OpenExchange(1, 3);
            Assert.IsFalse(wrongExchange);
        }

        private void ProcessExchangeShouldReturnItemsForBoth()
        {
            IInventoryService inventory1 =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1012, Type = NoscorePocketType.Main } },
                    WorldConfiguration!, Logger);
            IInventoryService inventory2 =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1013, Type = NoscorePocketType.Main } },
                    WorldConfiguration!, Logger);
            var item1 = inventory1.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 1), 0))!
                .First();
            var item2 = inventory2.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1013, 1), 0))!
                .First();

            ExchangeProvider!.OpenExchange(1, 2);
            ExchangeProvider.AddItems(1, item1, 1);
            ExchangeProvider.AddItems(2, item2, 1);
            var itemList = ExchangeProvider.ProcessExchange(1, 2, inventory1, inventory2);
            Assert.IsTrue((itemList.Count(s => s.Key == 1) == 2) && (itemList.Count(s => s.Key == 2) == 2));
        }
    }
}
