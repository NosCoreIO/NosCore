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
    public class BazaarTests
    {
        private const int EquipmentPocket = 0;
        private const int EtcPocket = 2;
        private const int Npc = 2;
        private const int OpenBazaar = 60;
        private const int BazaarWindow = 32;

        // Without a medal a listing worth up to 100 000 gold costs this much to put up.
        private const long ListingTax = 500;

        private static Func<string, bool> Bag(int slot, int amount) =>
            packet => packet.StartsWith($"ivn {EtcPocket} {slot}.{WorldData.Seed}.{amount}.", StringComparison.Ordinal);

        private static Func<string, bool> EmptySlot(int slot) =>
            packet => packet.StartsWith($"ivn {EtcPocket} {slot}.0.0.", StringComparison.Ordinal);

        // c_reg <type> <pocket> <slot> 0 0 <duration: 1 is a day> <package> <amount> <price each> <tax> <medal>
        private static string Register(int amount, long price, bool package = false) =>
            $"c_reg 0 {EtcPocket} 0 0 0 1 {(package ? 1 : 0)} {amount} {price} 0 0";

        private static async Task<TestClient> EnterAsync(Player player, int channel = 0)
        {
            var client = Harness.Client(player, channel: channel);
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

        private static Task<Player> AtTheBrokerAsync(string name, long gold) =>
            Harness.AddPlayerAsync(name, x: 76, y: 117, configure: c => c.Gold = gold);

        private static async Task<Player> SellerAsync(long gold, short seeds)
        {
            var seller = await AtTheBrokerAsync("Alice", gold);
            await Harness.GiveAsync(seller, WorldData.Seed, seeds);
            return seller;
        }

        private static async Task OpenBazaarAsync(TestClient client)
        {
            await client.SendAsync($"npc_req {Npc} {WorldData.Broker}");
            await client.ExpectAsync(p => p == $"npc_req {Npc} {WorldData.Broker} {WorldData.BrokerDialog}", "the broker's dialog");
            await client.SendAsync($"n_run {OpenBazaar} 0 {Npc} {WorldData.Broker}");
            await client.ExpectAsync(p => p.StartsWith($"wopen {BazaarWindow} ", StringComparison.Ordinal), "the bazaar window");
        }

        private static async Task ListAsync(TestClient seller, int amount, long price, bool package = false)
        {
            await seller.SendAsync(Register(amount, price, package));
            await seller.ExpectAsync(p => p == "rc_reg 1", "the listing confirmed");
        }

        // rc_blist <page>, then a listing each: id|seller|seller name|vnum|amount|package|price|minutes left|...
        private static async Task<string[]?> SearchAsync(TestClient client, long sellerId)
        {
            // A purchase is answered with a list of its own, so only what follows this request counts.
            var asked = client.Mark();
            await client.SendAsync($"c_blist  0 0 0 0 0 0 0 0 0 {WorldData.Seed}");
            return Listing(await client.ExpectSinceAsync(asked, p => TestClient.HeaderOf(p) == "rc_blist", "rc_blist"), sellerId);
        }

        private static string[]? Listing(string list, long sellerId) =>
            list.Split(' ').Skip(2).Select(entry => entry.Split('|'))
                .FirstOrDefault(entry => entry.Length > 7 && entry[1] == sellerId.ToString(CultureInfo.InvariantCulture));

        private static async Task<long> FindAsync(TestClient client, long sellerId)
        {
            var listing = await SearchAsync(client, sellerId);
            Assert.IsNotNull(listing, "the listing is found");
            return long.Parse(listing[0], CultureInfo.InvariantCulture);
        }

        private static async Task<int> StoredSeedsAsync(long characterId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<InventoryItemInstance>().AsNoTracking()
                .Where(i => i.CharacterId == characterId && i.ItemInstance.ItemVNum == WorldData.Seed)
                .SumAsync(i => (int)i.ItemInstance.Amount);
        }

        private static async Task<BazaarItem[]> StoredListingsAsync(long sellerId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<BazaarItem>().AsNoTracking().Include(b => b.ItemInstance).Where(b => b.SellerId == sellerId).ToArrayAsync();
        }

        [TestMethod]
        public async Task AListingTakesTheItemAndTheTaxAndIsStored()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            var listed = alice.Mark();

            await alice.SendAsync(Register(5, 100));

            await alice.ExpectSinceAsync(listed, p => p == "rc_reg 1", "the listing confirmed");
            await alice.ExpectSinceAsync(listed, EmptySlot(0), "the emptied slot");
            await alice.ExpectSinceAsync(listed, p => p == $"gold {1000 - ListingTax} 0", "the gold left after the tax");
            Assert.AreEqual(1000 - ListingTax, (await Harness.StoredAsync(seller.CharacterId)).Gold);
            Assert.AreEqual(0, await StoredSeedsAsync(seller.CharacterId), "nothing is left in the stored bag");
            var stored = (await StoredListingsAsync(seller.CharacterId)).Single();
            Assert.AreEqual(5, stored.Amount);
            Assert.AreEqual(100, stored.Price);
            Assert.AreEqual(5, stored.ItemInstance.Amount);
            Assert.AreEqual(WorldData.Seed, stored.ItemInstance.ItemVNum);
        }

        [TestMethod]
        public async Task ABuyerFindsAListingBuysFromItAndTheSellerCollects()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await ListAsync(alice, 5, 100);
            var buyer = await AtTheBrokerAsync("Bob", 1000);
            await using var bob = await EnterAsync(buyer);
            await OpenBazaarAsync(bob);

            var found = await SearchAsync(bob, seller.CharacterId);

            Assert.IsNotNull(found, "the listing is found");
            Assert.AreEqual($"{seller.Name}|{WorldData.Seed}|5|0|100", string.Join('|', found.Skip(2).Take(5)));
            var id = found[0];

            var bought = bob.Mark();
            await bob.SendAsync($"c_buy {id} {WorldData.Seed} 2 100");

            await bob.ExpectSinceAsync(bought, p => p == "gold 800 0", "Bob's gold after buying two");
            await bob.ExpectSinceAsync(bought, Bag(0, 2), "the two bought");
            await bob.ExpectSinceAsync(bought, p => p.StartsWith($"rc_buy 1 {WorldData.Seed} {seller.Name} 2 100 ", StringComparison.Ordinal),
                "the purchase confirmed");
            Assert.AreEqual(800, (await Harness.StoredAsync(buyer.CharacterId)).Gold);
            Assert.AreEqual(2, await StoredSeedsAsync(buyer.CharacterId));
            Assert.AreEqual(3, (await StoredListingsAsync(seller.CharacterId)).Single().ItemInstance.Amount, "three are still for sale");

            // rc_slist <page>, then a listing each: id|seller|vnum|sold|amount|package|price|status|...
            await alice.SendAsync("c_slist 0 0");
            var own = (await alice.ExpectAsync("rc_slist")).Split(' ').Skip(2).Select(e => e.Split('|')).Single(e => e[0] == id);
            Assert.AreEqual($"{WorldData.Seed}|2|5|0|100", string.Join('|', own.Skip(2).Take(5)), "two of five are sold");

            // The seller takes the gold for what was sold, less a tenth, and what was not sold.
            var collected = alice.Mark();
            await alice.SendAsync($"c_scalc {id} {WorldData.Seed} 2 5 100");

            await alice.ExpectSinceAsync(collected, p => p == $"gold {1000 - ListingTax + 180} 0", "Alice's gold after collecting");
            await alice.ExpectSinceAsync(collected, Bag(0, 3), "the three unsold");
            await alice.ExpectSinceAsync(collected, p => p.StartsWith("rc_scalc 1 100 ", StringComparison.Ordinal), "the collection confirmed");
            Assert.AreEqual(1000 - ListingTax + 180, (await Harness.StoredAsync(seller.CharacterId)).Gold);
            Assert.AreEqual(3, await StoredSeedsAsync(seller.CharacterId));
            Assert.AreEqual(0, (await StoredListingsAsync(seller.CharacterId)).Length, "the listing is gone");
            Assert.IsNull(await SearchAsync(bob, seller.CharacterId), "and is no longer found");
        }

        [TestMethod]
        public async Task AListingIsFoundAndBoughtFromAnotherChannel()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await ListAsync(alice, 5, 100);
            var buyer = await AtTheBrokerAsync("Bob", 1000);
            await using var bob = await EnterAsync(buyer, channel: 1);
            await OpenBazaarAsync(bob);

            var id = await FindAsync(bob, seller.CharacterId);
            var bought = bob.Mark();
            await bob.SendAsync($"c_buy {id} {WorldData.Seed} 5 100");

            await bob.ExpectSinceAsync(bought, p => p == "gold 500 0", "Bob's gold after buying five");
            await bob.ExpectSinceAsync(bought, Bag(0, 5), "the five bought");
            Assert.AreEqual(500, (await Harness.StoredAsync(buyer.CharacterId)).Gold);
            Assert.AreEqual(5, await StoredSeedsAsync(buyer.CharacterId));
        }

        [TestMethod]
        public async Task AnUnsoldListingIsTakenBackWithoutTheTax()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await ListAsync(alice, 5, 100);
            var id = await FindAsync(alice, seller.CharacterId);
            var collected = alice.Mark();

            await alice.SendAsync($"c_scalc {id} {WorldData.Seed} 0 5 100");

            await alice.ExpectSinceAsync(collected, Bag(0, 5), "all five back in the bag");
            await alice.ExpectSinceAsync(collected, p => p == $"gold {1000 - ListingTax} 0", "the gold, still without the tax");
            Assert.AreEqual(5, await StoredSeedsAsync(seller.CharacterId));
            Assert.AreEqual(1000 - ListingTax, (await Harness.StoredAsync(seller.CharacterId)).Gold);
            Assert.AreEqual(0, (await StoredListingsAsync(seller.CharacterId)).Length, "the listing is gone");
        }

        [TestMethod]
        public async Task NothingIsListedWithoutTheBazaarOpen()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            var asked = alice.Mark();

            await alice.SendAsync(Register(5, 100));

            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "rc_reg" or "ivn" or "gold", TimeSpan.FromSeconds(1)));
            Assert.AreEqual(0, (await StoredListingsAsync(seller.CharacterId)).Length);
        }

        [TestMethod]
        public async Task AListingNeedsTheGoldForItsTax()
        {
            var seller = await SellerAsync(ListingTax - 1, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            var asked = alice.Mark();

            await alice.SendAsync(Register(5, 100));

            // 10 is the client's not-enough-gold line.
            await alice.ExpectSinceAsync(asked, p => p.StartsWith("msgi 0 10 ", StringComparison.Ordinal), "the not-enough-gold notice");
            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "rc_reg" or "ivn", TimeSpan.FromSeconds(1)),
                "the item stays in the bag");
            Assert.AreEqual(0, (await StoredListingsAsync(seller.CharacterId)).Length);
        }

        [TestMethod]
        public async Task AListingSoldOutPaysTheSellerAndReturnsNothing()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await ListAsync(alice, 5, 100);
            var buyer = await AtTheBrokerAsync("Bob", 1000);
            await using var bob = await EnterAsync(buyer);
            await OpenBazaarAsync(bob);
            var id = await FindAsync(bob, seller.CharacterId);
            var bought = bob.Mark();
            await bob.SendAsync($"c_buy {id} {WorldData.Seed} 5 100");
            await bob.ExpectSinceAsync(bought, p => p == "gold 500 0", "Bob's gold after buying five");
            await bob.ExpectSinceAsync(bought, p => p.StartsWith("rc_buy ", StringComparison.Ordinal), "the purchase confirmed");
            Assert.IsNull(await SearchAsync(bob, seller.CharacterId), "a listing with nothing left is no longer found");
            var collected = alice.Mark();

            await alice.SendAsync($"c_scalc {id} {WorldData.Seed} 5 5 100");

            await alice.ExpectSinceAsync(collected, p => p == $"gold {1000 - ListingTax + 450} 0", "Alice's gold after collecting");
            Assert.AreEqual(1000 - ListingTax + 450, (await Harness.StoredAsync(seller.CharacterId)).Gold);
            Assert.AreEqual(0, (await StoredListingsAsync(seller.CharacterId)).Length, "the listing is gone");
            Assert.IsTrue(await alice.NoneSinceAsync(collected, p => TestClient.HeaderOf(p) == "ivn", TimeSpan.FromSeconds(1)), "nothing comes back");
            Assert.AreEqual(0, await StoredSeedsAsync(seller.CharacterId));
        }

        [TestMethod]
        public async Task ASoldWeaponKeepsItsRarityAndUpgrade()
        {
            var seller = await AtTheBrokerAsync("Alice", 1000);
            await Harness.GiveAsync(seller, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5, Upgrade = 7 },
                NoscorePocketType.Equipment);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await alice.SendAsync($"c_reg 0 {EquipmentPocket} 0 0 0 1 0 1 300 0 0");
            await alice.ExpectAsync(p => p == "rc_reg 1", "the listing confirmed");
            var buyer = await AtTheBrokerAsync("Bob", 1000);
            await using var bob = await EnterAsync(buyer);
            await OpenBazaarAsync(bob);
            var asked = bob.Mark();
            await bob.SendAsync($"c_blist  0 0 0 0 0 0 0 0 0 {WorldData.Blade}");
            var found = Listing(await bob.ExpectSinceAsync(asked, p => TestClient.HeaderOf(p) == "rc_blist", "rc_blist"), seller.CharacterId);
            Assert.IsNotNull(found, "the listing is found");
            // ...|price|minutes left|0|2|rarity|upgrade|...
            Assert.AreEqual("5|7", $"{found[10]}|{found[11]}");
            var bought = bob.Mark();

            await bob.SendAsync($"c_buy {found[0]} {WorldData.Blade} 1 300");

            await bob.ExpectSinceAsync(bought, p => p == "gold 700 0", "Bob's gold after buying");
            // In the equipment pocket a slot reads slot.vnum.rarity.upgrade.
            await bob.ExpectSinceAsync(bought, p => p.StartsWith($"ivn {EquipmentPocket} 0.{WorldData.Blade}.5.7.", StringComparison.Ordinal), "the weapon as it was");
            await using var context = Harness.Database.CreateContext();
            var stored = await context.Set<InventoryItemInstance>().AsNoTracking()
                .Where(i => i.CharacterId == buyer.CharacterId).Select(i => i.ItemInstance).SingleAsync();
            Assert.IsInstanceOfType<WearableInstance>(stored);
            Assert.AreEqual((WorldData.Blade, (short)5, (byte)7), (stored.ItemVNum, stored.Rare, stored.Upgrade));
        }

        [TestMethod]
        public async Task AnEleventhListingIsRefusedAndItsItemStays()
        {
            var seller = await AtTheBrokerAsync("Alice", 10000);
            for (short slot = 0; slot <= 10; slot++)
            {
                await Harness.GiveAsync(seller, WorldData.Seed, 1, slot);
            }

            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            for (var slot = 0; slot < 10; slot++)
            {
                await alice.SendAsync($"c_reg 0 {EtcPocket} {slot} 0 0 1 0 1 100 0 0");
                await alice.ExpectAsync(p => p == "rc_reg 1", "the listing confirmed");
            }

            var asked = alice.Mark();
            await alice.SendAsync($"c_reg 0 {EtcPocket} 10 0 0 1 0 1 100 0 0");

            // 1840 is the client's notice that the most listings allowed are up.
            await alice.ExpectSinceAsync(asked, p => p.StartsWith("modali 1 1840 ", StringComparison.Ordinal), "the too-many-listings notice");
            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "rc_reg" or "ivn" or "gold", TimeSpan.FromSeconds(1)));
            Assert.AreEqual(10, (await StoredListingsAsync(seller.CharacterId)).Length);
            Assert.AreEqual(1, await StoredSeedsAsync(seller.CharacterId), "the eleventh is still stored in the bag");
        }

        [TestMethod]
        public async Task AListingWorthMoreThanAMillionIsRefused()
        {
            var seller = await SellerAsync(100000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            var asked = alice.Mark();

            await alice.SendAsync(Register(5, 200001));

            // 1842 is the client's notice of the highest price allowed.
            await alice.ExpectSinceAsync(asked, p => p.StartsWith("modali 1 1842 4 1000000 ", StringComparison.Ordinal), "the price-too-high notice");
            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "rc_reg" or "ivn" or "gold", TimeSpan.FromSeconds(1)));
            Assert.AreEqual(0, (await StoredListingsAsync(seller.CharacterId)).Length);
        }

        [TestMethod]
        public async Task ASellerCannotBuyTheirOwnListing()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await ListAsync(alice, 5, 100);
            var id = await FindAsync(alice, seller.CharacterId);
            var asked = alice.Mark();

            await alice.SendAsync($"c_buy {id} {WorldData.Seed} 1 100");

            // 1831 is the client's notice that the offer has changed.
            await alice.ExpectSinceAsync(asked, p => p.StartsWith("modali 1 1831 ", StringComparison.Ordinal), "the offer-changed notice");
            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "gold" or "ivn" or "rc_buy", TimeSpan.FromSeconds(1)));
            Assert.AreEqual(5, (await StoredListingsAsync(seller.CharacterId)).Single().ItemInstance.Amount);
        }

        [TestMethod]
        public async Task ABuyerWithoutTheGoldBuysNothing()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await ListAsync(alice, 5, 100);
            var buyer = await AtTheBrokerAsync("Bob", 199);
            await using var bob = await EnterAsync(buyer);
            await OpenBazaarAsync(bob);
            var id = await FindAsync(bob, seller.CharacterId);
            var asked = bob.Mark();

            await bob.SendAsync($"c_buy {id} {WorldData.Seed} 2 100");

            // 1830 is the client's notice of not having the gold.
            await bob.ExpectSinceAsync(asked, p => p.StartsWith("modali 1 1830 ", StringComparison.Ordinal), "the not-enough-gold notice");
            Assert.IsTrue(await bob.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "gold" or "ivn" or "rc_buy", TimeSpan.FromSeconds(1)));
            Assert.AreEqual(0, await StoredSeedsAsync(buyer.CharacterId));
            Assert.AreEqual(5, (await StoredListingsAsync(seller.CharacterId)).Single().ItemInstance.Amount);
        }

        [TestMethod]
        public async Task APriceOtherThanTheListedOneBuysNothing()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await ListAsync(alice, 5, 100);
            var buyer = await AtTheBrokerAsync("Bob", 1000);
            await using var bob = await EnterAsync(buyer);
            await OpenBazaarAsync(bob);
            var id = await FindAsync(bob, seller.CharacterId);
            var asked = bob.Mark();

            await bob.SendAsync($"c_buy {id} {WorldData.Seed} 2 1");

            await bob.ExpectSinceAsync(asked, p => p.StartsWith("modali 1 1831 ", StringComparison.Ordinal), "the offer-changed notice");
            Assert.IsTrue(await bob.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "gold" or "ivn" or "rc_buy", TimeSpan.FromSeconds(1)));
            Assert.AreEqual(1000, (await Harness.StoredAsync(buyer.CharacterId)).Gold);
            Assert.AreEqual(5, (await StoredListingsAsync(seller.CharacterId)).Single().ItemInstance.Amount);
        }

        [TestMethod]
        public async Task APackageIsOnlySoldWhole()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await ListAsync(alice, 5, 100, package: true);
            var buyer = await AtTheBrokerAsync("Bob", 1000);
            await using var bob = await EnterAsync(buyer);
            await OpenBazaarAsync(bob);
            var id = await FindAsync(bob, seller.CharacterId);
            var asked = bob.Mark();

            await bob.SendAsync($"c_buy {id} {WorldData.Seed} 2 100");

            Assert.IsTrue(await bob.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "gold" or "ivn" or "rc_buy", TimeSpan.FromSeconds(1)),
                "a part of a package is not sold");
            Assert.AreEqual(5, (await StoredListingsAsync(seller.CharacterId)).Single().ItemInstance.Amount);

            var bought = bob.Mark();
            await bob.SendAsync($"c_buy {id} {WorldData.Seed} 5 100");

            await bob.ExpectSinceAsync(bought, p => p == "gold 500 0", "Bob's gold after buying the package");
            await bob.ExpectSinceAsync(bought, Bag(0, 5), "the package");
        }

        [TestMethod]
        public async Task AChangedPriceIsThePriceBuyersFindAndPay()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            await ListAsync(alice, 5, 100);
            var id = await FindAsync(alice, seller.CharacterId);

            await alice.SendAsync($"c_mod {id} {WorldData.Seed} 5 50");
            await alice.ExpectAsync("rc_slist");

            var buyer = await AtTheBrokerAsync("Bob", 1000);
            await using var bob = await EnterAsync(buyer);
            await OpenBazaarAsync(bob);
            var found = await SearchAsync(bob, seller.CharacterId);
            Assert.IsNotNull(found, "the listing is found");
            Assert.AreEqual("50", found[6], "at its new price");
            Assert.AreEqual(50, (await StoredListingsAsync(seller.CharacterId)).Single().Price);
            var bought = bob.Mark();
            await bob.SendAsync($"c_buy {id} {WorldData.Seed} 5 50");
            await bob.ExpectSinceAsync(bought, p => p == "gold 750 0", "Bob's gold after buying five at fifty");
        }
    }
}
