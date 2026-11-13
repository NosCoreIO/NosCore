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
    public class InventoryTests
    {
        private const int EquipmentPocket = 0;
        private const int MainPocket = 1;
        private const int EtcPocket = 2;
        private const int CostumePocket = 7;
        private const int Player = 1;
        private const int MainWeaponSlot = 0;
        private const int HatSlot = 2;
        private const int FairySlot = 10;
        private const int MostInASlot = 999;
        private const int BagSlots = 48;

        // ivn <pocket> <slot>.<vnum>.<amount, or rare for equipment>.<upgrade>...
        private static Func<string, bool> Slot(int pocket, int slot, int vnum, int amount) =>
            packet => packet.StartsWith($"ivn {pocket} {slot}.{vnum}.{amount}.", StringComparison.Ordinal);

        private static Func<string, bool> EmptySlot(int pocket, int slot) =>
            packet => packet.StartsWith($"ivn {pocket} {slot}.", StringComparison.Ordinal)
                && packet.Split(' ')[2].Split('.')[1] is "0" or "-1";

        private static Func<string, bool> AnySlot(int pocket, int slot) =>
            packet => packet.StartsWith($"ivn {pocket} {slot}.", StringComparison.Ordinal);

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

        // What a pocket holds as the server lists it on entering: "slot.vnum.amount" for each slot,
        // the amount being the rarity in the equipment pocket.
        private static string[] Listed(TestClient client, int pocket) =>
            client.Received.Where(p => p.StartsWith($"inv {pocket}", StringComparison.Ordinal))
                .SelectMany(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(2))
                .Select(slot => slot.Split('.'))
                .OrderBy(slot => int.Parse(slot[0], CultureInfo.InvariantCulture))
                .Select(slot => string.Join('.', slot.Take(3)))
                .ToArray();

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

        private static int Hp(string stat) => int.Parse(stat.Split(' ')[1], CultureInfo.InvariantCulture);

        [TestMethod]
        public async Task AStackMovedToAnEmptySlotLeavesItsOldSlotEmpty()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync($"mvi {EtcPocket} 0 5 3");

                await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 3, WorldData.Seed, 5), "the stack in its new slot");
                await alice.ExpectSinceAsync(moved, EmptySlot(EtcPocket, 0), "the emptied slot");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"3.{WorldData.Seed}.5" }, Listed(back, EtcPocket));
        }

        [TestMethod]
        public async Task PartOfAStackMovedToAnEmptySlotSplitsIt()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync($"mvi {EtcPocket} 0 2 1");

                await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 1, WorldData.Seed, 2), "the two split off");
                await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 0, WorldData.Seed, 3), "the three left behind");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.3", $"1.{WorldData.Seed}.2" }, Listed(back, EtcPocket));
            Same(new[] { $"0.{WorldData.Seed}.3", $"1.{WorldData.Seed}.2" },
                await StoredAsync(player.CharacterId, NoscorePocketType.Etc));
        }

        [TestMethod]
        public async Task AStackMovedOntoAnotherOfTheSameItemMergesThem()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await Harness.GiveAsync(player, WorldData.Seed, 7, slot: 1);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync($"mvi {EtcPocket} 0 5 1");

                await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 1, WorldData.Seed, 12), "the merged stack");
                await alice.ExpectSinceAsync(moved, EmptySlot(EtcPocket, 0), "the emptied slot");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"1.{WorldData.Seed}.12" }, Listed(back, EtcPocket));
            Same(new[] { $"1.{WorldData.Seed}.12" }, await StoredAsync(player.CharacterId, NoscorePocketType.Etc));
        }

        [TestMethod]
        public async Task PartOfAStackMovedOntoAnotherOfTheSameItemMovesOnlyThatMany()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await Harness.GiveAsync(player, WorldData.Seed, 7, slot: 1);
            await using var alice = await EnterAsync(player);
            var moved = alice.Mark();

            await alice.SendAsync($"mvi {EtcPocket} 0 2 1");

            await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 1, WorldData.Seed, 9), "the stack that grew");
            await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 0, WorldData.Seed, 3), "the stack that shrank");
        }

        [TestMethod]
        public async Task AMergeStopsAtTheMostASlotHoldsAndLosesNothing()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 600);
            await Harness.GiveAsync(player, WorldData.Seed, 700, slot: 1);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync($"mvi {EtcPocket} 0 600 1");

                await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 1, WorldData.Seed, MostInASlot), "the full stack");
                await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 0, WorldData.Seed, 600 + 700 - MostInASlot), "what did not fit");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.{600 + 700 - MostInASlot}", $"1.{WorldData.Seed}.{MostInASlot}" },
                Listed(back, EtcPocket));
        }

        [TestMethod]
        public async Task PartOfAStackMovedOntoAnAlmostFullOneMovesNoMoreThanWasAsked()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 600);
            await Harness.GiveAsync(player, WorldData.Seed, 990, slot: 1);
            await using var alice = await EnterAsync(player);
            var moved = alice.Mark();

            // Five are asked for and nine would fit.
            await alice.SendAsync($"mvi {EtcPocket} 0 5 1");

            await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 1, WorldData.Seed, 995), "the stack that grew by five");
            await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 0, WorldData.Seed, 595), "the stack that shrank by five");
        }

        [TestMethod]
        public async Task MovingMoreThanTheStackHoldsChangesNothing()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync($"mvi {EtcPocket} 0 9 1");

                await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 0, WorldData.Seed, 5), "the stack where it was");
                await alice.ExpectSinceAsync(moved, EmptySlot(EtcPocket, 1), "the slot still empty");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(back, EtcPocket));
        }

        [TestMethod]
        public async Task AMoveBeyondTheLastSlotOfTheBagChangesNothing()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync($"mvi {EtcPocket} 0 5 {BagSlots}");

                await alice.ExpectSinceAsync(moved, Slot(EtcPocket, 0, WorldData.Seed, 5), "the stack where it was");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(back, EtcPocket));
        }

        [TestMethod]
        public async Task SortingAPocketPacksItFromTheFirstSlotInNameOrder()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5, slot: 9);
            await Harness.GiveAsync(player, WorldData.Pebble, 3, slot: 5);
            await using (var alice = await EnterAsync(player))
            {
                await alice.SendAsync($"isort {EtcPocket}");

                var sorted = (await alice.ExpectAsync("ivn")).Split(' ');
                Assert.AreEqual(EtcPocket.ToString(CultureInfo.InvariantCulture), sorted[1]);
                Same(new[] { $"0.{WorldData.Pebble}.3", $"1.{WorldData.Seed}.5" },
                    sorted.Skip(2).Take(2).Select(slot => string.Join('.', slot.Split('.').Take(3))).ToArray());
                Same(new[] { "5", "9" }, sorted.Skip(4).Select(slot => slot.Split('.')[0]).ToArray(),
                    "the slots they left are sent as well");
                Assert.IsTrue(sorted.Skip(4).All(slot => slot.Split('.')[1] is "0" or "-1"), "and are empty");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Pebble}.3", $"1.{WorldData.Seed}.5" }, Listed(back, EtcPocket));
        }

        [TestMethod]
        public async Task SomethingWornCannotBeMovedAsIfItWereInABag()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5 },
                NoscorePocketType.Wear, MainWeaponSlot);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using (var alice = await EnterAsync(player))
            {
                // 8 is no pocket a client names; the head is slot 2 of what is worn.
                await alice.SendAsync($"mvi 8 {MainWeaponSlot} 1 {HatSlot}");
                await alice.WalkAsync(80, 117);
                await bob.ExpectAsync(p => p.StartsWith($"mv {Player} {alice.CharacterId} 80 117 ", StringComparison.Ordinal), "the step after it");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"{MainWeaponSlot}.{WorldData.Blade}.1" }, await StoredAsync(player.CharacterId, NoscorePocketType.Wear));
        }

        [TestMethod]
        public async Task WithAFullBagADropStaysOnTheMap()
        {
            var owner = await Harness.AddPlayerAsync("Alice");
            for (short slot = 0; slot < BagSlots; slot++)
            {
                await Harness.GiveAsync(owner, WorldData.Pebble, 1, slot);
            }

            var dropper = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await Harness.GiveAsync(dropper, WorldData.Seed, 5);
            await using var bob = await EnterAsync(dropper);
            await using var alice = await EnterAsync(owner);
            await bob.SendAsync($"put {EtcPocket} 0 5");
            var dropId = (await alice.ExpectAsync(p => p.StartsWith($"drop {WorldData.Seed} ", StringComparison.Ordinal), "the drop")).Split(' ')[2];
            var refused = alice.Mark();

            await alice.SendAsync($"get {Player} {alice.CharacterId} {dropId} 0");

            await alice.ExpectSinceAsync(refused, p => TestClient.HeaderOf(p) == "msgi", "the refusal");
            var picked = bob.Mark();
            await bob.SendAsync($"get {Player} {bob.CharacterId} {dropId} 0");
            await bob.ExpectSinceAsync(picked, Slot(EtcPocket, 0, WorldData.Seed, 5), "the five still there to pick up");
        }

        [TestMethod]
        public async Task AWeaponPutOnLeavesTheBagAndIsShownToBystanders()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5, Upgrade = 7 },
                NoscorePocketType.Equipment);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using (var alice = await EnterAsync(player))
            {
                var worn = alice.Mark();

                await alice.SendAsync("wear 0 0");

                await alice.ExpectSinceAsync(worn, EmptySlot(EquipmentPocket, 0), "the emptied bag slot");
                var equipment = await alice.ExpectSinceAsync(worn, p => TestClient.HeaderOf(p) == "equip", "what is worn");
                CollectionAssert.Contains(equipment.Split(' '), $"{MainWeaponSlot}.{WorldData.Blade}.5.7.0");
                var seen = (await bob.ExpectAsync(p => p.StartsWith($"eq {alice.CharacterId} ", StringComparison.Ordinal), "Alice's look")).Split(' ');
                Assert.AreEqual(WorldData.Blade.ToString(CultureInfo.InvariantCulture), seen[7].Split('.')[2], "the weapon in her hand");
                // Upgrade and rarity of the weapon, written without a separator.
                Assert.AreEqual("75", seen[8]);
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(Array.Empty<string>(), Listed(back, EquipmentPocket));
            CollectionAssert.Contains(back.Received.Single(p => TestClient.HeaderOf(p) == "equip").Split(' '),
                $"{MainWeaponSlot}.{WorldData.Blade}.5.7.0");
            Same(new[] { $"{MainWeaponSlot}.{WorldData.Blade}.1" }, await StoredAsync(player.CharacterId, NoscorePocketType.Wear));
        }

        [TestMethod]
        public async Task AWeaponTakenOffGoesBackToTheBagAndBystandersSeeTheEmptyHand()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5, Upgrade = 7 },
                NoscorePocketType.Wear, MainWeaponSlot);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using (var alice = await EnterAsync(player))
            {
                CollectionAssert.Contains(alice.Received.Single(p => TestClient.HeaderOf(p) == "equip").Split(' '),
                    $"{MainWeaponSlot}.{WorldData.Blade}.5.7.0");
                var arrival = (await bob.ExpectAsync(p => p.StartsWith($"in {Player} ", StringComparison.Ordinal)
                    && p.Split(' ')[4] == alice.CharacterId.ToString(CultureInfo.InvariantCulture), "Alice arriving")).Split(' ');
                Assert.IsTrue(arrival.Any(field => field.Split('.') is [_, _, var weapon, ..] && weapon == WorldData.Blade.ToString(CultureInfo.InvariantCulture)
                    && field.Count(c => c == '.') >= 8), "she arrives with the weapon in her hand");
                var removed = alice.Mark();

                await alice.SendAsync($"remove {MainWeaponSlot} 0");

                await alice.ExpectSinceAsync(removed, Slot(EquipmentPocket, 0, WorldData.Blade, 5), "the weapon in the bag");
                var equipment = await alice.ExpectSinceAsync(removed, p => TestClient.HeaderOf(p) == "equip", "what is worn");
                Assert.IsFalse(equipment.Contains($".{WorldData.Blade}.", StringComparison.Ordinal), "nothing is worn any more");
                var seen = (await bob.ExpectAsync(p => p.StartsWith($"eq {alice.CharacterId} ", StringComparison.Ordinal), "Alice's look")).Split(' ');
                Assert.AreEqual("-1", seen[7].Split('.')[2], "the empty hand");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Blade}.5" }, Listed(back, EquipmentPocket));
            Same(Array.Empty<string>(), await StoredAsync(player.CharacterId, NoscorePocketType.Wear));
        }

        [TestMethod]
        public async Task AHatPutOnIsSeenOnTheHeadByBystanders()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Hat, Amount = 1 }, NoscorePocketType.Equipment);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync("wear 0 0");

            var seen = (await bob.ExpectAsync(p => p.StartsWith($"eq {alice.CharacterId} ", StringComparison.Ordinal), "Alice's look")).Split(' ');
            Assert.AreEqual(WorldData.Hat.ToString(CultureInfo.InvariantCulture), seen[7].Split('.')[0], "the hat on her head");

            await alice.SendAsync($"remove {HatSlot} 0");

            seen = (await bob.ExpectAsync(p => p.StartsWith($"eq {alice.CharacterId} ", StringComparison.Ordinal), "Alice's look")).Split(' ');
            Assert.AreEqual("-1", seen[7].Split('.')[0], "the bare head");
        }

        [TestMethod]
        public async Task AFairyPutOnAndTakenOffIsSeenByBystanders()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Fairy, Amount = 1, ElementRate = 12 },
                NoscorePocketType.Equipment);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync("wear 0 0");

            // pairy <type> <id> <move> <element> <rate> <morph>: the fairy's own rate on top of its item's.
            Assert.AreEqual($"pairy {Player} {alice.CharacterId} 4 1 {WorldData.FairyElementRate + 12} 0", await bob.ExpectAsync("pairy"));
            var removed = alice.Mark();

            await alice.SendAsync($"remove {FairySlot} 0");

            Assert.AreEqual($"pairy {Player} {alice.CharacterId} 0 0 0 0", await bob.ExpectAsync("pairy"));
            await alice.ExpectSinceAsync(removed, Slot(EquipmentPocket, 0, WorldData.Fairy, 0), "the fairy in the bag");
        }

        [TestMethod]
        public async Task SomethingThatCannotBeWornStaysInTheBag()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using (var alice = await EnterAsync(player))
            {
                var worn = bob.Mark();

                await alice.SendAsync($"u_i {Player} {alice.CharacterId} {EtcPocket} 0 0 0");

                Assert.IsTrue(await bob.NoneSinceAsync(worn, p => p.StartsWith($"eq {alice.CharacterId} ", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(1)), "nothing changes in what she wears");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(back, EtcPocket));
        }

        [TestMethod]
        public async Task TakingSomethingOffWithAFullBagLeavesItOn()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5 },
                NoscorePocketType.Wear, MainWeaponSlot);
            for (short slot = 0; slot < BagSlots; slot++)
            {
                await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Hat, Amount = 1 }, NoscorePocketType.Equipment, slot);
            }

            await using (var alice = await EnterAsync(player))
            {
                var removed = alice.Mark();

                await alice.SendAsync($"remove {MainWeaponSlot} 0");

                await alice.ExpectSinceAsync(removed, p => TestClient.HeaderOf(p) == "msgi", "the refusal");
                Assert.IsFalse(alice.Since(removed).Any(p => TestClient.HeaderOf(p) == "ivn"), "the bag does not change");
            }

            await using var back = await Harness.ComeBackAsync(player);
            CollectionAssert.Contains(back.Received.Single(p => TestClient.HeaderOf(p) == "equip").Split(' '),
                $"{MainWeaponSlot}.{WorldData.Blade}.5.0.0");
            Assert.AreEqual(BagSlots, Listed(back, EquipmentPocket).Length);
        }

        [TestMethod]
        public async Task ACostumeMovesToTheCostumePocketAndBack()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.CostumeHat, Amount = 1 }, NoscorePocketType.Equipment);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync($"mve {EquipmentPocket} 0 {CostumePocket} 2");

                await alice.ExpectSinceAsync(moved, AnySlot(CostumePocket, 2), "the costume pocket");
                StringAssert.StartsWith(alice.Since(moved).First(AnySlot(CostumePocket, 2)), $"ivn {CostumePocket} 2.{WorldData.CostumeHat}.");
                await alice.ExpectSinceAsync(moved, EmptySlot(EquipmentPocket, 0), "the emptied bag slot");
                var back = alice.Mark();

                await alice.SendAsync($"mve {CostumePocket} 2 {EquipmentPocket} 3");

                await alice.ExpectSinceAsync(back, Slot(EquipmentPocket, 3, WorldData.CostumeHat, 0), "the costume in the bag again");
                await alice.ExpectSinceAsync(back, EmptySlot(CostumePocket, 2), "the emptied costume slot");

                await alice.SendAsync($"mve {EquipmentPocket} 3 {CostumePocket} 1");
                await alice.ExpectAsync(AnySlot(CostumePocket, 1), "the costume pocket");
            }

            await using var again = await Harness.ComeBackAsync(player);
            Same(new[] { $"1.{WorldData.CostumeHat}.1" }, await StoredAsync(player.CharacterId, NoscorePocketType.Costume));
            Same(Array.Empty<string>(), Listed(again, EquipmentPocket));
        }

        [TestMethod]
        public async Task APotionRestoresHpAndIsUsedUp()
        {
            var player = await Harness.AddPlayerAsync("Alice", configure: c => c.Hp = 100);
            await Harness.GiveAsync(player, WorldData.Potion, 3, pocket: NoscorePocketType.Main);
            await using (var alice = await EnterAsync(player))
            {
                Assert.AreEqual(100, Hp(alice.Received.Last(p => TestClient.HeaderOf(p) == "stat")));
                var drunk = alice.Mark();

                await alice.SendAsync($"u_i {Player} {alice.CharacterId} {MainPocket} 0 0 0");

                await alice.ExpectSinceAsync(drunk, Slot(MainPocket, 0, WorldData.Potion, 2), "the two potions left");
                var stat = await alice.ExpectSinceAsync(drunk, p => TestClient.HeaderOf(p) == "stat", "the HP after the potion");
                Assert.AreEqual(100 + WorldData.PotionHp, Hp(stat));
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Potion}.2" }, Listed(back, MainPocket));
            Assert.IsTrue((await Harness.StoredAsync(player.CharacterId)).Hp >= 100 + WorldData.PotionHp, "the restored HP is stored");
        }

        [TestMethod]
        public async Task APotionNeverRestoresMoreThanTheMostHp()
        {
            var player = await Harness.AddPlayerAsync("Alice", configure: c => c.Hp = 200);
            await Harness.GiveAsync(player, WorldData.Potion, 2, pocket: NoscorePocketType.Main);
            await using var alice = await EnterAsync(player);
            var most = int.Parse(alice.Received.Last(p => TestClient.HeaderOf(p) == "stat").Split(' ')[2], CultureInfo.InvariantCulture);
            Assert.IsTrue(most < 200 + WorldData.PotionHp, "the potion would overfill");
            var drunk = alice.Mark();

            await alice.SendAsync($"u_i {Player} {alice.CharacterId} {MainPocket} 0 0 0");

            await alice.ExpectSinceAsync(drunk, Slot(MainPocket, 0, WorldData.Potion, 1), "the potion left");
            Assert.AreEqual(most, Hp(await alice.ExpectSinceAsync(drunk, p => TestClient.HeaderOf(p) == "stat", "the HP after the potion")));
        }

        [TestMethod]
        public async Task APotionIsNotUsedUpAtFullHp()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Potion, 3, pocket: NoscorePocketType.Main);
            await using (var alice = await EnterAsync(player))
            {
                var drunk = alice.Mark();

                await alice.SendAsync($"u_i {Player} {alice.CharacterId} {MainPocket} 0 0 0");

                Assert.IsTrue(await alice.NoneSinceAsync(drunk, p => TestClient.HeaderOf(p) == "ivn", TimeSpan.FromSeconds(1)),
                    "no potion leaves the bag");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Potion}.3" }, Listed(back, MainPocket));
        }

        [TestMethod]
        public async Task DroppingMoreThanIsOwnedDropsNothing()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using (var alice = await EnterAsync(player))
            {
                var dropped = alice.Mark();
                var seen = bob.Mark();

                await alice.SendAsync($"put {EtcPocket} 0 9");

                await alice.ExpectSinceAsync(dropped, p => p.StartsWith($"sayi {Player} {alice.CharacterId} ", StringComparison.Ordinal), "the refusal");
                Assert.IsTrue(await bob.NoneSinceAsync(seen, p => p.StartsWith($"drop {WorldData.Seed} ", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(1)), "nothing lies on the map");
                Assert.IsFalse(alice.Since(dropped).Any(p => TestClient.HeaderOf(p) == "ivn"), "the bag does not change");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(new[] { $"0.{WorldData.Seed}.5" }, Listed(back, EtcPocket));
        }

        [TestMethod]
        public async Task AWholeStackDroppedIsGoneFromTheBagForGood()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using (var alice = await EnterAsync(player))
            {
                var dropped = alice.Mark();

                await alice.SendAsync($"put {EtcPocket} 0 5");

                await alice.ExpectSinceAsync(dropped, EmptySlot(EtcPocket, 0), "the emptied slot");
                var drop = await bob.ExpectAsync(p => p.StartsWith($"drop {WorldData.Seed} ", StringComparison.Ordinal), "the drop");
                Assert.AreEqual("5", drop.Split(' ')[5], "five were dropped");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(Array.Empty<string>(), Listed(back, EtcPocket));
            Same(Array.Empty<string>(), await StoredAsync(player.CharacterId, NoscorePocketType.Etc));
        }

        [TestMethod]
        public async Task AnItemIsDestroyedOnlyOnceItHasBeenConfirmedTwice()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var asked = alice.Mark();

                await alice.SendAsync($"b_i {EtcPocket} 0");
                StringAssert.StartsWith(await alice.ExpectAsync("dlgi"), $"dlgi #b_i^{EtcPocket}^0^1 #b_i^{EtcPocket}^0^5 ");
                await alice.SendAsync($"b_i {EtcPocket} 0 1");
                StringAssert.StartsWith(await alice.ExpectAsync("dlgi"), $"dlgi #b_i^{EtcPocket}^0^2 #b_i^{EtcPocket}^0^5 ");
                Assert.IsFalse(alice.Since(asked).Any(p => TestClient.HeaderOf(p) == "ivn"), "nothing is destroyed while it is only asked");

                await alice.SendAsync($"b_i {EtcPocket} 0 2");

                await alice.ExpectAsync(EmptySlot(EtcPocket, 0), "the emptied slot");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same(Array.Empty<string>(), Listed(back, EtcPocket));
        }
    }
}
