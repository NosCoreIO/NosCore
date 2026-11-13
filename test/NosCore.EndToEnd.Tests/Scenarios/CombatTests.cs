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

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class CombatTests
    {
        private const int Monster = 3;

        [TestMethod]
        public async Task AHitTakesHpFromAMonsterAndEveryoneSeesIt()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 60, 61);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 62, 62);

            await alice.SendAsync($"u_s 0 {Monster} {WorldData.SturdyMonster}");

            var hit = (await bob.ExpectAsync(p => p.StartsWith($"su 1 {alice.CharacterId} {Monster} {WorldData.SturdyMonster} 200 ", StringComparison.Ordinal),
                "the hit")).Split(' ');
            Assert.AreEqual("1", hit[11], "the monster is still alive");
            Assert.IsTrue(int.Parse(hit[13], CultureInfo.InvariantCulture) > 0, "the hit does damage");
        }

        [TestMethod]
        public async Task ADeadMonsterIsNotShownToSomeoneEnteringTheMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 45, 46);
            Assert.IsTrue(alice.Received.Any(p => p.StartsWith(In(WorldData.SlowMonster), StringComparison.Ordinal)), "alive, it is shown");

            await alice.SendAsync($"u_s 0 {Monster} {WorldData.SlowMonster}");
            var hit = (await alice.ExpectAsync(p => p.StartsWith($"su 1 {alice.CharacterId} {Monster} {WorldData.SlowMonster} 200 ", StringComparison.Ordinal),
                "the killing hit")).Split(' ');
            Assert.AreEqual("0", hit[11], "the monster is dead");

            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 47, 47);

            Assert.IsFalse(bob.Received.Any(p => p.Split(' ') is ["in", "3", _, var id, ..] && id == WorldData.SlowMonster.ToString(CultureInfo.InvariantCulture)),
                "a monster waiting to respawn is not on the map for a newcomer");
            Assert.IsTrue(bob.Received.Any(p => p.StartsWith(In(WorldData.SturdyMonster), StringComparison.Ordinal)), "the living ones are");
        }

        private static string In(int monster) => monster switch
        {
            WorldData.SlowMonster => $"in 3 4 {monster} ",
            WorldData.SturdyMonster => $"in 3 3 {monster} ",
            _ => $"in 3 2 {monster} "
        };

        [TestMethod]
        public async Task KillingAMonsterGivesExperience()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 31);
            var before = alice.Mark();

            await alice.SendAsync($"u_s 0 {Monster} {WorldData.WeakMonster}");

            var hit = (await alice.ExpectAsync(p => p.StartsWith($"su 1 {alice.CharacterId} {Monster} {WorldData.WeakMonster} 200 ", StringComparison.Ordinal),
                "the killing hit")).Split(' ');
            Assert.AreEqual("0", hit[11], "the monster is dead");
            var level = (await alice.ExpectSinceAsync(before, p => TestClient.HeaderOf(p) == "lev", "the experience")).Split(' ');
            Assert.IsTrue(long.Parse(level[2], CultureInfo.InvariantCulture) > 0, "experience was gained");
        }
    }
}
