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
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Tests.Shared.BDD.Steps;

namespace NosCore.PacketHandlers.Tests.Bank
{
    [TestClass]
    public class GboxPacketHandlerTests : SpecBase
    {
        private const long GreenBeginnerFee = 528_000;
        private GboxPacketHandler _handler;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            Session.WithNpcAt(100, 1, 1);
            Session.WithWindowOpenedAt(100, NpcDialogWindow.Bank);
            Session.Character.Reputation = 0;
            _handler = new GboxPacketHandler(new BankService(), Options.Create(new WorldConfiguration
            {
                Database = new(),
                MasterCommunication = new(),
                MaxGoldAmount = 1_000_000_000,
                MaxBankGoldAmount = 100_000_000_000
            }), World.NpcDialogService);
        }

        [TestMethod]
        public async Task DepositWithoutTheBankOpenIsIgnored()
        {
            await new Spec("A deposit with no bank window open moves no gold")
                .Given(CharacterHasGold, 5_000_000L)
                .And(NoBankWindowIsOpen)
                .WhenAsync(GboxIsSent_, BankActionType.DepositGold, 1_500L, true)
                .Then(GoldShouldBe, 5_000_000L)
                .And(BankGoldShouldBe_, 0L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithdrawAfterWalkingAwayFromTheBankerIsIgnored()
        {
            await new Spec("A withdrawal after walking away from the banker moves no gold")
                .Given(CharacterHasGold, 1_000_000L)
                .And(BankHolds_, 2_000_000L)
                .And(CharacterWalksTo_, (short)30, (short)30)
                .WhenAsync(GboxIsSent_, BankActionType.WithdrawGold, 1_000L, true)
                .Then(GoldShouldBe, 1_000_000L)
                .And(BankGoldShouldBe_, 2_000_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSavingsBookBankWorksAwayFromTheBanker()
        {
            await new Spec("A bank opened with the savings book takes a deposit wherever the character stands")
                .Given(CharacterHasGold, 5_000_000L)
                .And(TheBankWasOpenedWithTheSavingsBook)
                .And(CharacterWalksTo_, (short)30, (short)30)
                .WhenAsync(GboxIsSent_, BankActionType.DepositGold, 1_500L, true)
                .Then(GoldShouldBe, 3_500_000L)
                .And(BankGoldShouldBe_, 1_500_000L)
                .ExecuteAsync();
        }

        private void NoBankWindowIsOpen() => Session.Character.NpcDialog = null;

        private void BankHolds_(long gold) => Session.Character.BankGold = gold;

        private void CharacterWalksTo_(short x, short y) => Session.WalkTo(x, y);

        private void TheBankWasOpenedWithTheSavingsBook() => Session.WithWindowOpenedRemotely(NpcDialogWindow.Bank);

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
