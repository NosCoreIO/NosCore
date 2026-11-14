//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class MountTests
    {
        private const int MainPocket = 1;
        private const int MountPocket = 9;
        private const int Boosted = WorldData.SteedSpeed + WorldData.SteedBoost;

        // The bar the client waits out before it asks to be put on the mount.
        private static readonly TimeSpan MountingTime = TimeSpan.FromSeconds(3.2);

        private static Func<string, bool> Buff(TestClient rider, short card, int deciseconds) =>
            packet => packet.StartsWith($"bf 1 {rider.CharacterId} 0.{card}.{deciseconds} ", StringComparison.Ordinal);

        private static Func<string, bool> Riding(TestClient rider, int speed) =>
            packet => packet == $"cond 1 {rider.CharacterId} 0 0 {speed}";

        [TestMethod]
        public async Task ABoosterGivesTheMountsBuffAtOnceAndItsOtherOneWhenTheBoostRunsOut()
        {
            await using var alice = await RiderAsync("Alice", boosters: 2);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await MountAsync(alice);
            await bob.ExpectAsync(p => p.StartsWith($"c_mode 1 {alice.CharacterId} {WorldData.SteedLook} ", StringComparison.Ordinal),
                "Alice on her mount");

            var used = alice.Mark();
            var seen = bob.Mark();
            await alice.SendAsync($"u_i 1 {alice.CharacterId} {MainPocket} 0 0 0");

            await bob.ExpectSinceAsync(seen, Buff(alice, WorldData.BoostCard, WorldData.SteedBoostSeconds * 10), "the boost");
            await bob.ExpectSinceAsync(seen, Buff(alice, WorldData.SteedStartCard, WorldData.SteedCardTime), "the buff a booster gives on this mount");
            await alice.ExpectSinceAsync(used, Riding(alice, Boosted), "the boosted speed");
            await alice.ExpectSinceAsync(used,
                p => p.StartsWith($"ivn {MainPocket} 0.{WorldData.Booster}.1.", StringComparison.Ordinal), "one booster left");
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
            await alice.SendAsync($"u_i 1 {alice.CharacterId} {MainPocket} 0 0 0");

            await bob.ExpectSinceAsync(seen, p => p == $"rc 1 {alice.CharacterId} {WorldData.SteedHpPerLevel} 0", "the HP given back");
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
            await rider.SendAsync($"u_i 1 {rider.CharacterId} {MountPocket} 0 0 0");
            await rider.ExpectAsync("delay");
            await Task.Delay(MountingTime);
            await rider.SendAsync($"u_i 1 {rider.CharacterId} {MountPocket} 0 2 0");
            await rider.ExpectAsync(Riding(rider, WorldData.SteedSpeed), "the mount's speed");
        }
    }
}
