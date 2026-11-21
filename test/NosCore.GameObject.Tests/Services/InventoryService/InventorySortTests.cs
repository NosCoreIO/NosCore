//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    [TestClass]
    public class InventorySortTests
    {
        private TestWorld World;
        private ClientSession Session;
        private IItemGenerationService ItemProvider;
        private IInventoryService Inventory => Session.Character.InventoryService;
        private readonly List<Guid> Added = [];

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.WorldConfiguration.Value.BackpackSize = 48;
            World.ItemList.AddRange(
            [
                Bag(NoscorePocketType.Main, ItemType.Main, 1034),
                Bag(NoscorePocketType.Main, ItemType.Main, 1031),
                Bag(NoscorePocketType.Main, ItemType.Upgrade, 1023),
                Bag(NoscorePocketType.Main, ItemType.Upgrade, 1021),
                Bag(NoscorePocketType.Mount, ItemType.Special, 9276),
                Bag(NoscorePocketType.Main, ItemType.Potion, 1011),
                Bag(NoscorePocketType.Main, ItemType.Potion, 1002),
                Bag(NoscorePocketType.Etc, ItemType.Sell, 2094),
                Bag(NoscorePocketType.Etc, ItemType.Sell, 2010),
                Bag(NoscorePocketType.Etc, ItemType.Food, 2306),
                Bag(NoscorePocketType.Etc, ItemType.Ammo, 2083),
                Gear(ItemType.Weapon, EquipmentType.MainWeapon, 13, 5),
                Gear(ItemType.Weapon, EquipmentType.SecondaryWeapon, 12, 10),
                Gear(ItemType.Fashion, EquipmentType.Hat, 20, 221),
                Gear(ItemType.Fashion, EquipmentType.Hat, 20, 216),
                Gear(ItemType.Armor, EquipmentType.Armor, 3, 13),
                Gear(ItemType.Fashion, EquipmentType.Mask, 20, 224),
                Gear(ItemType.Fashion, EquipmentType.Boots, 51, 714),
                Gear(ItemType.Fashion, EquipmentType.Boots, 45, 4998),
                Gear(ItemType.Fashion, EquipmentType.Boots, 43, 321),
                Gear(ItemType.Jewelery, EquipmentType.Fairy, 0, 803),
                Gear(ItemType.Jewelery, EquipmentType.Fairy, 0, 274),
                Gear(ItemType.Jewelery, EquipmentType.Amulet, 10, 182),
                Gear(ItemType.Jewelery, EquipmentType.Amulet, 1, 347),
                new Item { Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist, VNum = 901, LevelJobMinimum = 30 },
                new Item { Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist, VNum = 902, LevelJobMinimum = 20 },
            ]);
            World.Inject<IItemCatalog>(new ItemCatalog(World.ItemList));
            Session = await World.CreateSessionAsync();
            ItemProvider = World.GenerateItemProvider();
        }

        [TestMethod]
        public void TheMainPocketGoesByKindThenHighestNumberFirst()
        {
            new Spec("The main pocket goes by kind of item, then highest number first")
                .Given(TheMainPocketIsScattered)
                .When(Sorting_, NoscorePocketType.Main)
                .Then(_ShouldRead_, NoscorePocketType.Main, "1034 1031 1023 1021 9276 1011 1002")
                .Execute();
        }

        [TestMethod]
        public void TheEtcPocketGoesByKindThenHighestNumberFirst()
        {
            new Spec("The etc pocket goes by kind of item, then highest number first")
                .Given(TheEtcPocketIsScattered)
                .When(Sorting_, NoscorePocketType.Etc)
                .Then(_ShouldRead_, NoscorePocketType.Etc, "2094 2010 2306 2083")
                .Execute();
        }

        [TestMethod]
        public void StacksOfOneItemKeepTheirOrder()
        {
            new Spec("Stacks of one item keep the order they were in")
                .Given(TwoStacksOfOnePotion)
                .When(Sorting_, NoscorePocketType.Main)
                .Then(_ShouldRead_, NoscorePocketType.Main, "1011x110 1011x10")
                .Execute();
        }

        [TestMethod]
        public void EquipmentGoesBySlotThenHighestLevelThenHighestNumber()
        {
            new Spec("Equipment goes by slot, then highest level, then highest number")
                .Given(TheEquipmentPocketIsScattered)
                .When(Sorting_, NoscorePocketType.Equipment)
                .Then(_ShouldRead_, NoscorePocketType.Equipment, "5 10 221 216 13 224 714 4998 321 803 274 182 347")
                .Execute();
        }

        [TestMethod]
        public void SortingLeavesTheOtherPocketsAlone()
        {
            new Spec("Sorting leaves the other pockets alone")
                .Given(TheMainPocketIsScattered)
                .And(TheEtcPocketIsScattered)
                .When(Sorting_, NoscorePocketType.Main)
                .Then(_ShouldRead_, NoscorePocketType.Etc, "2083@1 2010@4 2306@6 2094@9")
                .Execute();
        }

        [TestMethod]
        public void SpecialistsStillGoByJobLevelRequirement()
        {
            new Spec("Specialist cards still go by job level requirement")
                .Given(SpecialistsAreScattered)
                .When(Sorting_, NoscorePocketType.Specialist)
                .Then(_ShouldRead_, NoscorePocketType.Specialist, "902 901")
                .Execute();
        }

        [TestMethod]
        public async Task SortingWhileItemsArriveLosesAndCopiesNothing()
        {
            await new Spec("Sorting while items arrive loses and copies nothing")
                .Given(TheEquipmentPocketIsScattered)
                .WhenAsync(SortingWhileOtherItemsArrive)
                .Then(EveryItemIsHeldOnce)
                .ExecuteAsync();
        }

        private static Item Bag(NoscorePocketType pocket, ItemType type, short vnum) =>
            new() { Type = pocket, ItemType = type, VNum = vnum };

        private static Item Gear(ItemType type, EquipmentType slot, byte level, short vnum) =>
            new() { Type = NoscorePocketType.Equipment, ItemType = type, EquipmentSlot = slot, LevelMinimum = level, VNum = vnum };

        private void Add(short vnum, short amount, NoscorePocketType pocket, short? slot)
        {
            var item = InventoryItemInstance.Create(ItemProvider.Create(vnum, amount), Session.Character.CharacterId);
            Inventory.AddItemToPocket(item, pocket, slot);
            Added.Add(item.ItemInstanceId);
        }

        private void TheMainPocketIsScattered()
        {
            Add(1002, 1, NoscorePocketType.Main, 0);
            Add(9276, 1, NoscorePocketType.Main, 2);
            Add(1031, 60, NoscorePocketType.Main, 3);
            Add(1021, 9, NoscorePocketType.Main, 7);
            Add(1034, 2, NoscorePocketType.Main, 11);
            Add(1011, 110, NoscorePocketType.Main, 12);
            Add(1023, 19, NoscorePocketType.Main, 20);
        }

        private void TheEtcPocketIsScattered()
        {
            Add(2083, 15, NoscorePocketType.Etc, 1);
            Add(2010, 8, NoscorePocketType.Etc, 4);
            Add(2306, 10, NoscorePocketType.Etc, 6);
            Add(2094, 7, NoscorePocketType.Etc, 9);
        }

        private void TwoStacksOfOnePotion()
        {
            Add(1011, 110, NoscorePocketType.Main, 2);
            Add(1011, 10, NoscorePocketType.Main, 7);
        }

        private void TheEquipmentPocketIsScattered()
        {
            short slot = 0;
            foreach (var vnum in new short[] { 347, 321, 13, 803, 216, 714, 182, 10, 224, 4998, 274, 221, 5 })
            {
                Add(vnum, 1, NoscorePocketType.Equipment, slot);
                slot += 2;
            }
        }

        private void SpecialistsAreScattered()
        {
            Add(901, 1, NoscorePocketType.Specialist, 1);
            Add(902, 1, NoscorePocketType.Specialist, 4);
        }

        private void Sorting_(NoscorePocketType pocket) => Inventory.SortPocket(pocket);

        private async Task SortingWhileOtherItemsArrive()
        {
            var arriving = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
            {
                var item = InventoryItemInstance.Create(ItemProvider.Create(5, 1), Session.Character.CharacterId);
                if (Inventory.AddItemToPocket(item, NoscorePocketType.Equipment)?.Contains(item) == true)
                {
                    lock (Added)
                    {
                        Added.Add(item.ItemInstanceId);
                    }
                }
            }));
            var sorting = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(() => Inventory.SortPocket(NoscorePocketType.Equipment)));
            await Task.WhenAll(arriving.Concat(sorting));
        }

        private void EveryItemIsHeldOnce()
        {
            CollectionAssert.AreEquivalent(Added, Inventory.Values.Select(i => i.ItemInstanceId).ToList());
            Assert.AreEqual(Added.Count, Inventory.Values.Select(i => i.ItemInstanceId).Distinct().Count());
        }

        private void _ShouldRead_(NoscorePocketType pocket, string expected)
        {
            var items = Inventory.Values.Where(i => i.Type == pocket).OrderBy(i => i.Slot).ToList();
            var showsSlots = expected.Contains('@');
            if (!showsSlots)
            {
                CollectionAssert.AreEqual(Enumerable.Range(0, items.Count).Select(s => (short)s).ToList(),
                    items.Select(i => i.Slot).ToList(), "the pocket is packed from its first slot");
            }

            var showsAmounts = expected.Contains('x');
            Assert.AreEqual(expected, string.Join(" ", items.Select(i =>
                showsSlots ? $"{i.ItemInstance.ItemVNum}@{i.Slot}"
                : showsAmounts ? $"{i.ItemInstance.ItemVNum}x{i.ItemInstance.Amount}"
                : i.ItemInstance.ItemVNum.ToString())));
        }
    }
}
