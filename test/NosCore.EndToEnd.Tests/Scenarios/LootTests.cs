//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class LootTests
    {
        private static Func<IvnPacket, bool> InTheBag(short vnum, int amount) =>
            packet => packet.Type == PocketType.Etc
                && packet.IvnSubPackets?.Single() is { Slot: 0 } item && item.VNum == vnum && item.RareAmount == amount;

        private static Func<InPacket, bool> PreyOnTheMap =>
            packet => packet.VisualType == VisualType.Monster && packet.VNum == WorldData.PreyVNum.ToString(CultureInfo.InvariantCulture)
                && packet.VisualId == WorldData.Prey;

        // Kills the prey once it is on the map, however many hits that takes, and returns what it left.
        private static async Task<DropPacket> KillPreyAsync(TestClient killer, int since)
        {
            await killer.ExpectSinceAsync(since, PreyOnTheMap, "the prey on the map");
            var (_, cast) = await Steps.KillAsync(killer, WorldData.Prey);
            return await killer.ExpectSinceAsync<DropPacket>(cast, p => p.VNum == WorldData.Pebble, "what it left");
        }

        private static GetPacket PickUp(TestClient picker, DropPacket drop) =>
            new() { PickerType = VisualType.Player, PickerId = picker.CharacterId, VisualId = drop.VisualId, IsDelayed = 0 };

        private static Func<ServerGetPacket, bool> PickedUpBy(TestClient picker) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == picker.CharacterId;

        private static Func<SayiPacket, bool> Refused(TestClient picker) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == picker.CharacterId
                && packet.Message == Game18NConstString.UnableToPickUp;

        private static bool IsAnnounced(IPacket packet) => packet is MsgiPacket { Message: Game18NConstString.ShareAsGroupChanged };

        private static bool IsRefused(IPacket packet) => packet is InfoiPacket { Message: Game18NConstString.CanNotChangeGroupMode };

        private static async Task GroupAsync(TestClient inviter, TestClient newcomer)
        {
            await inviter.SendAsync(new PjoinPacket { RequestType = GroupRequestType.Invited, CharacterId = newcomer.CharacterId });
            await newcomer.ExpectAsync<Dlgi2Packet>(p => p.YesPacket is PjoinPacket { RequestType: GroupRequestType.Accepted } yes
                && yes.CharacterId == inviter.CharacterId, "the group invitation");
            var joined = inviter.Mark();
            await newcomer.SendAsync(new PjoinPacket { RequestType = GroupRequestType.Accepted, CharacterId = inviter.CharacterId });
            await inviter.ExpectSinceAsync<PinitPacket>(joined, p => p.GroupSize == 2, "the group of two");
            await newcomer.ExpectAsync<PinitPacket>(p => p.GroupSize == 2, "the group of two");
        }

        [TestMethod]
        public async Task WhatAMonsterLeavesBelongsToItsKillerAtFirst()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 71);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 31, 71);

            var drop = await KillPreyAsync(alice, 0);

            Assert.AreEqual(WorldData.PreyDrop, drop.Amount);
            Assert.AreEqual(alice.CharacterId, drop.OwnerId, "the killer owns it");
            var refused = bob.Mark();
            var watched = alice.Mark();

            await bob.SendAsync(PickUp(bob, drop));

            await bob.ExpectSinceAsync(refused, Refused(bob), "the refusal");
            Assert.IsFalse(bob.Since<IvnPacket>(refused).Any(), "nothing reaches the bystander's bag");
            Assert.IsFalse(alice.Since<ServerGetPacket>(watched).Any(PickedUpBy(bob)), "nothing is picked up");
            var picked = alice.Mark();

            await alice.SendAsync(PickUp(alice, drop));

            await alice.ExpectSinceAsync(picked, InTheBag(WorldData.Pebble, WorldData.PreyDrop), "the pebbles in the killer's bag");
            await bob.ExpectSinceAsync<ServerGetPacket>(refused, p => PickedUpBy(alice)(p) && p.ItemId == drop.VisualId, "the pick-up");
        }

        [TestMethod]
        public async Task AGroupLootingInTurnHandsEachDropToTheNextMember()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 71);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 31, 71);
            await GroupAsync(alice, bob);

            var first = await KillPreyAsync(alice, 0);
            var again = alice.Mark();
            var second = await KillPreyAsync(alice, again);

            CollectionAssert.AreEquivalent(new long?[] { alice.CharacterId, bob.CharacterId },
                new[] { first.OwnerId, second.OwnerId }, "one drop each, whoever killed");

            var owner = second.OwnerId == bob.CharacterId ? bob : alice;
            var other = owner == bob ? alice : bob;
            var refused = other.Mark();
            await other.SendAsync(PickUp(other, second));
            await other.ExpectSinceAsync(refused, Refused(other), "the member whose turn it is not, refused");
            var picked = owner.Mark();
            await owner.SendAsync(PickUp(owner, second));
            await owner.ExpectSinceAsync(picked, InTheBag(WorldData.Pebble, WorldData.PreyDrop), "the pebbles in the bag of the member whose turn it is");
        }

        [TestMethod]
        public async Task WhenAGroupSharesItsLootAnyMemberMayPickUpWhatAnotherKilledFor()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 71);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 31, 71);
            await GroupAsync(alice, bob);
            var shared = bob.Mark();
            var asked = alice.Mark();
            // Whichever of the two leads: the other one is refused, and only one of them asks at a time.
            await alice.SendAsync(new GopPacket { Option = GopPacketType.GroupSharing, IsActive = true });
            var answer = await alice.ExpectSinceAsync<IPacket>(asked, p => IsAnnounced(p) || IsRefused(p), "the answer to the loot mode");
            if (IsRefused(answer))
            {
                await bob.SendAsync(new GopPacket { Option = GopPacketType.GroupSharing, IsActive = true });
            }

            await bob.ExpectSinceAsync<MsgiPacket>(shared, IsAnnounced, "the new way of looting announced");

            var drop = await KillPreyAsync(alice, 0);

            Assert.AreEqual(alice.CharacterId, drop.OwnerId, "the killer owns it");
            var picked = bob.Mark();
            await bob.SendAsync(PickUp(bob, drop));
            await bob.ExpectSinceAsync(picked, InTheBag(WorldData.Pebble, WorldData.PreyDrop), "the pebbles in the other member's bag");
        }

        [TestMethod]
        public async Task SomethingLyingTooFarAwayCannotBePickedUp()
        {
            var owner = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 20, 85);
            await Harness.GiveAsync(owner, WorldData.Seed, 5);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 32, 85);
            await using var alice = Harness.Client(owner);
            await alice.EnterWorldAsync(owner.Slot);
            await alice.SendAsync(new PutPacket { PocketType = PocketType.Etc, Slot = 0, Amount = 5 });
            var drop = await bob.ExpectAsync<DropPacket>(p => p.VNum == WorldData.Seed, "the drop");
            var reached = bob.Mark();
            var watched = alice.Mark();

            await bob.SendAsync(PickUp(bob, drop));

            Assert.IsTrue(await alice.NoneSinceAsync(watched, PickedUpBy(bob), TimeSpan.FromSeconds(1)),
                "nothing is picked up from eleven cells away");
            Assert.IsFalse(bob.Since<IvnPacket>(reached).Any(), "and nothing reaches his bag");
            var picked = alice.Mark();
            await alice.SendAsync(PickUp(alice, drop));
            await alice.ExpectSinceAsync(picked, InTheBag(WorldData.Seed, 5), "the seeds still there for her");
        }
    }
}
