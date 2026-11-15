//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
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
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class NpcShopTests
    {
        private const int BagSlots = 48;
        private const int MostInASlot = 999;
        private const long MostGold = 1_000_000_000;

        private static Func<NInvPacket, bool> Shelf =>
            packet => packet.VisualType == VisualType.Npc && packet.VisualId == WorldData.Grocer;

        private static List<NInvItemSubPacket> OnShelf(NInvPacket shelf) => (shelf.Items ?? []).OfType<NInvItemSubPacket>().ToList();

        private static NInvItemSubPacket Sold(PocketType pocket, short slot, short vnum, int price) => new()
        {
            Type = pocket,
            Slot = slot,
            VNum = vnum,
            RareAmount = pocket == PocketType.Equipment ? 0 : null,
            UpgradeDesign = pocket == PocketType.Equipment ? 0 : null,
            Price = price,
        };

        private static Func<IvnPacket, bool> Slot(PocketType pocket, int slot, int vnum, int amount) =>
            packet => packet.Type == pocket
                && packet.IvnSubPackets?.Any(item => item.Slot == slot && item.VNum == vnum && item.RareAmount == amount) == true;

        private static Func<IvnPacket, bool> EmptySlot(PocketType pocket, int slot) =>
            packet => packet.Type == pocket && packet.IvnSubPackets?.Single() is { } item && item.Slot == slot && item.VNum is 0 or -1;

        private static Func<GoldPacket, bool> Gold(long gold) => packet => packet.Gold == gold && packet.Unknown == 0;

        private static Func<SMemoiPacket, bool> Memo(SMemoType type, Game18NConstString message) =>
            packet => packet.Type == type && packet.Message == message && packet.ArgumentType == 0;

        private static Func<MsgiPacket, bool> Announced(Game18NConstString message) => packet => packet.Message == message;

        private static bool Moves(IPacket packet) => packet is IvnPacket or GoldPacket or FdPacket;

        private static BuyPacket Buy(short slot, short amount) =>
            new() { VisualType = VisualType.Npc, VisualId = WorldData.Grocer, Slot = slot, Amount = amount };

        private static SellPacket Sell(PocketType pocket, byte slot, short amount) =>
            new() { Unknown1 = 0, Unknown2 = 0, Data = (short)pocket, Slot = slot, Amount = amount };

        private static readonly ShoppingPacket SecondShelf = new() { ShopType = 1, Unknown = 0, VisualType = VisualType.Npc, VisualId = WorldData.Grocer };

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
        private static async Task<NInvPacket> TalkAsync(TestClient customer)
        {
            var talked = customer.Mark();
            await customer.SendAsync(new RequestNpcPacket { Type = VisualType.Npc, TargetId = WorldData.Grocer });
            return await customer.ExpectSinceAsync(talked, Shelf, "the first shelf");
        }

        private static (short Slot, short VNum, short Amount)[] Listed(TestClient client, PocketType pocket) =>
            client.Received.OfType<InvPacket>().Where(p => p.Type == pocket)
                .SelectMany(p => p.IvnSubPackets ?? [])
                .OrderBy(slot => slot.Slot)
                .Select(slot => (slot.Slot, slot.VNum, slot.RareAmount))
                .ToArray();

        // The gold is sent a moment after the character is in the world.
        private static async Task<long> GoldOnEnteringAsync(TestClient client) =>
            (await client.ExpectSinceAsync<GoldPacket>(0, null, "the gold on entering")).Gold;

        private static async Task<long> ReputationOnEnteringAsync(TestClient client) =>
            (await client.ExpectSinceAsync<FdPacket>(0, null, "the reputation on entering")).Reput;

        private static async Task<(short Slot, short VNum, short Amount)[]> StoredAsync(long characterId, NoscorePocketType pocket)
        {
            await using var context = Harness.Database.CreateContext();
            var rows = await context.Set<InventoryItemInstance>().AsNoTracking()
                .Where(i => i.CharacterId == characterId && i.Type == pocket)
                .OrderBy(i => i.Slot)
                .Select(i => new { i.Slot, i.ItemInstance.ItemVNum, i.ItemInstance.Amount })
                .ToArrayAsync();
            return rows.Select(r => (r.Slot, r.ItemVNum, r.Amount)).ToArray();
        }

        private static void Same((short Slot, short VNum, short Amount)[] expected, (short Slot, short VNum, short Amount)[] actual, string? what = null) =>
            CollectionAssert.AreEqual(expected, actual, what);

        [TestMethod]
        public async Task EachShelfListsWhatIsSoldOnItAndForHowMuch()
        {
            await using var alice = await EnterAsync(await CustomerAsync());

            var first = await TalkAsync(alice);
            var asked = alice.Mark();
            await alice.SendAsync(SecondShelf);
            var second = await alice.ExpectSinceAsync(asked, Shelf, "the second shelf");

            PacketAssert.AreEqual<List<NInvItemSubPacket>>(
            [
                Sold(PocketType.Etc, 0, WorldData.Seed, (int)WorldData.SeedPrice),
                Sold(PocketType.Equipment, 1, WorldData.Blade, (int)WorldData.BladePrice),
                Sold(PocketType.Etc, 2, WorldData.Token, (int)WorldData.TokenReputation),
                Sold(PocketType.Etc, 3, WorldData.Gem, (int)WorldData.GemPrice),
            ], OnShelf(first));
            PacketAssert.AreEqual<List<NInvItemSubPacket>>([Sold(PocketType.Etc, 4, WorldData.Trinket, (int)WorldData.TrinketPrice)], OnShelf(second));
        }

        [TestMethod]
        public async Task ABuyerWithoutTheGoldIsRefusedAndKeepsWhatSheHas()
        {
            var player = await CustomerAsync(c => c.Gold = WorldData.SeedPrice - 1);
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var refused = alice.Mark();

                await alice.SendAsync(Buy(0, 1));

                await alice.ExpectSinceAsync(refused, Memo(SMemoType.FailNpc, Game18NConstString.NotEnoughGold5), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(0, Listed(back, PocketType.Etc).Length);
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

                await alice.SendAsync(Buy(0, 3));

                await alice.ExpectSinceAsync(bought, Slot(PocketType.Etc, 0, WorldData.Seed, 3), "the three seeds");
                await alice.ExpectSinceAsync(bought, Memo(SMemoType.SuccessNpc, Game18NConstString.TradeSuccessfull), "the purchase confirmed");
                await alice.ExpectSinceAsync(bought, Gold(0), "no gold left");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { ((short)0, WorldData.Seed, (short)3) }, Listed(back, PocketType.Etc));
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

                await alice.SendAsync(Buy(0, 1));

                await alice.ExpectSinceAsync(refused, Announced(Game18NConstString.NotEnoughSpace), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(1000, await GoldOnEnteringAsync(back));
            Assert.IsFalse(Listed(back, PocketType.Etc).Any(slot => slot.VNum == WorldData.Seed), "no seed in the bag");
        }

        [TestMethod]
        public async Task BuyingMoreThanAStackCanTakeFillsItAndStartsAnother()
        {
            var player = await CustomerAsync(c => c.Gold = 5000);
            await Harness.GiveAsync(player, WorldData.Seed, MostInASlot - 9);
            IvnSubPacket[] told;
            await using (var alice = await EnterAsync(player))
            {
                await TalkAsync(alice);
                var bought = alice.Mark();

                await alice.SendAsync(Buy(0, 20));

                await alice.ExpectSinceAsync(bought, Gold(5000 - 20 * WorldData.SeedPrice), "the gold left after paying for twenty");
                await alice.ExpectSinceAsync(bought, Slot(PocketType.Etc, 0, WorldData.Seed, MostInASlot), "the stack filled up");
                told = alice.Since<IvnPacket>(bought).Where(p => p.Type == PocketType.Etc).SelectMany(p => p.IvnSubPackets ?? []).ToArray();
            }

            Assert.IsTrue(told.Any(slot => slot is { Slot: 1, RareAmount: 11 } && slot.VNum == WorldData.Seed),
                "the client is told of the rest, in a slot of its own");
            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { ((short)0, WorldData.Seed, (short)MostInASlot), ((short)1, WorldData.Seed, (short)11) }, Listed(back, PocketType.Etc));
            Same(new[] { ((short)0, WorldData.Seed, (short)MostInASlot), ((short)1, WorldData.Seed, (short)11) }, await StoredAsync(player.CharacterId, NoscorePocketType.Etc));
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
                await alice.SendAsync(SecondShelf);
                await alice.ExpectSinceAsync(asked, Shelf, "the second shelf");
                var bought = alice.Mark();

                await alice.SendAsync(Buy(4, 2));

                await alice.ExpectSinceAsync(bought, Slot(PocketType.Etc, 0, WorldData.Trinket, 2), "the two trinkets");
                await alice.ExpectSinceAsync(bought, Gold(1000 - 2 * WorldData.TrinketPrice), "the gold left");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { ((short)0, WorldData.Trinket, (short)2) }, Listed(back, PocketType.Etc));
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

                await alice.SendAsync(Buy(2, 2));

                await alice.ExpectSinceAsync(bought, Slot(PocketType.Etc, 0, WorldData.Token, 2), "the two tokens");
                await alice.ExpectSinceAsync<FdPacket>(bought, p => p.Reput == 1000 - 2 * WorldData.TokenReputation, "the reputation left");
                PacketAssert.AreEqual(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = alice.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.ReputationReduced,
                    ArgumentType = 4,
                    Game18NArguments = { 2 * WorldData.TokenReputation },
                }, await alice.ExpectSinceAsync<SayiPacket>(bought, p => p.Message == Game18NConstString.ReputationReduced, "the reputation it cost"));
                Assert.IsTrue(await alice.NoneSinceAsync<GoldPacket>(bought, null, TimeSpan.FromSeconds(1)), "no gold is taken");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { ((short)0, WorldData.Token, (short)2) }, Listed(back, PocketType.Etc));
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

                await alice.SendAsync(Buy(2, 1));

                await alice.ExpectSinceAsync(refused, Memo(SMemoType.FailNpc, Game18NConstString.ReputationNotHighEnough), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(0, Listed(back, PocketType.Etc).Length);
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

                await alice.SendAsync(Buy(9, 1));

                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(0, Listed(back, PocketType.Etc).Length);
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

                await alice.SendAsync(Sell(PocketType.Etc, 0, 1));

                await alice.ExpectSinceAsync(refused, Memo(SMemoType.FailNpc, Game18NConstString.ItemCanNotBeSold), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { ((short)0, WorldData.Keepsake, (short)3) }, Listed(back, PocketType.Etc));
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

                await alice.SendAsync(Sell(PocketType.Etc, 0, 4));

                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { ((short)0, WorldData.Seed, (short)3) }, Listed(back, PocketType.Etc));
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

                await alice.SendAsync(Sell(PocketType.Etc, 0, 1));

                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { ((short)0, WorldData.Seed, (short)3) }, Listed(back, PocketType.Etc));
            Assert.AreEqual(0, await GoldOnEnteringAsync(back));
        }

        [TestMethod]
        public async Task NothingIsBoughtWithoutAShopOpen()
        {
            var player = await CustomerAsync(c => c.Gold = 1000);
            await using (var alice = await EnterAsync(player))
            {
                var asked = alice.Mark();

                await alice.SendAsync(Buy(0, 1));

                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(0, Listed(back, PocketType.Etc).Length);
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

                await alice.SendAsync(Sell(PocketType.Etc, 0, 4));

                await alice.ExpectSinceAsync(sold, EmptySlot(PocketType.Etc, 0), "the emptied slot");
                await alice.ExpectSinceAsync(sold, Memo(SMemoType.SuccessPlayer, Game18NConstString.TradeSuccessfull), "the sale confirmed");
                await alice.ExpectSinceAsync(sold, Gold(4 * WorldData.GemPrice / 20), "the gold it paid");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(0, Listed(back, PocketType.Etc).Length);
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

                await alice.SendAsync(Sell(PocketType.Equipment, 0, 1));

                await alice.ExpectSinceAsync(sold, EmptySlot(PocketType.Equipment, 0), "the emptied slot");
                await alice.ExpectSinceAsync(sold, Gold(WorldData.BladePrice / 20), "the gold it paid");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.AreEqual(0, Listed(back, PocketType.Equipment).Length);
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

                await alice.SendAsync(Sell(PocketType.Etc, 0, 1));

                await alice.ExpectSinceAsync(refused, Announced(Game18NConstString.MaxGoldReached), "the refusal");
                Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(refused, Moves, TimeSpan.FromSeconds(1)), "nothing moves");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { ((short)0, WorldData.Gem, (short)1) }, Listed(back, PocketType.Etc));
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
                    shown = OnShelf(await TalkAsync(alice)).Single(place => place is { Type: PocketType.Etc, Slot: 0 } && place.VNum == WorldData.Seed).Price;
                    var bought = alice.Mark();
                    await alice.SendAsync(Buy(0, 1));
                    await alice.ExpectSinceAsync(bought, Slot(PocketType.Etc, 0, WorldData.Seed, 1), "the seed");
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
