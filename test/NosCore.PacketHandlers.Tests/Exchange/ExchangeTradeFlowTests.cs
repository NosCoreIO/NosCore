//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Core.Configuration;
using NosCore.Dao;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.TransferService;
using NosCore.PacketHandlers.Exchange;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DbEquipmentOption = NosCore.Database.Entities.EquipmentOption;
using DbInventoryItemInstance = NosCore.Database.Entities.InventoryItemInstance;
using DbItemInstance = NosCore.Database.Entities.ItemInstance;

namespace NosCore.PacketHandlers.Tests.Exchange
{
    [TestClass]
    public class ExchangeTradeFlowTests
    {
        private ClientSession Requester = null!;
        private ClientSession Target = null!;
        private ExchangeService Exchange = null!;
        private ExchangeRequestPackettHandler RequestHandler = null!;
        private ExcListPacketHandler ListHandler = null!;
        private Mock<IBlacklistHub> Blacklist = null!;
        private FakeClock Clock = null!;
        private ItemGenerationService ItemProvider = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Requester = await TestHelpers.Instance.GenerateSessionAsync();
            Target = await TestHelpers.Instance.GenerateSessionAsync();
            Requester.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Target.Character.MapInstance = Requester.Character.MapInstance;
            Requester.Character.Gold = 0;
            Target.Character.Gold = 0;
            Requester.Character.BankGold = 0;
            Target.Character.BankGold = 0;

            Clock = new FakeClock(Instant.FromUtc(2021, 01, 01, 01, 01, 01));
            Blacklist = new Mock<IBlacklistHub>();
            Blacklist.Setup(x => x.GetBlacklistedAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>()));

            Exchange = new ExchangeService(
                Options.Create(new WorldConfiguration
                {
                    BackpackSize = 48,
                    MaxItemAmount = 999,
                    MaxGoldAmount = 1_000_000_000,
                    MaxBankGoldAmount = 100_000_000_000
                }),
                NullLogger<ExchangeService>.Instance, new ExchangeRequestRegistry(),
                TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.SessionRegistry, Clock);

            RequestHandler = new ExchangeRequestPackettHandler(Exchange, NullLogger<ExchangeRequestPackettHandler>.Instance,
                Blacklist.Object, TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.SessionRegistry,
                CreateTransferService());
            ListHandler = new ExcListPacketHandler(Exchange, NullLogger<ExcListPacketHandler>.Instance,
                TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.SessionRegistry);

            ItemProvider = new ItemGenerationService(new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Main, VNum = 1012, IsTradable = true },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, IsTradable = true, ItemType = ItemType.Armor,
                    EquipmentSlot = EquipmentType.Armor
                }
            }, NullLoggerFactory.Instance, TestHelpers.Instance.LogLanguageLocalizer);
        }

        private static TransferService CreateTransferService()
        {
            var context = TestHelpers.Instance.DbContextBuilder;
            return new TransferService(
                Options.Create(new WorldConfiguration
                {
                    BackpackSize = 48,
                    MaxItemAmount = 999,
                    MaxGoldAmount = 1_000_000_000,
                    MaxBankGoldAmount = 100_000_000_000
                }),
                NullLogger<TransferService>.Instance, TestHelpers.Instance.LogLanguageLocalizer,
                new DaoTransactionScope(context), TestHelpers.Instance.CharacterDao, TestHelpers.Instance.AccountDao,
                new Dao<DbItemInstance, IItemInstanceDto?, Guid>(NullLogger<Dao<DbItemInstance, IItemInstanceDto?, Guid>>.Instance, context),
                new Dao<DbInventoryItemInstance, InventoryItemInstanceDto, Guid>(NullLogger<Dao<DbInventoryItemInstance, InventoryItemInstanceDto, Guid>>.Instance, context),
                new Dao<DbEquipmentOption, EquipmentOptionDto, Guid>(NullLogger<Dao<DbEquipmentOption, EquipmentOptionDto, Guid>>.Instance, context));
        }

        [TestMethod]
        public async Task AcceptingWithoutARequestOpensNoExchange()
        {
            await new Spec("Accepting a trade nobody requested opens no exchange")
                .WhenAsync(TargetAccepts)
                .Then(NoExchangeIsOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AcceptingARequestOpensTheExchange()
        {
            await new Spec("Accepting a pending request opens the exchange")
                .GivenAsync(RequesterAsksForATrade)
                .WhenAsync(TargetAccepts)
                .Then(ExchangeIsOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARequestCanOnlyBeAcceptedOnce()
        {
            await new Spec("A request is consumed when it is accepted")
                .GivenAsync(ExchangeWasOpenedAndCancelled)
                .WhenAsync(TargetAccepts)
                .Then(NoExchangeIsOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExpiredRequestCannotBeAccepted()
        {
            await new Spec("A request older than its lifetime can no longer be accepted")
                .GivenAsync(RequesterAsksForATrade)
                .And(SecondsPass_, 31)
                .WhenAsync(TargetAccepts)
                .Then(NoExchangeIsOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DecliningWithoutARequestSendsNothing()
        {
            await new Spec("Declining a trade nobody requested tells nobody")
                .WhenAsync(TargetDeclines)
                .Then(RequesterReceivedNoCancellation)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATargetWhoBlacklistedTheRequesterIsNotAsked()
        {
            await new Spec("A player who blacklisted the requester is not asked to trade")
                .Given(TargetBlacklistedRequester)
                .WhenAsync(RequesterAsksForATrade)
                .Then(TargetWasAskedTimes_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestsAreRateLimitedPerRequester()
        {
            await new Spec("A second request inside the cooldown is not forwarded")
                .GivenAsync(RequesterAsksForATrade)
                .WhenAsync(RequesterAsksForATrade)
                .Then(TargetWasAskedTimes_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BankGoldIsOfferedInThousandsAndCappedByTheBalance()
        {
            await new Spec("A bank offer above the balance, counted in thousands, is refused")
                .GivenAsync(ExchangeIsOpened)
                .And(RequesterBankGoldIs_, 5_000L)
                .WhenAsync(RequesterOffersBankUnits_, 6L)
                .AndAsync(BothConfirm)
                .Then(RequesterBankGoldShouldBe_, 5_000L)
                .And(TargetBankGoldShouldBe_, 0L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BankGoldOfferMovesTheThousandsShown()
        {
            await new Spec("A bank offer moves the offered thousands")
                .GivenAsync(ExchangeIsOpened)
                .And(RequesterBankGoldIs_, 5_000L)
                .WhenAsync(RequesterOffersBankUnits_, 5L)
                .AndAsync(BothConfirm)
                .Then(RequesterBankGoldShouldBe_, 0L)
                .And(TargetBankGoldShouldBe_, 5_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BalancesAreCheckedAgainAtConfirmation()
        {
            await new Spec("An offer the balance no longer covers at confirmation is not traded")
                .GivenAsync(ExchangeIsOpened)
                .And(RequesterBankGoldIs_, 5_000L)
                .AndAsync(RequesterOffersBankUnits_, 5L)
                .And(RequesterBankGoldIs_, 0L)
                .WhenAsync(BothConfirm)
                .Then(RequesterBankGoldShouldBe_, 0L)
                .And(TargetBankGoldShouldBe_, 0L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingTheOfferClearsBothConfirmations()
        {
            await new Spec("Changing an offer after the partner confirmed needs a new confirmation")
                .GivenAsync(ExchangeIsOpened)
                .And(RequesterGoldIs_, 1_000L)
                .AndAsync(RequesterOffersGold_, 1_000L)
                .AndAsync(TargetConfirms)
                .AndAsync(RequesterOffersGold_, 1L)
                .WhenAsync(RequesterConfirms)
                .Then(TargetGoldShouldBe_, 0L)
                .And(ExchangeIsOpen)
                .And(TargetConfirmationIsCleared)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConcurrentConfirmationsSettleTheTradeOnce()
        {
            await new Spec("Both sides confirming at the same moment settle the trade once")
                .WhenAsync(TradesAreConfirmedConcurrently_, 50)
                .Then(EveryTradeSettledOnce)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnOfferWithAZeroAmountIsRefused()
        {
            await new Spec("An exc_list entry with a zero amount is refused")
                .GivenAsync(ExchangeIsOpened)
                .And(RequesterHoldsItemsInSlotZero)
                .WhenAsync(RequesterOffersAmount_, (short)0)
                .Then(NothingIsOffered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWornItemCannotBeOffered()
        {
            await new Spec("An exc_list entry pointing at a worn item is refused")
                .GivenAsync(ExchangeIsOpened)
                .And(RequesterWearsArmor)
                .WhenAsync(RequesterOffersWornArmor)
                .Then(NothingIsOffered)
                .ExecuteAsync();
        }

        private Task RequesterAsksForATrade() => RequestHandler.ExecuteAsync(new ExchangeRequestPacket
        {
            RequestType = RequestExchangeType.Requested,
            VisualId = Target.Character.VisualId
        }, Requester);

        private Task TargetAccepts() => RequestHandler.ExecuteAsync(new ExchangeRequestPacket
        {
            RequestType = RequestExchangeType.List,
            VisualId = Requester.Character.VisualId
        }, Target);

        private Task TargetDeclines() => RequestHandler.ExecuteAsync(new ExchangeRequestPacket
        {
            RequestType = RequestExchangeType.Declined,
            VisualId = Requester.Character.VisualId
        }, Target);

        private Task RequesterConfirms() => Confirm(Requester);

        private Task TargetConfirms() => Confirm(Target);

        private Task Confirm(ClientSession session) => RequestHandler.ExecuteAsync(new ExchangeRequestPacket
        {
            RequestType = RequestExchangeType.Confirmed
        }, session);

        private async Task BothConfirm()
        {
            await RequesterConfirms();
            await TargetConfirms();
        }

        private async Task ExchangeIsOpened()
        {
            await RequesterAsksForATrade();
            await TargetAccepts();
            Assert.IsTrue(Exchange.CheckExchange(Requester.Character.VisualId));
        }

        private async Task ExchangeWasOpenedAndCancelled()
        {
            await ExchangeIsOpened();
            await RequestHandler.ExecuteAsync(new ExchangeRequestPacket { RequestType = RequestExchangeType.Cancelled }, Requester);
        }

        private void SecondsPass_(int seconds) => Clock.AdvanceSeconds(seconds);

        private void TargetBlacklistedRequester()
        {
            var requesterId = Requester.Character.VisualId;
            var targetId = Target.Character.VisualId;
            Blacklist.Setup(x => x.GetBlacklistedAsync(targetId))
                .Returns(Task.FromResult(new List<CharacterRelationStatus> { new() { CharacterId = requesterId } }));
        }

        private void RequesterBankGoldIs_(long gold) => Requester.Character.BankGold = gold;

        private void RequesterGoldIs_(long gold) => Requester.Character.Gold = gold;

        private Task RequesterOffersBankUnits_(long units) => ListHandler.ExecuteAsync(new ExcListPacket
        {
            Gold = 0,
            BankGold = units,
            SubPackets = null
        }, Requester);

        private Task RequesterOffersGold_(long gold) => ListHandler.ExecuteAsync(new ExcListPacket
        {
            Gold = gold,
            BankGold = 0,
            SubPackets = null
        }, Requester);

        private void RequesterHoldsItemsInSlotZero() =>
            Requester.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1012, 10), 0), NoscorePocketType.Main, 0);

        private Task RequesterOffersAmount_(short amount) => ListHandler.ExecuteAsync(new ExcListPacket
        {
            Gold = 0,
            BankGold = 0,
            SubPackets = new List<ExcListSubPacket?> { new() { Slot = 0, PocketType = PocketType.Main, Amount = amount } }
        }, Requester);

        private void RequesterWearsArmor() =>
            Assert.IsNotNull(Requester.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1), 0), NoscorePocketType.Wear)?.FirstOrDefault());

        private Task RequesterOffersWornArmor() => ListHandler.ExecuteAsync(new ExcListPacket
        {
            Gold = 0,
            BankGold = 0,
            SubPackets = new List<ExcListSubPacket?>
            {
                new() { Slot = (short)EquipmentType.Armor, PocketType = (PocketType)NoscorePocketType.Wear, Amount = 1 }
            }
        }, Requester);

        private readonly List<(long RequesterGold, long TargetGold)> _concurrentResults = new();

        private async Task TradesAreConfirmedConcurrently_(int rounds)
        {
            for (var round = 0; round < rounds; round++)
            {
                Clock.AdvanceSeconds(10);
                Requester.Character.Gold = 1_000;
                Target.Character.Gold = 0;
                await ExchangeIsOpened();
                await RequesterOffersGold_(1_000);

                using var barrier = new Barrier(2);
                await Task.WhenAll(
                    Task.Run(async () => { barrier.SignalAndWait(); await RequesterConfirms(); }),
                    Task.Run(async () => { barrier.SignalAndWait(); await TargetConfirms(); }));

                _concurrentResults.Add((Requester.Character.Gold, Target.Character.Gold));
                if (Exchange.CheckExchange(Requester.Character.VisualId))
                {
                    Exchange.CloseExchange(Requester.Character.VisualId, ExchangeResultType.Failure);
                }
            }
        }

        private void EveryTradeSettledOnce()
        {
            foreach (var (requesterGold, targetGold) in _concurrentResults)
            {
                Assert.AreEqual(0L, requesterGold);
                Assert.AreEqual(1_000L, targetGold);
            }
        }

        private void NoExchangeIsOpen()
        {
            Assert.IsFalse(Exchange.CheckExchange(Requester.Character.VisualId));
            Assert.IsFalse(Exchange.CheckExchange(Target.Character.VisualId));
            Assert.IsFalse(Target.Character.InExchange);
        }

        private void ExchangeIsOpen()
        {
            Assert.AreEqual(Target.Character.VisualId, Exchange.GetTargetId(Requester.Character.VisualId));
        }

        private void TargetConfirmationIsCleared() =>
            Assert.IsFalse(Exchange.IsExchangeConfirmed(Target.Character.VisualId));

        private void RequesterReceivedNoCancellation() =>
            Assert.IsFalse(Requester.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.TradeCancelled2));

        private void TargetWasAskedTimes_(int times) =>
            Assert.AreEqual(times, Target.LastPackets.OfType<Dlgi2Packet>().Count(p => p.Question == Game18NConstString.WantAcceptTrade));

        private void RequesterBankGoldShouldBe_(long gold) => Assert.AreEqual(gold, Requester.Character.BankGold);

        private void TargetBankGoldShouldBe_(long gold) => Assert.AreEqual(gold, Target.Character.BankGold);

        private void TargetGoldShouldBe_(long gold) => Assert.AreEqual(gold, Target.Character.Gold);

        private void NothingIsOffered()
        {
            Assert.IsTrue(Exchange.GetData(Requester.Character.VisualId).ExchangeItems.IsEmpty);
        }
    }
}
