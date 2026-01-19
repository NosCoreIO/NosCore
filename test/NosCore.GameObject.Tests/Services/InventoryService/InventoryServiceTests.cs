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
using NosCore.Data.Enumerations.Items;
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

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    [TestClass]
    public class InventoryServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private GameObject.Services.ItemGenerationService.ItemGenerationService? ItemProvider;
        private IInventoryService? Inventory { get; set; }

        [TestInitialize]
        public void Setup()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Main, VNum = 1012},
                new Item {Type = NoscorePocketType.Main, VNum = 1013},
                new Item {Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
                new Item {Type = NoscorePocketType.Equipment, VNum = 2, ItemType = ItemType.Weapon},
                new Item {Type = NoscorePocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist},
                new Item {Type = NoscorePocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion}
            };
            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            Inventory = new GameObject.Services.InventoryService.InventoryService(items, Options.Create(new WorldConfiguration { BackpackSize = 3, MaxItemAmount = 999 }),
                Logger);
        }

        [TestMethod]
        public void CreatingItemShouldAddToInventory()
        {
            new Spec("Creating item should add to inventory")
                .When(AddingItemToPocket)
                .Then(ItemShouldBeInInventory)
                .Execute();
        }

        [TestMethod]
        public void CreatingItemAndStackingShouldIncreaseAmount()
        {
            new Spec("Creating item and stacking should increase amount")
                .Given(ItemAlreadyInPocket)
                .When(AddingSameItemAgain)
                .Then(AmountShouldBeTwo)
                .Execute();
        }

        [TestMethod]
        public void CreatingItemWhenSlotIsMaxShouldCreateNewStack()
        {
            new Spec("Creating item when slot is max should create new stack")
                .Given(FullStackInPocket)
                .When(AddingOneMoreItem)
                .Then(NewStackShouldHaveOneItem)
                .Execute();
        }

        [TestMethod]
        public void CreatingItemWhenSlotFilledShouldSplit()
        {
            new Spec("Creating item when slot filled should split")
                .Given(AlmostFullStackInPocket)
                .When(AddingItemsThatOverflow)
                .Then(ShouldSplitIntoTwoStacks)
                .Execute();
        }

        [TestMethod]
        public void CreatingItemShouldFillMultipleSlots()
        {
            new Spec("Creating item should fill multiple slots")
                .Given(ThreeAlmostFullStacks)
                .When(AddingItemsToFillAll)
                .Then(AllStacksShouldBeFull)
                .Execute();
        }

        [TestMethod]
        public void CreatingMoreItemsThanInventoryPlaceShouldFail()
        {
            new Spec("Creating more items than inventory place should fail")
                .Given(ThreeAlmostFullStacks)
                .When(AddingTooManyItems)
                .Then(StacksShouldRemainUnchanged)
                .Execute();
        }

        [TestMethod]
        public void CreatingStackOnSpecificItemShouldStackCorrectly()
        {
            new Spec("Creating stack on specific item should stack correctly")
                .Given(MixedItemsInInventory)
                .When(AddingItemThatStacksOnSecond)
                .Then(SecondStackShouldIncrease)
                .Execute();
        }

        [TestMethod]
        public void CreatingDoesntStackOnWrongItem()
        {
            new Spec("Creating doesnt stack on wrong item")
                .Given(MixedItemsInInventory)
                .When(AddingDifferentItem)
                .Then(FirstStackShouldRemainUnchanged)
                .Execute();
        }

        [TestMethod]
        public void LoadingItemOnNonEmptySlotShouldReturnItem()
        {
            new Spec("Loading item on non empty slot should return item")
                .Given(ItemInSlotZero)
                .Then(LoadShouldReturnItem)
                .Execute();
        }

        [TestMethod]
        public void LoadingNonExistingItemShouldReturnNull()
        {
            new Spec("Loading non existing item should return null")
                .Given(ItemInSlotZero)
                .Then(LoadSlotOneShouldReturnNull)
                .Execute();
        }

        [TestMethod]
        public void DeletingFromTypeAndSlotShouldRemoveItem()
        {
            new Spec("Deleting from type and slot should remove item")
                .Given(TwoStacksInInventory)
                .When(DeletingFromSlotZero)
                .Then(OnlyOneItemShouldRemain)
                .Execute();
        }

        [TestMethod]
        public void DeletingByIdShouldRemoveItem()
        {
            new Spec("Deleting by id should remove item")
                .Given(TwoStacksInInventoryForDelete)
                .When(DeletingByInstanceId)
                .Then(OnlyOneItemShouldRemain)
                .Execute();
        }

        [TestMethod]
        public void MovingFullSlotShouldMoveAll()
        {
            new Spec("Moving full slot should move all")
                .Given(FullStackForMove)
                .When(MovingFullStack)
                .Then(OriginShouldBeNullDestinationShouldHaveAll)
                .Execute();
        }

        [TestMethod]
        public void MovingHalfSlotShouldSplit()
        {
            new Spec("Moving half slot should split")
                .Given(FullStackForMove)
                .When(MovingHalfStack)
                .Then(BothShouldHaveItems)
                .Execute();
        }

        [TestMethod]
        public void MovingHalfAndMergingShouldCombine()
        {
            new Spec("Moving half and merging should combine")
                .Given(FullStackForMove)
                .When(MovingHalfThenRemaining)
                .Then(AllShouldBeInDestination)
                .Execute();
        }

        [TestMethod]
        public void MovingWithOverflowShouldCapAtMax()
        {
            new Spec("Moving with overflow should cap at max")
                .Given(TwoStacksForOverflowTest)
                .When(MovingWithOverflow)
                .Then(OriginShouldHaveOverflow)
                .Execute();
        }

        [TestMethod]
        public void MovingFashionToFashionPocketShouldSucceed()
        {
            new Spec("Moving fashion to fashion pocket should succeed")
                .Given(FashionItemInEquipment)
                .When(MovingToCostumePocket)
                .Then(ItemShouldBeInCostume)
                .Execute();
        }

        [TestMethod]
        public void MovingFashionToSpecialistPocketShouldFail()
        {
            new Spec("Moving fashion to specialist pocket should fail")
                .Given(FashionItemInEquipment)
                .When(MovingFashionToSpecialist)
                .Then(MoveShouldReturnNull)
                .Execute();
        }

        [TestMethod]
        public void MovingSpecialistToFashionPocketShouldFail()
        {
            new Spec("Moving specialist to fashion pocket should fail")
                .Given(SpecialistItemInEquipment)
                .When(MovingSpecialistToCostume)
                .Then(MoveShouldReturnNull)
                .Execute();
        }

        [TestMethod]
        public void MovingSpecialistToSpecialistPocketShouldSucceed()
        {
            new Spec("Moving specialist to specialist pocket should succeed")
                .Given(SpecialistItemInEquipment)
                .When(MovingToSpecialistPocket)
                .Then(ItemShouldBeInSpecialist)
                .Execute();
        }

        [TestMethod]
        public void MovingWeaponToWearPocketShouldSucceed()
        {
            new Spec("Moving weapon to wear pocket should succeed")
                .Given(WeaponInEquipment)
                .When(MovingToWear)
                .Then(ItemShouldBeInWear)
                .Execute();
        }

        [TestMethod]
        public void SwappingWithEmptyShouldMove()
        {
            new Spec("Swapping with empty should move")
                .Given(WeaponInEquipment)
                .When(SwappingToEmptyWearSlot)
                .Then(WeaponShouldBeInWearSlotOriginEmpty)
                .Execute();
        }

        [TestMethod]
        public void SwappingWithNotEmptyShouldExchange()
        {
            new Spec("Swapping with not empty should exchange")
                .Given(TwoWeaponsInEquipment)
                .When(SwappingWeapons)
                .Then(WeaponsShouldBeExchanged)
                .Execute();
        }

        private InventoryItemInstance? ItemForDelete;
        private InventoryItemInstance? ItemToMove;
        private InventoryItemInstance? FashionItem;
        private InventoryItemInstance? SpecialistItem;
        private InventoryItemInstance? WeaponItem;
        private InventoryItemInstance? WeaponItem2;
        private InventoryItemInstance? OriginItem;
        private InventoryItemInstance? DestItem;
        private InventoryItemInstance? MovedItem;

        private void AddingItemToPocket()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012), 0));
        }

        private void ItemShouldBeInInventory()
        {
            var item = Inventory!.Values.First();
            Assert.IsTrue((item.ItemInstance?.Amount == 1) && (item.ItemInstance.ItemVNum == 1012) &&
                (item.Type == NoscorePocketType.Main));
        }

        private void ItemAlreadyInPocket()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012), 0));
        }

        private void AddingSameItemAgain()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012), 0));
        }

        private void AmountShouldBeTwo()
        {
            var item = Inventory!.Values.First();
            Assert.IsTrue((item.ItemInstance?.Amount == 2) && (item.ItemInstance.ItemVNum == 1012));
        }

        private void FullStackInPocket()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 999), 0));
        }

        private void AddingOneMoreItem()
        {
            AddedItems = Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012), 0));
        }

        private void NewStackShouldHaveOneItem()
        {
            Assert.IsTrue(AddedItems![0].ItemInstance?.Amount == 1);
        }

        private void AlmostFullStackInPocket()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 990), 0));
        }

        private IList<InventoryItemInstance>? AddedItems;

        private void AddingItemsThatOverflow()
        {
            AddedItems = Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 29), 0));
        }

        private void ShouldSplitIntoTwoStacks()
        {
            Assert.IsTrue((AddedItems![0].ItemInstance?.Amount == 999) && (AddedItems.Last().ItemInstance?.Amount == 20));
        }

        private void ThreeAlmostFullStacks()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 990), 0), NoscorePocketType.Main, 0);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1012, 990), 0), NoscorePocketType.Main, 1);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1012, 990), 0), NoscorePocketType.Main, 2);
        }

        private void AddingItemsToFillAll()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 27), 0));
        }

        private void AllStacksShouldBeFull()
        {
            var items = Inventory!.Values.ToList();
            Assert.IsTrue(items.All(item => item.ItemInstance.Amount == 999) && (items.Count == 3));
        }

        private void AddingTooManyItems()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 99), 0));
        }

        private void StacksShouldRemainUnchanged()
        {
            Assert.IsTrue(Inventory!.Values.All(item => item.ItemInstance?.Amount == 990));
        }

        private void MixedItemsInInventory()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1013, 990), 0));
        }

        private void AddingItemThatStacksOnSecond()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1013), 0));
        }

        private void SecondStackShouldIncrease()
        {
            Assert.IsTrue(Inventory!.Values.First(item => item.Slot == 1).ItemInstance.Amount == 991);
        }

        private void AddingDifferentItem()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1013, 19), 0));
        }

        private void FirstStackShouldRemainUnchanged()
        {
            Assert.IsTrue(Inventory!.Values.First(item => item.Slot == 0).ItemInstance?.Amount == 990);
        }

        private void ItemInSlotZero()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 990), 0));
        }

        private void LoadShouldReturnItem()
        {
            var item = Inventory!.LoadBySlotAndType(0, NoscorePocketType.Main);
            Assert.IsTrue((item!.ItemInstance?.ItemVNum == 1012) && (item.ItemInstance?.Amount == 990));
        }

        private void LoadSlotOneShouldReturnNull()
        {
            var item = Inventory!.LoadBySlotAndType(1, NoscorePocketType.Main);
            Assert.IsNull(item);
        }

        private void TwoStacksInInventory()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1012, 990), 0));
        }

        private void DeletingFromSlotZero()
        {
            Inventory!.DeleteFromTypeAndSlot(NoscorePocketType.Main, 0);
        }

        private void OnlyOneItemShouldRemain()
        {
            Assert.IsTrue(Inventory!.Count == 1);
        }

        private void TwoStacksInInventoryForDelete()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 990), 0));
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1012, 990), 0));
            ItemForDelete = items![0];
        }

        private void DeletingByInstanceId()
        {
            Inventory!.DeleteById(ItemForDelete!.ItemInstanceId);
        }

        private void FullStackForMove()
        {
            var items = Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 999), 0));
            ItemToMove = items![0];
        }

        private void MovingFullStack()
        {
            Inventory!.TryMoveItem(ItemToMove!.Type, ItemToMove.Slot, 999, 1, out OriginItem, out DestItem);
        }

        private void OriginShouldBeNullDestinationShouldHaveAll()
        {
            Assert.IsTrue(OriginItem == null);
            Assert.IsTrue((DestItem?.ItemInstance?.Amount == 999) && (DestItem.Slot == 1));
        }

        private void MovingHalfStack()
        {
            Inventory!.TryMoveItem(ItemToMove!.Type, ItemToMove.Slot, 499, 1, out OriginItem, out DestItem);
        }

        private void BothShouldHaveItems()
        {
            Assert.IsTrue((OriginItem?.ItemInstance?.Amount == 500) && (OriginItem.Slot == 0));
            Assert.IsTrue((DestItem?.ItemInstance?.Amount == 499) && (DestItem.Slot == 1));
        }

        private void MovingHalfThenRemaining()
        {
            Inventory!.TryMoveItem(ItemToMove!.Type, ItemToMove.Slot, 499, 1, out _, out _);
            Inventory.TryMoveItem(ItemToMove.Type, 0, 500, 1, out OriginItem, out DestItem);
        }

        private void AllShouldBeInDestination()
        {
            Assert.IsTrue(OriginItem == null);
            Assert.IsTrue((DestItem?.ItemInstance?.Amount == 999) && (DestItem.Slot == 1));
        }

        private void TwoStacksForOverflowTest()
        {
            var items = Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 999), 0));
            ItemToMove = items![0];
            Inventory.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1012, 500), 0));
        }

        private void MovingWithOverflow()
        {
            Inventory!.TryMoveItem(ItemToMove!.Type, ItemToMove.Slot, 600, 1, out OriginItem, out DestItem);
        }

        private void OriginShouldHaveOverflow()
        {
            Assert.IsTrue((OriginItem?.ItemInstance?.Amount == 500) && (OriginItem.Slot == 0));
            Assert.IsTrue((DestItem?.ItemInstance?.Amount == 999) && (DestItem.Slot == 1));
        }

        private void FashionItemInEquipment()
        {
            var items = Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(924), 0));
            FashionItem = items![0];
        }

        private void MovingToCostumePocket()
        {
            MovedItem = Inventory!.MoveInPocket(FashionItem!.Slot, FashionItem.Type, NoscorePocketType.Costume);
        }

        private void ItemShouldBeInCostume()
        {
            Assert.IsTrue(MovedItem?.Type == NoscorePocketType.Costume);
        }

        private void MovingFashionToSpecialist()
        {
            MovedItem = Inventory!.MoveInPocket(FashionItem!.Slot, FashionItem.Type, NoscorePocketType.Specialist);
        }

        private void MoveShouldReturnNull()
        {
            Assert.IsNull(MovedItem);
        }

        private void SpecialistItemInEquipment()
        {
            var items = Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(912), 0));
            SpecialistItem = items![0];
        }

        private void MovingSpecialistToCostume()
        {
            MovedItem = Inventory!.MoveInPocket(SpecialistItem!.Slot, SpecialistItem.Type, NoscorePocketType.Costume);
        }

        private void MovingToSpecialistPocket()
        {
            MovedItem = Inventory!.MoveInPocket(SpecialistItem!.Slot, SpecialistItem.Type, NoscorePocketType.Specialist);
        }

        private void ItemShouldBeInSpecialist()
        {
            Assert.IsTrue(MovedItem?.Type == NoscorePocketType.Specialist);
        }

        private void WeaponInEquipment()
        {
            var items = Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1), 0));
            WeaponItem = items![0];
        }

        private void MovingToWear()
        {
            MovedItem = Inventory!.MoveInPocket(WeaponItem!.Slot, WeaponItem.Type, NoscorePocketType.Wear);
        }

        private void ItemShouldBeInWear()
        {
            Assert.IsTrue(MovedItem?.Type == NoscorePocketType.Wear);
        }

        private void SwappingToEmptyWearSlot()
        {
            MovedItem = Inventory!.MoveInPocket(WeaponItem!.Slot, WeaponItem.Type, NoscorePocketType.Wear,
                (short)EquipmentType.MainWeapon, true);
        }

        private void WeaponShouldBeInWearSlotOriginEmpty()
        {
            Assert.IsTrue((MovedItem?.Type == NoscorePocketType.Wear) &&
                (Inventory!.LoadBySlotAndType(0, NoscorePocketType.Equipment) == null));
        }

        private void TwoWeaponsInEquipment()
        {
            var items = Inventory!.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(2), 0));
            WeaponItem = items![0];
            var items2 = Inventory.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1), 0));
            WeaponItem2 = items2![0];
        }

        private void SwappingWeapons()
        {
            MovedItem = Inventory!.MoveInPocket(WeaponItem!.Slot, WeaponItem.Type, NoscorePocketType.Wear,
                (short)EquipmentType.MainWeapon, true);
            MovedItem = Inventory.MoveInPocket(WeaponItem2!.Slot, WeaponItem2.Type, NoscorePocketType.Wear,
                (short)EquipmentType.MainWeapon, true);
        }

        private void WeaponsShouldBeExchanged()
        {
            Assert.IsTrue((WeaponItem?.Type == NoscorePocketType.Equipment) && (WeaponItem.Slot == 1) &&
                (MovedItem?.Type == NoscorePocketType.Wear) &&
                (MovedItem.Slot == (short)EquipmentType.MainWeapon));
        }
    }
}
