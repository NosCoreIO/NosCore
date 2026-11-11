//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.TransferService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DbEquipmentOption = NosCore.Database.Entities.EquipmentOption;
using DbInventoryItemInstance = NosCore.Database.Entities.InventoryItemInstance;
using DbItemInstance = NosCore.Database.Entities.ItemInstance;

namespace NosCore.GameObject.Tests.Services.TransferService
{
    [TestClass]
    public class TransferServiceTests
    {
        private static readonly Item Stackable = new() { Type = NoscorePocketType.Main, VNum = 1012, IsTradable = true };
        private static readonly Item Filler = new() { Type = NoscorePocketType.Main, VNum = 1013, IsTradable = true };
        private static readonly Item Other = new() { Type = NoscorePocketType.Main, VNum = 1014, IsTradable = true };
        private static readonly Item Armor = new()
        {
            Type = NoscorePocketType.Equipment, VNum = 1, IsTradable = true, ItemType = ItemType.Armor,
            EquipmentSlot = EquipmentType.Armor
        };

        private readonly IOptions<WorldConfiguration> _configuration = Options.Create(new WorldConfiguration
        {
            BackpackSize = 2,
            MaxItemAmount = 999,
            MaxGoldAmount = 1_000_000_000,
            MaxBankGoldAmount = 100_000_000_000
        });

        private ClientSession _first;
        private ClientSession _second;
        private IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private IDao<InventoryItemInstanceDto, Guid> _inventoryDao;
        private IDao<EquipmentOptionDto, Guid> _optionDao;
        private GameObject.Services.TransferService.TransferService _service;
        private TransferOffer _firstOffer = Nothing;
        private TransferOffer _secondOffer = Nothing;
        private List<KeyValuePair<long, IvnPacket>>? _result;
        private Mock<IDaoTransaction> _transaction;

        private InventoryItemInstance _firstStack;
        private InventoryItemInstance _secondStack;
        private InventoryItemInstance _secondOther;
        private WearableInstance _armor;
        private InventoryItemInstance _armorEntry;
        private Snapshot _before;

        private static TransferOffer Nothing => new(0, 0, new Dictionary<InventoryItemInstance, short>());

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _first = await TestHelpers.Instance.GenerateSessionAsync();
            _second = await TestHelpers.Instance.GenerateSessionAsync();
            foreach (var session in new[] { _first, _second })
            {
                session.Character.Gold = 0;
                session.Character.BankGold = 0;
                session.Character.InventoryService.Clear();
            }

            var context = TestHelpers.Instance.DbContextBuilder;
            _itemInstanceDao = new Dao<DbItemInstance, IItemInstanceDto?, Guid>(
                NullLogger<Dao<DbItemInstance, IItemInstanceDto?, Guid>>.Instance, context);
            _inventoryDao = new Dao<DbInventoryItemInstance, InventoryItemInstanceDto, Guid>(
                NullLogger<Dao<DbInventoryItemInstance, InventoryItemInstanceDto, Guid>>.Instance, context);
            _optionDao = new Dao<DbEquipmentOption, EquipmentOptionDto, Guid>(
                NullLogger<Dao<DbEquipmentOption, EquipmentOptionDto, Guid>>.Instance, context);
            _service = Create(new DaoTransactionScope(context), _inventoryDao);
        }

        private GameObject.Services.TransferService.TransferService Create(IDaoTransactionScope scope,
            IDao<InventoryItemInstanceDto, Guid> inventoryDao) =>
            new(_configuration, NullLogger<GameObject.Services.TransferService.TransferService>.Instance,
                TestHelpers.Instance.LogLanguageLocalizer, scope, TestHelpers.Instance.CharacterDao,
                TestHelpers.Instance.AccountDao, _itemInstanceDao, inventoryDao, _optionDao);

        [TestMethod]
        public async Task ATransferPersistsBothCharactersTogether()
        {
            await new Spec("A transfer writes both characters' gold, bank and items")
                .GivenAsync(FirstOffersGoldAndPartOfAStack)
                .AndAsync(SecondOffersBankGoldAndAnArmorWithOptions)
                .WhenAsync(Transferring)
                .Then(TheTransferSucceeds)
                .AndAsync(BothCharactersAreStoredWithTheirNewBalances)
                .AndAsync(TheItemsAreStoredOnTheirNewOwners)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedWriteLeavesBothCharactersUnchanged()
        {
            await new Spec("A write that fails rolls back the transaction and both characters in memory")
                .GivenAsync(FirstOffersGoldAndPartOfAStack)
                .AndAsync(SecondOffersBankGoldAndAnArmorWithOptions)
                .And(TheInventoryWriteFails)
                .WhenAsync(Transferring)
                .Then(TheTransferFails)
                .And(NothingIsCommitted)
                .And(BothCharactersAreAsBefore)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATransferThatDoesNotFitLeavesEveryStackAsItWas()
        {
            await new Spec("A transfer the receiver has no room for restores every stack it touched")
                .Given(FirstOffersPartOfAStackSecondAlreadyHolds)
                .And(SecondOffersAnItemFirstHasNoRoomFor)
                .WhenAsync(Transferring)
                .Then(TheTransferFails)
                .And(BothCharactersAreAsBefore)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnOfferNoLongerCoveredIsRefused()
        {
            await new Spec("An offer the giver no longer covers moves nothing")
                .GivenAsync(FirstOffersGoldAndPartOfAStack)
                .And(FirstSpendsTheirGold)
                .WhenAsync(Transferring)
                .Then(TheTransferFails)
                .And(BothCharactersAreAsBefore)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATransferredItemKeepsItsInstanceData()
        {
            await new Spec("A transferred item arrives with its instance data under a new id")
                .GivenAsync(SecondOffersBankGoldAndAnArmorWithOptions)
                .WhenAsync(Transferring)
                .Then(TheTransferSucceeds)
                .And(TheArmorKeepsItsData)
                .ExecuteAsync();
        }

        private static InventoryItemInstance Stack(Item item, short amount, ClientSession owner) =>
            InventoryItemInstance.Create(new ItemInstance(item) { Amount = amount }, owner.Character.CharacterId);

        private Task FirstOffersGoldAndPartOfAStack()
        {
            _first.Character.Gold = 1_000;
            _firstStack = _first.Character.InventoryService.AddItemToPocket(Stack(Stackable, 10, _first), NoscorePocketType.Main, 0).NotNull().First();
            _firstOffer = new TransferOffer(600, 0, new Dictionary<InventoryItemInstance, short> { [_firstStack] = 4 });
            _before = new Snapshot(_first, _second);
            return Task.CompletedTask;
        }

        private async Task SecondOffersBankGoldAndAnArmorWithOptions()
        {
            _second.Character.BankGold = 5_000;
            _armor = new WearableInstance(Armor, NullLogger<WearableInstance>.Instance, TestHelpers.Instance.LogLanguageLocalizer)
            {
                Amount = 1,
                Rare = 5,
                Upgrade = 7,
                IsFixed = true,
                PityGauge = 12,
                ShellRarity = 3,
                DurabilityPoint = 40
            };
            _armorEntry = _second.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_armor, _second.Character.CharacterId)).NotNull().First();
            await _itemInstanceDao.TryInsertOrUpdateAsync(_armor);
            await _inventoryDao.TryInsertOrUpdateAsync(_armorEntry);
            await _optionDao.TryInsertOrUpdateAsync(new EquipmentOptionDto
            {
                Id = Guid.NewGuid(), WearableInstanceId = _armor.Id, Level = 1, Type = 2, Value = 3
            });
            _secondOffer = new TransferOffer(0, 5_000, new Dictionary<InventoryItemInstance, short> { [_armorEntry] = 1 });
            _before = new Snapshot(_first, _second);
        }

        private void FirstOffersPartOfAStackSecondAlreadyHolds()
        {
            _firstStack = _first.Character.InventoryService.AddItemToPocket(Stack(Stackable, 5, _first), NoscorePocketType.Main, 0).NotNull().First();
            _first.Character.InventoryService.AddItemToPocket(Stack(Filler, 999, _first), NoscorePocketType.Main, 1);
            _secondStack = _second.Character.InventoryService.AddItemToPocket(Stack(Stackable, 10, _second), NoscorePocketType.Main, 0).NotNull().First();
            _firstOffer = new TransferOffer(0, 0, new Dictionary<InventoryItemInstance, short> { [_firstStack] = 3 });
        }

        private void SecondOffersAnItemFirstHasNoRoomFor()
        {
            _secondOther = _second.Character.InventoryService.AddItemToPocket(Stack(Other, 1, _second), NoscorePocketType.Main, 1).NotNull().First();
            _secondOffer = new TransferOffer(0, 0, new Dictionary<InventoryItemInstance, short> { [_secondOther] = 1 });
            _before = new Snapshot(_first, _second);
        }

        private void FirstSpendsTheirGold()
        {
            _first.Character.Gold = 100;
            _before = new Snapshot(_first, _second);
        }

        private void TheInventoryWriteFails()
        {
            _transaction = new Mock<IDaoTransaction>();
            var scope = new Mock<IDaoTransactionScope>();
            scope.Setup(s => s.Begin()).Returns(_transaction.Object);
            var inventoryDao = new Mock<IDao<InventoryItemInstanceDto, Guid>>();
            inventoryDao.Setup(d => d.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<InventoryItemInstanceDto>>()))
                .ReturnsAsync(false);
            _service = Create(scope.Object, inventoryDao.Object);
        }

        private async Task Transferring() =>
            _result = await _service.TransferAsync(_first.Character, _firstOffer, _second.Character, _secondOffer);

        private void TheTransferSucceeds() => Assert.IsNotNull(_result);

        private void TheTransferFails() => Assert.IsNull(_result);

        private void NothingIsCommitted()
        {
            _transaction.Verify(t => t.CommitAsync(), Times.Never);
            _transaction.Verify(t => t.DisposeAsync(), Times.Once);
        }

        private async Task BothCharactersAreStoredWithTheirNewBalances()
        {
            var firstId = _first.Character.CharacterId;
            var secondId = _second.Character.CharacterId;
            var firstAccountId = _first.Character.Account.AccountId;
            var secondAccountId = _second.Character.Account.AccountId;
            var first = (await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(c => c.CharacterId == firstId)).NotNull();
            var second = (await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(c => c.CharacterId == secondId)).NotNull();
            Assert.AreEqual(400L, first.Gold);
            Assert.AreEqual(600L, second.Gold);
            var firstAccount = (await TestHelpers.Instance.AccountDao.FirstOrDefaultAsync(a => a.AccountId == firstAccountId)).NotNull();
            var secondAccount = (await TestHelpers.Instance.AccountDao.FirstOrDefaultAsync(a => a.AccountId == secondAccountId)).NotNull();
            Assert.AreEqual(5_000L, firstAccount.BankMoney);
            Assert.AreEqual(0L, secondAccount.BankMoney);
        }

        private async Task TheItemsAreStoredOnTheirNewOwners()
        {
            var firstId = _first.Character.CharacterId;
            var secondId = _second.Character.CharacterId;
            var firstRows = _inventoryDao.Where(i => i.CharacterId == firstId).NotNull().ToList();
            var secondRows = _inventoryDao.Where(i => i.CharacterId == secondId).NotNull().ToList();
            Assert.AreEqual(2, firstRows.Count, "the rest of the stack and the armor");
            Assert.AreEqual(1, secondRows.Count, "the received part of the stack");

            var stackId = _firstStack.ItemInstanceId;
            var armorId = _armor.Id;
            var remaining = (await _itemInstanceDao.FirstOrDefaultAsync(i => i.Id == stackId)).NotNull();
            Assert.AreEqual((short)6, remaining.Amount);

            Assert.IsNull(await _itemInstanceDao.FirstOrDefaultAsync(i => i.Id == armorId), "the given armor row is gone");
            var receivedArmor = _first.Character.InventoryService.Values.Single(i => i.ItemInstance.ItemVNum == Armor.VNum);
            var receivedId = receivedArmor.ItemInstanceId;
            Assert.IsNotNull(await _itemInstanceDao.FirstOrDefaultAsync(i => i.Id == receivedId));
            var options = _optionDao.Where(o => o.WearableInstanceId == receivedId).NotNull().ToList();
            Assert.AreEqual(1, options.Count);
            Assert.AreEqual(3, options[0].Value);
        }

        private void TheArmorKeepsItsData()
        {
            var received = (WearableInstance)_first.Character.InventoryService.Values.Single().ItemInstance;
            Assert.AreNotSame(_armor, received);
            Assert.AreNotEqual(_armor.Id, received.Id);
            Assert.AreEqual(_armor.Rare, received.Rare);
            Assert.AreEqual(_armor.Upgrade, received.Upgrade);
            Assert.AreEqual(_armor.IsFixed, received.IsFixed);
            Assert.AreEqual(_armor.PityGauge, received.PityGauge);
            Assert.AreEqual(_armor.ShellRarity, received.ShellRarity);
            Assert.AreEqual(_armor.DurabilityPoint, received.DurabilityPoint);
            Assert.AreEqual(0, _second.Character.InventoryService.Count);
        }

        private void BothCharactersAreAsBefore() => _before.AssertUnchanged(_first, _second);

        private sealed class Snapshot
        {
            private readonly List<(long Gold, long BankGold, List<(Guid Id, short Vnum, short Amount, short Slot)> Items)> _state;

            public Snapshot(params ClientSession[] sessions) => _state = sessions.Select(Capture).ToList();

            private static (long, long, List<(Guid, short, short, short)>) Capture(ClientSession session) =>
                (session.Character.Gold, session.Character.BankGold,
                    session.Character.InventoryService.Values
                        .Select(i => (i.ItemInstanceId, i.ItemInstance.ItemVNum, i.ItemInstance.Amount, i.Slot))
                        .OrderBy(i => i.ItemInstanceId).ToList());

            public void AssertUnchanged(params ClientSession[] sessions)
            {
                for (var i = 0; i < sessions.Length; i++)
                {
                    var now = Capture(sessions[i]);
                    Assert.AreEqual(_state[i].Gold, now.Item1);
                    Assert.AreEqual(_state[i].BankGold, now.Item2);
                    CollectionAssert.AreEqual(_state[i].Items, now.Item3);
                }
            }
        }
    }
}
