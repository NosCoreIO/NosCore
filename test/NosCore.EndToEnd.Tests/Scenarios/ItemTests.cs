//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ItemTests
    {
        private const int EtcPocket = 2;
        private const int Npc = 2;

        private static Func<string, bool> Bag(int slot, int amount) =>
            packet => packet.StartsWith($"ivn {EtcPocket} {slot}.{WorldData.Seed}.{amount}.", StringComparison.Ordinal);

        [TestMethod]
        public async Task WhatACharacterCarriesIsSentWhenItEnters()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using var alice = Harness.Client(player);

            var entry = await alice.EnterWorldAsync(player.Slot);

            Assert.IsTrue(entry.Any(p => p.StartsWith($"inv {EtcPocket} 0.{WorldData.Seed}.5.", StringComparison.Ordinal)),
                "the five seeds are listed in the Etc pocket");
        }

        [TestMethod]
        public async Task ADroppedItemLiesOnTheMapAndCanBePickedUpAgain()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);

            await alice.SendAsync($"put {EtcPocket} 0 3");

            await alice.ExpectAsync(Bag(0, 2), "the two left in the bag");
            var drop = await bob.ExpectAsync(p => p.StartsWith($"drop {WorldData.Seed} ", StringComparison.Ordinal), "the drop");
            var dropId = drop.Split(' ')[2];
            Assert.AreEqual("3", drop.Split(' ')[5], "three were dropped");

            await alice.SendAsync($"get 1 {alice.CharacterId} {dropId} 0");

            await bob.ExpectAsync(p => p.StartsWith($"get 1 {alice.CharacterId} {dropId}", StringComparison.Ordinal), "the pick-up");
            await alice.ExpectAsync(Bag(0, 5), "all five back in the bag");
        }

        [TestMethod]
        public async Task AShopkeeperSellsAndBuysBack()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 84, y: 117, configure: c => c.Gold = 1000);

            // Talking to a shopkeeper who has nothing to say opens the shop at once.
            await alice.SendAsync($"npc_req {Npc} {WorldData.Shopkeeper}");
            var shelf = await alice.ExpectAsync("n_inv");
            Assert.AreEqual($"n_inv {Npc} {WorldData.Shopkeeper} 0 100 {EtcPocket}.0.{WorldData.Seed}.-1.{WorldData.SeedPrice}", shelf);

            var bought = alice.Mark();
            await alice.SendAsync($"buy {Npc} {WorldData.Shopkeeper} 0 3");
            await alice.ExpectSinceAsync(bought, Bag(0, 3), "three in the bag");
            await alice.ExpectSinceAsync(bought, p => p == $"gold {1000 - 3 * WorldData.SeedPrice} 0", "gold after buying three");

            var sold = alice.Mark();
            await alice.SendAsync($"sell 0 0 {EtcPocket} 0 1");
            await alice.ExpectSinceAsync(sold, Bag(0, 2), "two left in the bag");
            // A shop pays a twentieth of the price.
            await alice.ExpectSinceAsync(sold, p => p == $"gold {1000 - 3 * WorldData.SeedPrice + WorldData.SeedPrice / 20} 0",
                "gold after selling one");
        }
    }
}
