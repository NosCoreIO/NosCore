//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Bank;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Bank;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class BankTests
    {
        // The bank counts in thousands of gold.
        private const long Unit = 1000;
        private const long MostInTheBank = 100_000_000_000;
        private const long MostCarried = 1_000_000_000;

        // A character next to the broker, with gold in hand and in the bank of its account.
        private static async Task<Player> CustomerAsync(long gold, long banked)
        {
            var player = await Harness.AddPlayerAsync("Alice", x: 76, y: 117, configure: c => c.Gold = gold);
            await using var context = Harness.Database.CreateContext();
            var account = await context.Set<Account>().SingleAsync(a => a.Name == player.Account);
            account.BankMoney = banked;
            await context.SaveChangesAsync();
            return player;
        }

        private static async Task<TestClient> EnterAsync(Player player)
        {
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }

            return client;
        }

        private static readonly RequestNpcPacket TalkToTheBroker = new() { Type = VisualType.Npc, TargetId = WorldData.Broker };

        private static readonly NrunPacket OpenTheBank = new()
        {
            Runner = NrunRunnerType.OpenBankFacilities, Type = 0, VisualType = VisualType.Npc, VisualId = WorldData.Broker
        };

        private static Func<NpcReqPacket, bool> BrokersDialog =>
            packet => packet.Type == VisualType.Npc && packet.TargetId == WorldData.Broker && packet.Data == WorldData.BrokerDialog;

        private static GboxPacket Deposit(long units, bool confirmed = true) =>
            new() { Type = BankActionType.DepositGold, Amount = units, Mode = confirmed };

        private static GboxPacket Withdraw(long units, bool confirmed = true) =>
            new() { Type = BankActionType.WithdrawGold, Amount = units, Mode = confirmed };

        private static Func<GbPacket, bool> Bank(BankActionType action, long units, long carried) =>
            packet => packet.Type == action && packet.GoldBank == units && packet.Gold == carried;

        private static Func<GoldPacket, bool> Carrying(long gold) => packet => packet.Gold == gold && packet.Unknown == 0;

        private static Func<SMemoi2Packet, bool> Receipt => packet => packet.Type == SMemoType.BankSuccess;

        private static Func<Qnai2Packet, bool> AskedToConfirm(GboxPacket asked) =>
            packet => packet.YesPacket is GboxPacket { Mode: true } yes && yes.Type == asked.Type && yes.Amount == asked.Amount;

        private static bool MovesGold(IPacket packet) => packet is GoldPacket or GbPacket;

        private static async Task<long> OpenBankAsync(TestClient client)
        {
            await client.SendAsync(TalkToTheBroker);
            await client.ExpectAsync(BrokersDialog, "the broker's dialog");
            await client.SendAsync(OpenTheBank);
            var opened = await client.ExpectAsync<GbPacket>(p => p.Type == BankActionType.OpenBank, "the bank window");
            return opened.BankTax;
        }

        private static async Task<(long Gold, long Banked)> StoredAsync(Player player)
        {
            await using var context = Harness.Database.CreateContext();
            var banked = await context.Set<Account>().AsNoTracking().Where(a => a.Name == player.Account).Select(a => a.BankMoney).SingleAsync();
            return ((await Harness.StoredAsync(player.CharacterId)).Gold, banked);
        }

        private static Func<SMemoiPacket, bool> Refusal(Game18NConstString message) =>
            packet => packet.Type == SMemoType.BankError && packet.Message == message;

        [TestMethod]
        public async Task OpeningTheBankShowsWhatIsInItAndWhatIsCarried()
        {
            var player = await CustomerAsync(gold: 5_000, banked: 7_000_000);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync(TalkToTheBroker);
            await alice.ExpectAsync(BrokersDialog, "the broker's dialog");
            await alice.SendAsync(OpenTheBank);

            var opened = await alice.ExpectAsync<GbPacket>();
            Assert.AreEqual((BankActionType.OpenBank, 7000L, 5000), (opened.Type, opened.GoldBank, opened.Gold));
        }

        [TestMethod]
        public async Task ADepositIsAskedAboutFirstAndThenMovesGoldToTheBank()
        {
            var player = await CustomerAsync(gold: 5_000, banked: 0);
            await using (var alice = await EnterAsync(player))
            {
                await OpenBankAsync(alice);
                var asked = alice.Mark();

                await alice.SendAsync(Deposit(3, confirmed: false));

                await alice.ExpectAsync(AskedToConfirm(Deposit(3)), "the question");
                Assert.IsFalse(alice.Since(asked).Any(MovesGold), "nothing moves while it is only asked");
                var deposited = alice.Mark();

                await alice.SendAsync(Deposit(3));

                await alice.ExpectSinceAsync(deposited, Carrying(2000), "the gold left in hand");
                await alice.ExpectSinceAsync(deposited, Bank(BankActionType.DepositGold, 3, 2000), "the bank after the deposit");
                await alice.ExpectSinceAsync(deposited, Receipt, "the receipt");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual((2_000L, 3_000L), await StoredAsync(player));
        }

        [TestMethod]
        public async Task AWithdrawalBringsGoldBackLessTheFee()
        {
            var player = await CustomerAsync(gold: 1_000_000, banked: 5_000_000);
            await using (var alice = await EnterAsync(player))
            {
                var fee = await OpenBankAsync(alice);
                Assert.IsTrue(fee > 0, "a withdrawal costs something");

                await alice.SendAsync(Withdraw(2000, confirmed: false));

                await alice.ExpectAsync(AskedToConfirm(Withdraw(2000)), "the question");
                var withdrawn = alice.Mark();

                await alice.SendAsync(Withdraw(2000));

                var carried = 1_000_000 + 2_000_000 - fee;
                await alice.ExpectSinceAsync(withdrawn, Carrying(carried), "the gold in hand");
                await alice.ExpectSinceAsync(withdrawn, Bank(BankActionType.WithdrawGold, 3000, carried), "the bank after the withdrawal");
                await alice.ExpectSinceAsync(withdrawn, Receipt, "the receipt");
            }

            await using var back = await Harness.ComeBackAsync(player);
            var stored = await StoredAsync(player);
            Assert.AreEqual(3_000_000L, stored.Banked);
            Assert.IsTrue(stored.Gold is > 2_000_000 and < 3_000_000, $"the gold in hand is stored, less the fee: {stored.Gold}");
        }

        [TestMethod]
        public async Task EverythingCarriedCanBeDepositedAndEverythingBankedWithdrawn()
        {
            var player = await CustomerAsync(gold: 2_000_000, banked: 0);
            await using (var alice = await EnterAsync(player))
            {
                var fee = await OpenBankAsync(alice);
                var deposited = alice.Mark();
                await alice.SendAsync(Deposit(1000));
                await alice.ExpectSinceAsync(deposited, Bank(BankActionType.DepositGold, 1000, 1_000_000), "the bank after the deposit");
                var withdrawn = alice.Mark();

                await alice.SendAsync(Withdraw(1000));

                await alice.ExpectSinceAsync(withdrawn, Bank(BankActionType.WithdrawGold, 0, 2_000_000 - fee), "the emptied bank");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(0L, (await StoredAsync(player)).Banked);
        }

        [TestMethod]
        public async Task ADepositOfMoreThanIsCarriedIsRefused()
        {
            var player = await CustomerAsync(gold: 5_000, banked: 1_000);
            await using (var alice = await EnterAsync(player))
            {
                await OpenBankAsync(alice);
                var refused = alice.Mark();

                await alice.SendAsync(Deposit(6));

                await alice.ExpectSinceAsync(refused, Refusal(Game18NConstString.NotEnoughGold3), "the refusal");
                Assert.IsFalse(alice.Since(refused).Any(MovesGold), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual((5_000L, 1_000L), await StoredAsync(player));
        }

        [TestMethod]
        public async Task AWithdrawalOfMoreThanIsInTheBankIsRefused()
        {
            var player = await CustomerAsync(gold: 1_000_000, banked: 5_000);
            await using (var alice = await EnterAsync(player))
            {
                await OpenBankAsync(alice);
                var refused = alice.Mark();

                await alice.SendAsync(Withdraw(6));

                await alice.ExpectSinceAsync(refused, Refusal(Game18NConstString.NotEnoughFounds), "the refusal");
                Assert.IsFalse(alice.Since(refused).Any(MovesGold), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual((1_000_000L, 5_000L), await StoredAsync(player));
        }

        [TestMethod]
        public async Task AWithdrawalIsRefusedToSomeoneWhoCannotPayTheFee()
        {
            var player = await CustomerAsync(gold: 10, banked: 5_000_000);
            await using (var alice = await EnterAsync(player))
            {
                var fee = await OpenBankAsync(alice);
                Assert.IsTrue(fee > 10, "the fee is more than she carries");
                var refused = alice.Mark();

                await alice.SendAsync(Withdraw(5000));

                await alice.ExpectSinceAsync(refused, Refusal(Game18NConstString.YouDontHaveAnyGoldToPay), "the refusal");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual((10L, 5_000_000L), await StoredAsync(player));
        }

        [TestMethod]
        public async Task AWithdrawalThatWouldPassTheMostACharacterCarriesIsRefused()
        {
            var player = await CustomerAsync(gold: MostCarried - 1_000, banked: 5_000_000);
            await using (var alice = await EnterAsync(player))
            {
                var fee = await OpenBankAsync(alice);
                var units = (fee + 2_000) / Unit;
                var refused = alice.Mark();

                await alice.SendAsync(Withdraw(units));

                await alice.ExpectSinceAsync(refused, Refusal(Game18NConstString.MaxGoldReached), "the refusal");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual((MostCarried - 1_000, 5_000_000L), await StoredAsync(player));
        }

        [TestMethod]
        public async Task ADepositThatWouldPassTheMostTheBankHoldsIsRefused()
        {
            var player = await CustomerAsync(gold: 5_000, banked: MostInTheBank - 1_000);
            await using (var alice = await EnterAsync(player))
            {
                await OpenBankAsync(alice);
                var refused = alice.Mark();

                await alice.SendAsync(Deposit(2));

                await alice.ExpectSinceAsync(refused, Refusal(Game18NConstString.CanNotDepositAnymore), "the refusal");
                var filled = alice.Mark();

                await alice.SendAsync(Deposit(1));

                await alice.ExpectSinceAsync(filled, Bank(BankActionType.DepositGold, MostInTheBank / Unit, 4000), "the bank filled to the brim");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual((4_000L, MostInTheBank), await StoredAsync(player));
        }

        [TestMethod]
        public async Task TheBankDoesNothingForSomeoneWhoHasNotOpenedIt()
        {
            var player = await CustomerAsync(gold: 5_000, banked: 0);
            await using (var alice = await EnterAsync(player))
            {
                var asked = alice.Mark();

                await alice.SendAsync(Deposit(3));

                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, p => p is GoldPacket or GbPacket or SMemoiPacket or SMemoi2Packet,
                    TimeSpan.FromSeconds(1)), "the bank does not answer");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual((5_000L, 0L), await StoredAsync(player));
        }

        [TestMethod]
        public async Task TheBankClosesForSomeoneWhoWalksAwayFromIt()
        {
            var player = await CustomerAsync(gold: 5_000, banked: 0);
            await using var bob = await Harness.EnterAsync("Bob", x: 76, y: 112);
            await using (var alice = await EnterAsync(player))
            {
                await OpenBankAsync(alice);
                for (short y = 116; y >= 110; y--)
                {
                    await alice.WalkAsync(76, y);
                    var step = y;
                    await bob.ExpectAsync<MovePacket>(p => p.VisualType == VisualType.Player && p.VisualEntityId == alice.CharacterId
                        && p.MapX == 76 && p.MapY == step, "the step");
                }

                var asked = alice.Mark();

                await alice.SendAsync(Deposit(3));

                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, p => p is GoldPacket or GbPacket or SMemoi2Packet,
                    TimeSpan.FromSeconds(1)), "the bank does not answer");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual((5_000L, 0L), await StoredAsync(player));
        }

        [TestMethod]
        public async Task TheBankIsSharedByTheCharactersOfAnAccountOnly()
        {
            var player = await CustomerAsync(gold: 5_000, banked: 0);
            var other = await CustomerAsync(gold: 0, banked: 0);
            await using (var alice = await EnterAsync(player))
            {
                await OpenBankAsync(alice);
                await alice.SendAsync(Deposit(3));
                await alice.ExpectAsync(Bank(BankActionType.DepositGold, 3, 2000), "the bank after the deposit");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(3_000L, (await StoredAsync(player)).Banked);
            Assert.AreEqual(0L, (await StoredAsync(other)).Banked, "another account's bank is not touched");
        }
    }
}
