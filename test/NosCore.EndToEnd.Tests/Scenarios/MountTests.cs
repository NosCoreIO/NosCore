//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class MountTests
    {
        private const int Boosted = WorldData.SteedSpeed + WorldData.SteedBoost;

        // The bar the client waits out before it asks to be put on the mount.
        private static readonly TimeSpan MountingTime = TimeSpan.FromSeconds(3.2);

        private static Func<BfPacket, bool> Buff(TestClient rider, short card, int deciseconds) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == rider.CharacterId
                && packet.Buff is { ChargeValue: 0 } buff && buff.BuffId == card && buff.Duration == deciseconds;

        private static Func<CondPacket, bool> Riding(TestClient rider, int speed) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == rider.CharacterId
                && !packet.NoAttack && !packet.NoMove && packet.Speed == speed;

        private static UseItemPacket Use(TestClient user, PocketType pocket, short mode = 0) => new()
        {
            VisualType = VisualType.Player,
            VisualId = user.CharacterId,
            Type = pocket,
            Slot = 0,
            Mode = mode,
            Parameter = 0,
        };

        [TestMethod]
        public async Task ABoosterGivesTheMountsBuffAtOnceAndItsOtherOneWhenTheBoostRunsOut()
        {
            await using var alice = await RiderAsync("Alice", boosters: 2);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await MountAsync(alice);
            await bob.ExpectAsync<CModePacket>(p => p.VisualType == VisualType.Player && p.VisualId == alice.CharacterId
                && p.Morph == WorldData.SteedLook, "Alice on her mount");

            var used = alice.Mark();
            var seen = bob.Mark();
            await alice.SendAsync(Use(alice, PocketType.Main));

            await bob.ExpectSinceAsync(seen, Buff(alice, WorldData.BoostCard, WorldData.SteedBoostSeconds * 10), "the boost");
            await bob.ExpectSinceAsync(seen, Buff(alice, WorldData.SteedStartCard, WorldData.SteedCardTime), "the buff a booster gives on this mount");
            await alice.ExpectSinceAsync(used, Riding(alice, Boosted), "the boosted speed");
            await alice.ExpectSinceAsync<IvnPacket>(used, p => p.Type == PocketType.Main
                && p.IvnSubPackets?.Single() is { Slot: 0 } item && item.VNum == WorldData.Booster && item.RareAmount == 1, "one booster left");
            Assert.IsTrue(await bob.NoneSinceAsync(seen, Buff(alice, WorldData.SteedEndCard, WorldData.SteedCardTime), TimeSpan.FromSeconds(1)),
                "nothing more while the boost lasts");

            await bob.ExpectSinceAsync(seen, Buff(alice, WorldData.BoostCard, 0), "the boost running out");
            await bob.ExpectSinceAsync(seen, Buff(alice, WorldData.SteedEndCard, WorldData.SteedCardTime), "the buff the boost leaves behind");
            await alice.ExpectSinceAsync(used, Riding(alice, WorldData.SteedSpeed), "the mount's own speed again");
        }

        [TestMethod]
        public async Task ABoosterOnAMountThatRestoresHpHealsItsRiderByTheLevel()
        {
            await using var alice = await RiderAsync("Alice", boosters: 1, configure: c => c.Hp = 100);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await MountAsync(alice);

            var seen = bob.Mark();
            await alice.SendAsync(Use(alice, PocketType.Main));

            await bob.ExpectSinceAsync<RcPacket>(seen, p => p.VisualType == VisualType.Player && p.VisualId == alice.CharacterId
                && p.HealAmount == WorldData.SteedHpPerLevel && p.Unknown == 0, "the HP given back");
        }

        private static async Task<TestClient> RiderAsync(string name, short boosters, Action<Character>? configure = null)
        {
            var player = await Harness.AddPlayerAsync(name, configure: configure);
            await Harness.GiveAsync(player, WorldData.Steed, 1, pocket: NoscorePocketType.Mount);
            await Harness.GiveAsync(player, WorldData.Booster, boosters, pocket: NoscorePocketType.Main);
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

        private static async Task MountAsync(TestClient rider)
        {
            await rider.SendAsync(Use(rider, PocketType.Mount));
            await rider.ExpectAsync<DelayPacket>();
            await Task.Delay(MountingTime);
            await rider.SendAsync(Use(rider, PocketType.Mount, mode: 2));
            await rider.ExpectAsync(Riding(rider, WorldData.SteedSpeed), "the mount's speed");
        }
    }
}
