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
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class PrivateShopTests
    {
        private const long ShopSign = 501;
        private const byte ShopMenu = 3;
        private const short BagSlots = 48;
        private const long MostGold = 1_000_000_000;

        private static MShopPacket Open(string name, params (PocketType Pocket, short Slot, short Amount, long Price)[] shelf) => new()
        {
            Type = CreateShopPacketType.Open,
            ItemList = shelf.Select(item => (MShopItemSubPacket?)new MShopItemSubPacket { Type = item.Pocket, Slot = item.Slot, Amount = item.Amount, Price = item.Price }).ToList(),
            Name = name,
        };

        private static readonly MShopPacket Close = new() { Type = CreateShopPacketType.Close };

        private static BuyPacket Buy(TestClient seller, short amount) => Buy(seller.CharacterId, amount);

        private static BuyPacket Buy(long sellerId, short amount) =>
            new() { VisualType = VisualType.Player, VisualId = sellerId, Slot = 0, Amount = amount };

        private static RequestNpcPacket LookAt(TestClient seller) => new() { Type = VisualType.Player, TargetId = seller.CharacterId };

        private static Func<IPacket, bool> Sign(TestClient seller, string name) =>
            packet => packet is ShopPacket { VisualType: VisualType.Player, ShopId: ShopSign, MenuType: ShopMenu, ShopType: 0 } shop
                && shop.VisualId == seller.CharacterId && shop.Name == name;

        private static Func<IPacket, bool> AnySign(long sellerId) =>
            packet => packet is ShopPacket { VisualType: VisualType.Player, ShopId: ShopSign } shop && shop.VisualId == sellerId;

        private static Func<IPacket, bool> AnySign(TestClient seller) => AnySign(seller.CharacterId);

        private static Func<IPacket, bool> NoSign(TestClient seller) =>
            packet => packet is ShopPacket { VisualType: VisualType.Player, ShopId: 0, MenuType: 0 } shop && shop.VisualId == seller.CharacterId;

        private static Func<IPacket, bool> Flag(long sellerId, long sign) =>
            packet => packet is PflagPacket { VisualType: VisualType.Player } flag && flag.VisualId == sellerId && flag.Flag == sign;

        private static Func<IPacket, bool> Flag(TestClient seller, long sign) => Flag(seller.CharacterId, sign);

        private static Func<IPacket, bool> Sitting(TestClient who, bool sitting) =>
            packet => packet is RestPacket { VisualType: VisualType.Player } rest && rest.VisualId == who.CharacterId && rest.IsSitting == sitting;

        private static Func<IPacket, bool> AShopShown(TestClient seller) => packet => AnySign(seller)(packet) || Flag(seller, ShopSign)(packet);

        private static Func<NInvPacket, bool> Shelf(TestClient seller) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == seller.CharacterId;

        private static (PocketType Pocket, int Slot, int VNum, int Amount, int Price)[] OnShelf(NInvPacket shelf) =>
            (shelf.Items ?? []).OfType<NInvItemSubPacket>().Select(item => (item.Type, (int)item.Slot, (int)item.VNum, (int)(item.RareAmount ?? 0), item.Price)).ToArray();

        private static bool IsEmpty(IvnSubPacket slot) => slot.VNum is 0 or -1;

        private static Func<IvnPacket, bool> Slot(PocketType pocket, short slot, short vnum, short amount) =>
            packet => packet.Type == pocket && packet.IvnSubPackets?.Any(item => item.Slot == slot && item.VNum == vnum && item.RareAmount == amount) == true;

        private static Func<IvnPacket, bool> EmptySlot(PocketType pocket, short slot) =>
            packet => packet.Type == pocket && packet.IvnSubPackets?.FirstOrDefault()?.Slot == slot && IsEmpty(packet.IvnSubPackets[0]);

        private static Func<GoldPacket, bool> Gold(long gold) => packet => packet.Gold == gold && packet.Unknown == 0;

        private static bool Moves(IPacket packet) => packet is IvnPacket or GoldPacket;

        private static Func<SMemoiPacket, bool> Memo(SMemoType type, Game18NConstString message) =>
            packet => packet.Type == type && packet.Message == message && packet.ArgumentType == 0;

        private static Func<InfoiPacket, bool> Told(Game18NConstString message) => packet => packet.Message == message;

        private static Func<SayiPacket, bool> Said(TestClient who, Game18NConstString message) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == who.CharacterId && packet.Message == message;

        private static Func<MsgiPacket, bool> Announced(Game18NConstString message) => packet => packet.Message == message;

        private static Func<ShopEndPacket, bool> ShopEnded => _ => true;

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

        private static (int Slot, int VNum, int Amount)[] Listed(TestClient client, PocketType pocket) =>
            client.Received.OfType<InvPacket>().Where(p => p.Type == pocket)
                .SelectMany(p => p.IvnSubPackets ?? [])
                .OrderBy(slot => slot.Slot)
                .Select(slot => ((int)slot.Slot, (int)slot.VNum, (int)slot.RareAmount))
                .ToArray();

        // The gold is sent a moment after the character is in the world.
        private static async Task<long> GoldOnEnteringAsync(TestClient client) =>
            (await client.ExpectSinceAsync<GoldPacket>(0, null, "the gold on entering")).Gold;

        private static async Task<(int Slot, int VNum, int Amount)[]> StoredAsync(long characterId, NoscorePocketType pocket)
        {
            await using var context = Harness.Database.CreateContext();
            var rows = await context.Set<InventoryItemInstance>().AsNoTracking()
                .Where(i => i.CharacterId == characterId && i.Type == pocket)
                .OrderBy(i => i.Slot)
                .Select(i => new { i.Slot, i.ItemInstance.ItemVNum, i.ItemInstance.Amount })
                .ToArrayAsync();
            return rows.Select(r => ((int)r.Slot, (int)r.ItemVNum, (int)r.Amount)).ToArray();
        }

        private static void Same((int Slot, int VNum, int Amount)[] expected, (int Slot, int VNum, int Amount)[] actual, string? what = null) =>
            Assert.AreEqual(string.Join(' ', expected), string.Join(' ', actual), what);

        private static void Same((PocketType Pocket, int Slot, int VNum, int Amount, int Price)[] expected,
            (PocketType Pocket, int Slot, int VNum, int Amount, int Price)[] actual, string? what = null) =>
            Assert.AreEqual(string.Join(' ', expected), string.Join(' ', actual), what);

        // The seller opens a shop and someone standing by sees its sign go up.
        private static async Task OpenAsync(TestClient seller, TestClient watcher, string name,
            params (PocketType Pocket, short Slot, short Amount, long Price)[] shelf)
        {
            var opened = watcher.Mark();
            await seller.SendAsync(Open(name, shelf));
            await watcher.ExpectSinceAsync(opened, Sign(seller, name), "the sign of the shop");
        }

        private static async Task<NInvPacket> LookAsync(TestClient buyer, TestClient seller)
        {
            var looked = buyer.Mark();
            await buyer.SendAsync(LookAt(seller));
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

            await alice.SendAsync(Open("stall", (PocketType.Etc, 0, 5, 30)));

            await alice.ExpectSinceAsync(opened, Sign(alice, "stall"), "her own sign");
            await alice.ExpectSinceAsync(opened, Told(Game18NConstString.ShopOpened), "the notice that the shop is open");
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

            await alice.SendAsync(Open("stall", (PocketType.Etc, 0, 5, 30)));

            await alice.ExpectSinceAsync(refused, Told(Game18NConstString.UseCommercialMapToShop), "the refusal");
            await alice.ExpectSinceAsync(refused, ShopEnded, "the end of the shop window");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(seen, p => AShopShown(alice)(p) || Sitting(alice, true)(p),
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

            await alice.SendAsync(Open("stall", (PocketType.Etc, 0, 5, 30)));

            await alice.ExpectSinceAsync(refused, Told(Game18NConstString.OpenShopAwayPortal), "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(seen, AShopShown(alice), TimeSpan.FromSeconds(1)),
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

            await alice.ExpectSinceAsync(refused, Said(alice, Game18NConstString.NoItemToSell), "the refusal");
            await alice.ExpectSinceAsync(refused, ShopEnded, "the end of the shop window");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(seen, AShopShown(alice), TimeSpan.FromSeconds(1)),
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

            await alice.SendAsync(Open("stall", (PocketType.Etc, 0, 5, 30)));

            await alice.ExpectSinceAsync(refused, Said(alice, Game18NConstString.SomeItemsCannotBeTraded), "the refusal");
            await alice.ExpectSinceAsync(refused, ShopEnded, "the end of the shop window");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(seen, AShopShown(alice), TimeSpan.FromSeconds(1)),
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

            await alice.SendAsync(Open("stall", (PocketType.Etc, 0, 5, 30), (PocketType.Etc, 0, 5, 10)));

            await alice.ExpectSinceAsync(refused, ShopEnded, "the end of the shop window");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(seen, AShopShown(alice), TimeSpan.FromSeconds(1)),
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
            await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 4, 30), (PocketType.Etc, 1, 2, 700));

            var shelf = await LookAsync(bob, alice);

            Same([(PocketType.Etc, 0, WorldData.Seed, 4, 30), (PocketType.Etc, 1, WorldData.Gem, 2, 700)], OnShelf(shelf));
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
                await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 5, 30));
                await LookAsync(bob, alice);
                var sold = alice.Mark();
                var bought = bob.Mark();

                await bob.SendAsync(Buy(alice, 2));

                await bob.ExpectSinceAsync(bought, Slot(PocketType.Etc, 0, WorldData.Seed, 2), "the two seeds in his bag");
                await bob.ExpectSinceAsync(bought, Memo(SMemoType.SuccessNpc, Game18NConstString.TradeSuccessfull), "the purchase confirmed");
                await bob.ExpectSinceAsync(bought, Gold(1000 - 2 * 30), "his gold after paying");
                Same([(PocketType.Etc, 0, WorldData.Seed, 3, 30)], OnShelf(await bob.ExpectSinceAsync(bought, Shelf(alice), "the shelf after the sale")));
                await alice.ExpectSinceAsync(sold, Slot(PocketType.Etc, 0, WorldData.Seed, 3), "the three seeds left in her bag");
                await alice.ExpectSinceAsync(sold, Gold(2 * 30), "her gold after the sale");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same([(0, WorldData.Seed, 3)], Listed(aliceBack, PocketType.Etc), "her bag when she comes back");
            Same([(0, WorldData.Seed, 2)], Listed(bobBack, PocketType.Etc), "his bag when he comes back");
            Assert.AreEqual(2 * 30, await GoldOnEnteringAsync(aliceBack), "her gold when she comes back");
            Assert.AreEqual(1000 - 2 * 30, await GoldOnEnteringAsync(bobBack), "his gold when he comes back");
            Same([(0, WorldData.Seed, 3)], await StoredAsync(seller.CharacterId, NoscorePocketType.Etc));
            Same([(0, WorldData.Seed, 2)], await StoredAsync(buyer.CharacterId, NoscorePocketType.Etc));
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
                await OpenAsync(alice, carol, "stall", (PocketType.Etc, 0, 5, 30));
                await LookAsync(bob, alice);
                var sold = alice.Mark();
                var bought = bob.Mark();
                var seen = carol.Mark();

                await bob.SendAsync(Buy(alice, 5));

                await bob.ExpectSinceAsync(bought, Slot(PocketType.Etc, 0, WorldData.Seed, 5), "the five seeds in his bag");
                await bob.ExpectSinceAsync(bought, Gold(1000 - 5 * 30), "his gold after paying");
                await alice.ExpectSinceAsync(sold, EmptySlot(PocketType.Etc, 0), "her emptied slot");
                await alice.ExpectSinceAsync(sold, Gold(5 * 30), "her gold after the sale");
                await alice.ExpectSinceAsync(sold, Sitting(alice, false), "herself standing up");
                await carol.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");
                await carol.ExpectSinceAsync(seen, Flag(alice, 0), "the flag taken down");
                await carol.ExpectSinceAsync(seen, Sitting(alice, false), "the seller standing up");
                await bob.ExpectSinceAsync(bought, NoSign(alice), "the sign taken down for the buyer");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same([], Listed(aliceBack, PocketType.Etc), "her bag when she comes back");
            Same([(0, WorldData.Seed, 5)], Listed(bobBack, PocketType.Etc), "his bag when he comes back");
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
            await OpenAsync(alice, carol, "stall", (PocketType.Etc, 0, 5, 30), (PocketType.Etc, 1, 2, 700));
            await LookAsync(bob, alice);
            var bought = bob.Mark();
            var seen = carol.Mark();

            await bob.SendAsync(Buy(alice, 5));

            Same([(PocketType.Etc, 1, WorldData.Gem, 2, 700)], OnShelf(await bob.ExpectSinceAsync(bought, Shelf(alice), "the shelf after the sale")));
            Assert.IsTrue(await carol.NoneSinceAsync<IPacket>(seen, p => NoSign(alice)(p) || Flag(alice, 0)(p) || Sitting(alice, false)(p),
                TimeSpan.FromSeconds(1)), "the shop stays open");
            Same([(PocketType.Etc, 1, WorldData.Gem, 2, 700)], OnShelf(await LookAsync(carol, alice)), "what someone else is shown");
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
                await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 5, 30));
                await LookAsync(bob, alice);
                var asked = alice.Mark();
                var refused = bob.Mark();

                await bob.SendAsync(Buy(alice, 2));

                await bob.ExpectSinceAsync(refused, Memo(SMemoType.FailNpc, Game18NConstString.NotEnoughGold5), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, Moves, TimeSpan.FromSeconds(1)),
                    "the seller is told of no sale");
                Assert.IsFalse(bob.Since(refused).Any(Moves), "nothing reaches the buyer");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same([(0, WorldData.Seed, 5)], Listed(aliceBack, PocketType.Etc));
            Same([], Listed(bobBack, PocketType.Etc));
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
                await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 5, 30));
                await LookAsync(bob, alice);
                var asked = alice.Mark();
                var refused = bob.Mark();

                await bob.SendAsync(Buy(alice, 5));

                await bob.ExpectSinceAsync(refused, Announced(Game18NConstString.NotEnoughSpace), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, p => Moves(p) || NoSign(alice)(p),
                    TimeSpan.FromSeconds(1)), "the seller is told of no sale and the shop stays open");
                Assert.IsFalse(bob.Since<GoldPacket>(refused).Any(), "the buyer pays nothing");
                Same([(PocketType.Etc, 0, WorldData.Seed, 5, 30)], OnShelf(await LookAsync(bob, alice)), "the shelf afterwards");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same([(0, WorldData.Seed, 5)], Listed(aliceBack, PocketType.Etc));
            Assert.AreEqual(0, await GoldOnEnteringAsync(aliceBack));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(bobBack));
            Assert.IsFalse(Listed(bobBack, PocketType.Etc).Any(slot => slot.VNum == WorldData.Seed), "no seed in his bag");
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
                await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 3, 30));
                await LookAsync(bob, alice);
                var asked = alice.Mark();
                var refused = bob.Mark();

                await bob.SendAsync(Buy(alice, 4));

                Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(refused, Moves, TimeSpan.FromSeconds(1)),
                    "nothing reaches the buyer");
                Assert.IsFalse(alice.Since(asked).Any(Moves), "the seller is told of no sale");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same([(0, WorldData.Seed, 5)], Listed(aliceBack, PocketType.Etc));
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
                await OpenAsync(alice, carol, "stall", (PocketType.Etc, 0, 3, 30));
                await LookAsync(bob, alice);
                var sold = alice.Mark();
                var seen = carol.Mark();

                await bob.SendAsync(Buy(alice, 3));

                await alice.ExpectSinceAsync(sold, Slot(PocketType.Etc, 0, WorldData.Seed, 2), "the two seeds she kept");
                await alice.ExpectSinceAsync(sold, Gold(3 * 30), "her gold after the sale");
                await carol.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same([(0, WorldData.Seed, 2)], Listed(aliceBack, PocketType.Etc));
            Same([(0, WorldData.Seed, 3)], Listed(bobBack, PocketType.Etc));
        }

        [TestMethod]
        public async Task ASellerCannotBuyFromHerOwnShop()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 40, c => c.Gold = 1000);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(seller))
            {
                await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 47, 40);
                await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 5, 30));
                await alice.SendAsync(LookAt(alice));
                var asked = alice.Mark();

                await alice.SendAsync(Buy(alice, 2));

                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, Moves, TimeSpan.FromSeconds(1)),
                    "nothing is sold");
            }

            await using var back = await Harness.ComeBackAsync(seller);
            Same([(0, WorldData.Seed, 5)], Listed(back, PocketType.Etc));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task AShopClosedByItsSellerIsGoneForTheMapAndSellsNothing()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 44);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 47, 44, c => c.Gold = 1000);
            await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 5, 30));
            await LookAsync(bob, alice);
            var closed = alice.Mark();
            var seen = bob.Mark();

            await alice.SendAsync(Close);

            await bob.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");
            await bob.ExpectSinceAsync(seen, Flag(alice, 0), "the flag taken down");
            await bob.ExpectSinceAsync(seen, Sitting(alice, false), "the seller standing up");
            await alice.ExpectSinceAsync(closed, Sitting(alice, false), "herself standing up");
            var asked = alice.Mark();
            var tried = bob.Mark();

            await bob.SendAsync(Buy(alice, 1));

            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(tried, Moves, TimeSpan.FromSeconds(1)), "nothing is bought");
            Assert.IsFalse(alice.Since(asked).Any(Moves), "nothing is sold");
        }

        [TestMethod]
        public async Task AShopCanBeOpenedAgainAfterItWasClosed()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 48);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 47, 48, c => c.Gold = 1000);
            await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 5, 30));
            var seen = bob.Mark();
            await alice.SendAsync(Close);
            await bob.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");

            await OpenAsync(alice, bob, "bargains", (PocketType.Etc, 0, 5, 20));

            Same([(PocketType.Etc, 0, WorldData.Seed, 5, 20)], OnShelf(await LookAsync(bob, alice)));
            var bought = bob.Mark();
            await bob.SendAsync(Buy(alice, 1));
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
                await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 5, 30));
                await LookAsync(bob, alice);
            }

            await bob.ExpectAsync<OutPacket>(p => p.VisualType == VisualType.Player && p.VisualId == sellerId, "the seller leaving");
            var tried = bob.Mark();
            await bob.SendAsync(Buy(sellerId, 1));
            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.IsFalse(bob.Since(tried).Any(p => p is IvnPacket or GoldPacket or SMemoiPacket or NInvPacket), "nothing is bought from someone who has left");

            var returned = bob.Mark();
            await using var back = await Harness.ComeBackAsync(seller);
            Same([(0, WorldData.Seed, 5)], Listed(back, PocketType.Etc));
            Assert.AreEqual(0, await GoldOnEnteringAsync(back));
            await bob.ExpectSinceAsync<InPacket>(returned, p => p.VisualType == VisualType.Player && p.VisualId == sellerId, "the seller back on the map");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(returned, p => AnySign(sellerId)(p) || Flag(sellerId, ShopSign)(p), TimeSpan.FromSeconds(1)),
                "she comes back without a shop");
        }

        [TestMethod]
        public async Task SomeoneArrivingLaterIsShownTheShopUntilItCloses()
        {
            var seller = await Harness.AddPlayerAsync("Alice", WorldData.Market, 46, 56);
            await Harness.GiveAsync(seller, WorldData.Seed, 5);
            await using var alice = await EnterAsync(seller);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Market, 47, 56);
            await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 5, 30));

            await using (var carol = await Harness.EnterAsync("Carol", WorldData.Market, 46, 57))
            {
                await carol.ExpectSinceAsync(0, Sign(alice, "stall"), "the sign");
                await carol.ExpectSinceAsync(0, Flag(alice, ShopSign), "the flag");
                Same([(PocketType.Etc, 0, WorldData.Seed, 5, 30)], OnShelf(await LookAsync(carol, alice)));
            }

            var seen = bob.Mark();
            await alice.SendAsync(Close);
            await bob.ExpectSinceAsync(seen, NoSign(alice), "the sign taken down");

            await using var dave = await Harness.EnterAsync("Dave", WorldData.Market, 46, 57);
            Assert.IsTrue(await dave.NoneSinceAsync<IPacket>(0, AShopShown(alice), TimeSpan.FromSeconds(1)),
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
                await OpenAsync(alice, bob, "smithy", (PocketType.Equipment, 0, 1, 2000));
                PacketAssert.AreEqual(new NInvItemSubPacket { Type = PocketType.Equipment, Slot = 0, VNum = WorldData.Blade, RareAmount = 5, UpgradeDesign = 7, Price = 2000 },
                    (await LookAsync(bob, alice)).Items?.OfType<NInvItemSubPacket>().Single());
                var sold = alice.Mark();
                var bought = bob.Mark();

                await bob.SendAsync(Buy(alice, 1));

                await bob.ExpectSinceAsync<IvnPacket>(bought, p => p.Type == PocketType.Equipment
                    && p.IvnSubPackets?.Any(item => item is { Slot: 0, VNum: WorldData.Blade, RareAmount: 5, UpgradeDesign: 7 }) == true, "the blade in his bag, as it was");
                await bob.ExpectSinceAsync(bought, Gold(5000 - 2000), "his gold after paying");
                await alice.ExpectSinceAsync(sold, EmptySlot(PocketType.Equipment, 0), "her emptied slot");
                await alice.ExpectSinceAsync(sold, Gold(2000), "her gold after the sale");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Same([], Listed(aliceBack, PocketType.Equipment), "her bag when she comes back");
            Assert.IsTrue(bobBack.Received.OfType<InvPacket>().Any(p => p.Type == PocketType.Equipment
                && p.IvnSubPackets?.Any(item => item is { Slot: 0, VNum: WorldData.Blade, RareAmount: 5, UpgradeDesign: 7 }) == true),
                "his blade when he comes back, as it was");
            await using var context = Harness.Database.CreateContext();
            var held = await context.Set<InventoryItemInstance>().AsNoTracking()
                .Where(i => (i.CharacterId == seller.CharacterId || i.CharacterId == buyer.CharacterId) && i.ItemInstance.ItemVNum == WorldData.Blade)
                .Select(i => i.CharacterId)
                .ToArrayAsync();
            CollectionAssert.AreEqual(new[] { buyer.CharacterId }, held, "who holds a blade");
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
                await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 5, 30));
                await LookAsync(bob, alice);
                var asked = alice.Mark();
                var refused = bob.Mark();

                await bob.SendAsync(Buy(alice, 1));

                await bob.ExpectSinceAsync(refused, Memo(SMemoType.FailPlayer, Game18NConstString.TradeFailedMaxMoney), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, Moves, TimeSpan.FromSeconds(1)),
                    "the seller is told of no sale");
                Assert.IsFalse(bob.Since(refused).Any(Moves), "nothing reaches the buyer");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(buyer);
            Assert.AreEqual(MostGold - 10, await GoldOnEnteringAsync(aliceBack));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(bobBack));
            Same([(0, WorldData.Seed, 5)], Listed(aliceBack, PocketType.Etc));
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
                await OpenAsync(alice, bob, "stall", (PocketType.Etc, 0, 1, 400));
                await LookAsync(bob, alice);
                await LookAsync(carol, alice);
                var bought = bob.Mark();
                await bob.SendAsync(Buy(alice, 1));
                await bob.ExpectSinceAsync(bought, Gold(600), "his gold after paying");
                var late = carol.Mark();

                await carol.SendAsync(Buy(alice, 1));

                Assert.IsTrue(await carol.NoneSinceAsync<IPacket>(late, Moves, TimeSpan.FromSeconds(1)),
                    "nothing is left for the second buyer");
            }

            await using var aliceBack = await Harness.ComeBackAsync(seller);
            await using var bobBack = await Harness.ComeBackAsync(first);
            await using var carolBack = await Harness.ComeBackAsync(second);
            Assert.AreEqual(400, await GoldOnEnteringAsync(aliceBack));
            Assert.AreEqual(600, await GoldOnEnteringAsync(bobBack));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(carolBack));
            Same([(0, WorldData.Gem, 1)], Listed(bobBack, PocketType.Etc));
            Same([], Listed(carolBack, PocketType.Etc));
            Same([], Listed(aliceBack, PocketType.Etc));
        }
    }
}
