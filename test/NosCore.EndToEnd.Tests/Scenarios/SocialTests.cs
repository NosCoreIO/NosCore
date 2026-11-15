//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class SocialTests
    {
        private static FinsPacket Befriend(TestClient other) => new() { Type = FinsPacketType.Accepted, CharacterId = other.CharacterId };

        private static Func<FinitPacket, bool> Lists(TestClient friend) =>
            packet => packet.SubPackets?.FirstOrDefault()?.CharacterId == friend.CharacterId;

        private static PjoinPacket Join(GroupRequestType type, TestClient other) => new() { RequestType = type, CharacterId = other.CharacterId };

        private static Func<Dlgi2Packet, bool> InvitedBy(TestClient other) =>
            packet => packet.YesPacket is PjoinPacket { RequestType: GroupRequestType.Accepted } yes && yes.CharacterId == other.CharacterId;

        [TestMethod]
        public async Task AnAcceptedFriendRequestPutsEachInTheOthersList()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            await alice.SendAsync(Befriend(bob));
            await bob.ExpectAsync<Dlgi2Packet>(p => p.YesPacket is FinsPacket { Type: FinsPacketType.Accepted } yes && yes.CharacterId == alice.CharacterId
                && p.NoPacket is FinsPacket { Type: FinsPacketType.Rejected } no && no.CharacterId == alice.CharacterId, "the friend request");
            var accepted = alice.Mark();
            await bob.SendAsync(Befriend(alice));

            await alice.ExpectSinceAsync(accepted, Lists(bob), "Bob in Alice's list");
            await bob.ExpectSinceAsync(0, Lists(alice), "Alice in Bob's list");
        }

        [TestMethod]
        public async Task AFriendMessageReachesTheFriend()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await alice.SendAsync(Befriend(bob));
            await bob.ExpectAsync<Dlgi2Packet>();
            await bob.SendAsync(Befriend(alice));
            await alice.ExpectAsync(Lists(bob), "Bob in Alice's list");

            await alice.SendAsync(new BtkPacket { CharacterId = bob.CharacterId, Message = "lunch at noon" });

            PacketAssert.AreEqual(new TalkPacket { CharacterId = alice.CharacterId, Message = "lunch at noon" }, await bob.ExpectAsync<TalkPacket>());
        }

        [TestMethod]
        public async Task AnAcceptedInvitationFormsAGroup()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            await alice.SendAsync(Join(GroupRequestType.Invited, bob));
            await bob.ExpectAsync(InvitedBy(alice), "the group invitation");
            var joined = alice.Mark();
            await bob.SendAsync(Join(GroupRequestType.Accepted, alice));

            var group = await alice.ExpectSinceAsync<PinitPacket>(joined, p => p.GroupSize == 2, "the group of two");
            CollectionAssert.AreEquivalent(new[] { alice.CharacterId, bob.CharacterId },
                (group.PinitSubPackets ?? []).Select(member => member?.VisualId ?? 0).ToArray());
        }

        [TestMethod]
        public async Task GroupedPlayersWhoLeaveAtTheSameMomentAreAllStoredWhereTheyStood()
        {
            var players = new[]
            {
                await Harness.AddPlayerAsync("Alice", x: 80, y: 116),
                await Harness.AddPlayerAsync("Bob", x: 81, y: 116),
                await Harness.AddPlayerAsync("Carol", x: 82, y: 116)
            };
            var clients = players.Select(player => Harness.Client(player)).ToArray();
            try
            {
                for (var i = 0; i < clients.Length; i++)
                {
                    await clients[i].EnterWorldAsync(players[i].Slot);
                }

                var alice = clients[0];
                // The others ask to join in turn: one player may only send a group request every five seconds.
                for (var i = 1; i < clients.Length; i++)
                {
                    await clients[i].SendAsync(Join(GroupRequestType.Invited, alice));
                    await alice.ExpectAsync(InvitedBy(clients[i]), "the group request");
                    var joined = clients[i].Mark();
                    await alice.SendAsync(Join(GroupRequestType.Accepted, clients[i]));
                    var size = i + 1;
                    await clients[i].ExpectSinceAsync<PinitPacket>(joined, p => p.GroupSize == size, $"the group of {size}");
                }

                for (var i = 0; i < clients.Length; i++)
                {
                    var watcher = clients[(i + 1) % clients.Length];
                    await clients[i].WalkAsync((short)(80 + i), 117);
                    var walker = clients[i];
                    var x = 80 + i;
                    await watcher.ExpectAsync<MovePacket>(p => p.VisualType == VisualType.Player && p.VisualEntityId == walker.CharacterId
                        && p.MapX == x && p.MapY == 117, "the step");
                }
            }
            finally
            {
                await Task.WhenAll(clients.Select(client => client.DisposeAsync().AsTask()));
            }

            var waited = Stopwatch.StartNew();
            var pending = players.ToList();
            while (pending.Count > 0 && waited.Elapsed < TimeSpan.FromSeconds(15))
            {
                foreach (var player in pending.ToList())
                {
                    if ((await Harness.StoredAsync(player.CharacterId)).MapY == 117)
                    {
                        pending.Remove(player);
                    }
                }

                await Task.Delay(250);
            }

            Assert.AreEqual(string.Empty, string.Join(", ", pending.Select(p => p.Name)), "not stored where they stood within 15 s of leaving");
        }

        [TestMethod]
        public async Task WhatIsSaidToTheGroupReachesItsMembersOnly()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);
            await using var carol = await Harness.EnterAsync("Carol", x: 82, y: 116);
            await alice.SendAsync(Join(GroupRequestType.Invited, bob));
            await bob.ExpectAsync(InvitedBy(alice), "the group invitation");
            await bob.SendAsync(Join(GroupRequestType.Accepted, alice));
            await alice.ExpectAsync<PinitPacket>(p => p.GroupSize == 2, "the group of two");
            var said = carol.Mark();

            await alice.SendAsync(new GroupTalkPacket { Message = "pull the next one" });

            var heard = await bob.ExpectAsync<SpeakPacket>();
            Assert.AreEqual((VisualType.Player, alice.CharacterId, SpeakType.Group), (heard.VisualType, heard.VisualId, heard.SpeakType));
            StringAssert.EndsWith(heard.Message, "pull the next one");
            Assert.IsTrue(await carol.NoneSinceAsync<SpeakPacket>(said, null, TimeSpan.FromSeconds(1)),
                "someone outside the group hears nothing");
        }
    }
}
