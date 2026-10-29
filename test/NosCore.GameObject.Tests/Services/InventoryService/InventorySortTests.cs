//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Configuration;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    [TestClass]
    public class InventorySortTests
    {
        private GameObject.Services.ItemGenerationService.ItemGenerationService ItemProvider = null!;
        private IInventoryService Inventory = null!;
        private IReadOnlyList<short> TouchedSlots = null!;
        private List<ItemDto> Items = null!;

        [TestInitialize]
        public void Setup()
        {
            Items = new List<ItemDto>
            {
                MainItem(1002, "Small Health Potion", "Petite potion de soin"),
                MainItem(1004, "Large Health Potion", "Grande potion de soin"),
                MainItem(1011, "Huge Recovery Potion", "Enorme potion de recuperation"),
                MainItem(1012, "Seed of Power", "Graine de puissance"),
                MainItem(1013, "seed of power", "graine de puissance"),
                new Item { Type = NoscorePocketType.Etc, VNum = 2024, Name = Name("Biscuits", "Biscuits") },
            };
            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(Items,
                NullLoggerFactory.Instance, TestHelpers.Instance.LogLanguageLocalizer);
            Inventory = new GameObject.Services.InventoryService.InventoryService(Items,
                Options.Create(new WorldConfiguration { BackpackSize = 48, MaxItemAmount = 999 }),
                NullLogger<GameObject.Services.InventoryService.InventoryService>.Instance);
        }

        [TestMethod]
        public void SortingShouldOrderByNameFromTheFirstSlot()
        {
            new Spec("Sorting should order by name from the first slot")
                .Given(MainPocketIsScattered)
                .When(SortingMainPocketIn_, RegionType.EN)
                .Then(MainPocketShouldRead_, "1011 1004 1012 1002")
                .And(TouchedSlotsShouldBe_, "0 1 2 3 5 9")
                .Execute();
        }

        [TestMethod]
        public void SortingShouldUseTheAccountLanguage()
        {
            new Spec("Sorting should use the account language")
                .Given(MainPocketIsScattered)
                .When(SortingMainPocketIn_, RegionType.FR)
                .Then(MainPocketShouldRead_, "1011 1012 1004 1002")
                .Execute();
        }

        [TestMethod]
        public void SameNamesShouldGoByVnumThenLargestStackFirst()
        {
            new Spec("Same names should go by vnum then largest stack first")
                .Given(StacksSharingAName)
                .When(SortingMainPocketIn_, RegionType.EN)
                .Then(MainPocketShouldRead_, "1012x9 1012x5 1013x1")
                .Execute();
        }

        [TestMethod]
        public void SortingShouldLeaveOtherPocketsAlone()
        {
            new Spec("Sorting should leave other pockets alone")
                .Given(MainPocketIsScattered)
                .And(EtcItemInSlot_, 4)
                .When(SortingMainPocketIn_, RegionType.EN)
                .Then(EtcItemShouldStayInSlot_, 4)
                .Execute();
        }

        [TestMethod]
        public void SortingAnEmptyPocketShouldTouchNothing()
        {
            new Spec("Sorting an empty pocket should touch nothing")
                .When(SortingMainPocketIn_, RegionType.EN)
                .Then(TouchedSlotsShouldBe_, "")
                .Execute();
        }

        private static Item MainItem(short vnum, string english, string french) =>
            new() { Type = NoscorePocketType.Main, VNum = vnum, Name = Name(english, french) };

        private static I18NString Name(string english, string french) =>
            new() { [RegionType.EN] = english, [RegionType.FR] = french };

        private void Add(short vnum, short amount, NoscorePocketType pocket, short slot)
        {
            var instance = ItemProvider.Create(vnum, amount);
            // The test mapping drops I18N names; the world server's keeps them.
            instance.Item.Name = Items.Single(i => i.VNum == vnum).Name;
            Inventory.AddItemToPocket(InventoryItemInstance.Create(instance, 0), pocket, slot);
        }

        private void MainPocketIsScattered()
        {
            Add(1002, 1, NoscorePocketType.Main, 0);
            Add(1012, 1, NoscorePocketType.Main, 3);
            Add(1011, 1, NoscorePocketType.Main, 5);
            Add(1004, 1, NoscorePocketType.Main, 9);
        }

        private void StacksSharingAName()
        {
            Add(1013, 1, NoscorePocketType.Main, 0);
            Add(1012, 5, NoscorePocketType.Main, 1);
            Add(1012, 9, NoscorePocketType.Main, 2);
        }

        private void EtcItemInSlot_(int slot) => Add(2024, 1, NoscorePocketType.Etc, (short)slot);

        private void SortingMainPocketIn_(RegionType language) =>
            TouchedSlots = Inventory.SortPocket(NoscorePocketType.Main, language);

        private void MainPocketShouldRead_(string expected)
        {
            var pocket = Inventory.Values.Where(i => i.Type == NoscorePocketType.Main).OrderBy(i => i.Slot).ToList();
            CollectionAssert.AreEqual(Enumerable.Range(0, pocket.Count).Select(s => (short)s).ToList(),
                pocket.Select(i => i.Slot).ToList());
            var showsAmounts = expected.Contains('x');
            Assert.AreEqual(expected, string.Join(" ", pocket.Select(i => showsAmounts
                ? $"{i.ItemInstance.ItemVNum}x{i.ItemInstance.Amount}"
                : i.ItemInstance.ItemVNum.ToString())));
        }

        private void TouchedSlotsShouldBe_(string expected) =>
            Assert.AreEqual(expected, string.Join(" ", TouchedSlots));

        private void EtcItemShouldStayInSlot_(int slot) =>
            Assert.AreEqual((short)slot, Inventory.Values.Single(i => i.Type == NoscorePocketType.Etc).Slot);
    }
}
