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
using NosCore.Data.Enumerations.Buff;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class WarehouseTests
    {
        private const int EquipmentPocket = 0;
        private const int EtcPocket = 2;
        private const int PetBasket = 201;

        private static Func<string, bool> Bag(int slot, int amount) =>
            packet => packet.StartsWith($"ivn {EtcPocket} {slot}.{WorldData.Seed}.{amount}.", StringComparison.Ordinal);

        private static Func<string, bool> EmptyBagSlot(int slot) =>
            packet => packet.StartsWith($"ivn {EtcPocket} {slot}.0.0.", StringComparison.Ordinal);

        // stash <slot>.<vnum>.<pocket>.<amount>.<rare>.<upgrade>
        private static string Stored(int slot, int amount) => $"stash {slot}.{WorldData.Seed}.{EtcPocket}.{amount}.0.0";

        private static string EmptyStorageSlot(int slot) => $"stash {slot}.0.0.0.0.0";

        // Seeds in the bag and, unless the scenario is about not having one, a chest to keep them in.
        private static async Task<Player> OwnerAsync(short seeds, bool chest = true)
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, seeds);
            if (chest)
            {
                await Harness.GiveAsync(player, WorldData.Chest, 1, pocket: NoscorePocketType.Miniland);
            }

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

        private static async Task<(short Slot, short Amount)[]> StoredInWarehouseAsync(long characterId)
        {
            await using var context = Harness.Database.CreateContext();
            var rows = await context.Set<WarehouseItem>().AsNoTracking()
                .Where(w => w.Warehouse.CharacterId == characterId)
                .Select(w => new { w.Slot, w.ItemInstance.Amount, w.ItemInstance.ItemVNum })
                .ToArrayAsync();
            Assert.IsTrue(rows.All(r => r.ItemVNum == WorldData.Seed));
            return rows.Select(r => (r.Slot, r.Amount)).OrderBy(r => r.Slot).ToArray();
        }

        private static async Task<(short Slot, short Amount)[]> StoredInFamilyWarehouseAsync(long familyId)
        {
            await using var context = Harness.Database.CreateContext();
            var rows = await context.Set<WarehouseItem>().AsNoTracking()
                .Where(w => w.Warehouse.FamilyId == familyId)
                .Select(w => new { w.Slot, w.ItemInstance.Amount })
                .ToArrayAsync();
            return rows.Select(r => (r.Slot, r.Amount)).OrderBy(r => r.Slot).ToArray();
        }

        private static async Task<int> StoredInBagAsync(long characterId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<InventoryItemInstance>().AsNoTracking()
                .Where(i => i.CharacterId == characterId && i.ItemInstance.ItemVNum == WorldData.Seed)
                .SumAsync(i => (int)i.ItemInstance.Amount);
        }

        [TestMethod]
        public async Task ADepositMovesItemsFromTheBagToTheWarehouse()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            var deposited = alice.Mark();

            await alice.SendAsync($"deposit {EtcPocket} 0 3 1 0");

            await alice.ExpectSinceAsync(deposited, Bag(0, 2), "the two left in the bag");
            await alice.ExpectSinceAsync(deposited, p => p == Stored(1, 3), "the three in the warehouse");
            CollectionAssert.AreEqual(new[] { ((short)1, (short)3) }, await StoredInWarehouseAsync(player.CharacterId));
        }

        [TestMethod]
        public async Task AWholeStackDepositedEmptiesItsBagSlot()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            var deposited = alice.Mark();

            await alice.SendAsync($"deposit {EtcPocket} 0 5 0 0");

            await alice.ExpectSinceAsync(deposited, EmptyBagSlot(0), "the emptied bag slot");
            await alice.ExpectSinceAsync(deposited, p => p == Stored(0, 5), "the five in the warehouse");
            CollectionAssert.AreEqual(new[] { ((short)0, (short)5) }, await StoredInWarehouseAsync(player.CharacterId));
            Assert.AreEqual(0, await StoredInBagAsync(player.CharacterId), "nothing is left in the stored bag");
        }

        [TestMethod]
        public async Task AWithdrawalBringsItemsBackToTheBag()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync($"deposit {EtcPocket} 0 5 0 0");
            await alice.ExpectAsync(p => p == Stored(0, 5), "the five in the warehouse");
            var withdrawn = alice.Mark();

            await alice.SendAsync("withdraw 0 2 0");

            await alice.ExpectSinceAsync(withdrawn, Bag(0, 2), "the two back in the bag");
            await alice.ExpectSinceAsync(withdrawn, p => p == Stored(0, 3), "the three left in the warehouse");
            CollectionAssert.AreEqual(new[] { ((short)0, (short)3) }, await StoredInWarehouseAsync(player.CharacterId));

            var emptied = alice.Mark();
            await alice.SendAsync("withdraw 0 3 0");

            await alice.ExpectSinceAsync(emptied, Bag(0, 5), "all five back in the bag");
            await alice.ExpectSinceAsync(emptied, p => p == EmptyStorageSlot(0), "the emptied warehouse slot");
            Assert.AreEqual(0, (await StoredInWarehouseAsync(player.CharacterId)).Length);
        }

        [TestMethod]
        public async Task APartlyDepositedStackIsStoredAsSplit()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync($"deposit {EtcPocket} 0 3 0 0");
            await alice.ExpectAsync(p => p == Stored(0, 3), "the three in the warehouse");

            Assert.AreEqual(3, (await StoredInWarehouseAsync(player.CharacterId)).Sum(s => s.Amount));
            Assert.AreEqual(2, await StoredInBagAsync(player.CharacterId), "the stored bag holds what was not deposited");
        }

        [TestMethod]
        public async Task AWithdrawnItemIsStoredInTheBag()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync($"deposit {EtcPocket} 0 5 0 0");
            await alice.ExpectAsync(p => p == Stored(0, 5), "the five in the warehouse");

            await alice.SendAsync("withdraw 0 5 0");
            await alice.ExpectAsync(p => p == EmptyStorageSlot(0), "the emptied warehouse slot");

            Assert.AreEqual(0, (await StoredInWarehouseAsync(player.CharacterId)).Length);
            Assert.AreEqual(5, await StoredInBagAsync(player.CharacterId), "the stored bag holds what was taken out");
        }

        [TestMethod]
        public async Task WhatIsInTheWarehouseIsThereAfterComingBack()
        {
            var player = await OwnerAsync(5);
            await using (var alice = await EnterAsync(player))
            {
                await alice.SendAsync($"deposit {EtcPocket} 0 5 2 0");
                await alice.ExpectAsync(p => p == Stored(2, 5), "the five in the warehouse");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.IsFalse(back.Received.Any(p => p.StartsWith($"inv {EtcPocket} ", StringComparison.Ordinal) && p.Contains($".{WorldData.Seed}.", StringComparison.Ordinal)),
                "the bag no longer holds them");
            var withdrawn = back.Mark();
            await back.SendAsync("withdraw 2 5 0");

            await back.ExpectSinceAsync(withdrawn, Bag(0, 5), "all five back in the bag");
        }

        [TestMethod]
        public async Task ThePetBasketOpensTheWarehouseWithItsContents()
        {
            var player = await OwnerAsync(5);
            await using (var context = Harness.Database.CreateContext())
            {
                context.Add(new StaticBonus { CharacterId = player.CharacterId, StaticBonusType = StaticBonusType.PetBasket });
                await context.SaveChangesAsync();
            }

            await using var alice = await EnterAsync(player);
            await alice.SendAsync($"deposit {EtcPocket} 0 5 3 0");
            await alice.ExpectAsync(p => p == Stored(3, 5), "the five in the warehouse");

            await alice.SendAsync($"guri {PetBasket} 0");

            Assert.AreEqual($"stash_all {WorldData.ChestSlots} 3.{WorldData.Seed}.{EtcPocket}.5.0.0", await alice.ExpectAsync("stash_all"));
        }

        [TestMethod]
        public async Task AStoredWeaponKeepsItsRarityAndUpgrade()
        {
            var player = await OwnerAsync(1);
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5, Upgrade = 7 },
                NoscorePocketType.Equipment);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync($"deposit {EquipmentPocket} 0 1 0 0");
            await alice.ExpectAsync(p => p == $"stash 0.{WorldData.Blade}.{EquipmentPocket}.1.5.7", "the weapon in the warehouse");
            var withdrawn = alice.Mark();
            await alice.SendAsync("withdraw 0 1 0");

            // In the equipment pocket a slot reads slot.vnum.rarity.upgrade.
            await alice.ExpectSinceAsync(withdrawn, p => p.StartsWith($"ivn {EquipmentPocket} 0.{WorldData.Blade}.5.7.", StringComparison.Ordinal),
                "the weapon as it was");
        }

        [TestMethod]
        public async Task WhatTheHeadPutsInTheFamilyWarehouseAMemberAllowedToTakesOut()
        {
            var head = await OwnerAsync(5, chest: false);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(f =>
            {
                f.WarehouseSize = 4;
                f.MemberAuthorityType = FamilyAuthorityType.ALL;
            }, head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            var deposited = alice.Mark();

            await alice.SendAsync($"f_deposit {EtcPocket} 0 5 1 0");

            await alice.ExpectSinceAsync(deposited, EmptyBagSlot(0), "the emptied bag slot");
            await alice.ExpectSinceAsync(deposited, p => p == $"f_stash 1.{WorldData.Seed}.{EtcPocket}.5.0.0", "the five in the family warehouse");
            CollectionAssert.AreEqual(new[] { ((short)1, (short)5) }, await StoredInFamilyWarehouseAsync(family.FamilyId));

            // 1600 is the n_run that opens the family warehouse.
            await bob.SendAsync("n_run 1600");
            Assert.AreEqual($"f_stash_all 4 1.{WorldData.Seed}.{EtcPocket}.5.0.0", await bob.ExpectAsync("f_stash_all"));
            var withdrawn = bob.Mark();
            await bob.SendAsync("f_withdraw 1 2 0");

            await bob.ExpectSinceAsync(withdrawn, Bag(0, 2), "the two in Bob's bag");
            await bob.ExpectSinceAsync(withdrawn, p => p == $"f_stash 1.{WorldData.Seed}.{EtcPocket}.3.0.0", "the three left in the family warehouse");
            CollectionAssert.AreEqual(new[] { ((short)1, (short)3) }, await StoredInFamilyWarehouseAsync(family.FamilyId));
        }

        [TestMethod]
        public async Task AMemberAllowedOnlyToPutCannotTakeFromTheFamilyWarehouse()
        {
            var head = await OwnerAsync(5, chest: false);
            var member = await Harness.AddPlayerAsync("Bob", x: 81, y: 116);
            var family = await Harness.AddFamilyAsync(f =>
            {
                f.WarehouseSize = 4;
                f.MemberAuthorityType = FamilyAuthorityType.PUT;
            }, head, member);
            await using var alice = await EnterAsync(head);
            await using var bob = await EnterAsync(member);
            await alice.SendAsync($"f_deposit {EtcPocket} 0 5 0 0");
            await alice.ExpectAsync(p => p == $"f_stash 0.{WorldData.Seed}.{EtcPocket}.5.0.0", "the five in the family warehouse");
            var asked = bob.Mark();

            await bob.SendAsync("f_withdraw 0 5 0");

            // 392 is the client's notice of not being qualified to use the warehouse.
            await bob.ExpectSinceAsync(asked, p => p.StartsWith("infoi 392 ", StringComparison.Ordinal), "the not-qualified notice");
            Assert.IsTrue(await bob.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "f_stash", TimeSpan.FromSeconds(1)));
            CollectionAssert.AreEqual(new[] { ((short)0, (short)5) }, await StoredInFamilyWarehouseAsync(family.FamilyId));
        }

        [TestMethod]
        public async Task SomeoneOutsideTheFamilyCannotTakeFromItsWarehouse()
        {
            var head = await OwnerAsync(5, chest: false);
            var family = await Harness.AddFamilyAsync(f => f.WarehouseSize = 4, head);
            await using var alice = await EnterAsync(head);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await alice.SendAsync($"f_deposit {EtcPocket} 0 5 0 0");
            await alice.ExpectAsync(p => p == $"f_stash 0.{WorldData.Seed}.{EtcPocket}.5.0.0", "the five in the family warehouse");
            var asked = carol.Mark();

            await carol.SendAsync("f_withdraw 0 5 0");

            await carol.ExpectSinceAsync(asked, p => p.StartsWith("infoi 392 ", StringComparison.Ordinal), "the not-qualified notice");
            Assert.IsTrue(await carol.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "f_stash", TimeSpan.FromSeconds(1)));
            CollectionAssert.AreEqual(new[] { ((short)0, (short)5) }, await StoredInFamilyWarehouseAsync(family.FamilyId));
        }

        [TestMethod]
        public async Task NothingIsDepositedWithoutAChest()
        {
            var player = await OwnerAsync(5, chest: false);
            await using var alice = await EnterAsync(player);
            var asked = alice.Mark();

            await alice.SendAsync($"deposit {EtcPocket} 0 5 0 0");

            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "stash", TimeSpan.FromSeconds(1)));
            Assert.AreEqual(0, (await StoredInWarehouseAsync(player.CharacterId)).Length);
        }

        [TestMethod]
        public async Task ASlotBeyondTheChestTakesNothing()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            var asked = alice.Mark();

            await alice.SendAsync($"deposit {EtcPocket} 0 5 {WorldData.ChestSlots} 0");

            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "stash", TimeSpan.FromSeconds(1)));
            Assert.AreEqual(0, (await StoredInWarehouseAsync(player.CharacterId)).Length);
        }

        [TestMethod]
        public async Task AnOccupiedSlotTakesNothingMore()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync($"deposit {EtcPocket} 0 2 0 0");
            await alice.ExpectAsync(p => p == Stored(0, 2), "the two in the warehouse");
            var asked = alice.Mark();

            await alice.SendAsync($"deposit {EtcPocket} 0 3 0 0");

            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "stash", TimeSpan.FromSeconds(1)));
            CollectionAssert.AreEqual(new[] { ((short)0, (short)2) }, await StoredInWarehouseAsync(player.CharacterId));
        }

        [TestMethod]
        public async Task MoreThanIsStoredCannotBeWithdrawn()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync($"deposit {EtcPocket} 0 3 0 0");
            await alice.ExpectAsync(p => p == Stored(0, 3), "the three in the warehouse");
            var asked = alice.Mark();

            await alice.SendAsync("withdraw 0 4 0");

            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => TestClient.HeaderOf(p) is "ivn" or "stash", TimeSpan.FromSeconds(1)));
            CollectionAssert.AreEqual(new[] { ((short)0, (short)3) }, await StoredInWarehouseAsync(player.CharacterId));
        }

        [TestMethod]
        public async Task AStoredStackMovesToAnotherSlot()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync($"deposit {EtcPocket} 0 5 0 0");
            await alice.ExpectAsync(p => p == Stored(0, 5), "the five in the warehouse");
            var moved = alice.Mark();

            await alice.SendAsync("repos 0 5 3 0");

            await alice.ExpectSinceAsync(moved, p => p == Stored(3, 5), "the five in their new slot");
            await alice.ExpectSinceAsync(moved, p => p == EmptyStorageSlot(0), "the emptied slot");
            CollectionAssert.AreEqual(new[] { ((short)3, (short)5) }, await StoredInWarehouseAsync(player.CharacterId));
        }
    }
}
