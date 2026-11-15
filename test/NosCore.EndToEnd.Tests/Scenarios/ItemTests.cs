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
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ItemTests
    {
        private const int EtcPocket = 2;

        private static Func<IvnPacket, bool> Bag(int slot, int amount) =>
            packet => packet.Type == PocketType.Etc
                && packet.IvnSubPackets?.Single() is { } item && item.Slot == slot && item.VNum == WorldData.Seed && item.RareAmount == amount;

        [TestMethod]
        public async Task WhatACharacterCarriesIsSentWhenItEnters()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using var alice = Harness.Client(player);

            var entry = await alice.EnterWorldAsync(player.Slot);

            Assert.IsTrue(entry.OfType<InvPacket>().Where(p => p.Type == PocketType.Etc).SelectMany(p => p.IvnSubPackets ?? [])
                    .Any(item => item.Slot == 0 && item.VNum == WorldData.Seed && item.RareAmount == 5),
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

            await alice.SendAsync(new PutPacket { PocketType = PocketType.Etc, Slot = 0, Amount = 3 });

            await alice.ExpectAsync(Bag(0, 2), "the two left in the bag");
            var drop = await bob.ExpectAsync<DropPacket>(p => p.VNum == WorldData.Seed, "the drop");
            Assert.AreEqual(3, drop.Amount, "three were dropped");

            await alice.SendAsync(new GetPacket { PickerType = VisualType.Player, PickerId = alice.CharacterId, VisualId = drop.VisualId });

            await bob.ExpectAsync<ServerGetPacket>(p => p.VisualId == alice.CharacterId && p.ItemId == drop.VisualId, "the pick-up");
            await alice.ExpectAsync(Bag(0, 5), "all five back in the bag");
        }

        [TestMethod]
        public async Task AShopkeeperSellsAndBuysBack()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 84, y: 117, configure: c => c.Gold = 1000);

            // Talking to a shopkeeper who has nothing to say opens the shop at once.
            await alice.SendAsync(new RequestNpcPacket { Type = VisualType.Npc, TargetId = WorldData.Shopkeeper });
            PacketAssert.AreEqual(new NInvPacket
            {
                VisualType = VisualType.Npc,
                VisualId = WorldData.Shopkeeper,
                Unknown = 0,
                ShopKind = 100,
                Items = [new NInvItemSubPacket { Type = PocketType.Etc, Slot = 0, VNum = WorldData.Seed, RareAmount = null, Price = (int)WorldData.SeedPrice }],
            }, await alice.ExpectAsync<NInvPacket>());

            var bought = alice.Mark();
            await alice.SendAsync(new BuyPacket { VisualType = VisualType.Npc, VisualId = WorldData.Shopkeeper, Slot = 0, Amount = 3 });
            await alice.ExpectSinceAsync(bought, Bag(0, 3), "three in the bag");
            await alice.ExpectSinceAsync<GoldPacket>(bought, p => p.Gold == 1000 - 3 * WorldData.SeedPrice, "gold after buying three");

            var sold = alice.Mark();
            await alice.SendAsync(new SellPacket { Data = EtcPocket, Slot = 0, Amount = 1 });
            await alice.ExpectSinceAsync(sold, Bag(0, 2), "two left in the bag");
            // A shop pays a twentieth of the price.
            await alice.ExpectSinceAsync<GoldPacket>(sold, p => p.Gold == 1000 - 3 * WorldData.SeedPrice + WorldData.SeedPrice / 20,
                "gold after selling one");
        }
    }
}
