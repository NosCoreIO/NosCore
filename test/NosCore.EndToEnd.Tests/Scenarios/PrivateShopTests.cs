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
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class PrivateShopTests
    {
        private const int Player = 1;
        private const int EquipmentPocket = 0;
        private const int EtcPocket = 2;
        private const int ShopSign = 501;
        private const int ShopMenu = 3;
        private const int BagSlots = 48;
        private const long MostGold = 1_000_000_000;
        private const int Bought = 117;
        private const int NotEnoughGold = 115;
        private const int OnlyOnAMarket = 127;
        private const int ShopOpened = 131;
        private const int CannotBeTraded = 704;
        private const int SellerWouldHoldTooMuchGold = 878;
        private const int AwayFromPortals = 924;
        private const int NothingToSell = 1658;
        private const int BagIsFull = 1841;

        // m_shop 0 <pocket slot amount price, twenty times> <name>
        private static string Open(string name, params (int Pocket, int Slot, int Amount, long Price)[] shelf) =>
            $"m_shop 0 {string.Join(' ', Enumerable.Range(0, 20).Select(i => i < shelf.Length
                ? $"{shelf[i].Pocket} {shelf[i].Slot} {shelf[i].Amount} {shelf[i].Price}" : "0 0 0 0"))} {name}";

        private static Func<string, bool> Sign(TestClient seller, string name) =>
            packet => packet == $"shop {Player} {seller.CharacterId} {ShopSign} {ShopMenu} 0 {name}";

        private static Func<string, bool> AnySign(TestClient seller) =>
            packet => packet.StartsWith($"shop {Player} {seller.CharacterId} {ShopSign} ", StringComparison.Ordinal);

        private static Func<string, bool> NoSign(TestClient seller) =>
            packet => packet == $"shop {Player} {seller.CharacterId} 0 0";

        private static Func<string, bool> Flag(TestClient seller, int sign) =>
            packet => packet == $"pflag {Player} {seller.CharacterId} {sign}";

        private static Func<string, bool> Sitting(TestClient who, bool sitting) =>
            packet => packet == $"rest {Player} {who.CharacterId} {(sitting ? 1 : 0)}";

        private static Func<string, bool> Shelf(TestClient seller) =>
            packet => packet.StartsWith($"n_inv {Player} {seller.CharacterId} ", StringComparison.Ordinal);

        // n_inv <type> <id> 0 <kind> <pocket.slot.vnum.amount.price, or -1 for an empty place>...
        private static string[] OnShelf(string shelf) => shelf.Split(' ').Skip(5).Where(place => place != "-1").ToArray();

        private static Func<string, bool> Slot(int pocket, int slot, int vnum, int amount) =>
            packet => packet.StartsWith($"ivn {pocket} {slot}.{vnum}.{amount}.", StringComparison.Ordinal);

        private static Func<string, bool> EmptySlot(int pocket, int slot) =>
            packet => packet.StartsWith($"ivn {pocket} {slot}.", StringComparison.Ordinal)
                && packet.Split(' ')[2].Split('.')[1] is "0" or "-1";

        private static Func<string, bool> Gold(long gold) => packet => packet == $"gold {gold} 0";

        private static Func<string, bool> AnyGold => packet => TestClient.HeaderOf(packet) == "gold";

        private static Func<string, bool> Memo(int type, int message) => packet => packet == $"s_memoi {type} {message} 0";

        private static Func<string, bool> Told(int message) =>
            packet => packet.Split(' ') is ["infoi", var id, ..] && id == message.ToString(CultureInfo.InvariantCulture);

        private static Func<string, bool> Said(TestClient who, int message) =>
            packet => packet.StartsWith($"sayi {Player} {who.CharacterId} ", StringComparison.Ordinal)
                && packet.Split(' ')[4] == message.ToString(CultureInfo.InvariantCulture);

        private static Func<string, bool> Announced(int message) =>
            packet => packet.Split(' ') is ["msgi", _, var id, ..] && id == message.ToString(CultureInfo.InvariantCulture);

        private static Func<string, bool> ShopEnded => packet => TestClient.HeaderOf(packet) == "shop_end";

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

        private static string[] Listed(TestClient client, int pocket) =>
            client.Received.Where(p => p.StartsWith($"inv {pocket}", StringComparison.Ordinal))
                .SelectMany(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(2))
                .Select(slot => slot.Split('.'))
                .OrderBy(slot => int.Parse(slot[0], CultureInfo.InvariantCulture))
                .Select(slot => string.Join('.', slot.Take(3)))
                .ToArray();

        // The gold is sent a moment after the character is in the world.
        private static async Task<long> GoldOnEnteringAsync(TestClient client) =>
            long.Parse((await client.ExpectSinceAsync(0, p => TestClient.HeaderOf(p) == "gold", "the gold on entering")).Split(' ')[1],
                CultureInfo.InvariantCulture);

        private static async Task<string[]> StoredAsync(long characterId, NoscorePocketType pocket)
        {
            await using var context = Harness.Database.CreateContext();
            var rows = await context.Set<InventoryItemInstance>().AsNoTracking()
                .Where(i => i.CharacterId == characterId && i.Type == pocket)
                .OrderBy(i => i.Slot)
                .Select(i => new { i.Slot, i.ItemInstance.ItemVNum, i.ItemInstance.Amount })
                .ToArrayAsync();
            return rows.Select(r => $"{r.Slot}.{r.ItemVNum}.{r.Amount}").ToArray();
        }

        private static void Same(string[] expected, string[] actual, string? what = null) =>
            Assert.AreEqual(string.Join(' ', expected), string.Join(' ', actual), what);

        // The seller opens a shop and someone standing by sees its sign go up.
        private static async Task OpenAsync(TestClient seller, TestClient watcher, string name,
            params (int Pocket, int Slot, int Amount, long Price)[] shelf)
        {
            var opened = watcher.Mark();
            await seller.SendAsync(Open(name, shelf));
            await watcher.ExpectSinceAsync(opened, Sign(seller, name), "the sign of the shop");
        }

        private static async Task<string> LookAsync(TestClient buyer, TestClient seller)
        {
            var looked = buyer.Mark();
            await buyer.SendAsync($"npc_req {Player} {seller.CharacterId}");
            return await buyer.ExpectSinceAsync(looked, Shelf(seller), "the shelf");
        }

        [TestMethod]
        public async Task AShopOpenedIsShownToTheMapAndItsSellerSitsDown()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 40);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 41, 40);
            var opened = alice.Mark();
            var seen = bob.Mark();

            await alice.SendAsync(Open("stall", (EtcPocket, 0, 5, 30)));

            await alice.ExpectSinceAsync(opened, Sign(alice, "stall"), "her own sign");
            await alice.ExpectSinceAsync(opened, Told(ShopOpened), "the notice that the shop is open");
            await alice.ExpectSinceAsync(opened, Sitting(alice, true), "herself sitting");
            await bob.ExpectSinceAsync(seen, Sign(alice, "stall"), "the sign");
            await bob.ExpectSinceAsync(seen, Flag(alice, ShopSign), "the flag over her head");
            await bob.ExpectSinceAsync(seen, Sitting(alice, true), "her sitting down");
        }

        [TestMethod]
        public async Task AShopIsRefusedOnAMapThatIsNoMarket()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Town, 30, 30);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Town, 31, 30);
            var refused = alice.Mark();
            var seen = bob.Mark();

            await alice.SendAsync(Open("stall", (EtcPocket, 0, 5, 30)));

            await alice.ExpectSinceAsync(refused, Told(OnlyOnAMarket), "the refusal");
            await alice.ExpectSinceAsync(refused, ShopEnded, "the end of the shop window");
            Assert.IsTrue(await bob.NoneSinceAsync(seen, p => AnySign(alice)(p) || Flag(alice, ShopSign)(p) || Sitting(alice, true)(p),
                TimeSpan.FromSeconds(1)), "nobody is shown a shop");
        }

        [TestMethod]
        public async Task AShopIsRefusedNextToAPortal()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, WorldData.MarketPortalX - 3, WorldData.MarketPortalY + 3);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, WorldData.MarketPortalX - 3, WorldData.MarketPortalY + 4);
            var refused = alice.Mark();
            var seen = bob.Mark();

            await alice.SendAsync(Open("stall", (EtcPocket, 0, 5, 30)));

            await alice.ExpectSinceAsync(refused, Told(AwayFromPortals), "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync(seen, p => AnySign(alice)(p) || Flag(alice, ShopSign)(p), TimeSpan.FromSeconds(1)),
                "nobody is shown a shop");
        }

        [TestMethod]
        public async Task AShopWithAnEmptyShelfIsRefused()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 44);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 41, 44);
            var refused = alice.Mark();
            var seen = bob.Mark();

            await alice.SendAsync(Open("stall"));

            await alice.ExpectSinceAsync(refused, Said(alice, NothingToSell), "the refusal");
            await alice.ExpectSinceAsync(refused, ShopEnded, "the end of the shop window");
            Assert.IsTrue(await bob.NoneSinceAsync(seen, p => AnySign(alice)(p) || Flag(alice, ShopSign)(p), TimeSpan.FromSeconds(1)),
                "nobody is shown a shop");
        }

        [TestMethod]
        public async Task SomethingThatCannotChangeHandsIsRefusedForAShelf()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 48);
            await Harness.GiveAsync(seller, WorldData.Keepsake, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 41, 48);
            var refused = alice.Mark();
            var seen = bob.Mark();

            await alice.SendAsync(Open("stall", (EtcPocket, 0, 5, 30)));

            await alice.ExpectSinceAsync(refused, Said(alice, CannotBeTraded), "the refusal");
            await alice.ExpectSinceAsync(refused, ShopEnded, "the end of the shop window");
            Assert.IsTrue(await bob.NoneSinceAsync(seen, p => AnySign(alice)(p) || Flag(alice, ShopSign)(p), TimeSpan.FromSeconds(1)),
                "nobody is shown a shop");
        }

        [TestMethod]
        public async Task TheSameStackCannotBePutOnTwoShelves()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 52);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 41, 52);
            var refused = alice.Mark();
            var seen = bob.Mark();

            await alice.SendAsync(Open("stall", (EtcPocket, 0, 5, 30), (EtcPocket, 0, 5, 10)));

            await alice.ExpectSinceAsync(refused, ShopEnded, "the end of the shop window");
            Assert.IsTrue(await bob.NoneSinceAsync(seen, p => AnySign(alice)(p) || Flag(alice, ShopSign)(p), TimeSpan.FromSeconds(1)),
                "nobody is shown a shop");
        }

        [TestMethod]
        public async Task TheShelfIsShownToSomeoneWhoLooks()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 60);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await Harness.GiveAsync(seller, WorldData.Gem, 2, slot: 1);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 41, 60);
            await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 4, 30), (EtcPocket, 1, 2, 700));

            var shelf = await LookAsync(bob, alice);

            Same(new[] { $"{EtcPocket}.0.{WorldData.Seed}.4.30", $"{EtcPocket}.1.{WorldData.Gem}.2.700" }, OnShelf(shelf));
        }

        [TestMethod]
        public async Task BuyingPartOfAStackMovesItemsAndGoldAndBothSidesAreStored()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 64);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            var buyer = await Harness.AddPlayerAsync("Bob", WorldData.Market, 41, 64, c => c.Gold = 1000);
            await using (var alice = await EnterAsync(seller))
            await using (var bob = await EnterAsync(buyer))
            {
                await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 5, 30));
                await LookAsync(bob, alice);
                var sold = alice.Mark();
                var bought = bob.Mark();

                await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 2");

                await bob.ExpectSinceAsync(bought, Slot(EtcPocket, 0, WorldData.Seed, 2), "the two seeds in his bag");
                await bob.ExpectSinceAsync(bought, Memo(0, Bought), "the purchase confirmed");
                await bob.ExpectSinceAsync(bought, Gold(1000 - 2 * 30), "his gold after paying");
                Same(new[] { $"{EtcPocket}.0.{WorldData.Seed}.3.30" }, OnShelf(await bob.ExpectSinceAsync(bought, Shelf(alice), "the shelf after the sale")));
                await alice.ExpectSinceAsync(sold, Slot(EtcPocket, 0, WorldData.Seed, 3), "the three seeds left in her bag");
                await alice.ExpectSinceAsync(sold, Gold(2 * 30), "her gold after the sale");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same(new[] { $"0.{WorldData.Seed}.3" }, Listed(aliceBack, EtcPocket), "her bag when she comes back");
            Same(new[] { $"0.{WorldData.Seed}.2" }, Listed(bobBack, EtcPocket), "his bag when he comes back");
            Assert.AreEqual(2 * 30, await GoldOnEnteringAsync(aliceBack), "her gold when she comes back");
            Assert.AreEqual(1000 - 2 * 30, await GoldOnEnteringAsync(bobBack), "his gold when he comes back");
            Same(new[] { $"0.{WorldData.Seed}.3" }, await StoredAsync(seller.CharacterId, NoscorePocketType.Etc));
            Same(new[] { $"0.{WorldData.Seed}.2" }, await StoredAsync(buyer.CharacterId, NoscorePocketType.Etc));
        }

        [TestMethod]
        public async Task BuyingTheLastOfTheShelfClosesTheShopForEveryone()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 68);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            var buyer = await Harness.AddPlayerAsync("Bob", WorldData.Market, 41, 68, c => c.Gold = 1000);
            await using var carol = await Harness.EnterAsync("Carol", WorldData.Market, 40, 69);
            await using (var alice = await EnterAsync(seller))
            await using (var bob = await EnterAsync(buyer))
            {
                await OpenAsync(alice, carol, "stall", (EtcPocket, 0, 5, 30));
                await LookAsync(bob, alice);
                var sold = alice.Mark();
                var bought = bob.Mark();
                var seen = carol.Mark();

                await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 5");

                await bob.ExpectSinceAsync(bought, Slot(EtcPocket, 0, WorldData.Seed, 5), "the five seeds in his bag");
                await bob.ExpectSinceAsync(bought, Gold(1000 - 5 * 30), "his gold after paying");
                await alice.ExpectSinceAsync(sold, EmptySlot(EtcPocket, 0), "her emptied slot");
                await alice.ExpectSinceAsync(sold, Gold(5 * 30), "her gold after the sale");
                await alice.ExpectSinceAsync(sold, Sitting(alice, false), "herself standing up");
                await carol.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");
                await carol.ExpectSinceAsync(seen, Flag(alice, 0), "the flag taken down");
                await carol.ExpectSinceAsync(seen, Sitting(alice, false), "the seller standing up");
                await bob.ExpectSinceAsync(bought, NoSign(alice), "the sign taken down for the buyer");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same([], Listed(aliceBack, EtcPocket), "her bag when she comes back");
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(bobBack, EtcPocket), "his bag when he comes back");
            Assert.AreEqual(5 * 30, await GoldOnEnteringAsync(aliceBack), "her gold when she comes back");
            Assert.AreEqual(1000 - 5 * 30, await GoldOnEnteringAsync(bobBack), "his gold when he comes back");
        }

        [TestMethod]
        public async Task BuyingOutOneOfTwoShelvesKeepsTheShopOpenWithTheOther()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 72);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await Harness.GiveAsync(seller, WorldData.Gem, 2, slot: 1);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 41, 72, c => c.Gold = 1000);
            await using var carol = await Harness.EnterAsync("Carol", WorldData.Market, 40, 73);
            await OpenAsync(alice, carol, "stall", (EtcPocket, 0, 5, 30), (EtcPocket, 1, 2, 700));
            await LookAsync(bob, alice);
            var bought = bob.Mark();
            var seen = carol.Mark();

            await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 5");

            Same(new[] { $"{EtcPocket}.1.{WorldData.Gem}.2.700" }, OnShelf(await bob.ExpectSinceAsync(bought, Shelf(alice), "the shelf after the sale")));
            Assert.IsTrue(await carol.NoneSinceAsync(seen, p => NoSign(alice)(p) || Flag(alice, 0)(p) || Sitting(alice, false)(p),
                TimeSpan.FromSeconds(1)), "the shop stays open");
            Same(new[] { $"{EtcPocket}.1.{WorldData.Gem}.2.700" }, OnShelf(await LookAsync(carol, alice)), "what someone else is shown");
        }

        [TestMethod]
        public async Task ABuyerWithoutTheGoldIsRefusedAndNothingMoves()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 76);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            var buyer = await Harness.AddPlayerAsync("Bob", WorldData.Market, 41, 76, c => c.Gold = 59);
            await using (var alice = await EnterAsync(seller))
            await using (var bob = await EnterAsync(buyer))
            {
                await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 5, 30));
                await LookAsync(bob, alice);
                var asked = alice.Mark();
                var refused = bob.Mark();

                await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 2");

                await bob.ExpectSinceAsync(refused, Memo(2, NotEnoughGold), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "gold", TimeSpan.FromSeconds(1)),
                    "the seller is told of no sale");
                Assert.IsFalse(bob.Since(refused).Any(p => TestClient.HeaderOf(p) is "ivn" or "gold"), "nothing reaches the buyer");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(aliceBack, EtcPocket));
            Same([], Listed(bobBack, EtcPocket));
            Assert.AreEqual(0, await GoldOnEnteringAsync(aliceBack));
            Assert.AreEqual(59, await GoldOnEnteringAsync(bobBack));
        }

        [TestMethod]
        public async Task ABuyerWithAFullBagIsRefusedAndNothingMoves()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 80);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            var buyer = await Harness.AddPlayerAsync("Bob", WorldData.Market, 41, 80, c => c.Gold = 1000);
            for (short slot = 0; slot < BagSlots; slot++)
            {
                await Harness.GiveAsync(buyer, WorldData.Pebble, 1, slot);
            }

            await using (var alice = await EnterAsync(seller))
            await using (var bob = await EnterAsync(buyer))
            {
                await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 5, 30));
                await LookAsync(bob, alice);
                var asked = alice.Mark();
                var refused = bob.Mark();

                await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 5");

                await bob.ExpectSinceAsync(refused, Announced(BagIsFull), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "gold" || NoSign(alice)(p),
                    TimeSpan.FromSeconds(1)), "the seller is told of no sale and the shop stays open");
                Assert.IsFalse(bob.Since(refused).Any(AnyGold), "the buyer pays nothing");
                Same(new[] { $"{EtcPocket}.0.{WorldData.Seed}.5.30" }, OnShelf(await LookAsync(bob, alice)), "the shelf afterwards");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(aliceBack, EtcPocket));
            Assert.AreEqual(0, await GoldOnEnteringAsync(aliceBack));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(bobBack));
            Assert.IsFalse(Listed(bobBack, EtcPocket).Any(slot => slot.Contains($".{WorldData.Seed}.", StringComparison.Ordinal)), "no seed in his bag");
        }

        [TestMethod]
        public async Task MoreThanTheShelfHoldsCannotBeBought()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 84);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            var buyer = await Harness.AddPlayerAsync("Bob", WorldData.Market, 41, 84, c => c.Gold = 1000);
            await using (var alice = await EnterAsync(seller))
            await using (var bob = await EnterAsync(buyer))
            {
                // Three of the five she holds are for sale.
                await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 3, 30));
                await LookAsync(bob, alice);
                var asked = alice.Mark();
                var refused = bob.Mark();

                await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 4");

                Assert.IsTrue(await bob.NoneSinceAsync(refused, p => TestClient.HeaderOf(p) is "ivn" or "gold", TimeSpan.FromSeconds(1)),
                    "nothing reaches the buyer");
                Assert.IsFalse(alice.Since(asked).Any(p => TestClient.HeaderOf(p) is "ivn" or "gold"), "the seller is told of no sale");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(aliceBack, EtcPocket));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(bobBack));
        }

        [TestMethod]
        public async Task OnlyWhatIsOnTheShelfIsSoldOutOfAStack()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 40, 88);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            var buyer = await Harness.AddPlayerAsync("Bob", WorldData.Market, 41, 88, c => c.Gold = 1000);
            await using var carol = await Harness.EnterAsync("Carol", WorldData.Market, 40, 89);
            await using (var alice = await EnterAsync(seller))
            await using (var bob = await EnterAsync(buyer))
            {
                await OpenAsync(alice, carol, "stall", (EtcPocket, 0, 3, 30));
                await LookAsync(bob, alice);
                var sold = alice.Mark();
                var seen = carol.Mark();

                await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 3");

                await alice.ExpectSinceAsync(sold, Slot(EtcPocket, 0, WorldData.Seed, 2), "the two seeds she kept");
                await alice.ExpectSinceAsync(sold, Gold(3 * 30), "her gold after the sale");
                await carol.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same(new[] { $"0.{WorldData.Seed}.2" }, Listed(aliceBack, EtcPocket));
            Same(new[] { $"0.{WorldData.Seed}.3" }, Listed(bobBack, EtcPocket));
        }

        [TestMethod]
        public async Task ASellerCannotBuyFromHerOwnShop()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 40, c => c.Gold = 1000);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(seller))
            {
                await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 47, 40);
                await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 5, 30));
                await alice.SendAsync($"npc_req {Player} {alice.CharacterId}");
                var asked = alice.Mark();

                await alice.SendAsync($"buy {Player} {alice.CharacterId} 0 2");

                Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "gold", TimeSpan.FromSeconds(1)),
                    "nothing is sold");
            }

            await using var back = await Harness.ComeBackAsync(seller);
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(back, EtcPocket));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task AShopClosedByItsSellerIsGoneForTheMapAndSellsNothing()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 44);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 47, 44, c => c.Gold = 1000);
            await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 5, 30));
            await LookAsync(bob, alice);
            var closed = alice.Mark();
            var seen = bob.Mark();

            await alice.SendAsync("m_shop 1");

            await bob.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");
            await bob.ExpectSinceAsync(seen, Flag(alice, 0), "the flag taken down");
            await bob.ExpectSinceAsync(seen, Sitting(alice, false), "the seller standing up");
            await alice.ExpectSinceAsync(closed, Sitting(alice, false), "herself standing up");
            var asked = alice.Mark();
            var tried = bob.Mark();

            await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 1");

            Assert.IsTrue(await bob.NoneSinceAsync(tried, p => TestClient.HeaderOf(p) is "ivn" or "gold", TimeSpan.FromSeconds(1)), "nothing is bought");
            Assert.IsFalse(alice.Since(asked).Any(p => TestClient.HeaderOf(p) is "ivn" or "gold"), "nothing is sold");
        }

        [TestMethod]
        public async Task AShopCanBeOpenedAgainAfterItWasClosed()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 48);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 47, 48, c => c.Gold = 1000);
            await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 5, 30));
            var seen = bob.Mark();
            await alice.SendAsync("m_shop 1");
            await bob.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");

            await OpenAsync(alice, bob, "bargains", (EtcPocket, 0, 5, 20));

            Same(new[] { $"{EtcPocket}.0.{WorldData.Seed}.5.20" }, OnShelf(await LookAsync(bob, alice)));
            var bought = bob.Mark();
            await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 1");
            await bob.ExpectSinceAsync(bought, Gold(1000 - 20), "his gold at the new price");
        }

        [TestMethod]
        public async Task ASellerWhoLeavesTheWorldKeepsWhatWasNotSold()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 52);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 47, 52, c => c.Gold = 1000);
            long sellerId;
            await using (var alice = await EnterAsync(seller))
            {
                sellerId = alice.CharacterId;
                await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 5, 30));
                await LookAsync(bob, alice);
            }

            await bob.ExpectAsync(p => p == $"out {Player} {sellerId}", "the seller leaving");
            var tried = bob.Mark();
            await bob.SendAsync($"buy {Player} {sellerId} 0 1");
            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.AreEqual(string.Empty, string.Join(" / ", bob.Since(tried).Where(p => TestClient.HeaderOf(p) is "ivn" or "gold" or "s_memoi" or "n_inv")),
                "nothing is bought from someone who has left");

            var returned = bob.Mark();
            await using var back = await Harness.ComeBackAsync(seller);
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(back, EtcPocket));
            Assert.AreEqual(0, await GoldOnEnteringAsync(back));
            await bob.ExpectSinceAsync(returned, p => p.StartsWith("in 1 ", StringComparison.Ordinal) && p.Split(' ')[4] == sellerId.ToString(CultureInfo.InvariantCulture),
                "the seller back on the map");
            Assert.IsTrue(await bob.NoneSinceAsync(returned, p => p.StartsWith($"shop {Player} {sellerId} {ShopSign} ", StringComparison.Ordinal)
                || p == $"pflag {Player} {sellerId} {ShopSign}", TimeSpan.FromSeconds(1)), "she comes back without a shop");
        }

        [TestMethod]
        public async Task SomeoneArrivingLaterIsShownTheShopUntilItCloses()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 56);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 47, 56);
            await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 5, 30));

            await using (var carol = await Harness.EnterAsync("Carol", WorldData.Market, 46, 57))
            {
                await carol.ExpectSinceAsync(0, Sign(alice, "stall"), "the sign");
                await carol.ExpectSinceAsync(0, Flag(alice, ShopSign), "the flag");
                Same(new[] { $"{EtcPocket}.0.{WorldData.Seed}.5.30" }, OnShelf(await LookAsync(carol, alice)));
            }

            var seen = bob.Mark();
            await alice.SendAsync("m_shop 1");
            await bob.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");

            await using var dave = await Harness.EnterAsync("Dave", WorldData.Market, 46, 57);
            Assert.IsTrue(await dave.NoneSinceAsync(0, p => AnySign(alice)(p) || Flag(alice, ShopSign)(p), TimeSpan.FromSeconds(1)),
                "nobody arriving afterwards is shown a shop");
        }

        [TestMethod]
        public async Task AWeaponSoldKeepsWhatWasDoneToItAndExistsOnce()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 60);
            await Harness.GiveAsync(seller, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5, Upgrade = 7 },
                NoscorePocketType.Equipment);
            var buyer = await Harness.AddPlayerAsync("Bob", WorldData.Market, 47, 60, c => c.Gold = 5000);
            await using (var alice = await EnterAsync(seller))
            await using (var bob = await EnterAsync(buyer))
            {
                await OpenAsync(alice, bob, "smithy", (EquipmentPocket, 0, 1, 2000));
                Same(new[] { $"{EquipmentPocket}.0.{WorldData.Blade}.5.7.2000" }, OnShelf(await LookAsync(bob, alice)));
                var sold = alice.Mark();
                var bought = bob.Mark();

                await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 1");

                await bob.ExpectSinceAsync(bought, p => p.StartsWith($"ivn {EquipmentPocket} 0.{WorldData.Blade}.5.7.", StringComparison.Ordinal),
                    "the blade in his bag, as it was");
                await bob.ExpectSinceAsync(bought, Gold(5000 - 2000), "his gold after paying");
                await alice.ExpectSinceAsync(sold, EmptySlot(EquipmentPocket, 0), "her emptied slot");
                await alice.ExpectSinceAsync(sold, Gold(2000), "her gold after the sale");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same([], Listed(aliceBack, EquipmentPocket), "her bag when she comes back");
            Assert.IsTrue(bobBack.Received.Any(p => p.StartsWith($"inv {EquipmentPocket} 0.{WorldData.Blade}.5.7.", StringComparison.Ordinal)),
                "his blade when he comes back, as it was");
            await using var context = Harness.Database.CreateContext();
            var held = await context.Set<InventoryItemInstance>().AsNoTracking()
                .Where(i => (i.CharacterId == seller.CharacterId || i.CharacterId == buyer.CharacterId) && i.ItemInstance.ItemVNum == WorldData.Blade)
                .Select(i => i.CharacterId)
                .ToArrayAsync();
            Assert.AreEqual(buyer.CharacterId.ToString(CultureInfo.InvariantCulture), string.Join(' ', held), "who holds a blade");
        }

        [TestMethod]
        public async Task ASaleThatWouldTakeTheSellerPastTheMostGoldIsRefused()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 64, c => c.Gold = MostGold - 10);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            var buyer = await Harness.AddPlayerAsync("Bob", WorldData.Market, 47, 64, c => c.Gold = 1000);
            await using (var alice = await EnterAsync(seller))
            await using (var bob = await EnterAsync(buyer))
            {
                await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 5, 30));
                await LookAsync(bob, alice);
                var asked = alice.Mark();
                var refused = bob.Mark();

                await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 1");

                await bob.ExpectSinceAsync(refused, Memo(3, SellerWouldHoldTooMuchGold), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "gold", TimeSpan.FromSeconds(1)),
                    "the seller is told of no sale");
                Assert.IsFalse(bob.Since(refused).Any(p => TestClient.HeaderOf(p) is "ivn" or "gold"), "nothing reaches the buyer");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Assert.AreEqual(MostGold - 10, await GoldOnEnteringAsync(aliceBack));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(bobBack));
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(aliceBack, EtcPocket));
        }

        [TestMethod]
        public async Task TwoBuyersCannotBothBuyTheLastOne()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 76);
            await Harness.GiveAsync(seller, WorldData.Gem, 1);
            var first = await Harness.AddPlayerAsync("Bob", WorldData.Market, 47, 76, c => c.Gold = 1000);
            var second = await Harness.AddPlayerAsync("Carol", WorldData.Market, 46, 77, c => c.Gold = 1000);
            await using (var alice = await EnterAsync(seller))
            await using (var bob = await EnterAsync(first))
            await using (var carol = await EnterAsync(second))
            {
                await OpenAsync(alice, bob, "stall", (EtcPocket, 0, 1, 400));
                await LookAsync(bob, alice);
                await LookAsync(carol, alice);
                var bought = bob.Mark();
                await bob.SendAsync($"buy {Player} {alice.CharacterId} 0 1");
                await bob.ExpectSinceAsync(bought, Gold(600), "his gold after paying");
                var late = carol.Mark();

                await carol.SendAsync($"buy {Player} {alice.CharacterId} 0 1");

                Assert.IsTrue(await carol.NoneSinceAsync(late, p => TestClient.HeaderOf(p) is "ivn" or "gold", TimeSpan.FromSeconds(1)),
                    "nothing is left for the second buyer");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(first);
            await using var carolBack = await Harness.ComeBackAsync(second);
            Assert.AreEqual(400, await GoldOnEnteringAsync(aliceBack));
            Assert.AreEqual(600, await GoldOnEnteringAsync(bobBack));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(carolBack));
            Same(new[] { $"0.{WorldData.Gem}.1" }, Listed(bobBack, EtcPocket));
            Same([], Listed(carolBack, EtcPocket));
            Same([], Listed(aliceBack, EtcPocket));
        }
    }
}
