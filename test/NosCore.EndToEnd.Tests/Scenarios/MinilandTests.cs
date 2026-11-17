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
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using MapType = NosCore.Packets.Enumerations.MapType;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class MinilandTests
    {
        // A portal is refused for four seconds after arriving on a map.
        private static readonly TimeSpan PortalRest = TimeSpan.FromSeconds(4.2);

        private static Func<InfoiPacket, bool> Told(Game18NConstString message) => packet => packet.Message == message;

        private static Func<MsgiPacket, bool> Announced(Game18NConstString message) => packet => packet.Message == message;

        private static Func<AtPacket, bool> InTheMiniland(TestClient who) =>
            packet => packet is { MapId: WorldData.MinilandMap, PositionX: 5, PositionY: 8, MapType: MapType.Miniland } && packet.CharacterId == who.CharacterId;

        private static Func<AtPacket, bool> BackInTown(TestClient who) =>
            packet => packet is { MapId: WorldData.Town, PositionX: 72, PositionY: 116, MapType: MapType.Normal } && packet.CharacterId == who.CharacterId;

        private static bool IsAMove(IPacket packet) => packet is AtPacket or CMapPacket;

        private static MJoinPacket Visit(VisualType type, TestClient owner) => new() { Type = type, VisualId = owner.CharacterId };

        private static MLEditPacket SetState(MinilandState state) => new() { Type = MinilandEditType.State, Value = ((byte)state).ToString(CultureInfo.InvariantCulture) };

        private static readonly AddobjPacket StandTheBench = new() { Slot = 0, PositionX = 10, PositionY = 12 };

        private static readonly RmvobjPacket TakeTheBenchAway = new() { Slot = 0 };

        private static Func<MlobjPacket, bool> BenchStood => packet => packet is { InUse: true, Slot: 0, MlobjSubPacket: { MapX: 10, MapY: 12 } };

        private static Func<MlobjPacket, bool> BenchTakenAway => packet => packet is { InUse: false, Slot: 0 };

        private static MlobjlstSubPacket? FirstObjectIn(TestClient client, int mark) =>
            client.Since<MlobjlstPacket>(mark).Last().MlobjlstSubPacket?.FirstOrDefault();

        private static string NameOf(TestClient client) => client.Received.OfType<CInfoPacket>().First().Name ?? string.Empty;

        private static Task<Player> OwnerAsync() => Harness.AddPlayerAsync("Alice", x: 71, y: 116);

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

        // The owner walks into the miniland through its portal: a portal only takes someone who
        // has moved since arriving, so she steps onto it or, standing on it already, beside it.
        private static async Task<int> GoHomeAsync(TestClient owner, short x = 70)
        {
            await Task.Delay(PortalRest);
            await owner.WalkAsync(x, 116);
            var entered = owner.Mark();
            await owner.SendAsync(new PreqPacket());
            await owner.ExpectSinceAsync(entered, InTheMiniland(owner), "the miniland");
            await owner.ExpectSinceAsync<MlobjlstPacket>(entered, null, "what stands in it");
            return entered;
        }

        private static async Task BefriendAsync(TestClient one, TestClient other)
        {
            await one.SendAsync(new FinsPacket { Type = FinsPacketType.Accepted, CharacterId = other.CharacterId });
            await other.ExpectAsync<Dlgi2Packet>(p => p.YesPacket is FinsPacket { Type: FinsPacketType.Accepted } yes && yes.CharacterId == one.CharacterId,
                "the friend request");
            var accepted = one.Mark();
            await other.SendAsync(new FinsPacket { Type = FinsPacketType.Accepted, CharacterId = one.CharacterId });
            await one.ExpectSinceAsync<FinitPacket>(accepted, p => p.SubPackets?.Any(f => f?.CharacterId == other.CharacterId) == true, "the new friend");
        }

        private static async Task SetStateAsync(TestClient owner, MinilandState state, Game18NConstString announcement)
        {
            var changed = owner.Mark();
            await owner.SendAsync(SetState(state));
            await owner.ExpectSinceAsync(changed, Announced(announcement), "the state of the miniland announced");
        }

        [TestMethod]
        public async Task TheOwnerWalksIntoTheMinilandAndIsShownItsState()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await Task.Delay(PortalRest);
            await alice.WalkAsync(70, 116);
            var entered = alice.Mark();

            await alice.SendAsync(new PreqPacket());

            await alice.ExpectSinceAsync(entered, InTheMiniland(alice), "the miniland");
            var info = await alice.ExpectSinceAsync<MlinfoPacket>(entered, null, "the miniland's state");
            Assert.AreEqual((0, 0), (info.DailyVisitCount, info.VisitCount), "the owner is no visitor");
            Assert.AreEqual(MinilandState.Open, info.MinilandState, "a new miniland is open");
        }

        [TestMethod]
        public async Task AVisitorIsShownTheOwnersMessageAsItWasWritten()
        {
            var owner = await OwnerAsync();
            await using (var context = Harness.Database.CreateContext())
            {
                context.Add(new Miniland
                {
                    MinilandId = Guid.NewGuid(), OwnerId = owner.CharacterId, State = MinilandState.Open,
                    MinilandMessage = "Come in and rest", WelcomeMusicInfo = 3800
                });
                await context.SaveChangesAsync();
            }

            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            var visited = bob.Mark();

            await bob.SendAsync(Visit(VisualType.Player, alice));

#pragma warning disable CS0618 // The owner's own words have no client text to send instead.
            await bob.ExpectSinceAsync<MsgPacket>(visited, p => p is { Type: MessageType.Default, Message: "Come in and rest" }, "the owner's message, spaces and all");
#pragma warning restore CS0618
        }

        [TestMethod]
        public async Task AFriendWalksIntoAnOpenMinilandAndIsCountedAsAVisitor()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            var visited = bob.Mark();
            var seen = alice.Mark();

            await bob.SendAsync(Visit(VisualType.Player, alice));

            await bob.ExpectSinceAsync(visited, InTheMiniland(bob), "the miniland");
            var info = await bob.ExpectSinceAsync<MlInfoBrPacket>(visited, null, "whose miniland it is");
            StringAssert.StartsWith(info.Name, "Alice");
            Assert.AreEqual((1, 1), (info.DailyVisitCount, info.VisitCount), "the first visitor of the day");
            await alice.ExpectSinceAsync<InPacket>(seen, p => p.VisualType == VisualType.Player && p.VisualId == bob.CharacterId, "the visitor arriving");
        }

        [TestMethod]
        public async Task AStrangerIsNotLetIntoAMiniland()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await GoHomeAsync(alice);
            var refused = bob.Mark();

            await bob.SendAsync(Visit(VisualType.Player, alice));

            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(refused, IsAMove, TimeSpan.FromSeconds(1)),
                "he stays where he is");
        }

        [TestMethod]
        public async Task ALockedMinilandRefusesEvenAFriend()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            await SetStateAsync(alice, MinilandState.Lock, Game18NConstString.MinilandLocked);
            var refused = bob.Mark();

            await bob.SendAsync(Visit(VisualType.Player, alice));

            await bob.ExpectSinceAsync(refused, Told(Game18NConstString.MinilandLocked), "the refusal");
            Assert.IsFalse(bob.Since(refused).Any(IsAMove), "he stays where he is");

            await SetStateAsync(alice, MinilandState.Open, Game18NConstString.MinilandPublic);
            var visited = bob.Mark();
            await bob.SendAsync(Visit(VisualType.Player, alice));
            await bob.ExpectSinceAsync(visited, InTheMiniland(bob), "the miniland, once it is open again");
        }

        [TestMethod]
        public async Task LockingAMinilandSendsItsVisitorsBackWhereTheyCameFrom()
        {
            await using var alice = await EnterAsync(await OwnerAsync());
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            await bob.SendAsync(Visit(VisualType.Player, alice));
            await bob.ExpectAsync(InTheMiniland(bob), "the miniland");
            var sentOut = bob.Mark();

            await alice.SendAsync(SetState(MinilandState.Lock));

            await bob.ExpectSinceAsync(sentOut, BackInTown(bob), "the town he came from");
        }

        [TestMethod]
        public async Task AVisitorIsSentBackWhenTheOwnerLeavesTheWorld()
        {
            var owner = await OwnerAsync();
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            int sentOut;
            await using (var alice = await EnterAsync(owner))
            {
                await BefriendAsync(alice, bob);
                await GoHomeAsync(alice);
                await bob.SendAsync(Visit(VisualType.Player, alice));
                await bob.ExpectAsync(InTheMiniland(bob), "the miniland");
                sentOut = bob.Mark();
            }

            await bob.ExpectSinceAsync(sentOut, BackInTown(bob), "the town he came from");
        }

        [TestMethod]
        public async Task SomethingIsStoodInAMinilandOnlyWhileItIsLocked()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using var alice = await EnterAsync(owner);
            var home = await GoHomeAsync(alice);
            Assert.IsTrue(FirstObjectIn(alice, home) is { Slot: 0, InUse: false, MlObjSubPacket: { MapX: 0, MapY: 0, Width: 1, Height: 1 } },
                "the bench is put away");
            var refused = alice.Mark();

            await alice.SendAsync(StandTheBench);

            await alice.ExpectSinceAsync(refused, Announced(Game18NConstString.InstallationOnlyLockMode), "the refusal");
            Assert.IsFalse(alice.Since<MlobjPacket>(refused).Any(), "nothing is stood there");
            await SetStateAsync(alice, MinilandState.Lock, Game18NConstString.MinilandLocked);
            var placed = alice.Mark();

            await alice.SendAsync(StandTheBench);

            PacketAssert.AreEqual(new MlobjPacket
            {
                InUse = true,
                Slot = 0,
                MlobjSubPacket = new MlobjSubPacket { MapX = 10, MapY = 12, Width = 1, Height = 1, Unknown = 0, DurabilityPoint = 0, Unknown2 = false, IsWarehouse = false },
            }, await alice.ExpectSinceAsync<MlobjPacket>(placed, null, "the bench where she stood it"));
            await alice.ExpectSinceAsync<MinilandPointPacket>(placed, null, "the points of the miniland");
        }

        [TestMethod]
        public async Task WhatStandsInAMinilandIsShownToItsVisitors()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            await BefriendAsync(alice, bob);
            await GoHomeAsync(alice);
            await SetStateAsync(alice, MinilandState.Lock, Game18NConstString.MinilandLocked);
            await alice.SendAsync(StandTheBench);
            await alice.ExpectAsync(BenchStood, "the bench where she stood it");
            await SetStateAsync(alice, MinilandState.Open, Game18NConstString.MinilandPublic);
            var visited = bob.Mark();

            await bob.SendAsync(Visit(VisualType.Player, alice));

            PacketAssert.AreEqual(new MltobjPacket { MltobjSubPacket = [new MltobjSubPacket { VNum = WorldData.Bench, Slot = 0, MapX = 10, MapY = 12 }] },
                await bob.ExpectSinceAsync<MltobjPacket>(visited, null, "what stands there"));
        }

        [TestMethod]
        public async Task SomethingIsTakenAwayOnlyWhileTheMinilandIsLocked()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using var alice = await EnterAsync(owner);
            await GoHomeAsync(alice);
            await SetStateAsync(alice, MinilandState.Lock, Game18NConstString.MinilandLocked);
            await alice.SendAsync(StandTheBench);
            await alice.ExpectAsync(BenchStood, "the bench where she stood it");
            await SetStateAsync(alice, MinilandState.Open, Game18NConstString.MinilandPublic);
            var refused = alice.Mark();

            await alice.SendAsync(TakeTheBenchAway);

            await alice.ExpectSinceAsync(refused, Announced(Game18NConstString.RemoveOnlyLockMode), "the refusal");
            Assert.IsFalse(alice.Since<MlobjPacket>(refused).Any(), "the bench stays");
            await SetStateAsync(alice, MinilandState.Lock, Game18NConstString.MinilandLocked);
            var removed = alice.Mark();

            await alice.SendAsync(TakeTheBenchAway);

            await alice.ExpectSinceAsync(removed, BenchTakenAway, "the bench taken away");
        }

        [TestMethod]
        public async Task WhatStandsInAMinilandIsStillThereWhenItsOwnerComesBack()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using (var alice = await EnterAsync(owner))
            {
                await GoHomeAsync(alice);
                await SetStateAsync(alice, MinilandState.Lock, Game18NConstString.MinilandLocked);
                await alice.SendAsync(StandTheBench);
                await alice.ExpectAsync(BenchStood, "the bench where she stood it");
            }

            await using var back = await Harness.ComeBackAsync(owner);
            var home = await GoHomeAsync(back, 71);

            Assert.IsTrue(FirstObjectIn(back, home) is { Slot: 0, InUse: true, MlObjSubPacket: { MapX: 10, MapY: 12, Width: 1, Height: 1 } },
                "the bench where she stood it");
            Assert.AreEqual(MinilandState.Lock, back.Since<MlinfoPacket>(home).Last().MinilandState, "and the miniland is still locked");
        }

        [TestMethod]
        public async Task WhatWasTakenAwayFromAMinilandStaysAwayWhenItsOwnerComesBack()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.Bench, 1, pocket: NoscorePocketType.Miniland);
            await using (var alice = await EnterAsync(owner))
            {
                await GoHomeAsync(alice);
                await SetStateAsync(alice, MinilandState.Lock, Game18NConstString.MinilandLocked);
                await alice.SendAsync(StandTheBench);
                await alice.ExpectAsync(BenchStood, "the bench where she stood it");
            }

            await using (var again = await Harness.ComeBackAsync(owner))
            {
                var home = await GoHomeAsync(again, 71);
                Assert.IsTrue(FirstObjectIn(again, home) is { Slot: 0, InUse: true, MlObjSubPacket: { MapX: 10, MapY: 12 } }, "the bench where she stood it");
                await SetStateAsync(again, MinilandState.Lock, Game18NConstString.MinilandLocked);
                await again.SendAsync(TakeTheBenchAway);
                await again.ExpectAsync(BenchTakenAway, "the bench taken away");
            }

            await using var back = await Harness.ComeBackAsync(owner);
            var returned = await GoHomeAsync(back);
            Assert.IsTrue(FirstObjectIn(back, returned) is { Slot: 0, InUse: false }, "the bench is put away");
        }

        [TestMethod]
        public async Task AnInvitationCostsASeedOfPowerAndLetsAStrangerIn()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.PowerSeed, 2);
            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            var bobName = NameOf(bob);
            await GoHomeAsync(alice);
            var invited = alice.Mark();

            await alice.SendAsync(new MinilandInvitePacket { CharacterName = bobName });

            var asked = await bob.ExpectAsync<Dlgi2Packet>();
            PacketAssert.AreEqual(Visit(VisualType.Player, alice), asked.YesPacket as MJoinPacket);
            PacketAssert.AreEqual(Visit(VisualType.Npc, alice), asked.NoPacket as MJoinPacket);
            Assert.AreEqual(Game18NConstString.HasInvitedToMiniland, asked.Question);
            await alice.ExpectSinceAsync<IvnPacket>(invited, p => p.Type == PocketType.Etc
                && p.IvnSubPackets?.SingleOrDefault() is { Slot: 0, VNum: WorldData.PowerSeed, RareAmount: 1 }, "the seed it cost");
            var visited = bob.Mark();

            await bob.SendAsync(Visit(VisualType.Player, alice));

            await bob.ExpectSinceAsync(visited, InTheMiniland(bob), "the miniland");
        }

        [TestMethod]
        public async Task ADeclinedInvitationLetsNobodyIn()
        {
            var owner = await OwnerAsync();
            await Harness.GiveAsync(owner, WorldData.PowerSeed, 2);
            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            var bobName = NameOf(bob);
            await GoHomeAsync(alice);
            await alice.SendAsync(new MinilandInvitePacket { CharacterName = bobName });
            await bob.ExpectAsync<Dlgi2Packet>();

            await bob.SendAsync(Visit(VisualType.Npc, alice));
            var late = bob.Mark();
            await bob.SendAsync(Visit(VisualType.Player, alice));

            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(late, IsAMove, TimeSpan.FromSeconds(1)),
                "he stays where he is");
        }

        [TestMethod]
        public async Task AnInvitationIsNotSentWithoutASeedOfPowerOrFromOutsideTheMiniland()
        {
            var owner = await OwnerAsync();
            await using var alice = await EnterAsync(owner);
            await using var bob = await Harness.EnterAsync("Bob", x: 72, y: 116);
            var bobName = NameOf(bob);
            var asked = bob.Mark();
            var outside = alice.Mark();

            await alice.SendAsync(new MinilandInvitePacket { CharacterName = bobName });

            await alice.ExpectSinceAsync(outside, Told(Game18NConstString.InvitationOnlyPossibleInMiniland), "the refusal outside the miniland");
            await GoHomeAsync(alice);
            var seedless = alice.Mark();

            await alice.SendAsync(new MinilandInvitePacket { CharacterName = bobName });

            await alice.ExpectSinceAsync(seedless, Told(Game18NConstString.NotEnoughSeedToInvite), "the refusal without a seed");
            Assert.IsFalse(bob.Since<Dlgi2Packet>(asked).Any(), "nobody is asked");
        }
    }
}
