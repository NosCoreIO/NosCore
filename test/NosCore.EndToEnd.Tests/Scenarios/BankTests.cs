//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class BankTests
    {
        private const int Npc = 2;
        private const int OpenBank = 321;
        private const int Deposit = 1;
        private const int Withdraw = 2;
        private const int Opened = 3;
        private const int BankSuccess = 4;
        private const int BankError = 5;
        private const int NotEnoughGold = 1072;
        private const int BankIsFull = 1073;
        private const int NotEnoughInTheBank = 1074;
        private const int TooMuchGoldToCarry = 1075;
        private const int NoGoldForTheFee = 2354;

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

        // gb <action> <thousands in the bank> <gold carried> <rank> <fee of a withdrawal>
        private static async Task<long> OpenBankAsync(TestClient client)
        {
            await client.SendAsync($"npc_req {Npc} {WorldData.Broker}");
            await client.ExpectAsync(p => p == $"npc_req {Npc} {WorldData.Broker} {WorldData.BrokerDialog}", "the broker's dialog");
            await client.SendAsync($"n_run {OpenBank} 0 {Npc} {WorldData.Broker}");
            var opened = await client.ExpectAsync(p => p.StartsWith($"gb {Opened} ", StringComparison.Ordinal), "the bank window");
            return long.Parse(opened.Split(' ')[5], CultureInfo.InvariantCulture);
        }

        private static async Task<(long Gold, long Banked)> StoredAsync(Player player)
        {
            await using var context = Harness.Database.CreateContext();
            var banked = await context.Set<Account>().AsNoTracking().Where(a => a.Name == player.Account).Select(a => a.BankMoney).SingleAsync();
            return ((await Harness.StoredAsync(player.CharacterId)).Gold, banked);
        }

        private static Func<string, bool> Refusal(int message) =>
            packet => packet.StartsWith($"s_memoi {BankError} {message} ", StringComparison.Ordinal) || packet == $"s_memoi {BankError} {message}";

        [TestMethod]
        public async Task OpeningTheBankShowsWhatIsInItAndWhatIsCarried()
        {
            var player = await CustomerAsync(gold: 5_000, banked: 7_000_000);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync($"npc_req {Npc} {WorldData.Broker}");
            await alice.ExpectAsync(p => p == $"npc_req {Npc} {WorldData.Broker} {WorldData.BrokerDialog}", "the broker's dialog");
            await alice.SendAsync($"n_run {OpenBank} 0 {Npc} {WorldData.Broker}");

            var opened = (await alice.ExpectAsync("gb")).Split(' ');
            Assert.AreEqual($"gb {Opened} 7000 5000", string.Join(' ', opened.Take(4)));
        }

        [TestMethod]
        public async Task ADepositIsAskedAboutFirstAndThenMovesGoldToTheBank()
        {
            var player = await CustomerAsync(gold: 5_000, banked: 0);
            await using (var alice = await EnterAsync(player))
            {
                await OpenBankAsync(alice);
                var asked = alice.Mark();

                await alice.SendAsync($"gbox {Deposit} 3 0");

                StringAssert.StartsWith(await alice.ExpectAsync("qnai2"), $"qnai2 #gbox^{Deposit}^3^1 ");
                Assert.IsFalse(alice.Since(asked).Any(p => TestClient.HeaderOf(p) is "gold" or "gb"), "nothing moves while it is only asked");
                var deposited = alice.Mark();

                await alice.SendAsync($"gbox {Deposit} 3 1");

                await alice.ExpectSinceAsync(deposited, p => p == "gold 2000 0", "the gold left in hand");
                await alice.ExpectSinceAsync(deposited, p => p.StartsWith($"gb {Deposit} 3 2000 ", StringComparison.Ordinal), "the bank after the deposit");
                await alice.ExpectSinceAsync(deposited, p => p.StartsWith($"s_memoi2 {BankSuccess} ", StringComparison.Ordinal), "the receipt");
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

                await alice.SendAsync($"gbox {Withdraw} 2000 0");

                StringAssert.StartsWith(await alice.ExpectAsync("qnai2"), $"qnai2 #gbox^{Withdraw}^2000^1 ");
                var withdrawn = alice.Mark();

                await alice.SendAsync($"gbox {Withdraw} 2000 1");

                var carried = 1_000_000 + 2_000_000 - fee;
                await alice.ExpectSinceAsync(withdrawn, p => p == $"gold {carried} 0", "the gold in hand");
                await alice.ExpectSinceAsync(withdrawn, p => p.StartsWith($"gb {Withdraw} 3000 {carried} ", StringComparison.Ordinal), "the bank after the withdrawal");
                await alice.ExpectSinceAsync(withdrawn, p => p.StartsWith($"s_memoi2 {BankSuccess} ", StringComparison.Ordinal), "the receipt");
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
                await alice.SendAsync($"gbox {Deposit} 1000 1");
                await alice.ExpectSinceAsync(deposited, p => p.StartsWith($"gb {Deposit} 1000 1000000 ", StringComparison.Ordinal), "the bank after the deposit");
                var withdrawn = alice.Mark();

                await alice.SendAsync($"gbox {Withdraw} 1000 1");

                await alice.ExpectSinceAsync(withdrawn, p => p.StartsWith($"gb {Withdraw} 0 {2_000_000 - fee} ", StringComparison.Ordinal), "the emptied bank");
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

                await alice.SendAsync($"gbox {Deposit} 6 1");

                await alice.ExpectSinceAsync(refused, Refusal(NotEnoughGold), "the refusal");
                Assert.IsFalse(alice.Since(refused).Any(p => TestClient.HeaderOf(p) is "gold" or "gb"), "nothing moves");
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

                await alice.SendAsync($"gbox {Withdraw} 6 1");

                await alice.ExpectSinceAsync(refused, Refusal(NotEnoughInTheBank), "the refusal");
                Assert.IsFalse(alice.Since(refused).Any(p => TestClient.HeaderOf(p) is "gold" or "gb"), "nothing moves");
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

                await alice.SendAsync($"gbox {Withdraw} 5000 1");

                await alice.ExpectSinceAsync(refused, Refusal(NoGoldForTheFee), "the refusal");
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

                await alice.SendAsync($"gbox {Withdraw} {units} 1");

                await alice.ExpectSinceAsync(refused, Refusal(TooMuchGoldToCarry), "the refusal");
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

                await alice.SendAsync($"gbox {Deposit} 2 1");

                await alice.ExpectSinceAsync(refused, Refusal(BankIsFull), "the refusal");
                var filled = alice.Mark();

                await alice.SendAsync($"gbox {Deposit} 1 1");

                await alice.ExpectSinceAsync(filled, p => p.StartsWith($"gb {Deposit} {MostInTheBank / Unit} 4000 ", StringComparison.Ordinal), "the bank filled to the brim");
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

                await alice.SendAsync($"gbox {Deposit} 3 1");
                await Task.Delay(TimeSpan.FromSeconds(1));

                Assert.AreEqual(string.Empty, string.Join(" / ", alice.Since(asked).Where(p => TestClient.HeaderOf(p) is "gold" or "gb" or "s_memoi" or "s_memoi2")),
                    "the bank does not answer");
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
                    await bob.ExpectAsync(p => p.StartsWith($"mv 1 {alice.CharacterId} 76 {y} ", StringComparison.Ordinal), "the step");
                }

                var asked = alice.Mark();

                await alice.SendAsync($"gbox {Deposit} 3 1");

                Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "gold" or "gb" or "s_memoi2",
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
                await alice.SendAsync($"gbox {Deposit} 3 1");
                await alice.ExpectAsync(p => p.StartsWith($"gb {Deposit} 3 2000 ", StringComparison.Ordinal), "the bank after the deposit");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(3_000L, (await StoredAsync(player)).Banked);
            Assert.AreEqual(0L, (await StoredAsync(other)).Banked, "another account's bank is not touched");
        }
    }
}
