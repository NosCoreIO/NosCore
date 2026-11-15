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
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Auction;
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class BazaarTests
    {
        private const byte EquipmentPocket = 0;
        private const byte EtcPocket = 2;

        // Without a medal a listing worth up to 100 000 gold costs this much to put up.
        private const long ListingTax = 500;

        private static readonly RequestNpcPacket TalkToTheBroker = new() { Type = VisualType.Npc, TargetId = WorldData.Broker };

        private static readonly NrunPacket OpenTheBazaar = new()
        {
            Runner = NrunRunnerType.OpenNosBazaar, Type = 0, VisualType = VisualType.Npc, VisualId = WorldData.Broker
        };

        private static Func<IvnPacket, bool> Bag(short slot, short amount) =>
            packet => packet.Type == PocketType.Etc
                && packet.IvnSubPackets?.Any(item => item is { Slot: var at, VNum: WorldData.Seed, RareAmount: var held } && at == slot && held == amount) == true;

        private static Func<IvnPacket, bool> EmptySlot(short slot) =>
            packet => packet.Type == PocketType.Etc
                && packet.IvnSubPackets?.Any(item => item is { Slot: var at, VNum: 0, RareAmount: 0 } && at == slot) == true;

        private static Func<GoldPacket, bool> Gold(long gold) => packet => packet.Gold == gold && packet.Unknown == 0;

        private static Func<RCRegPacket, bool> Listed => packet => packet.Type == VisualType.Player;

        private static Func<ModaliPacket, bool> Refused(Game18NConstString message) => packet => packet.Type == 1 && packet.Message == message;

        private static bool MovesGoodsOrGold(IPacket packet) => packet is RCRegPacket or IvnPacket or GoldPacket;

        private static bool IsAPurchase(IPacket packet) => packet is GoldPacket or IvnPacket or RCBuyPacket;

        private static CRegPacket Register(short amount, long price, bool package = false, byte pocket = EtcPocket, byte slot = 0) => new()
        {
            Type = 0, Inventory = pocket, Slot = slot, Durability = BazaarListingDuration.OneDay, IsPackage = package, Amount = amount, Price = price,
        };

        private static CBuyPacket Buy(long id, short amount, long price, short vnum = WorldData.Seed) =>
            new() { BazaarId = id, VNum = vnum, Amount = amount, Price = price };

        private static CScalcPacket Collect(long id, short sold, short listed, long price) =>
            new() { BazaarId = id, VNum = WorldData.Seed, Amount = sold, MaxAmount = listed, Price = price };

        private static CBListPacket SearchFor(short vnum) => new() { ItemVNumFilter = [vnum] };

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
            await client.SendAsync(TalkToTheBroker);
            await client.ExpectAsync<NpcReqPacket>(p => p.Type == VisualType.Npc && p.TargetId == WorldData.Broker && p.Data == WorldData.BrokerDialog,
                "the broker's dialog");
            await client.SendAsync(OpenTheBazaar);
            await client.ExpectAsync<WopenPacket>(p => p.Type == WindowType.NosBazaar, "the bazaar window");
        }

        private static async Task ListAsync(TestClient seller, short amount, long price, bool package = false)
        {
            await seller.SendAsync(Register(amount, price, package));
            await seller.ExpectAsync(Listed, "the listing confirmed");
        }

        private static async Task<RcbListPacket.RcbListElementPacket?> SearchAsync(TestClient client, long sellerId)
        {
            // A purchase is answered with a list of its own, so only what follows this request counts.
            var asked = client.Mark();
            await client.SendAsync(SearchFor(WorldData.Seed));
            return Listing(await client.ExpectSinceAsync<RcbListPacket>(asked, null, "rc_blist"), sellerId);
        }

        private static RcbListPacket.RcbListElementPacket? Listing(RcbListPacket list, long sellerId) =>
            list.Items?.FirstOrDefault(entry => entry?.OwnerId == sellerId);

        private static async Task<long> FindAsync(TestClient client, long sellerId)
        {
            var listing = await SearchAsync(client, sellerId);
            Assert.IsNotNull(listing, "the listing is found");
            return listing.AuctionId;
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

            await alice.ExpectSinceAsync(listed, Listed, "the listing confirmed");
            await alice.ExpectSinceAsync(listed, EmptySlot(0), "the emptied slot");
            await alice.ExpectSinceAsync(listed, Gold(1000 - ListingTax), "the gold left after the tax");
            Assert.AreEqual(1000 - ListingTax, (await Harness.StoredAsync(seller.CharacterId)).Gold);
            Assert.AreEqual(0, await StoredSeedsAsync(seller.CharacterId), "nothing is left in the stored bag");
            var stored = (await StoredListingsAsync(seller.CharacterId)).Single();
            Assert.AreEqual(5, stored.Amount);
            Assert.AreEqual(100, stored.Price);
            Assert.AreEqual(5, stored.ItemInstance.Amount);
            Assert.AreEqual(WorldData.Seed, stored.ItemInstance.ItemVNum);
        }

        [TestMethod]
        public async Task ListingPartOfAStackLeavesTheRestInTheBag()
        {
            var seller = await SellerAsync(1000, 5);
            await using var alice = await EnterAsync(seller);
            await OpenBazaarAsync(alice);
            var listed = alice.Mark();

            await alice.SendAsync(Register(3, 100));

            await alice.ExpectSinceAsync(listed, Listed, "the listing confirmed");
            Assert.AreEqual(2, await StoredSeedsAsync(seller.CharacterId), "two are still stored in the bag");
            Assert.AreEqual(3, (await StoredListingsAsync(seller.CharacterId)).Single().ItemInstance.Amount);
            await alice.ExpectSinceAsync(listed, Bag(0, 2), "the two left in the bag");
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
            Assert.AreEqual((seller.Name, (long)WorldData.Seed, (short)5, false, 100L), (found.OwnerName, found.ItemId, found.Amount, found.IsPackage, found.Price));
            var id = found.AuctionId;

            var bought = bob.Mark();
            await bob.SendAsync(Buy(id, 2, 100));

            await bob.ExpectSinceAsync(bought, Gold(800), "Bob's gold after buying two");
            await bob.ExpectSinceAsync(bought, Bag(0, 2), "the two bought");
            await bob.ExpectSinceAsync<RCBuyPacket>(bought, p => p is { Type: VisualType.Player, VNum: WorldData.Seed, Amount: 2, Price: 100 } && p.Owner == seller.Name,
                "the purchase confirmed");
            Assert.AreEqual(800, (await Harness.StoredAsync(buyer.CharacterId)).Gold);
            Assert.AreEqual(2, await StoredSeedsAsync(buyer.CharacterId));
            Assert.AreEqual(3, (await StoredListingsAsync(seller.CharacterId)).Single().ItemInstance.Amount, "three are still for sale");

            await alice.SendAsync(new CSListPacket { Index = 0, Filter = 0 });
            var own = (await alice.ExpectAsync<RcsListPacket>()).Items?.Single(e => e?.AuctionId == id);
            Assert.AreEqual(((long)WorldData.Seed, 2L, 5L, false, 100L), (own?.ItemId, own?.SoldAmount, own?.Amount, own?.IsPackage, own?.Price), "two of five are sold");

            // The seller takes the gold for what was sold, less a tenth, and what was not sold.
            var collected = alice.Mark();
            await alice.SendAsync(Collect(id, 2, 5, 100));

            await alice.ExpectSinceAsync(collected, Gold(1000 - ListingTax + 180), "Alice's gold after collecting");
            await alice.ExpectSinceAsync(collected, Bag(0, 3), "the three unsold");
            await alice.ExpectSinceAsync<RCScalcPacket>(collected, p => p is { Type: VisualType.Player, Price: 100 }, "the collection confirmed");
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
            await bob.SendAsync(Buy(id, 5, 100));

            await bob.ExpectSinceAsync(bought, Gold(500), "Bob's gold after buying five");
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

            await alice.SendAsync(Collect(id, 0, 5, 100));

            await alice.ExpectSinceAsync(collected, Bag(0, 5), "all five back in the bag");
            await alice.ExpectSinceAsync(collected, Gold(1000 - ListingTax), "the gold, still without the tax");
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

            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, MovesGoodsOrGold, TimeSpan.FromSeconds(1)));
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

            await alice.ExpectSinceAsync<MsgiPacket>(asked, p => p is { Type: MessageType.Default, Message: Game18NConstString.NotEnoughGold }, "the not-enough-gold notice");
            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, p => p is RCRegPacket or IvnPacket, TimeSpan.FromSeconds(1)),
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
            await bob.SendAsync(Buy(id, 5, 100));
            await bob.ExpectSinceAsync(bought, Gold(500), "Bob's gold after buying five");
            await bob.ExpectSinceAsync<RCBuyPacket>(bought, null, "the purchase confirmed");
            Assert.IsNull(await SearchAsync(bob, seller.CharacterId), "a listing with nothing left is no longer found");
            var collected = alice.Mark();

            await alice.SendAsync(Collect(id, 5, 5, 100));

            await alice.ExpectSinceAsync(collected, Gold(1000 - ListingTax + 450), "Alice's gold after collecting");
            Assert.AreEqual(1000 - ListingTax + 450, (await Harness.StoredAsync(seller.CharacterId)).Gold);
            Assert.AreEqual(0, (await StoredListingsAsync(seller.CharacterId)).Length, "the listing is gone");
            Assert.IsTrue(await alice.NoneSinceAsync<IvnPacket>(collected, null, TimeSpan.FromSeconds(1)), "nothing comes back");
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
            await alice.SendAsync(Register(1, 300, pocket: EquipmentPocket));
            await alice.ExpectAsync(Listed, "the listing confirmed");
            var buyer = await AtTheBrokerAsync("Bob", 1000);
            await using var bob = await EnterAsync(buyer);
            await OpenBazaarAsync(bob);
            var asked = bob.Mark();
            await bob.SendAsync(SearchFor(WorldData.Blade));
            var found = Listing(await bob.ExpectSinceAsync<RcbListPacket>(asked, null, "rc_blist"), seller.CharacterId);
            Assert.IsNotNull(found, "the listing is found");
            Assert.AreEqual((5L, 7L), (found.Rarity, found.Upgrade));
            var bought = bob.Mark();

            await bob.SendAsync(Buy(found.AuctionId, 1, 300, WorldData.Blade));

            await bob.ExpectSinceAsync(bought, Gold(700), "Bob's gold after buying");
            await bob.ExpectSinceAsync<IvnPacket>(bought, p => p.Type == PocketType.Equipment
                && p.IvnSubPackets?.Any(item => item is { Slot: 0, VNum: WorldData.Blade, RareAmount: 5, UpgradeDesign: 7 }) == true, "the weapon as it was");
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
                await alice.SendAsync(Register(1, 100, slot: (byte)slot));
                await alice.ExpectAsync(Listed, "the listing confirmed");
            }

            var asked = alice.Mark();
            await alice.SendAsync(Register(1, 100, slot: 10));

            await alice.ExpectSinceAsync(asked, Refused(Game18NConstString.ListedMaxItemsNumber), "the too-many-listings notice");
            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, MovesGoodsOrGold, TimeSpan.FromSeconds(1)));
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

            var notice = await alice.ExpectSinceAsync(asked, Refused(Game18NConstString.NotExceedMaxPrice), "the price-too-high notice");
            Assert.AreEqual(4, notice.ArgumentType);
            Assert.AreEqual(1000000L, Convert.ToInt64(notice.Game18NArguments[0], CultureInfo.InvariantCulture), "the highest price allowed");
            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, MovesGoodsOrGold, TimeSpan.FromSeconds(1)));
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

            await alice.SendAsync(Buy(id, 1, 100));

            await alice.ExpectSinceAsync(asked, Refused(Game18NConstString.OfferUpdated), "the offer-changed notice");
            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, IsAPurchase, TimeSpan.FromSeconds(1)));
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

            await bob.SendAsync(Buy(id, 2, 100));

            await bob.ExpectSinceAsync(asked, Refused(Game18NConstString.InsufficientGoldAvailable), "the not-enough-gold notice");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(asked, IsAPurchase, TimeSpan.FromSeconds(1)));
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

            await bob.SendAsync(Buy(id, 2, 1));

            await bob.ExpectSinceAsync(asked, Refused(Game18NConstString.OfferUpdated), "the offer-changed notice");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(asked, IsAPurchase, TimeSpan.FromSeconds(1)));
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

            await bob.SendAsync(Buy(id, 2, 100));

            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(asked, IsAPurchase, TimeSpan.FromSeconds(1)),
                "a part of a package is not sold");
            Assert.AreEqual(5, (await StoredListingsAsync(seller.CharacterId)).Single().ItemInstance.Amount);

            var bought = bob.Mark();
            await bob.SendAsync(Buy(id, 5, 100));

            await bob.ExpectSinceAsync(bought, Gold(500), "Bob's gold after buying the package");
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

            await alice.SendAsync(new CModPacket { BazaarId = id, VNum = WorldData.Seed, Amount = 5, NewPrice = 50 });
            await alice.ExpectAsync<RcsListPacket>();

            var buyer = await AtTheBrokerAsync("Bob", 1000);
            await using var bob = await EnterAsync(buyer);
            await OpenBazaarAsync(bob);
            var found = await SearchAsync(bob, seller.CharacterId);
            Assert.IsNotNull(found, "the listing is found");
            Assert.AreEqual(50, found.Price, "at its new price");
            Assert.AreEqual(50, (await StoredListingsAsync(seller.CharacterId)).Single().Price);
            var bought = bob.Mark();
            await bob.SendAsync(Buy(id, 5, 50));
            await bob.ExpectSinceAsync(bought, Gold(750), "Bob's gold after buying five at fifty");
        }
    }
}
