//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class CombatTests
    {
        private static async Task<SuPacket> HitAsync(TestClient attacker, TestClient watcher, int monster) =>
            (await Steps.HitUntilAsync(attacker, watcher, monster, hit => hit.SkillVnum == 200 && hit.Damage != 0)).Hit;

        [TestMethod]
        public async Task AHitTakesHpFromAMonsterAndEveryoneSeesIt()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 60, 61);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 62, 62);

            var hit = await HitAsync(alice, bob, WorldData.SturdyMonster);

            Assert.IsTrue(hit.TargetIsAlive, "the monster is still alive");
            Assert.IsTrue(hit.Damage > 0, "the hit does damage");
        }

        [TestMethod]
        public async Task ADeadMonsterIsNotShownToSomeoneEnteringTheMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 45, 46);
            Assert.IsTrue(alice.Received.OfType<InPacket>().Any(Shows(WorldData.SlowMonster)), "alive, it is shown");

            var hit = await HitAsync(alice, alice, WorldData.SlowMonster);
            Assert.IsFalse(hit.TargetIsAlive, "the monster is dead");

            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 47, 47);

            Assert.IsFalse(bob.Received.OfType<InPacket>().Any(p => p.VisualType == VisualType.Monster && p.VisualId == WorldData.SlowMonster),
                "a monster waiting to respawn is not on the map for a newcomer");
            Assert.IsTrue(bob.Received.OfType<InPacket>().Any(Shows(WorldData.SturdyMonster)), "the living ones are");
        }

        private static Func<InPacket, bool> Shows(int monster) => packet =>
            packet.VisualType == VisualType.Monster && packet.VisualId == monster && packet.VNum == (monster switch
            {
                WorldData.SlowMonster => "4",
                WorldData.SturdyMonster => "3",
                _ => "2"
            });

        [TestMethod]
        public async Task KillingAMonsterGivesExperience()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 30, 31);
            var before = alice.Mark();

            var hit = await HitAsync(alice, alice, WorldData.WeakMonster);

            Assert.IsFalse(hit.TargetIsAlive, "the monster is dead");
            var level = await alice.ExpectSinceAsync<LevPacket>(before, null, "the experience");
            Assert.IsTrue(level.LevelXp > 0, "experience was gained");
        }
    }
}
