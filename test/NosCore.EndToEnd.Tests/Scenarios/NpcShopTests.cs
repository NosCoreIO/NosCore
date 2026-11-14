//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
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
    public class NpcShopTests
    {
        private const int Npc = 2;
        private const int EquipmentPocket = 0;
        private const int EtcPocket = 2;
        private const int BagSlots = 48;
        private const int MostInASlot = 999;
        private const long MostGold = 1_000_000_000;
        private const int NotEnoughGold = 115;
        private const int Traded = 117;
        private const int CannotBeSold = 124;
        private const int TooMuchGold = 1075;
        private const int BagIsFull = 1841;
        private const int ReputationReduced = 2168;
        private const int NotEnoughReputation = 2184;

        private static Func<string, bool> Shelf =>
            packet => packet.StartsWith($"n_inv {Npc} {WorldData.Grocer} ", StringComparison.Ordinal);

        // n_inv <type> <id> 0 <price in percent> <pocket.slot.vnum.amount or rare.[upgrade.]price, or -1 for an empty place>...
        private static string[] OnShelf(string shelf) => shelf.Split(' ').Skip(5).Where(place => place != "-1").ToArray();

        private static Func<string, bool> Slot(int pocket, int slot, int vnum, int amount) =>
            packet => packet.StartsWith($"ivn {pocket} {slot}.{vnum}.{amount}.", StringComparison.Ordinal);

        private static Func<string, bool> EmptySlot(int pocket, int slot) =>
            packet => packet.StartsWith($"ivn {pocket} {slot}.", StringComparison.Ordinal)
                && packet.Split(' ')[2].Split('.')[1] is "0" or "-1";

        private static Func<string, bool> Gold(long gold) => packet => packet == $"gold {gold} 0";

        private static Func<string, bool> Memo(int type, int message) => packet => packet == $"s_memoi {type} {message} 0";

        private static Func<string, bool> Announced(int message) =>
            packet => packet.Split(' ') is ["msgi", _, var id, ..] && id == message.ToString(CultureInfo.InvariantCulture);

        private static bool Moves(string packet) => TestClient.HeaderOf(packet) is "ivn" or "gold" or "fd";

        private static Task<Player> CustomerAsync(Action<Character>? configure = null) =>
            Harness.AddPlayerAsync("Alice", WorldData.Market, WorldData.GrocerX, WorldData.GrocerY + 1, configure);

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

        // Talking to a shopkeeper who has nothing to say opens the first shelf at once.
        private static async Task<string> TalkAsync(TestClient customer)
        {
            var talked = customer.Mark();
            await customer.SendAsync($"npc_req {Npc} {WorldData.Grocer}");
            return await customer.ExpectSinceAsync(talked, Shelf, "the first shelf");
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

        // fd <reputation> <its icon> <dignity> <its icon>
        private static async Task<long> ReputationOnEnteringAsync(TestClient client) =>
            long.Parse((await client.ExpectSinceAsync(0, p => TestClient.HeaderOf(p) == "fd", "the reputation on entering")).Split(' ')[1],
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

        [TestMethod]
        public async Task EachShelfListsWhatIsSoldOnItAndForHowMuch()
        {
            await using var alice = await EnterAsync(await CustomerAsync());

            var first = await TalkAsync(alice);
            var asked = alice.Mark();
            await alice.SendAsync($"shopping 1 0 {Npc} {WorldData.Grocer}");
            var second = await alice.ExpectSinceAsync(asked, Shelf, "the second shelf");

            Same(new[]
            {
                $"{EtcPocket}.0.{WorldData.Seed}.-1.{WorldData.SeedPrice}",
                $"{EquipmentPocket}.1.{WorldData.Blade}.0.0.{WorldData.BladePrice}",
                $"{EtcPocket}.2.{WorldData.Token}.-1.{WorldData.TokenReputation}",
                $"{EtcPocket}.3.{WorldData.Gem}.-1.{WorldData.GemPrice}"
            }, OnShelf(first), "the first shelf");
            Same(new[] { $"{EtcPocket}.4.{WorldData.Trinket}.-1.{WorldData.TrinketPrice}" }, OnShelf(second), "the second shelf");
        }

        [TestMethod]
        public async Task ABuyerWithoutTheGoldIsRefusedAndKeepsWhatSheHas()
        {
            var player = await CustomerAsync(c => c.Gold = WorldData.SeedPrice - 1);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var refused = alice.Mark();

                await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 0 1");

                await alice.ExpectSinceAsync(refused, Memo(2, NotEnoughGold), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, EtcPocket));
            Assert.AreEqual(WorldData.SeedPrice - 1, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task TheLastCoinBuys()
        {
            var player = await CustomerAsync(c => c.Gold = 3 * WorldData.SeedPrice);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var bought = alice.Mark();

                await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 0 3");

                await alice.ExpectSinceAsync(bought, Slot(EtcPocket, 0, WorldData.Seed, 3), "the three seeds");
                await alice.ExpectSinceAsync(bought, Memo(0, Traded), "the purchase confirmed");
                await alice.ExpectSinceAsync(bought, Gold(0), "no gold left");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.3" }, Listed(back, EtcPocket));
            Assert.AreEqual(0, await GoldOnEnteringAsync(back));
            Assert.AreEqual(0, (await Harness.StoredAsync(player.CharacterId)).Gold);
        }

        [TestMethod]
        public async Task ABuyerWithAFullBagIsRefusedAndPaysNothing()
        {
            var player = await CustomerAsync(c => c.Gold = 1000);
            for (short slot = 0; slot < BagSlots; slot++)
            {
                await Harness.GiveAsync(player, WorldData.Pebble, 1, slot);
            }

            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var refused = alice.Mark();

                await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 0 1");

                await alice.ExpectSinceAsync(refused, Announced(BagIsFull), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(1000, await GoldOnEnteringAsync(back));
            Assert.IsFalse(Listed(back, EtcPocket).Any(slot => slot.Contains($".{WorldData.Seed}.", StringComparison.Ordinal)), "no seed in the bag");
        }

        [TestMethod]
        public async Task BuyingMoreThanAStackCanTakeFillsItAndStartsAnother()
        {
            var player = await CustomerAsync(c => c.Gold = 5000);
            await Harness.GiveAsync(player, WorldData.Seed, MostInASlot - 9);
            IReadOnlyList<string> told;
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var bought = alice.Mark();

                await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 0 20");

                await alice.ExpectSinceAsync(bought, Gold(5000 - 20 * WorldData.SeedPrice), "the gold left after paying for twenty");
                await alice.ExpectSinceAsync(bought, Slot(EtcPocket, 0, WorldData.Seed, MostInASlot), "the stack filled up");
                told = alice.Since(bought);
            }

            Assert.IsTrue(told.Where(p => p.StartsWith($"ivn {EtcPocket} ", StringComparison.Ordinal))
                    .SelectMany(p => p.Split(' ').Skip(2)).Any(slot => slot.StartsWith($"1.{WorldData.Seed}.11.", StringComparison.Ordinal)),
                "the client is told of the rest, in a slot of its own");
            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.{MostInASlot}", $"1.{WorldData.Seed}.11" }, Listed(back, EtcPocket));
            Same(new[] { $"0.{WorldData.Seed}.{MostInASlot}", $"1.{WorldData.Seed}.11" }, await StoredAsync(player.CharacterId, NoscorePocketType.Etc));
            Assert.AreEqual(5000 - 20 * WorldData.SeedPrice, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task WhatIsBoughtFromTheSecondShelfIsWhatWasShownThere()
        {
            var player = await CustomerAsync(c => c.Gold = 1000);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var asked = alice.Mark();
                await alice.SendAsync($"shopping 1 0 {Npc} {WorldData.Grocer}");
                await alice.ExpectSinceAsync(asked, Shelf, "the second shelf");
                var bought = alice.Mark();

                await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 4 2");

                await alice.ExpectSinceAsync(bought, Slot(EtcPocket, 0, WorldData.Trinket, 2), "the two trinkets");
                await alice.ExpectSinceAsync(bought, Gold(1000 - 2 * WorldData.TrinketPrice), "the gold left");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Trinket}.2" }, Listed(back, EtcPocket));
            Assert.AreEqual(1000 - 2 * WorldData.TrinketPrice, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task ATokenCostsReputationAndNoGold()
        {
            var player = await CustomerAsync(c =>
            {
                c.Gold = 1000;
                c.Reput = 1000;
            });
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var bought = alice.Mark();

                await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 2 2");

                await alice.ExpectSinceAsync(bought, Slot(EtcPocket, 0, WorldData.Token, 2), "the two tokens");
                await alice.ExpectSinceAsync(bought, p => p.StartsWith($"fd {1000 - 2 * WorldData.TokenReputation} ", StringComparison.Ordinal),
                    "the reputation left");
                await alice.ExpectSinceAsync(bought,
                    p => p == $"sayi 1 {alice.CharacterId} 11 {ReputationReduced} 4 {2 * WorldData.TokenReputation} 0 0 0", "the reputation it cost");
                Assert.IsTrue(await alice.NoneSinceAsync(bought, p => TestClient.HeaderOf(p) == "gold", TimeSpan.FromSeconds(1)), "no gold is taken");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Token}.2" }, Listed(back, EtcPocket));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(back));
            Assert.AreEqual(1000 - 2 * WorldData.TokenReputation, await ReputationOnEnteringAsync(back), "the reputation shown on coming back");
            Assert.AreEqual(1000 - 2 * WorldData.TokenReputation, (await Harness.StoredAsync(player.CharacterId)).Reput, "the reputation stored");
        }

        [TestMethod]
        public async Task ATokenIsRefusedWithoutTheReputationForIt()
        {
            var player = await CustomerAsync(c =>
            {
                c.Gold = 1000;
                c.Reput = WorldData.TokenReputation - 1;
            });
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var refused = alice.Mark();

                await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 2 1");

                await alice.ExpectSinceAsync(refused, Memo(2, NotEnoughReputation), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, EtcPocket));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(back));
            Assert.AreEqual(WorldData.TokenReputation - 1, (await Harness.StoredAsync(player.CharacterId)).Reput);
        }

        [TestMethod]
        public async Task WhatTheShopDoesNotSellCannotBeBought()
        {
            var player = await CustomerAsync(c => c.Gold = 1000);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var asked = alice.Mark();

                await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 9 1");

                Assert.IsTrue(await alice.NoneSinceAsync(asked, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, EtcPocket));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task SomethingThatCannotBeSoldIsRefusedAndStaysInTheBag()
        {
            var player = await CustomerAsync();
            await Harness.GiveAsync(player, WorldData.Keepsake, 3);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var refused = alice.Mark();

                await alice.SendAsync($"sell 0 0 {EtcPocket} 0 1");

                await alice.ExpectSinceAsync(refused, Memo(2, CannotBeSold), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Keepsake}.3" }, Listed(back, EtcPocket));
            Assert.AreEqual(0, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task SellingMoreThanIsHeldSellsNothing()
        {
            var player = await CustomerAsync();
            await Harness.GiveAsync(player, WorldData.Seed, 3);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var asked = alice.Mark();

                await alice.SendAsync($"sell 0 0 {EtcPocket} 0 4");

                Assert.IsTrue(await alice.NoneSinceAsync(asked, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.3" }, Listed(back, EtcPocket));
            Assert.AreEqual(0, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task NothingIsSoldWithoutAShopOpen()
        {
            var player = await CustomerAsync();
            await Harness.GiveAsync(player, WorldData.Seed, 3);
            await using (var alice = await EnterAsync(player))
            {
                var asked = alice.Mark();

                await alice.SendAsync($"sell 0 0 {EtcPocket} 0 1");

                Assert.IsTrue(await alice.NoneSinceAsync(asked, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.3" }, Listed(back, EtcPocket));
            Assert.AreEqual(0, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task NothingIsBoughtWithoutAShopOpen()
        {
            var player = await CustomerAsync(c => c.Gold = 1000);
            await using (var alice = await EnterAsync(player))
            {
                var asked = alice.Mark();

                await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 0 1");

                Assert.IsTrue(await alice.NoneSinceAsync(asked, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, EtcPocket));
            Assert.AreEqual(1000, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task AWholeStackSoldLeavesTheBagAndPaysATwentiethOfItsPrice()
        {
            var player = await CustomerAsync();
            await Harness.GiveAsync(player, WorldData.Gem, 4);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var sold = alice.Mark();

                await alice.SendAsync($"sell 0 0 {EtcPocket} 0 4");

                await alice.ExpectSinceAsync(sold, EmptySlot(EtcPocket, 0), "the emptied slot");
                await alice.ExpectSinceAsync(sold, Memo(1, Traded), "the sale confirmed");
                await alice.ExpectSinceAsync(sold, Gold(4 * WorldData.GemPrice / 20), "the gold it paid");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, EtcPocket));
            Same([], await StoredAsync(player.CharacterId, NoscorePocketType.Etc));
            Assert.AreEqual(4 * WorldData.GemPrice / 20, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task AWeaponIsBoughtBackForATwentiethOfItsPrice()
        {
            var player = await CustomerAsync();
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5 }, NoscorePocketType.Equipment);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var sold = alice.Mark();

                await alice.SendAsync($"sell 0 0 {EquipmentPocket} 0 1");

                await alice.ExpectSinceAsync(sold, EmptySlot(EquipmentPocket, 0), "the emptied slot");
                await alice.ExpectSinceAsync(sold, Gold(WorldData.BladePrice / 20), "the gold it paid");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, EquipmentPocket));
            Same([], await StoredAsync(player.CharacterId, NoscorePocketType.Equipment));
            Assert.AreEqual(WorldData.BladePrice / 20, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task ASaleThatWouldPassTheMostGoldIsRefused()
        {
            var player = await CustomerAsync(c => c.Gold = MostGold - 10);
            await Harness.GiveAsync(player, WorldData.Gem, 1);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var refused = alice.Mark();

                await alice.SendAsync($"sell 0 0 {EtcPocket} 0 1");

                await alice.ExpectSinceAsync(refused, Announced(TooMuchGold), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Gem}.1" }, Listed(back, EtcPocket));
            Assert.AreEqual(MostGold - 10, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task ThePriceShownToSomeoneOfIllReputeIsThePriceCharged()
        {
            var wrong = new List<string>();
            foreach (short dignity in new[] { -99, -100, -200, -201, -400, -401, -600, -601, -800, -801, -1000 })
            {
                var player = await CustomerAsync(c =>
                {
                    c.Gold = 1000;
                    c.Dignity = dignity;
                });
                long shown;
                await using (var alice = await EnterAsync(player))
                {
                    var seed = OnShelf(await TalkAsync(alice)).Single(place => place.StartsWith($"{EtcPocket}.0.{WorldData.Seed}.", StringComparison.Ordinal));
                    shown = long.Parse(seed.Split('.')[4], CultureInfo.InvariantCulture);
                    var bought = alice.Mark();
                    await alice.SendAsync($"buy {Npc} {WorldData.Grocer} 0 1");
                    await alice.ExpectSinceAsync(bought, Slot(EtcPocket, 0, WorldData.Seed, 1), "the seed");
                }

                await using var back = await Harness.ComeBackAsync(player);
                var charged = 1000 - await GoldOnEnteringAsync(back);
                if (charged != shown)
                {
                    wrong.Add($"dignity {dignity}: shown {shown}, charged {charged}");
                }
            }

            Assert.AreEqual(string.Empty, string.Join("; ", wrong));
        }
    }
}
