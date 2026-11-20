//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.RarityPointService;
using NosCore.Core.Configuration;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    // Each test stands a second writer in at the moment the first one is between its check and
    // its write: a hook on a read the first writer makes starts the second on another thread and
    // lets the first carry on only once the second has finished or is blocked.
    [TestClass]
    public class InventoryServiceConcurrencyTests
    {
        private readonly WorldConfiguration _config = new()
        { Database = new(), MasterCommunication = new(), BackpackSize = 3, MaxItemAmount = 999 };

        private Func<bool>? _hook;
        private Thread? _testThread;
        private Thread? _otherWriter;
        private GameObject.Services.ItemGenerationService.ItemGenerationService _itemProvider;
        private GameObject.Services.InventoryService.InventoryService _inventory;

        [TestInitialize]
        public void Setup()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Main, VNum = 1012 },
                new Item { Type = NoscorePocketType.Main, VNum = 1013 },
                new Item { Type = NoscorePocketType.Main, VNum = 1014 },
                new Item { Type = NoscorePocketType.Main, VNum = 1015 },
            };
            var options = new Mock<IOptions<WorldConfiguration>>();
            options.Setup(o => o.Value).Returns(() =>
            {
                RunHook();
                return _config;
            });
            _itemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(new ItemCatalog(items),
                NullLoggerFactory.Instance, TestLocalizers.Log, new RarityPointService(),
                new NosCore.GameObject.Services.BattleService.RandomProvider());
            _inventory = new GameObject.Services.InventoryService.InventoryService(new ItemCatalog(items), options.Object,
                NullLogger<GameObject.Services.InventoryService.InventoryService>.Instance, TestLocalizers.Log);
            _testThread = Thread.CurrentThread;
        }

        [TestMethod]
        public void ASlotTakenBetweenTheMergeAndThePlacementLosesNothing()
        {
            var held = Add(1012, 998);
            Add(1013, 999);
            var otherWriter = Entry(1014, 1);
            var readsAfterMerge = 0;
            WhenReading(() => held.ItemInstance.Amount == 999 && ++readsAfterMerge == 2,
                () => _inventory.AddItemToPocket(otherWriter, NoscorePocketType.Main, 2));
            var reward = Entry(1012, 5);

            var result = _inventory.AddItemToPocket(reward);
            _otherWriter?.Join();

            Assert.IsNotNull(result, "the caller is told what was added");
            CollectionAssert.AreEquivalent(new[] { held, reward }, result);
            Assert.AreEqual(1003, _inventory.CountItem(1012), "every unit of the reward is in the bag");
            Assert.AreEqual(4, _inventory[reward.ItemInstanceId].ItemInstance.Amount);
        }

        [TestMethod]
        public void AWriterArrivingDuringAnAddWaitsForIt()
        {
            var held = Add(1012, 998);
            Add(1013, 999);
            var otherWriter = Entry(1014, 1);
            List<InventoryItemInstance>? otherResult = [];
            WhenReading(() => true, () => otherResult = _inventory.AddItemToPocket(otherWriter, NoscorePocketType.Main, 2));
            var reward = Entry(1012, 5);

            var result = _inventory.AddItemToPocket(reward);
            _otherWriter?.Join();

            CollectionAssert.AreEquivalent(new[] { held, reward }, result);
            Assert.AreEqual(1003, _inventory.CountItem(1012));
            Assert.IsNull(otherResult, "the later writer finds its slot taken");
            Assert.AreEqual(0, _inventory.CountItem(1014));
        }

        [TestMethod]
        public void AStackGivenAwayDuringAnAddLeavesWhatWasMergedIntoItWithIt()
        {
            var held = Add(1012, 998);
            IInventoryService asDictionary = _inventory;
            short givenAway = 0;
            WhenReading(() => true, () =>
            {
                asDictionary.Remove(held.ItemInstanceId);
                givenAway = held.ItemInstance.Amount;
            });

            _inventory.AddItemToPocket(Entry(1012, 5));
            _otherWriter?.Join();

            Assert.AreEqual(1003, givenAway + _inventory.CountItem(1012));
        }

        [TestMethod]
        public void TwoConsumersOfTheLastUnitTakeItOnce()
        {
            var amount = (short)1;
            var entry = HookedEntry(() => amount, value => amount = value);
            var otherConsumed = false;
            WhenReading(() => true, () => otherConsumed = _inventory.TryConsume(entry, 1));

            var consumed = _inventory.TryConsume(entry, 1);
            _otherWriter?.Join();

            Assert.IsTrue(consumed ^ otherConsumed, "exactly one of them gets it");
            Assert.AreEqual(0, amount);
            Assert.IsFalse(_inventory.ContainsKey(entry.ItemInstanceId));
        }

        [TestMethod]
        public void TwoRemovalsFromOneStackBothCount()
        {
            var amount = (short)2;
            var entry = HookedEntry(() => amount, value => amount = value);
            WhenReading(() => true, () => _inventory.RemoveItemAmountFromInventory(1, entry.ItemInstanceId));

            _inventory.RemoveItemAmountFromInventory(1, entry.ItemInstanceId);
            _otherWriter?.Join();

            Assert.AreEqual(0, amount);
            Assert.IsFalse(_inventory.ContainsKey(entry.ItemInstanceId));
        }

        [TestMethod]
        public void AMoveIntoASlotTakenMeanwhileLeavesOneItemPerSlot()
        {
            var moved = Add(1012, 10);
            var otherWriter = Entry(1013, 1);
            List<InventoryItemInstance>? otherResult = [];
            WhenReading(() => true, () => otherResult = _inventory.AddItemToPocket(otherWriter, NoscorePocketType.Main, 1));

            _inventory.TryMoveItem(NoscorePocketType.Main, 0, 10, 1, out _, out _);
            _otherWriter?.Join();

            Assert.AreEqual(1, moved.Slot);
            Assert.IsNull(otherResult);
            Assert.AreEqual(1, _inventory.Values.Count(i => i.Type == NoscorePocketType.Main && i.Slot == 1));
        }

        [TestMethod]
        public void ItemsThatDoNotAllFitAreNotAddedAtAll()
        {
            var held = Add(1012, 998);
            Add(1013, 999);
            var first = Entry(1012, 5);
            var second = Entry(1014, 1);

            var result = _inventory.AddItemsToPocket([first, second]);

            Assert.IsNull(result);
            Assert.AreEqual(998, held.ItemInstance.Amount, "the merge into the held stack is undone");
            Assert.AreEqual(5, first.ItemInstance.Amount, "the item keeps all it held");
            Assert.AreEqual(2, _inventory.Count);
        }

        [TestMethod]
        public void ItemsThatAllFitAreAllAdded()
        {
            var held = Add(1012, 998);
            var first = Entry(1012, 5);
            var second = Entry(1014, 1);

            var result = _inventory.AddItemsToPocket([first, second]);

            CollectionAssert.AreEquivalent(new[] { held, first, second }, result);
            Assert.AreEqual(1003, _inventory.CountItem(1012));
            Assert.AreEqual(1, _inventory.CountItem(1014));
        }

        [TestMethod]
        public void DeletingAnItemAlreadyGoneLeavesNothing()
        {
            var entry = Add(1012, 1);
            _inventory.DeleteById(entry.ItemInstanceId);

            Assert.IsNull(_inventory.DeleteById(entry.ItemInstanceId));
        }

        [TestMethod]
        public void AConsumedItemWhoseSlotWasTakenComesBackElsewhere()
        {
            var source = Add(1012, 1);
            Assert.IsTrue(_inventory.TryConsume(source, 1));
            Add(1013, 1);

            var restored = _inventory.RestoreConsumed(source, 1);

            CollectionAssert.AreEqual(new[] { source }, restored);
            Assert.AreEqual(1, source.Slot);
        }

        [TestMethod]
        public void ACollectionRemovalDuringAnAddWaitsForIt()
        {
            var held = Add(1012, 998);
            ICollection<KeyValuePair<Guid, InventoryItemInstance>> asCollection = _inventory;
            short givenAway = 0;
            var reads = 0;
            WhenReading(() => ++reads == 2, () =>
            {
                asCollection.Remove(new KeyValuePair<Guid, InventoryItemInstance>(held.ItemInstanceId, held));
                givenAway = held.ItemInstance.Amount;
            });

            _inventory.AddItemToPocket(Entry(1012, 5));
            _otherWriter?.Join();

            Assert.AreEqual(1003, givenAway + _inventory.CountItem(1012));
        }

        [TestMethod]
        public void CountingDuringABatchSeesItWholeOrNotAtAll()
        {
            var held = Add(1012, 998);
            Add(1013, 999);
            var counted = 0;
            WhenReading(() => held.ItemInstance.Amount == 999, () => counted = _inventory.CountItem(1012));

            var result = _inventory.AddItemsToPocket([Entry(1012, 5), Entry(1014, 1)]);
            _otherWriter?.Join();

            Assert.IsNull(result);
            Assert.AreEqual(998, counted, "the count never shows a batch that is later undone");
        }

        [TestMethod]
        public void AnAmountAboveOneStackFillsSeveralSlots()
        {
            var item = Entry(1012, 1500);

            var result = _inventory.AddItemToPocket(item);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(_inventory.Values.All(i => i.ItemInstance.Amount <= 999));
            Assert.AreEqual(1500, _inventory.CountItem(1012));
        }

        [TestMethod]
        public void AnAmountNeedingMoreSlotsThanAreFreeIsNotAdded()
        {
            Add(1013, 999);
            Add(1013, 999);
            var item = Entry(1012, 1500);

            var result = _inventory.AddItemToPocket(item);

            Assert.AreEqual(0, result?.Count);
            Assert.AreEqual(0, _inventory.CountItem(1012));
            Assert.AreEqual(1500, item.ItemInstance.Amount);
        }

        [TestMethod]
        public void ConsumingAndAddingPutsTheSourceBackWhenTheItemsDoNotFit()
        {
            var source = Add(1012, 1);
            Add(1013, 999);
            Add(1013, 999);

            var result = _inventory.ConsumeAndAdd(source, 1, [Entry(1014, 1), Entry(1015, 1)]);

            Assert.AreEqual(0, result?.Count);
            Assert.AreSame(source, _inventory.LoadBySlotAndType(0, NoscorePocketType.Main));
            Assert.AreEqual(1, source.ItemInstance.Amount);
            Assert.AreEqual(0, _inventory.CountItem(1014));
        }

        [TestMethod]
        public void ConsumingAndAddingGivesTheItemsForTheSource()
        {
            var source = Add(1012, 1);
            Add(1013, 999);
            Add(1013, 999);

            var result = _inventory.ConsumeAndAdd(source, 1, [Entry(1014, 1)]);

            Assert.AreEqual(1, result?.Count);
            Assert.IsFalse(_inventory.ContainsKey(source.ItemInstanceId));
            Assert.AreEqual(1, _inventory.CountItem(1014));
        }

        [TestMethod]
        public void ConsumingWhatIsGoneAddsNothing()
        {
            var source = Add(1012, 1);
            _inventory.DeleteById(source.ItemInstanceId);

            Assert.IsNull(_inventory.ConsumeAndAdd(source, 1, [Entry(1014, 1)]));
            Assert.AreEqual(0, _inventory.CountItem(1014));
        }

        [TestMethod]
        public void RestoringIntoAFullBagReportsNowhere()
        {
            var source = Add(1012, 1);
            Assert.IsTrue(_inventory.TryConsume(source, 1));
            Add(1013, 999);
            Add(1013, 999);
            Add(1013, 999);

            Assert.AreEqual(0, _inventory.RestoreConsumed(source, 1).Count);
            Assert.IsFalse(_inventory.ContainsKey(source.ItemInstanceId));
        }

        private void WhenReading(Func<bool> trigger, Action otherWrite) =>
            _hook = () =>
            {
                if (!trigger())
                {
                    return false;
                }

                _otherWriter = new Thread(() => otherWrite()) { IsBackground = true };
                _otherWriter.Start();
                var waited = Stopwatch.StartNew();
                while (_otherWriter.IsAlive && (_otherWriter.ThreadState & System.Threading.ThreadState.WaitSleepJoin) == 0
                    && waited.Elapsed < TimeSpan.FromSeconds(5))
                {
                    Thread.Yield();
                }

                return true;
            };

        private void RunHook()
        {
            if (Thread.CurrentThread == _testThread && _hook is { } hook && hook())
            {
                _hook = null;
            }
        }

        private InventoryItemInstance Entry(short vnum, short amount) =>
            InventoryItemInstance.Create(_itemProvider.Create(vnum, amount), 0);

        private InventoryItemInstance Add(short vnum, short amount)
        {
            var entry = Entry(vnum, amount);
            _inventory.AddItemToPocket(entry);
            return entry;
        }

        private InventoryItemInstance HookedEntry(Func<short> get, Action<short> set)
        {
            var item = new Mock<IItemInstance>();
            item.SetupGet(i => i.Id).Returns(Guid.NewGuid());
            item.SetupGet(i => i.ItemVNum).Returns(1012);
            item.SetupGet(i => i.Amount).Returns(() =>
            {
                var amount = get();
                RunHook();
                return amount;
            });
            item.SetupSet(i => i.Amount = It.IsAny<short>()).Callback<short>(set);
            var entry = new InventoryItemInstance(item.Object) { Id = Guid.NewGuid(), Type = NoscorePocketType.Main };
            _inventory[entry.ItemInstanceId] = entry;
            return entry;
        }
    }
}
