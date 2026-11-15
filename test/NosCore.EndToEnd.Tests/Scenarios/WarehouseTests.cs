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
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Warehouse;
using NosCore.Tests.Shared;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class WarehouseTests
    {
        private static Func<IvnPacket, bool> Bag(int slot, int amount) =>
            packet => packet.Type == PocketType.Etc
                && packet.IvnSubPackets?.Single() is { } item && item.Slot == slot && item.VNum == WorldData.Seed && item.RareAmount == amount;

        private static Func<IvnPacket, bool> EmptyBagSlot(int slot) =>
            packet => packet.Type == PocketType.Etc
                && packet.IvnSubPackets?.Single() is { VNum: 0, RareAmount: 0 } item && item.Slot == slot;

        private static WarehouseSubPacket Seeds(short slot, short amount) =>
            new() { Slot = slot, VNum = WorldData.Seed, Type = PocketType.Etc, Amount = amount, Rare = 0, Upgrade = 0 };

        private static WarehouseSubPacket Nothing(short slot) =>
            new() { Slot = slot, VNum = 0, Type = PocketType.Equipment, Amount = 0, Rare = 0, Upgrade = 0 };

        private static Func<StashClientPacket, bool> Stored(short slot, short amount) => Shows(Seeds(slot, amount));

        private static Func<StashClientPacket, bool> EmptyStorageSlot(short slot) => Shows(Nothing(slot));

        private static Func<StashClientPacket, bool> Shows(WarehouseSubPacket expected) =>
            packet => packet.Items?.Count == 1 && PacketAssert.Matches(expected, packet.Items[0]);

        private static Func<FStashClientPacket, bool> FamilyStored(short slot, short amount) =>
            packet => packet.Items?.Count == 1 && PacketAssert.Matches(Seeds(slot, amount), packet.Items[0]);

        private static DepositPacket Deposit(byte slot, short amount, byte newSlot, PocketType pocket = PocketType.Etc) =>
            new() { Inventory = pocket, Slot = slot, Amount = amount, NewSlot = newSlot, PartnerBackpack = false };

        private static WithdrawPacket Withdraw(short slot, short amount) => new() { Slot = slot, Amount = amount, PetBackpack = false };

        private static FDepositPacket FamilyDeposit(byte slot, short amount, byte newSlot) =>
            new() { Inventory = PocketType.Etc, Slot = slot, Amount = amount, NewSlot = newSlot, Unknown = 0 };

        private static FWithdrawPacket FamilyWithdraw(short slot, short amount) => new() { Slot = slot, Amount = amount, Unknown = 0 };

        private static bool MovesAnything(IPacket packet) => packet is IvnPacket or StashClientPacket;

        private static bool MovesAnythingOfTheFamily(IPacket packet) => packet is IvnPacket or FStashClientPacket;

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

            await alice.SendAsync(Deposit(0, 3, 1));

            await alice.ExpectSinceAsync(deposited, Bag(0, 2), "the two left in the bag");
            await alice.ExpectSinceAsync(deposited, Stored(1, 3), "the three in the warehouse");
            CollectionAssert.AreEqual(new[] { ((short)1, (short)3) }, await StoredInWarehouseAsync(player.CharacterId));
        }

        [TestMethod]
        public async Task AWholeStackDepositedEmptiesItsBagSlot()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            var deposited = alice.Mark();

            await alice.SendAsync(Deposit(0, 5, 0));

            await alice.ExpectSinceAsync(deposited, EmptyBagSlot(0), "the emptied bag slot");
            await alice.ExpectSinceAsync(deposited, Stored(0, 5), "the five in the warehouse");
            CollectionAssert.AreEqual(new[] { ((short)0, (short)5) }, await StoredInWarehouseAsync(player.CharacterId));
            Assert.AreEqual(0, await StoredInBagAsync(player.CharacterId), "nothing is left in the stored bag");
        }

        [TestMethod]
        public async Task AWithdrawalBringsItemsBackToTheBag()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync(Deposit(0, 5, 0));
            await alice.ExpectAsync(Stored(0, 5), "the five in the warehouse");
            var withdrawn = alice.Mark();

            await alice.SendAsync(Withdraw(0, 2));

            await alice.ExpectSinceAsync(withdrawn, Bag(0, 2), "the two back in the bag");
            await alice.ExpectSinceAsync(withdrawn, Stored(0, 3), "the three left in the warehouse");
            CollectionAssert.AreEqual(new[] { ((short)0, (short)3) }, await StoredInWarehouseAsync(player.CharacterId));

            var emptied = alice.Mark();
            await alice.SendAsync(Withdraw(0, 3));

            await alice.ExpectSinceAsync(emptied, Bag(0, 5), "all five back in the bag");
            await alice.ExpectSinceAsync(emptied, EmptyStorageSlot(0), "the emptied warehouse slot");
            Assert.AreEqual(0, (await StoredInWarehouseAsync(player.CharacterId)).Length);
        }

        [TestMethod]
        public async Task APartlyDepositedStackIsStoredAsSplit()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync(Deposit(0, 3, 0));
            await alice.ExpectAsync(Stored(0, 3), "the three in the warehouse");

            Assert.AreEqual(3, (await StoredInWarehouseAsync(player.CharacterId)).Sum(s => s.Amount));
            Assert.AreEqual(2, await StoredInBagAsync(player.CharacterId), "the stored bag holds what was not deposited");
        }

        [TestMethod]
        public async Task AWithdrawnItemIsStoredInTheBag()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync(Deposit(0, 5, 0));
            await alice.ExpectAsync(Stored(0, 5), "the five in the warehouse");

            await alice.SendAsync(Withdraw(0, 5));
            await alice.ExpectAsync(EmptyStorageSlot(0), "the emptied warehouse slot");

            Assert.AreEqual(0, (await StoredInWarehouseAsync(player.CharacterId)).Length);
            Assert.AreEqual(5, await StoredInBagAsync(player.CharacterId), "the stored bag holds what was taken out");
        }

        [TestMethod]
        public async Task WhatIsInTheWarehouseIsThereAfterComingBack()
        {
            var player = await OwnerAsync(5);
            await using (var alice = await EnterAsync(player))
            {
                await alice.SendAsync(Deposit(0, 5, 2));
                await alice.ExpectAsync(Stored(2, 5), "the five in the warehouse");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Assert.IsFalse(back.Received.OfType<InvPacket>().Any(p => p.Type == PocketType.Etc && p.IvnSubPackets?.Any(item => item.VNum == WorldData.Seed) == true),
                "the bag no longer holds them");
            var withdrawn = back.Mark();
            await back.SendAsync(Withdraw(2, 5));

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
            await alice.SendAsync(Deposit(0, 5, 3));
            await alice.ExpectAsync(Stored(3, 5), "the five in the warehouse");

            await alice.SendAsync(new GuriPacket { Type = GuriPacketType.PetBasket, Argument = 0 });

            PacketAssert.AreEqual(new StashAllPacket { WarehouseSize = WorldData.ChestSlots, Items = [Seeds(3, 5)] },
                await alice.ExpectAsync<StashAllPacket>());
        }

        [TestMethod]
        public async Task AStoredWeaponKeepsItsRarityAndUpgrade()
        {
            var player = await OwnerAsync(1);
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5, Upgrade = 7 },
                NoscorePocketType.Equipment);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync(Deposit(0, 1, 0, PocketType.Equipment));
            await alice.ExpectAsync(Shows(new WarehouseSubPacket
            {
                Slot = 0, VNum = WorldData.Blade, Type = PocketType.Equipment, Amount = 1, Rare = 5, Upgrade = 7
            }), "the weapon in the warehouse");
            var withdrawn = alice.Mark();
            await alice.SendAsync(Withdraw(0, 1));

            // In the equipment pocket a slot reads slot.vnum.rarity.upgrade.
            await alice.ExpectSinceAsync<IvnPacket>(withdrawn, p => p.Type == PocketType.Equipment
                && p.IvnSubPackets?.Single() is { Slot: 0, RareAmount: 5, UpgradeDesign: 7 } item && item.VNum == WorldData.Blade, "the weapon as it was");
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

            await alice.SendAsync(FamilyDeposit(0, 5, 1));

            await alice.ExpectSinceAsync(deposited, EmptyBagSlot(0), "the emptied bag slot");
            await alice.ExpectSinceAsync(deposited, FamilyStored(1, 5), "the five in the family warehouse");
            CollectionAssert.AreEqual(new[] { ((short)1, (short)5) }, await StoredInFamilyWarehouseAsync(family.FamilyId));

            await bob.SendAsync(new NrunPacket { Runner = NrunRunnerType.OpenFamilyWarehouse });
            PacketAssert.AreEqual(new FStashAllPacket { WarehouseSize = 4, Items = [Seeds(1, 5)] }, await bob.ExpectAsync<FStashAllPacket>());
            var withdrawn = bob.Mark();
            await bob.SendAsync(FamilyWithdraw(1, 2));

            await bob.ExpectSinceAsync(withdrawn, Bag(0, 2), "the two in Bob's bag");
            await bob.ExpectSinceAsync(withdrawn, FamilyStored(1, 3), "the three left in the family warehouse");
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
            await alice.SendAsync(FamilyDeposit(0, 5, 0));
            await alice.ExpectAsync(FamilyStored(0, 5), "the five in the family warehouse");
            var asked = bob.Mark();

            await bob.SendAsync(FamilyWithdraw(0, 5));

            await bob.ExpectSinceAsync<InfoiPacket>(asked, p => p.Message == Game18NConstString.NotQualifiedForWarehouse, "the not-qualified notice");
            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(asked, MovesAnythingOfTheFamily, TimeSpan.FromSeconds(1)));
            CollectionAssert.AreEqual(new[] { ((short)0, (short)5) }, await StoredInFamilyWarehouseAsync(family.FamilyId));
        }

        [TestMethod]
        public async Task SomeoneOutsideTheFamilyCannotTakeFromItsWarehouse()
        {
            var head = await OwnerAsync(5, chest: false);
            var family = await Harness.AddFamilyAsync(f => f.WarehouseSize = 4, head);
            await using var alice = await EnterAsync(head);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await alice.SendAsync(FamilyDeposit(0, 5, 0));
            await alice.ExpectAsync(FamilyStored(0, 5), "the five in the family warehouse");
            var asked = carol.Mark();

            await carol.SendAsync(FamilyWithdraw(0, 5));

            await carol.ExpectSinceAsync<InfoiPacket>(asked, p => p.Message == Game18NConstString.NotQualifiedForWarehouse, "the not-qualified notice");
            Assert.IsTrue(await carol.NoneSinceAsync<IPacket>(asked, MovesAnythingOfTheFamily, TimeSpan.FromSeconds(1)));
            CollectionAssert.AreEqual(new[] { ((short)0, (short)5) }, await StoredInFamilyWarehouseAsync(family.FamilyId));
        }

        [TestMethod]
        public async Task NothingIsDepositedWithoutAChest()
        {
            var player = await OwnerAsync(5, chest: false);
            await using var alice = await EnterAsync(player);
            var asked = alice.Mark();

            await alice.SendAsync(Deposit(0, 5, 0));

            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, MovesAnything, TimeSpan.FromSeconds(1)));
            Assert.AreEqual(0, (await StoredInWarehouseAsync(player.CharacterId)).Length);
        }

        [TestMethod]
        public async Task ASlotBeyondTheChestTakesNothing()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            var asked = alice.Mark();

            await alice.SendAsync(Deposit(0, 5, WorldData.ChestSlots));

            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, MovesAnything, TimeSpan.FromSeconds(1)));
            Assert.AreEqual(0, (await StoredInWarehouseAsync(player.CharacterId)).Length);
        }

        [TestMethod]
        public async Task AnOccupiedSlotTakesNothingMore()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync(Deposit(0, 2, 0));
            await alice.ExpectAsync(Stored(0, 2), "the two in the warehouse");
            var asked = alice.Mark();

            await alice.SendAsync(Deposit(0, 3, 0));

            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, MovesAnything, TimeSpan.FromSeconds(1)));
            CollectionAssert.AreEqual(new[] { ((short)0, (short)2) }, await StoredInWarehouseAsync(player.CharacterId));
        }

        [TestMethod]
        public async Task MoreThanIsStoredCannotBeWithdrawn()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync(Deposit(0, 3, 0));
            await alice.ExpectAsync(Stored(0, 3), "the three in the warehouse");
            var asked = alice.Mark();

            await alice.SendAsync(Withdraw(0, 4));

            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, MovesAnything, TimeSpan.FromSeconds(1)));
            CollectionAssert.AreEqual(new[] { ((short)0, (short)3) }, await StoredInWarehouseAsync(player.CharacterId));
        }

        [TestMethod]
        public async Task AStoredStackMovesToAnotherSlot()
        {
            var player = await OwnerAsync(5);
            await using var alice = await EnterAsync(player);
            await alice.SendAsync(Deposit(0, 5, 0));
            await alice.ExpectAsync(Stored(0, 5), "the five in the warehouse");
            var moved = alice.Mark();

            await alice.SendAsync(new ReposPacket { OldSlot = 0, Amount = 5, NewSlot = 3, PartnerBackpack = false });

            await alice.ExpectSinceAsync(moved, Stored(3, 5), "the five in their new slot");
            await alice.ExpectSinceAsync(moved, EmptyStorageSlot(0), "the emptied slot");
            CollectionAssert.AreEqual(new[] { ((short)3, (short)5) }, await StoredInWarehouseAsync(player.CharacterId));
        }
    }
}
