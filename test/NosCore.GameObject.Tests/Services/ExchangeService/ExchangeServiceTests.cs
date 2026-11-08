//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.Data.Dto;
using NosCore.Dao.Interfaces;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExchangeRequestRegistry = NosCore.GameObject.Services.ExchangeService.ExchangeRequestRegistry;

namespace NosCore.GameObject.Tests.Services.ExchangeService
{
    [TestClass]
    public class ExchangeServiceTests
    {
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

            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items, NullLoggerFactory.Instance, TestHelpers.Instance.LogLanguageLocalizer);
            ShellOptions = new Mock<IShellOptionService>();
            ShellOptions.Setup(s => s.Load(It.IsAny<Guid>())).Returns(new List<EquipmentOptionDto>());
            ItemInstanceDao = new Mock<IDao<IItemInstanceDto?, Guid>>();
            ExchangeProvider = CreateExchangeService();
        }

        private Mock<IShellOptionService> ShellOptions = null!;
        private Mock<IDao<IItemInstanceDto?, Guid>> ItemInstanceDao = null!;

        private GameObject.Services.ExchangeService.ExchangeService CreateExchangeService() =>
            new(WorldConfiguration!, NullLogger<NosCore.GameObject.Services.ExchangeService.ExchangeService>.Instance,
                new ExchangeRequestRegistry(), TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.SessionRegistry,
                TestHelpers.Instance.Clock, ShellOptions.Object, ItemInstanceDao.Object);

        private static Dictionary<InventoryItemInstance, short> NoItems => new();

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
        public async Task ProcessingExchangeShouldSwapItems()
        {
            await new Spec("Processing exchange should swap items")
                .ThenAsync(ProcessExchangeShouldReturnItemsForBoth)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateExchangeFailsWhenTargetsGoldWouldExceedCap()
        {
            await new Spec("Validate returns Failure + MaxGoldReached when target's gold + session's gold exceed MaxGoldAmount")
                .GivenAsync(SessionAndTargetAreSetUp)
                .And(SessionCharacterGoldIs_, 900_000_000L)
                .And(TargetCharacterGoldIs_, 500_000_000L)
                .And(ExchangeOfferTargetsGoldIs_, 500_000_000L)
                .WhenAsync(ValidatingExchange)
                .Then(ResultTypeShouldBe_, ExchangeResultType.Failure)
                .And(DictionaryShouldContainPacketFor_WithMessage_, true, Game18NConstString.MaxGoldReached)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateExchangeFailsWhenTargetsBankWouldExceedCap()
        {
            await new Spec("Validate returns Failure + CanNotDepositAnymore to the target when the offered bank gold would overflow the target's bank")
                .GivenAsync(SessionAndTargetAreSetUp)
                .And(TargetBankGoldIs_, 99_000_000_000L)
                .And(SessionBankGoldIs_, 2_000_000_000L)
                .And(ExchangeOfferSessionsBankGoldIs_, 2_000_000_000L)
                .WhenAsync(ValidatingExchange)
                .Then(ResultTypeShouldBe_, ExchangeResultType.Failure)
                .And(DictionaryShouldContainPacketFor_WithMessage_, true, Game18NConstString.CanNotDepositAnymore)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateExchangeFailsWhenSessionsBankWouldExceedCap()
        {
            await new Spec("Validate returns Failure + CanNotDepositAnymore to the session when the target's bank gold would overflow the session's bank")
                .GivenAsync(SessionAndTargetAreSetUp)
                .And(SessionBankGoldIs_, 99_000_000_000L)
                .And(TargetBankGoldIs_, 2_000_000_000L)
                .And(ExchangeOfferTargetsBankGoldIs_, 2_000_000_000L)
                .WhenAsync(ValidatingExchange)
                .Then(ResultTypeShouldBe_, ExchangeResultType.Failure)
                .And(DictionaryShouldContainPacketFor_WithMessage_, false, Game18NConstString.CanNotDepositAnymore)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateExchangeFailsWhenNonTradableItemIsOffered()
        {
            await new Spec("Validate returns Failure + ItemCanNotBeSold when the exchange offer contains a non-tradable item")
                .GivenAsync(SessionAndTargetAreSetUp)
                .And(SessionOffersANonTradableItem)
                .WhenAsync(ValidatingExchange)
                .Then(ResultTypeShouldBe_, ExchangeResultType.Failure)
                .And(DictionaryShouldContainPacketFor_WithMessage_, false, Game18NConstString.ItemCanNotBeSold)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateExchangeSucceedsWhenBothSidesAreWithinCaps()
        {
            await new Spec("Validate returns Success with a null packet dict when both sides stay within all caps and fit inventory")
                .GivenAsync(SessionAndTargetAreSetUp)
                .And(SessionCharacterGoldIs_, 1_000L)
                .And(TargetCharacterGoldIs_, 1_000L)
                .WhenAsync(ValidatingExchange)
                .Then(ResultTypeShouldBe_, ExchangeResultType.Success)
                .And(DictionaryShouldBeNull)
                .ExecuteAsync();
        }

        private ClientSession? _sessionA;
        private ClientSession? _sessionB;
        private Tuple<ExchangeResultType, Dictionary<long, IPacket>?>? _validationResult;
        private GameObject.Services.ExchangeService.ExchangeService? _realExchange;

        private async Task SessionAndTargetAreSetUp()
        {
            await TestHelpers.ResetAsync();
            _sessionA = await TestHelpers.Instance.GenerateSessionAsync();
            _sessionB = await TestHelpers.Instance.GenerateSessionAsync();
            _realExchange = CreateExchangeService();
            _realExchange.OpenExchange(_sessionA.Character.CharacterId, _sessionB.Character.VisualId);
        }

        private void SessionCharacterGoldIs_(long gold) => _sessionA!.Character.Gold = gold;

        private void TargetCharacterGoldIs_(long gold) => _sessionB!.Character.Gold = gold;

        private void ExchangeOfferTargetsGoldIs_(long gold) =>
            _realExchange!.SetOffer(_sessionB!.Character.VisualId, gold, 0, NoItems);

        private void SessionBankGoldIs_(long bankGold) => _sessionA!.Character.BankGold = bankGold;

        private void TargetBankGoldIs_(long bankGold) => _sessionB!.Character.BankGold = bankGold;

        private void ExchangeOfferSessionsBankGoldIs_(long bankGold) =>
            _realExchange!.SetOffer(_sessionA!.Character.CharacterId, 0, bankGold, NoItems);

        private void ExchangeOfferTargetsBankGoldIs_(long bankGold) =>
            _realExchange!.SetOffer(_sessionB!.Character.VisualId, 0, bankGold, NoItems);

        private void SessionOffersANonTradableItem()
        {
            _sessionA!.Character.Gold = 0;
            _sessionB!.Character.Gold = 0;
            var item = new InventoryItemInstance(new GameObject.Services.ItemGenerationService.Item.ItemInstance(
                new Item { VNum = 999, Type = NoscorePocketType.Main, IsTradable = false })
            { Amount = 1 });
            _realExchange!.SetOffer(_sessionA.Character.CharacterId, 0, 0, new Dictionary<InventoryItemInstance, short> { [item] = 1 });
        }

        private Task ValidatingExchange()
        {
            _validationResult = _realExchange!.ValidateExchange(_sessionA!, _sessionB!.Character);
            return Task.CompletedTask;
        }

        private void ResultTypeShouldBe_(ExchangeResultType expected) =>
            Assert.AreEqual(expected, _validationResult!.Item1);

        private void DictionaryShouldBeNull() => Assert.IsNull(_validationResult!.Item2);

        private void DictionaryShouldContainPacketFor_WithMessage_(bool forTarget, Game18NConstString expected)
        {
            var id = forTarget ? _sessionB!.Character.VisualId : _sessionA!.Character.CharacterId;
            var dict = _validationResult!.Item2;
            Assert.IsNotNull(dict);
            Assert.IsTrue(dict.ContainsKey(id), $"Expected packet for id={id}");
            var packet = dict[id];
            var infoi = packet as InfoiPacket;
            Assert.IsNotNull(infoi);
            Assert.AreEqual(expected, infoi.Message);
        }

        private void ExchangeIsOpen()
        {
            ExchangeProvider!.OpenExchange(1, 2);
        }

        private void SettingGoldForBothParties()
        {
            ExchangeProvider!.SetOffer(1, 1000, 1000, NoItems);
            ExchangeProvider.SetOffer(2, 2000, 2000, NoItems);
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
            ExchangeProvider!.SetOffer(1, 0, 0, new Dictionary<InventoryItemInstance, short> { [item] = item.ItemInstance.Amount });
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

        private async Task ProcessExchangeShouldReturnItemsForBoth()
        {
            IInventoryService inventory1 =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1012, Type = NoscorePocketType.Main } },
                    WorldConfiguration!, NullLogger<NosCore.GameObject.Services.InventoryService.InventoryService>.Instance);
            IInventoryService inventory2 =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1013, Type = NoscorePocketType.Main } },
                    WorldConfiguration!, NullLogger<NosCore.GameObject.Services.InventoryService.InventoryService>.Instance);
            var item1 = inventory1.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 1), 0))!
                .First();
            var item2 = inventory2.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1013, 1), 0))!
                .First();

            ExchangeProvider!.OpenExchange(1, 2);
            ExchangeProvider.SetOffer(1, 0, 0, new Dictionary<InventoryItemInstance, short> { [item1] = 1 });
            ExchangeProvider.SetOffer(2, 0, 0, new Dictionary<InventoryItemInstance, short> { [item2] = 1 });
            var itemList = await ExchangeProvider.ProcessExchangeAsync(1, 2, inventory1, inventory2);
            Assert.IsNotNull(itemList);
            Assert.IsTrue((itemList.Count(s => s.Key == 1) == 2) && (itemList.Count(s => s.Key == 2) == 2));
        }

        [TestMethod]
        public async Task AnOfferedItemThatLeftTheInventoryDoesNotReachTheOtherSide()
        {
            IInventoryService giver =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1012, Type = NoscorePocketType.Main } },
                    WorldConfiguration!, NullLogger<NosCore.GameObject.Services.InventoryService.InventoryService>.Instance);
            IInventoryService receiver =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1012, Type = NoscorePocketType.Main } },
                    WorldConfiguration!, NullLogger<NosCore.GameObject.Services.InventoryService.InventoryService>.Instance);

            var offered = giver.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 1), 0))!.First();

            ExchangeProvider!.OpenExchange(1, 2);
            ExchangeProvider.SetOffer(1, 0, 0, new Dictionary<InventoryItemInstance, short> { [offered] = 1 });

            // The destination item is built fresh rather than moved, so dropping the offered
            // item after putting it in the window used to hand over a second copy of it.
            giver.Remove(offered.ItemInstanceId);

            var itemList = await ExchangeProvider.ProcessExchangeAsync(1, 2, giver, receiver);

            Assert.IsNull(itemList, "a vanished offer must not transfer");
            Assert.AreEqual(0, receiver.Count, "the receiver must not gain a duplicate");
        }

        [TestMethod]
        public async Task ProcessExchangeNotifiesReceiverWithTheirActualMergedAmount()
        {
            IInventoryService giver =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1012, Type = NoscorePocketType.Main } },
                    WorldConfiguration!, NullLogger<NosCore.GameObject.Services.InventoryService.InventoryService>.Instance);
            IInventoryService receiver =
                new GameObject.Services.InventoryService.InventoryService(new List<ItemDto> { new Item { VNum = 1012, Type = NoscorePocketType.Main } },
                    WorldConfiguration!, NullLogger<NosCore.GameObject.Services.InventoryService.InventoryService>.Instance);

            var offered = giver.AddItemToPocket(InventoryItemInstance.Create(ItemProvider!.Create(1012, 5), 0))!.First();
            receiver.AddItemToPocket(InventoryItemInstance.Create(ItemProvider.Create(1012, 4), 0));

            ExchangeProvider!.OpenExchange(1, 2);
            // Offering less than the full stack (2 of 5) forces the partial-transfer branch,
            // which mutates the giver's own item object in place - this used to leak into the
            // receiver's notification packet.
            ExchangeProvider.SetOffer(1, 0, 0, new Dictionary<InventoryItemInstance, short> { [offered] = 2 });

            var itemList = await ExchangeProvider.ProcessExchangeAsync(1, 2, giver, receiver);

            var receiverPacket = itemList!.First(s => s.Key == 2).Value;
            var receiverAmount = receiverPacket.IvnSubPackets!.First()!.RareAmount;
            Assert.AreEqual((short)6, receiverAmount, "the receiver must be notified of their true merged total (4 + 2), not the giver's remaining balance");
        }

        [TestMethod]
        public async Task AFailedTradeLeavesBothInventoriesAsTheyWere()
        {
            await new Spec("A trade that cannot complete restores every stack it touched")
                .Given(GiverOffersPartOfAStackTheReceiverAlreadyHolds)
                .And(ReceiverOffersAnItemTheGiverHasNoRoomFor)
                .WhenAsync(ProcessingTheTrade)
                .Then(TheTradeFails)
                .And(ReceiverStackIs_, (short)10)
                .And(GiverStackIs_, (short)5)
                .And(ReceiverStillHoldsTheirOffer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATradedItemKeepsItsInstanceData()
        {
            await new Spec("A traded item arrives with its instance data under a new id")
                .Given(GiverOffersAnUpgradedArmor)
                .WhenAsync(ProcessingTheTrade)
                .Then(ReceiverArmorKeepsItsData)
                .And(ReceiverArmorIsADistinctInstance)
                .And(ShellOptionsAreCopiedToTheNewInstance)
                .ExecuteAsync();
        }

        private IInventoryService? _giver;
        private IInventoryService? _receiver;
        private InventoryItemInstance? _giverStack;
        private InventoryItemInstance? _receiverStack;
        private InventoryItemInstance? _receiverOffer;
        private WearableInstance? _armor;
        private List<KeyValuePair<long, IvnPacket>>? _tradeResult;

        private IInventoryService NewInventory(params Item[] items) =>
            new GameObject.Services.InventoryService.InventoryService(items.Cast<ItemDto>().ToList(),
                WorldConfiguration!, NullLogger<NosCore.GameObject.Services.InventoryService.InventoryService>.Instance);

        private static readonly Item Stackable = new() { Type = NoscorePocketType.Main, VNum = 1012, IsTradable = true };
        private static readonly Item Filler = new() { Type = NoscorePocketType.Main, VNum = 1013, IsTradable = true };
        private static readonly Item Other = new() { Type = NoscorePocketType.Main, VNum = 1014, IsTradable = true };
        private static readonly Item Armor = new()
        {
            Type = NoscorePocketType.Equipment, VNum = 1, IsTradable = true,
            ItemType = NosCore.Data.Enumerations.Items.ItemType.Armor,
            EquipmentSlot = NosCore.Packets.Enumerations.EquipmentType.Armor
        };

        private static InventoryItemInstance Stack(Item item, short amount) =>
            new(new ItemInstance(item) { Amount = amount });

        private void GiverOffersPartOfAStackTheReceiverAlreadyHolds()
        {
            _giver = NewInventory(Stackable, Filler, Other);
            _receiver = NewInventory(Stackable, Filler, Other);
            _giverStack = _giver.AddItemToPocket(Stack(Stackable, 5), NoscorePocketType.Main, 0)!.First();
            for (short slot = 1; slot < _giver.GetMaxSlots(NoscorePocketType.Main); slot++)
            {
                _giver.AddItemToPocket(Stack(Filler, 999), NoscorePocketType.Main, slot);
            }

            _receiverStack = _receiver.AddItemToPocket(Stack(Stackable, 10), NoscorePocketType.Main, 0)!.First();
            ExchangeProvider!.OpenExchange(1, 2);
            ExchangeProvider.SetOffer(1, 0, 0, new Dictionary<InventoryItemInstance, short> { [_giverStack] = 3 });
        }

        private void ReceiverOffersAnItemTheGiverHasNoRoomFor()
        {
            _receiverOffer = _receiver!.AddItemToPocket(Stack(Other, 1), NoscorePocketType.Main, 1)!.First();
            ExchangeProvider!.SetOffer(2, 0, 0, new Dictionary<InventoryItemInstance, short> { [_receiverOffer] = 1 });
        }

        private void GiverOffersAnUpgradedArmor()
        {
            _giver = NewInventory(Armor);
            _receiver = NewInventory(Armor);
            _armor = new WearableInstance(Armor, NullLogger<WearableInstance>.Instance, TestHelpers.Instance.LogLanguageLocalizer)
            {
                Amount = 1,
                Rare = 5,
                Upgrade = 7,
                IsFixed = true,
                PityGauge = 12,
                ShellRarity = 3,
                DurabilityPoint = 40,
                BoundCharacterId = 1
            };
            var offered = _giver.AddItemToPocket(new InventoryItemInstance(_armor))!.First();
            ShellOptions.Setup(s => s.Load(_armor.Id)).Returns(new List<EquipmentOptionDto>
            {
                new() { Id = Guid.NewGuid(), WearableInstanceId = _armor.Id, Level = 1, Type = 2, Value = 3 }
            });
            ExchangeProvider!.OpenExchange(1, 2);
            ExchangeProvider.SetOffer(1, 0, 0, new Dictionary<InventoryItemInstance, short> { [offered] = 1 });
        }

        private async Task ProcessingTheTrade() =>
            _tradeResult = await ExchangeProvider!.ProcessExchangeAsync(1, 2, _giver!, _receiver!);

        private void TheTradeFails() => Assert.IsNull(_tradeResult);

        private void ReceiverStackIs_(short amount)
        {
            Assert.IsTrue(_receiver!.TryGetValue(_receiverStack!.ItemInstanceId, out var stack));
            Assert.AreEqual(amount, stack.ItemInstance.Amount);
            Assert.AreEqual(1, _receiver.Values.Count(s => s.ItemInstance.ItemVNum == Stackable.VNum));
        }

        private void GiverStackIs_(short amount)
        {
            Assert.IsTrue(_giver!.TryGetValue(_giverStack!.ItemInstanceId, out var stack));
            Assert.AreEqual(amount, stack.ItemInstance.Amount);
        }

        private void ReceiverStillHoldsTheirOffer() =>
            Assert.IsTrue(_receiver!.ContainsKey(_receiverOffer!.ItemInstanceId));

        private WearableInstance ReceivedArmor() =>
            (WearableInstance)_receiver!.Values.Single().ItemInstance;

        private void ReceiverArmorKeepsItsData()
        {
            Assert.IsNotNull(_tradeResult);
            var received = ReceivedArmor();
            Assert.AreEqual(_armor!.Rare, received.Rare);
            Assert.AreEqual(_armor.Upgrade, received.Upgrade);
            Assert.AreEqual(_armor.IsFixed, received.IsFixed);
            Assert.AreEqual(_armor.PityGauge, received.PityGauge);
            Assert.AreEqual(_armor.ShellRarity, received.ShellRarity);
            Assert.AreEqual(_armor.DurabilityPoint, received.DurabilityPoint);
            Assert.AreEqual(_armor.BoundCharacterId, received.BoundCharacterId);
            Assert.AreEqual(0, _giver!.Count);
        }

        private void ReceiverArmorIsADistinctInstance()
        {
            var received = ReceivedArmor();
            Assert.AreNotSame(_armor, received);
            Assert.AreNotEqual(_armor!.Id, received.Id);
        }

        private void ShellOptionsAreCopiedToTheNewInstance()
        {
            var received = ReceivedArmor();
            ItemInstanceDao.Verify(d => d.TryInsertOrUpdateAsync(It.Is<IItemInstanceDto?>(i => i!.Id == received.Id)), Times.Once);
            ShellOptions.Verify(s => s.SaveAsync(received.Id,
                It.Is<IEnumerable<EquipmentOptionDto>>(o => o.Single().Type == 2 && o.Single().Value == 3)), Times.Once);
        }
    }
}
