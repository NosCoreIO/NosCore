//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

namespace NosCore.GameObject.Tests.Services.ExchangeService
{
    [TestClass]
    public class ExchangeServiceTests
    {
        private TestWorld World;
        private GameObject.Services.ExchangeService.ExchangeService ExchangeProvider;
        private GameObject.Services.ItemGenerationService.ItemGenerationService ItemProvider;
        private IOptions<WorldConfiguration> WorldConfiguration;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            WorldConfiguration = Options.Create(new WorldConfiguration
            {
                Database = new(),
                MasterCommunication = new(),
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

            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());
            ExchangeProvider = CreateExchangeService();
            _sessionA = await World.CreateSessionAsync();
            _sessionB = await World.CreateSessionAsync();
            _sessionC = await World.CreateSessionAsync();
        }

        private GameObject.Services.ExchangeService.ExchangeService CreateExchangeService() =>
            new(WorldConfiguration, NullLogger<NosCore.GameObject.Services.ExchangeService.ExchangeService>.Instance,
                World.LogLanguageLocalizer, World.SessionRegistry,
                World.Clock);

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
        public async Task ValidateExchangeFailsWhenSessionsGoldWouldExceedCap()
        {
            await new Spec("Validate returns Failure + MaxGoldReached to the session when the target's gold would push it past MaxGoldAmount")
                .GivenAsync(SessionAndTargetAreSetUp)
                .And(SessionCharacterGoldIs_, 900_000_000L)
                .And(TargetCharacterGoldIs_, 500_000_000L)
                .And(ExchangeOfferTargetsGoldIs_, 500_000_000L)
                .WhenAsync(ValidatingExchange)
                .Then(ResultTypeShouldBe_, ExchangeResultType.Failure)
                .And(DictionaryShouldContainPacketFor_WithMessage_, false, Game18NConstString.MaxGoldReached)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateExchangeFailsWhenTargetsGoldWouldExceedCap()
        {
            await new Spec("Validate returns Failure + MaxGoldReached to the target when the session's gold would push it past MaxGoldAmount")
                .GivenAsync(SessionAndTargetAreSetUp)
                .And(SessionCharacterGoldIs_, 900_000_000L)
                .And(TargetCharacterGoldIs_, 500_000_000L)
                .And(ExchangeOfferSessionsGoldIs_, 600_000_000L)
                .WhenAsync(ValidatingExchange)
                .Then(ResultTypeShouldBe_, ExchangeResultType.Failure)
                .And(DictionaryShouldContainPacketFor_WithMessage_, true, Game18NConstString.MaxGoldReached)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidateExchangeFailsWhenBothGoldWouldExceedCap()
        {
            await new Spec("Validate returns Failure when both sides' gold would pass MaxGoldAmount")
                .GivenAsync(SessionAndTargetAreSetUp)
                .And(SessionCharacterGoldIs_, 900_000_000L)
                .And(TargetCharacterGoldIs_, 500_000_000L)
                .And(BothOfferGold_, 600_000_000L, 500_000_000L)
                .WhenAsync(ValidatingExchange)
                .Then(ResultTypeShouldBe_, ExchangeResultType.Failure)
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

        private ClientSession _sessionA;
        private ClientSession _sessionB;
        private ClientSession _sessionC;
        private Tuple<ExchangeResultType, Dictionary<long, IPacket>?>? _validationResult;
        private GameObject.Services.ExchangeService.ExchangeService _realExchange;

        private Task SessionAndTargetAreSetUp()
        {
            _realExchange = CreateExchangeService();
            _realExchange.OpenExchange(_sessionA.Character, _sessionB.Character);
            return Task.CompletedTask;
        }

        private void SessionCharacterGoldIs_(long gold) => _sessionA.Character.Gold = gold;

        private void TargetCharacterGoldIs_(long gold) => _sessionB.Character.Gold = gold;

        private void ExchangeOfferTargetsGoldIs_(long gold) =>
            _realExchange.SetOffer(_sessionB.Character, gold, 0, NoItems);

        private void ExchangeOfferSessionsGoldIs_(long gold) =>
            _realExchange.SetOffer(_sessionA.Character, gold, 0, NoItems);

        private void BothOfferGold_(long sessionGold, long targetGold)
        {
            ExchangeOfferSessionsGoldIs_(sessionGold);
            ExchangeOfferTargetsGoldIs_(targetGold);
        }

        private void SessionBankGoldIs_(long bankGold) => _sessionA.Character.BankGold = bankGold;

        private void TargetBankGoldIs_(long bankGold) => _sessionB.Character.BankGold = bankGold;

        private void ExchangeOfferSessionsBankGoldIs_(long bankGold) =>
            _realExchange.SetOffer(_sessionA.Character, 0, bankGold, NoItems);

        private void ExchangeOfferTargetsBankGoldIs_(long bankGold) =>
            _realExchange.SetOffer(_sessionB.Character, 0, bankGold, NoItems);

        private void SessionOffersANonTradableItem()
        {
            _sessionA.Character.Gold = 0;
            _sessionB.Character.Gold = 0;
            var item = new InventoryItemInstance(new GameObject.Services.ItemGenerationService.Item.ItemInstance(
                new Item { VNum = 999, Type = NoscorePocketType.Main, IsTradable = false })
            { Amount = 1 });
            _realExchange.SetOffer(_sessionA.Character, 0, 0, new Dictionary<InventoryItemInstance, short> { [item] = 1 });
        }

        private Task ValidatingExchange()
        {
            _validationResult = _realExchange.ValidateExchange(_sessionA, _sessionB.Character,
                _sessionA.Character.Exchange.NotNull(), _sessionB.Character.Exchange.NotNull());
            return Task.CompletedTask;
        }

        private void ResultTypeShouldBe_(ExchangeResultType expected) =>
            Assert.AreEqual(expected, _validationResult.NotNull().Item1);

        private void DictionaryShouldBeNull() => Assert.IsNull(_validationResult.NotNull().Item2);

        private void DictionaryShouldContainPacketFor_WithMessage_(bool forTarget, Game18NConstString expected)
        {
            var id = forTarget ? _sessionB.Character.VisualId : _sessionA.Character.CharacterId;
            var dict = _validationResult.NotNull().Item2;
            Assert.IsNotNull(dict);
            Assert.IsTrue(dict.ContainsKey(id), $"Expected packet for id={id}");
            var packet = dict[id];
            var infoi = packet as InfoiPacket;
            Assert.IsNotNull(infoi);
            Assert.AreEqual(expected, infoi.Message);
        }

        private void ExchangeIsOpen()
        {
            ExchangeProvider.OpenExchange(_sessionA.Character, _sessionB.Character);
        }

        private void SettingGoldForBothParties()
        {
            ExchangeProvider.SetOffer(_sessionA.Character, 1000, 1000, NoItems);
            ExchangeProvider.SetOffer(_sessionB.Character, 2000, 2000, NoItems);
        }

        private void BothPartiesShouldHaveCorrectGold()
        {
            var data1 = _sessionA.Character.Exchange.NotNull();
            var data2 = _sessionB.Character.Exchange.NotNull();
            Assert.IsTrue((data1.Gold == 1000) && (data1.BankGold == 1000) && (data2.Gold == 2000) &&
                (data2.BankGold == 2000));
        }

        private void BothPartiesConfirm()
        {
            ExchangeProvider.ConfirmExchange(_sessionA.Character);
            ExchangeProvider.ConfirmExchange(_sessionB.Character);
        }

        private void BothShouldBeConfirmed()
        {
            var data1 = _sessionA.Character.Exchange.NotNull();
            var data2 = _sessionB.Character.Exchange.NotNull();
            Assert.IsTrue(data1.ExchangeConfirmed && data2.ExchangeConfirmed);
        }

        private void AddingItemToExchange()
        {
            var item = new InventoryItemInstance(new ItemInstance(new Item { VNum = 1012 })
            {
                Amount = 1
            });
            ExchangeProvider.SetOffer(_sessionA.Character, 0, 0, new Dictionary<InventoryItemInstance, short> { [item] = item.ItemInstance.Amount });
        }

        private void ExchangeShouldContainItem()
        {
            var data1 = _sessionA.Character.Exchange.NotNull();
            Assert.IsTrue(data1.ExchangeItems.Any(s =>
                (s.Key.ItemInstance?.ItemVNum == 1012) && (s.Key.ItemInstance.Amount == 1)));
        }

        private void CheckBeforeOpenShouldBeFalse()
        {
            var wrongExchange = _sessionA.Character.InExchange;
            Assert.IsFalse(wrongExchange);
        }

        private void CheckAfterOpenShouldBeTrue()
        {
            ExchangeProvider.OpenExchange(_sessionA.Character, _sessionB.Character);
            var goodExchange = _sessionA.Character.InExchange;
            Assert.IsTrue(goodExchange);
        }

        private void CloseWithoutExchangeShouldReturnNull()
        {
            var wrongClose = ExchangeProvider.CloseExchange(_sessionA.Character, ExchangeResultType.Failure);
            Assert.IsNull(wrongClose);
        }

        private void CloseWithExchangeShouldReturnResult()
        {
            ExchangeProvider.OpenExchange(_sessionA.Character, _sessionB.Character);
            var goodClose = ExchangeProvider.CloseExchange(_sessionA.Character, ExchangeResultType.Failure);
            Assert.IsTrue((goodClose != null) && (goodClose.Type == ExchangeResultType.Failure));
            Assert.IsFalse(_sessionA.Character.InExchange);
            Assert.IsFalse(_sessionB.Character.InExchange);
        }

        private void FirstExchangeShouldSucceed()
        {
            var exchange = ExchangeProvider.OpenExchange(_sessionA.Character, _sessionB.Character);
            Assert.IsTrue(exchange);
        }

        private void SecondExchangeShouldFail()
        {
            var wrongExchange = ExchangeProvider.OpenExchange(_sessionA.Character, _sessionC.Character);
            Assert.IsFalse(wrongExchange);
        }

    }
}
