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
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class InventoryTests
    {
        private const short MostInASlot = 999;
        private const short BagSlots = 48;

        private static bool IsEmpty(IvnSubPacket slot) => slot.VNum is 0 or -1;

        private static Func<IvnPacket, bool> Slot(PocketType pocket, short slot, short vnum, short amount) =>
            packet => packet.Type == pocket && packet.IvnSubPackets?.Any(item => item.Slot == slot && item.VNum == vnum && item.RareAmount == amount) == true;

        private static Func<IvnPacket, bool> EmptySlot(PocketType pocket, short slot) =>
            packet => packet.Type == pocket && packet.IvnSubPackets?.FirstOrDefault()?.Slot == slot && IsEmpty(packet.IvnSubPackets[0]);

        private static Func<IvnPacket, bool> AnySlot(PocketType pocket, short slot) =>
            packet => packet.Type == pocket && packet.IvnSubPackets?.FirstOrDefault()?.Slot == slot;

        private static UseItemPacket Use(TestClient user, PocketType pocket) =>
            new() { VisualType = VisualType.Player, VisualId = user.CharacterId, Type = pocket, Slot = 0, Mode = 0, Parameter = 0 };

        private static Func<EqPacket, bool> LookOf(TestClient who) => packet => packet.VisualId == who.CharacterId;

        private static Func<DropPacket, bool> SeedsDropped => packet => packet.VNum == WorldData.Seed;

        private static GetPacket PickUp(TestClient picker, long drop) =>
            new() { PickerType = VisualType.Player, PickerId = picker.CharacterId, VisualId = drop, IsDelayed = 0 };

        private static BiPacket Destroy(RequestDeletionType? option = null) => new() { PocketType = PocketType.Etc, Slot = 0, Option = option };

        private static EquipmentSubPacket Blade(short rare, short upgrade) =>
            new() { EquipmentType = EquipmentType.MainWeapon, VNum = WorldData.Blade, Rare = rare, Upgrade = upgrade, Unknown = 0 };

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

        // What a pocket holds as the server lists it on entering, the amount being the rarity in the
        // equipment pocket.
        private static (int Slot, int VNum, int Amount)[] Listed(TestClient client, PocketType pocket) =>
            client.Received.OfType<InvPacket>().Where(p => p.Type == pocket)
                .SelectMany(p => p.IvnSubPackets ?? [])
                .OrderBy(slot => slot.Slot)
                .Select(slot => ((int)slot.Slot, (int)slot.VNum, (int)slot.RareAmount))
                .ToArray();

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

        private static void Same((int Slot, int VNum)[] expected, (int Slot, int VNum)[] actual, string? what = null) =>
            Assert.AreEqual(string.Join(' ', expected), string.Join(' ', actual), what);

        private static void Same(int[] expected, int[] actual, string? what = null) =>
            Assert.AreEqual(string.Join(' ', expected), string.Join(' ', actual), what);

        [TestMethod]
        public async Task AStackMovedToAnEmptySlotLeavesItsOldSlotEmpty()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync(new MviPacket { InventoryType = PocketType.Etc, Slot = 0, Amount = 5, DestinationSlot = 3 });

                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 3, WorldData.Seed, 5), "the stack in its new slot");
                await alice.ExpectSinceAsync(moved, EmptySlot(PocketType.Etc, 0), "the emptied slot");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(3, WorldData.Seed, 5)], Listed(back, PocketType.Etc));
        }

        [TestMethod]
        public async Task PartOfAStackMovedToAnEmptySlotSplitsIt()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync(new MviPacket { InventoryType = PocketType.Etc, Slot = 0, Amount = 2, DestinationSlot = 1 });

                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 1, WorldData.Seed, 2), "the two split off");
                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 0, WorldData.Seed, 3), "the three left behind");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Seed, 3), (1, WorldData.Seed, 2)], Listed(back, PocketType.Etc));
            Same([(0, WorldData.Seed, 3), (1, WorldData.Seed, 2)],
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

                await alice.SendAsync(new MviPacket { InventoryType = PocketType.Etc, Slot = 0, Amount = 5, DestinationSlot = 1 });

                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 1, WorldData.Seed, 12), "the merged stack");
                await alice.ExpectSinceAsync(moved, EmptySlot(PocketType.Etc, 0), "the emptied slot");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(1, WorldData.Seed, 12)], Listed(back, PocketType.Etc));
            Same([(1, WorldData.Seed, 12)], await StoredAsync(player.CharacterId, NoscorePocketType.Etc));
        }

        [TestMethod]
        public async Task AStackMovedOntoADifferentItemSwapsThem()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await Harness.GiveAsync(player, WorldData.Pebble, 3, slot: 1);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync(new MviPacket { InventoryType = PocketType.Etc, Slot = 0, Amount = 5, DestinationSlot = 1 });

                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 1, WorldData.Seed, 5), "the seeds in the pebbles' slot");
                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 0, WorldData.Pebble, 3), "the pebbles in the seeds' slot");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Pebble, 3), (1, WorldData.Seed, 5)], Listed(back, PocketType.Etc));
            Same([(0, WorldData.Pebble, 3), (1, WorldData.Seed, 5)], await StoredAsync(player.CharacterId, NoscorePocketType.Etc));
        }

        [TestMethod]
        public async Task PuttingOnASecondWeaponPutsTheFirstBackInItsBagSlot()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5, Upgrade = 7 },
                NoscorePocketType.Wear, (short)EquipmentType.MainWeapon);
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 2, Upgrade = 1 },
                NoscorePocketType.Equipment);
            await using (var alice = await EnterAsync(player))
            {
                var worn = alice.Mark();

                await alice.SendAsync(new WearPacket { InventorySlot = 0, PartnerSlot = 0 });

                var equipment = await alice.ExpectSinceAsync<EquipPacket>(worn, null, "what is worn");
                PacketAssert.AreEqual(Blade(2, 1), equipment.MainWeapon);
                await alice.ExpectSinceAsync(worn, Slot(PocketType.Equipment, 0, WorldData.Blade, 5), "the first weapon in the bag");
            }

            await using var back = await Harness.ComeBackAsync(player);
            PacketAssert.AreEqual(Blade(2, 1), back.Received.OfType<EquipPacket>().Single().MainWeapon);
            Same([(0, WorldData.Blade, 5)], Listed(back, PocketType.Equipment));
        }

        [TestMethod]
        public async Task PartOfAStackMovedOntoAnotherOfTheSameItemMovesOnlyThatMany()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await Harness.GiveAsync(player, WorldData.Seed, 7, slot: 1);
            await using var alice = await EnterAsync(player);
            var moved = alice.Mark();

            await alice.SendAsync(new MviPacket { InventoryType = PocketType.Etc, Slot = 0, Amount = 2, DestinationSlot = 1 });

            await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 1, WorldData.Seed, 9), "the stack that grew");
            await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 0, WorldData.Seed, 3), "the stack that shrank");
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

                await alice.SendAsync(new MviPacket { InventoryType = PocketType.Etc, Slot = 0, Amount = 600, DestinationSlot = 1 });

                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 1, WorldData.Seed, MostInASlot), "the full stack");
                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 0, WorldData.Seed, 600 + 700 - MostInASlot), "what did not fit");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Seed, 600 + 700 - MostInASlot), (1, WorldData.Seed, MostInASlot)],
                Listed(back, PocketType.Etc));
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
            await alice.SendAsync(new MviPacket { InventoryType = PocketType.Etc, Slot = 0, Amount = 5, DestinationSlot = 1 });

            await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 1, WorldData.Seed, 995), "the stack that grew by five");
            await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 0, WorldData.Seed, 595), "the stack that shrank by five");
        }

        [TestMethod]
        public async Task MovingMoreThanTheStackHoldsChangesNothing()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync(new MviPacket { InventoryType = PocketType.Etc, Slot = 0, Amount = 9, DestinationSlot = 1 });

                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 0, WorldData.Seed, 5), "the stack where it was");
                await alice.ExpectSinceAsync(moved, EmptySlot(PocketType.Etc, 1), "the slot still empty");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Seed, 5)], Listed(back, PocketType.Etc));
        }

        [TestMethod]
        public async Task AMoveBeyondTheLastSlotOfTheBagChangesNothing()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync(new MviPacket { InventoryType = PocketType.Etc, Slot = 0, Amount = 5, DestinationSlot = (byte)BagSlots });

                await alice.ExpectSinceAsync(moved, Slot(PocketType.Etc, 0, WorldData.Seed, 5), "the stack where it was");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Seed, 5)], Listed(back, PocketType.Etc));
        }

        [TestMethod]
        public async Task SortingAPocketPacksItFromTheFirstSlotInNameOrder()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5, slot: 9);
            await Harness.GiveAsync(player, WorldData.Pebble, 3, slot: 5);
            await using (var alice = await EnterAsync(player))
            {
                await alice.SendAsync(new ISortPacket { PocketType = PocketType.Etc });

                var sorted = await alice.ExpectAsync<IvnPacket>();
                var slots = sorted.IvnSubPackets ?? [];
                Assert.AreEqual(PocketType.Etc, sorted.Type);
                Same([(0, WorldData.Pebble, 3), (1, WorldData.Seed, 5)], slots.Take(2).Select(slot => ((int)slot.Slot, (int)slot.VNum, (int)slot.RareAmount)).ToArray());
                Same([5, 9], slots.Skip(2).Select(slot => (int)slot.Slot).ToArray(), "the slots they left are sent as well");
                Assert.IsTrue(slots.Skip(2).All(IsEmpty), "and are empty");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Pebble, 3), (1, WorldData.Seed, 5)], Listed(back, PocketType.Etc));
        }

        [TestMethod]
        public async Task SomethingWornCannotBeMovedAsIfItWereInABag()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5 },
                NoscorePocketType.Wear, (short)EquipmentType.MainWeapon);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using (var alice = await EnterAsync(player))
            {
                // 8 is no pocket a client names.
                await alice.SendAsync(new MviPacket { InventoryType = (PocketType)8, Slot = (short)EquipmentType.MainWeapon, Amount = 1, DestinationSlot = (byte)EquipmentType.Hat });
                await alice.WalkAsync(80, 117);
                await bob.ExpectAsync<MovePacket>(p => p is { VisualType: VisualType.Player, MapX: 80, MapY: 117 } && p.VisualEntityId == alice.CharacterId, "the step after it");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([((int)EquipmentType.MainWeapon, (int)WorldData.Blade, 1)], await StoredAsync(player.CharacterId, NoscorePocketType.Wear));
        }

        [TestMethod]
        public async Task PickingUpMoreThanAStackCanTakeFillsItAndStartsAnother()
        {
            var owner = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(owner, WorldData.Seed, MostInASlot - 2);
            var dropper = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await Harness.GiveAsync(dropper, WorldData.Seed, 5);
            await using var bob = await EnterAsync(dropper);
            IReadOnlyList<IPacket> told;
            await using (var alice = await EnterAsync(owner))
            {
                await bob.SendAsync(new PutPacket { PocketType = PocketType.Etc, Slot = 0, Amount = 5 });
                var drop = await alice.ExpectAsync(SeedsDropped, "the drop");
                var picked = alice.Mark();

                await alice.SendAsync(PickUp(alice, drop.VisualId));

                await alice.ExpectSinceAsync(picked, Slot(PocketType.Etc, 0, WorldData.Seed, MostInASlot), "the stack filled up");
                // The pick-up is shown to the map once the bag has been sent.
                await alice.ExpectSinceAsync<ServerGetPacket>(picked, p => p.VisualType == VisualType.Player && p.VisualId == alice.CharacterId, "the pick-up");
                told = alice.Since(picked);
            }

            await using var back = await Harness.ComeBackAsync(owner);
            Same([(0, WorldData.Seed, MostInASlot), (1, WorldData.Seed, 3)], Listed(back, PocketType.Etc));
            Assert.IsTrue(told.OfType<IvnPacket>().Any(Slot(PocketType.Etc, 1, WorldData.Seed, 3)), "the client is told of the rest, in a slot of its own");
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
            await bob.SendAsync(new PutPacket { PocketType = PocketType.Etc, Slot = 0, Amount = 5 });
            var dropId = (await alice.ExpectAsync(SeedsDropped, "the drop")).VisualId;
            var refused = alice.Mark();

            await alice.SendAsync(PickUp(alice, dropId));

            await alice.ExpectSinceAsync<MsgiPacket>(refused, null, "the refusal");
            var picked = bob.Mark();
            await bob.SendAsync(PickUp(bob, dropId));
            await bob.ExpectSinceAsync(picked, Slot(PocketType.Etc, 0, WorldData.Seed, 5), "the five still there to pick up");
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

                await alice.SendAsync(new WearPacket { InventorySlot = 0, PartnerSlot = 0 });

                await alice.ExpectSinceAsync(worn, EmptySlot(PocketType.Equipment, 0), "the emptied bag slot");
                var equipment = await alice.ExpectSinceAsync<EquipPacket>(worn, null, "what is worn");
                PacketAssert.AreEqual(Blade(5, 7), equipment.MainWeapon);
                var seen = await bob.ExpectAsync(LookOf(alice), "Alice's look");
                Assert.AreEqual(WorldData.Blade, seen.EqSubPacket?.MainWeapon, "the weapon in her hand");
                Assert.AreEqual(((byte?)7, (sbyte)5), (seen.WeaponUpgradeRarePacket?.Upgrade, seen.WeaponUpgradeRarePacket?.Rare ?? 0));
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, PocketType.Equipment));
            PacketAssert.AreEqual(Blade(5, 7), back.Received.OfType<EquipPacket>().Single().MainWeapon);
            Same([((int)EquipmentType.MainWeapon, (int)WorldData.Blade, 1)], await StoredAsync(player.CharacterId, NoscorePocketType.Wear));
        }

        [TestMethod]
        public async Task AWeaponTakenOffGoesBackToTheBagAndBystandersSeeTheEmptyHand()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5, Upgrade = 7 },
                NoscorePocketType.Wear, (short)EquipmentType.MainWeapon);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using (var alice = await EnterAsync(player))
            {
                PacketAssert.AreEqual(Blade(5, 7), alice.Received.OfType<EquipPacket>().Single().MainWeapon);
                var arrival = await bob.ExpectAsync<InPacket>(p => p.VisualType == VisualType.Player && p.VisualId == alice.CharacterId, "Alice arriving");
                Assert.AreEqual(WorldData.Blade, arrival.InCharacterSubPacket?.Equipment?.MainWeapon, "she arrives with the weapon in her hand");
                var removed = alice.Mark();

                await alice.SendAsync(new RemovePacket { InventorySlot = EquipmentType.MainWeapon, Type = 0 });

                await alice.ExpectSinceAsync(removed, Slot(PocketType.Equipment, 0, WorldData.Blade, 5), "the weapon in the bag");
                var equipment = await alice.ExpectSinceAsync<EquipPacket>(removed, null, "what is worn");
                Assert.IsNull(equipment.MainWeapon?.VNum, "nothing is worn any more");
                var seen = await bob.ExpectAsync(LookOf(alice), "Alice's look");
                Assert.IsNull(seen.EqSubPacket?.MainWeapon, "the empty hand");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Blade, 5)], Listed(back, PocketType.Equipment));
            Same([], await StoredAsync(player.CharacterId, NoscorePocketType.Wear));
        }

        [TestMethod]
        public async Task AHatPutOnIsSeenOnTheHeadByBystanders()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Hat, Amount = 1 }, NoscorePocketType.Equipment);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync(new WearPacket { InventorySlot = 0, PartnerSlot = 0 });

            var seen = await bob.ExpectAsync(LookOf(alice), "Alice's look");
            Assert.AreEqual(WorldData.Hat, seen.EqSubPacket?.Hat, "the hat on her head");

            await alice.SendAsync(new RemovePacket { InventorySlot = EquipmentType.Hat, Type = 0 });

            seen = await bob.ExpectAsync(LookOf(alice), "Alice's look");
            Assert.IsNull(seen.EqSubPacket?.Hat, "the bare head");
        }

        [TestMethod]
        public async Task AFairyPutOnAndTakenOffIsSeenByBystanders()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Fairy, Amount = 1, ElementRate = 12 },
                NoscorePocketType.Equipment);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(player);

            await alice.SendAsync(new WearPacket { InventorySlot = 0, PartnerSlot = 0 });

            // The fairy's own rate on top of its item's.
            PacketAssert.AreEqual(new PairyPacket
            {
                VisualType = VisualType.Player, VisualId = alice.CharacterId, FairyMoveType = 4, Element = ElementType.Fire, ElementRate = WorldData.FairyElementRate + 12, Morph = 0,
            }, await bob.ExpectAsync<PairyPacket>());
            var removed = alice.Mark();

            await alice.SendAsync(new RemovePacket { InventorySlot = EquipmentType.Fairy, Type = 0 });

            PacketAssert.AreEqual(new PairyPacket { VisualType = VisualType.Player, VisualId = alice.CharacterId }, await bob.ExpectAsync<PairyPacket>());
            await alice.ExpectSinceAsync(removed, Slot(PocketType.Equipment, 0, WorldData.Fairy, 0), "the fairy in the bag");
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

                await alice.SendAsync(Use(alice, PocketType.Etc));

                Assert.IsTrue(await bob.NoneSinceAsync(worn, LookOf(alice), TimeSpan.FromSeconds(1)), "nothing changes in what she wears");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Seed, 5)], Listed(back, PocketType.Etc));
        }

        [TestMethod]
        public async Task TakingSomethingOffWithAFullBagLeavesItOn()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Blade, Amount = 1, Rare = 5 },
                NoscorePocketType.Wear, (short)EquipmentType.MainWeapon);
            for (short slot = 0; slot < BagSlots; slot++)
            {
                await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.Hat, Amount = 1 }, NoscorePocketType.Equipment, slot);
            }

            await using (var alice = await EnterAsync(player))
            {
                var removed = alice.Mark();

                await alice.SendAsync(new RemovePacket { InventorySlot = EquipmentType.MainWeapon, Type = 0 });

                await alice.ExpectSinceAsync<MsgiPacket>(removed, null, "the refusal");
                Assert.IsFalse(alice.Since<IvnPacket>(removed).Any(), "the bag does not change");
            }

            await using var back = await Harness.ComeBackAsync(player);
            PacketAssert.AreEqual(Blade(5, 0), back.Received.OfType<EquipPacket>().Single().MainWeapon);
            Assert.AreEqual(BagSlots, Listed(back, PocketType.Equipment).Length);
        }

        [TestMethod]
        public async Task ACostumeMovesToTheCostumePocketAndBack()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.CostumeHat, Amount = 1 }, NoscorePocketType.Equipment);
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync(new MvePacket { InventoryType = PocketType.Equipment, Slot = 0, DestinationInventoryType = PocketType.Costume, DestinationSlot = 2 });

                var costume = await alice.ExpectSinceAsync(moved, AnySlot(PocketType.Costume, 2), "the costume pocket");
                Assert.AreEqual(WorldData.CostumeHat, costume.IvnSubPackets?[0].VNum);
                await alice.ExpectSinceAsync(moved, EmptySlot(PocketType.Equipment, 0), "the emptied bag slot");
                var back = alice.Mark();

                await alice.SendAsync(new MvePacket { InventoryType = PocketType.Costume, Slot = 2, DestinationInventoryType = PocketType.Equipment, DestinationSlot = 3 });

                await alice.ExpectSinceAsync(back, Slot(PocketType.Equipment, 3, WorldData.CostumeHat, 0), "the costume in the bag again");
                await alice.ExpectSinceAsync(back, EmptySlot(PocketType.Costume, 2), "the emptied costume slot");

                await alice.SendAsync(new MvePacket { InventoryType = PocketType.Equipment, Slot = 3, DestinationInventoryType = PocketType.Costume, DestinationSlot = 1 });
                await alice.ExpectAsync(AnySlot(PocketType.Costume, 1), "the costume pocket");
            }

            await using var again = await Harness.ComeBackAsync(player);
            Same([(1, WorldData.CostumeHat, 1)], await StoredAsync(player.CharacterId, NoscorePocketType.Costume));
            Same([], Listed(again, PocketType.Equipment));
        }

        [TestMethod]
        public async Task ACostumeMovedOntoATakenSlotIsNotShownAsGone()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.CostumeHat, Amount = 1 }, NoscorePocketType.Equipment);
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.CostumeHat, Amount = 1 }, NoscorePocketType.Costume);
            (int, int)[] toldBag;
            (int, int)[] toldCostumes;
            await using (var alice = await EnterAsync(player))
            {
                var moved = alice.Mark();

                await alice.SendAsync(new MvePacket { InventoryType = PocketType.Equipment, Slot = 0, DestinationInventoryType = PocketType.Costume, DestinationSlot = 0 });

                await alice.ExpectSinceAsync(moved, AnySlot(PocketType.Costume, 0), "the costume slot");
                await alice.ExpectSinceAsync(moved, AnySlot(PocketType.Equipment, 0), "the bag slot");
                toldBag = Held(alice.Since<IvnPacket>(moved).Last(AnySlot(PocketType.Equipment, 0)));
                toldCostumes = Held(alice.Since<IvnPacket>(moved).Last(AnySlot(PocketType.Costume, 0)));
            }

            // Whether the two swap or the move is refused, the client must be told what the server kept.
            await using var back = await Harness.ComeBackAsync(player);
            Same(Listed(back, PocketType.Equipment).Select(slot => (slot.Slot, slot.VNum)).ToArray(), toldBag,
                "the bag as the client was told against the bag as it is");
            Same(Listed(back, PocketType.Costume).Select(slot => (slot.Slot, slot.VNum)).ToArray(), toldCostumes,
                "the costume pocket as the client was told against the pocket as it is");
        }

        // What an ivn says the slots hold, leaving out those it says are empty.
        private static (int, int)[] Held(IvnPacket ivn) =>
            (ivn.IvnSubPackets ?? []).Where(slot => !IsEmpty(slot)).Select(slot => ((int)slot.Slot, (int)slot.VNum)).ToArray();

        [TestMethod]
        public async Task APotionRestoresHpAndIsUsedUp()
        {
            var player = await Harness.AddPlayerAsync("Alice", configure: c => c.Hp = 100);
            await Harness.GiveAsync(player, WorldData.Potion, 3, pocket: NoscorePocketType.Main);
            await using (var alice = await EnterAsync(player))
            {
                Assert.AreEqual(100, alice.Received.OfType<StatPacket>().Last().Hp);
                var drunk = alice.Mark();

                await alice.SendAsync(Use(alice, PocketType.Main));

                await alice.ExpectSinceAsync(drunk, Slot(PocketType.Main, 0, WorldData.Potion, 2), "the two potions left");
                var stat = await alice.ExpectSinceAsync<StatPacket>(drunk, null, "the HP after the potion");
                Assert.AreEqual(100 + WorldData.PotionHp, stat.Hp);
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Potion, 2)], Listed(back, PocketType.Main));
            Assert.IsTrue((await Harness.StoredAsync(player.CharacterId)).Hp >= 100 + WorldData.PotionHp, "the restored HP is stored");
        }

        [TestMethod]
        public async Task APotionNeverRestoresMoreThanTheMostHp()
        {
            var player = await Harness.AddPlayerAsync("Alice", configure: c => c.Hp = 200);
            await Harness.GiveAsync(player, WorldData.Potion, 2, pocket: NoscorePocketType.Main);
            await using var alice = await EnterAsync(player);
            var most = (int)alice.Received.OfType<StatPacket>().Last().HpMaximum;
            Assert.IsTrue(most < 200 + WorldData.PotionHp, "the potion would overfill");
            var drunk = alice.Mark();

            await alice.SendAsync(Use(alice, PocketType.Main));

            await alice.ExpectSinceAsync(drunk, Slot(PocketType.Main, 0, WorldData.Potion, 1), "the potion left");
            Assert.AreEqual(most, (await alice.ExpectSinceAsync<StatPacket>(drunk, null, "the HP after the potion")).Hp);
        }

        [TestMethod]
        public async Task TheLastPotionOfAStackEmptiesItsSlot()
        {
            var player = await Harness.AddPlayerAsync("Alice", configure: c => c.Hp = 100);
            await Harness.GiveAsync(player, WorldData.Potion, 1, pocket: NoscorePocketType.Main);
            IvnPacket told;
            await using (var alice = await EnterAsync(player))
            {
                var drunk = alice.Mark();

                await alice.SendAsync(Use(alice, PocketType.Main));

                told = await alice.ExpectSinceAsync(drunk, AnySlot(PocketType.Main, 0), "the slot the potion was in");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, PocketType.Main));
            Assert.IsTrue(EmptySlot(PocketType.Main, 0)(told), $"the client is told the slot is empty: {Wire.Serialize(told)}");
        }

        [TestMethod]
        public async Task APotionIsNotUsedUpAtFullHp()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Potion, 3, pocket: NoscorePocketType.Main);
            await using (var alice = await EnterAsync(player))
            {
                var drunk = alice.Mark();

                await alice.SendAsync(Use(alice, PocketType.Main));

                Assert.IsTrue(await alice.NoneSinceAsync<IvnPacket>(drunk, null, TimeSpan.FromSeconds(1)),
                    "no potion leaves the bag");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Potion, 3)], Listed(back, PocketType.Main));
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

                await alice.SendAsync(new PutPacket { PocketType = PocketType.Etc, Slot = 0, Amount = 9 });

                await alice.ExpectSinceAsync<SayiPacket>(dropped, p => p.VisualType == VisualType.Player && p.VisualId == alice.CharacterId, "the refusal");
                Assert.IsTrue(await bob.NoneSinceAsync(seen, SeedsDropped, TimeSpan.FromSeconds(1)), "nothing lies on the map");
                Assert.IsFalse(alice.Since<IvnPacket>(dropped).Any(), "the bag does not change");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([(0, WorldData.Seed, 5)], Listed(back, PocketType.Etc));
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

                await alice.SendAsync(new PutPacket { PocketType = PocketType.Etc, Slot = 0, Amount = 5 });

                await alice.ExpectSinceAsync(dropped, EmptySlot(PocketType.Etc, 0), "the emptied slot");
                var drop = await bob.ExpectAsync(SeedsDropped, "the drop");
                Assert.AreEqual(5, drop.Amount, "five were dropped");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, PocketType.Etc));
            Same([], await StoredAsync(player.CharacterId, NoscorePocketType.Etc));
        }

        [TestMethod]
        public async Task AnItemIsDestroyedOnlyOnceItHasBeenConfirmedTwice()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.Seed, 5);
            await using (var alice = await EnterAsync(player))
            {
                var asked = alice.Mark();

                await alice.SendAsync(Destroy());
                var first = await alice.ExpectAsync<DlgiPacket>();
                PacketAssert.AreEqual(Destroy(RequestDeletionType.Requested), first.YesPacket as BiPacket);
                PacketAssert.AreEqual(Destroy(RequestDeletionType.Declined), first.NoPacket as BiPacket);
                await alice.SendAsync(Destroy(RequestDeletionType.Requested));
                var second = await alice.ExpectAsync<DlgiPacket>();
                PacketAssert.AreEqual(Destroy(RequestDeletionType.Confirmed), second.YesPacket as BiPacket);
                PacketAssert.AreEqual(Destroy(RequestDeletionType.Declined), second.NoPacket as BiPacket);
                Assert.IsFalse(alice.Since<IvnPacket>(asked).Any(), "nothing is destroyed while it is only asked");

                await alice.SendAsync(Destroy(RequestDeletionType.Confirmed));

                await alice.ExpectAsync(EmptySlot(PocketType.Etc, 0), "the emptied slot");
            }

            await using var back = await Harness.ComeBackAsync(player);
            Same([], Listed(back, PocketType.Etc));
        }
    }
}
