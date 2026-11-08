//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.BankService;
using NosCore.Core.Configuration;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.PacketHandlers.Bank;
using NosCore.Packets.ClientPackets.Bank;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bank;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Bank
{
    [TestClass]
    public class GboxPacketHandlerTests : SpecBase
    {
        private const long GreenBeginnerFee = 528_000;
        private GboxPacketHandler _handler = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            Session.Character.Reputation = 0;
            _handler = new GboxPacketHandler(new BankService(), Options.Create(new WorldConfiguration
            {
                MaxGoldAmount = 1_000_000_000,
                MaxBankGoldAmount = 100_000_000_000
            }));
        }

        [TestMethod]
        public async Task DepositAsksForConfirmationFirst()
        {
            await new Spec("A deposit asks for confirmation before any gold moves")
                .Given(CharacterHasGold, 5_000_000L)
                .WhenAsync(GboxIsSent_, BankActionType.DepositGold, 1_500L, false)
                .Then(GoldShouldBe, 5_000_000L)
                .And(BankGoldShouldBe_, 0L)
                .And(QuestionShouldBe_, Game18NConstString.AskDeposit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmedDepositMovesGoldIntoTheBank()
        {
            await new Spec("A confirmed deposit of 1 500 units moves 1 500 000 gold into the bank")
                .Given(CharacterHasGold, 5_000_000L)
                .WhenAsync(GboxIsSent_, BankActionType.DepositGold, 1_500L, true)
                .Then(GoldShouldBe, 3_500_000L)
                .And(BankGoldShouldBe_, 1_500_000L)
                .And(BankWindowShouldShow_, BankActionType.DepositGold, 1_500L)
                .And(GoldPacketShouldCarryBankBalance_, 15_000)
                .And(BankMessageShouldBe_, Game18NConstString.DepositBank)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DepositingMoreThanCarriedIsRefused()
        {
            await new Spec("A deposit larger than the gold carried is refused")
                .Given(CharacterHasGold, 999_999L)
                .WhenAsync(GboxIsSent_, BankActionType.DepositGold, 1_000L, true)
                .Then(GoldShouldBe, 999_999L)
                .And(BankGoldShouldBe_, 0L)
                .And(BankErrorShouldBe_, Game18NConstString.NotEnoughGold3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DepositingPastTheBankCapIsRefused()
        {
            await new Spec("A deposit that would push the bank past its cap is refused")
                .Given(CharacterHasGold, 1_000_000L)
                .And(BankGoldIs_, 99_999_500_000L)
                .WhenAsync(GboxIsSent_, BankActionType.DepositGold, 1_000L, true)
                .Then(GoldShouldBe, 1_000_000L)
                .And(BankGoldShouldBe_, 99_999_500_000L)
                .And(BankErrorShouldBe_, Game18NConstString.CanNotDepositAnymore)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithdrawAsksForConfirmationWithTheFee()
        {
            await new Spec("A withdrawal asks for confirmation and quotes the fee")
                .Given(CharacterHasGold, 600_000L)
                .And(BankGoldIs_, 1_000_000L)
                .WhenAsync(GboxIsSent_, BankActionType.WithdrawGold, 500L, false)
                .Then(GoldShouldBe, 600_000L)
                .And(BankGoldShouldBe_, 1_000_000L)
                .And(QuestionShouldBe_, Game18NConstString.AskWithdraw)
                .And(QuestionShouldQuoteFee)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmedWithdrawChargesTheReputationFee()
        {
            await new Spec("A confirmed withdrawal hands the gold over minus the flat fee of the reputation level")
                .Given(CharacterHasGold, 600_000L)
                .And(BankGoldIs_, 1_000_000L)
                .WhenAsync(GboxIsSent_, BankActionType.WithdrawGold, 500L, true)
                .Then(GoldShouldBe, 600_000L + 500_000L - GreenBeginnerFee)
                .And(BankGoldShouldBe_, 500_000L)
                .And(BankWindowShouldShow_, BankActionType.WithdrawGold, 500L)
                .And(BankMessageShouldBe_, Game18NConstString.WithdrawBank)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithdrawingMoreThanTheBalanceIsRefused()
        {
            await new Spec("A withdrawal larger than the balance is refused")
                .Given(CharacterHasGold, 600_000L)
                .And(BankGoldIs_, 400_000L)
                .WhenAsync(GboxIsSent_, BankActionType.WithdrawGold, 500L, true)
                .Then(GoldShouldBe, 600_000L)
                .And(BankGoldShouldBe_, 400_000L)
                .And(BankErrorShouldBe_, Game18NConstString.NotEnoughFounds)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithdrawingWithoutGoldForTheFeeIsRefused()
        {
            await new Spec("A withdrawal is refused when the gold carried cannot cover the fee")
                .Given(CharacterHasGold, GreenBeginnerFee - 1)
                .And(BankGoldIs_, 1_000_000L)
                .WhenAsync(GboxIsSent_, BankActionType.WithdrawGold, 1_000L, true)
                .Then(GoldShouldBe, GreenBeginnerFee - 1)
                .And(BankGoldShouldBe_, 1_000_000L)
                .And(BankErrorShouldBe_, Game18NConstString.YouDontHaveAnyGoldToPay)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithdrawingPastTheCarryCapIsRefused()
        {
            await new Spec("A withdrawal that would push the gold carried past its cap is refused")
                .Given(CharacterHasGold, 999_000_000L)
                .And(BankGoldIs_, 10_000_000L)
                .WhenAsync(GboxIsSent_, BankActionType.WithdrawGold, 10_000L, true)
                .Then(GoldShouldBe, 999_000_000L)
                .And(BankGoldShouldBe_, 10_000_000L)
                .And(BankErrorShouldBe_, Game18NConstString.MaxGoldReached)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BankIsClosedWhileInShop()
        {
            await new Spec("The bank refuses any operation while the character has a private shop open")
                .Given(CharacterHasGold, 5_000_000L)
                .And(CharacterIsInShop)
                .WhenAsync(GboxIsSent_, BankActionType.DepositGold, 1_000L, true)
                .Then(GoldShouldBe, 5_000_000L)
                .And(BankGoldShouldBe_, 0L)
                .And(BankErrorShouldBe_, Game18NConstString.NoBankInShop)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task VipWithdrawsWithoutFee()
        {
            await new Spec("A Cuarry Bank VIP withdraws without paying any fee")
                .Given(CharacterHasGold, 0L)
                .And(BankGoldIs_, 1_000_000L)
                .And(CharacterHasBankVip)
                .WhenAsync(GboxIsSent_, BankActionType.WithdrawGold, 500L, true)
                .Then(GoldShouldBe, 500_000L)
                .And(BankGoldShouldBe_, 500_000L)
                .And(BankWindowShouldShowRank_, BankRankType.Vip)
                .ExecuteAsync();
        }

        private void CharacterHasBankVip() =>
            Session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                StaticBonusType = StaticBonusType.CuarryBankVip,
            });

        private void BankWindowShouldShowRank_(BankRankType rank)
        {
            var gb = Session.LastPackets.OfType<GbPacket>().Single();
            Assert.AreEqual(rank, gb.BankRank);
            Assert.AreEqual(0, gb.BankTax);
        }

        private void BankGoldIs_(long gold) => Session.Character.BankGold = gold;

        private Task GboxIsSent_(BankActionType type, long amount, bool mode) =>
            _handler.ExecuteAsync(new GboxPacket { Type = type, Amount = amount, Mode = mode }, Session);

        private void BankGoldShouldBe_(long expected) => Assert.AreEqual(expected, Session.Character.BankGold);

        private void QuestionShouldBe_(Game18NConstString expected)
        {
            var question = Session.LastPackets.OfType<Qnai2Packet>().Single();
            Assert.AreEqual(expected, question.Question);
        }

        private void QuestionShouldQuoteFee()
        {
            var question = Session.LastPackets.OfType<Qnai2Packet>().Single();
            Assert.AreEqual(GreenBeginnerFee, question.Game18NArguments[1]);
        }

        private void BankWindowShouldShow_(BankActionType type, long goldBank)
        {
            var gb = Session.LastPackets.OfType<GbPacket>().Single();
            Assert.AreEqual(type, gb.Type);
            Assert.AreEqual(goldBank, gb.GoldBank);
            Assert.AreEqual(Session.Character.Gold, gb.Gold);
        }

        private void GoldPacketShouldCarryBankBalance_(int hundreds)
        {
            var gold = Session.LastPackets.OfType<GoldPacket>().Single();
            Assert.AreEqual(Session.Character.Gold, gold.Gold);
            Assert.AreEqual(hundreds, gold.Unknown);
        }

        private void BankMessageShouldBe_(Game18NConstString expected) =>
            Assert.IsTrue(Session.LastPackets.OfType<SMemoi2Packet>()
                .Any(p => p.Type == SMemoType.BankSuccess && p.Message == expected));

        private void BankErrorShouldBe_(Game18NConstString expected)
        {
            var error = Session.LastPackets.OfType<SMemoiPacket>().Single();
            Assert.AreEqual(SMemoType.BankError, error.Type);
            Assert.AreEqual(expected, error.Message);
        }
    }
}
