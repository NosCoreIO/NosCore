//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FightTests
    {
        private const int Player = 1;
        private const int Monster = 3;
        private const int MainPocket = 1;
        private const int MostHp = 221;
        private const int MostMp = 60;
        private const int ReviveHere = 0;
        private const int ReviveAtTheSavePoint = 1;
        private const int ReviveHereForFree = 570;
        private const int RangedSkill = 1;
        private const int RangedSkillRange = 7;

        // su <type> <id> <target type> <target id> <skill> <cooldown> <animation> <effect> <x> <y> <target alive> <target hp %> <damage> ...
        private static Func<string, bool> HitBy(TestClient attacker, int monster) =>
            packet => packet.StartsWith($"su {Player} {attacker.CharacterId} {Monster} {monster} ", StringComparison.Ordinal);

        private static Func<string, bool> HitOn(TestClient victim, int monster) =>
            packet => packet.StartsWith($"su {Monster} {monster} {Player} {victim.CharacterId} ", StringComparison.Ordinal);

        private static Func<string, bool> Stat => packet => TestClient.HeaderOf(packet) == "stat";

        private static int Hp(string stat) => int.Parse(stat.Split(' ')[1], CultureInfo.InvariantCulture);

        private static int Mp(string stat) => int.Parse(stat.Split(' ')[3], CultureInfo.InvariantCulture);

        // A hit may miss, and a monster that was missed does not mind: she hits until one lands.
        private static async Task<int> ProvokeAsync(TestClient attacker)
        {
            var provoked = attacker.Mark();
            var tried = Stopwatch.StartNew();
            while (true)
            {
                var cast = attacker.Mark();
                await attacker.SendAsync($"u_s 0 {Monster} {WorldData.Biter}");
                var hit = (await attacker.ExpectSinceAsync(cast, HitBy(attacker, WorldData.Biter), "her hit")).Split(' ');
                if (hit[13] != "0")
                {
                    return provoked;
                }

                Assert.IsTrue(tried.Elapsed < TimeSpan.FromSeconds(30), "no hit landed in thirty seconds");
                await attacker.ExpectSinceAsync(cast, p => p == "sr 0", "the skill ready again");
            }
        }

        // With little HP left, hitting the biter is a sure way to die.
        private static async Task<int> DieAsync(TestClient victim)
        {
            var provoked = await ProvokeAsync(victim);
            await victim.ExpectSinceAsync(provoked, p => HitOn(victim, WorldData.Biter)(p) && p.Split(' ')[11] == "0", "the blow that kills",
                TimeSpan.FromSeconds(20));
            await victim.ExpectSinceAsync(provoked, p => p.StartsWith($"dlgi #revival^{ReviveHere} #revival^{ReviveAtTheSavePoint} ", StringComparison.Ordinal),
                "the question how to revive");
            return provoked;
        }

        [TestMethod]
        public async Task AMonsterThatIsHitHitsBackAndTakesHp()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 80, 21);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 82, 22);
            Assert.IsTrue(await alice.NoneSinceAsync(0, HitOn(alice, WorldData.Biter), TimeSpan.FromSeconds(1)), "left alone, it leaves her alone");

            var hit = await ProvokeAsync(alice);

            var blow = (await bob.ExpectAsync(p => HitOn(alice, WorldData.Biter)(p) && p.Split(' ')[13] != "0", "the biter hitting back",
                TimeSpan.FromSeconds(20))).Split(' ');
            var damage = int.Parse(blow[13], CultureInfo.InvariantCulture);
            Assert.AreEqual("1", blow[11], "she is still alive");
            Assert.AreEqual(((MostHp - damage) * 100 / MostHp).ToString(CultureInfo.InvariantCulture), blow[12], "the part of her HP she has left");
            var stat = await alice.ExpectSinceAsync(hit, p => Stat(p) && Hp(p) < MostHp, "her HP after the blow");
            Assert.AreEqual(MostHp - damage, Hp(stat));
        }

        [TestMethod]
        public async Task ACharacterWhoseHpRunsOutDiesAndIsAskedHowToRevive()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);

            var died = await DieAsync(alice);

            await alice.ExpectSinceAsync(died, p => Stat(p) && Hp(p) == 0 && Mp(p) == 0, "neither HP nor MP left");
            StringAssert.StartsWith(alice.Since(died).First(p => TestClient.HeaderOf(p) == "dlgi"),
                $"dlgi #revival^{ReviveHere} #revival^{ReviveAtTheSavePoint} {ReviveHereForFree} ");
        }

        [TestMethod]
        public async Task TheDeadNeitherWalkNorFightNorDrinkPotions()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);
            await Harness.GiveAsync(player, WorldData.Potion, 2, pocket: NoscorePocketType.Main);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 82, 22);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await DieAsync(alice);
            var dead = alice.Mark();
            var watched = bob.Mark();

            await alice.WalkAsync(80, 22);
            await alice.SendAsync($"u_s 0 {Monster} {WorldData.Biter}");
            await alice.SendAsync($"u_i {Player} {alice.CharacterId} {MainPocket} 0 0 0");

            Assert.IsTrue(await bob.NoneSinceAsync(watched, p => p.StartsWith($"mv {Player} {alice.CharacterId} ", StringComparison.Ordinal)
                || HitBy(alice, WorldData.Biter)(p), TimeSpan.FromSeconds(2)), "she neither walks nor hits");
            Assert.IsFalse(alice.Since(dead).Any(p => TestClient.HeaderOf(p) == "ivn" || (Stat(p) && Hp(p) > 0)), "no potion is drunk");
        }

        [TestMethod]
        public async Task RevivingOnTheSpotGivesBackAllHpAndIsSeenByBystanders()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 82, 22);
            await DieAsync(alice);
            var revived = alice.Mark();
            var watched = bob.Mark();

            await alice.SendAsync($"#revival^{ReviveHere}");

            await bob.ExpectSinceAsync(watched, p => p == $"revive {Player} {alice.CharacterId} 0", "her revival");
            await bob.ExpectSinceAsync(watched, p => p == $"tp {Player} {alice.CharacterId} 80 21 0", "her standing where she fell");
            var stat = await alice.ExpectSinceAsync(revived, p => Stat(p) && Hp(p) > 0, "her HP after reviving");
            Assert.AreEqual((MostHp, MostMp), (Hp(stat), Mp(stat)));

            await alice.WalkAsync(80, 22);
            await bob.ExpectSinceAsync(watched, p => p.StartsWith($"mv {Player} {alice.CharacterId} 80 22 ", StringComparison.Ordinal), "her walking again");
        }

        [TestMethod]
        public async Task RevivingAtTheSavePointBringsTheCharacterBackToTown()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 82, 22);
            await using (var alice = Harness.Client(player))
            {
                await alice.EnterWorldAsync(player.Slot);
                await DieAsync(alice);
                var revived = alice.Mark();
                var watched = bob.Mark();

                await alice.SendAsync($"#revival^{ReviveAtTheSavePoint}");

                // c_map <type> <map> <1 on arriving, 0 on leaving>
                await alice.ExpectSinceAsync(revived, p => p == $"c_map 0 {WorldData.Town} 1", "the town she revives in");
                var stat = await alice.ExpectSinceAsync(revived, p => Stat(p) && Hp(p) > 0, "her HP after reviving");
                Assert.AreEqual((MostHp, MostMp), (Hp(stat), Mp(stat)));
                await bob.ExpectSinceAsync(watched, p => p == $"out {Player} {alice.CharacterId}", "her leaving the meadow");
            }

            await using var back = await Harness.ComeBackAsync(player);
            var stored = await Harness.StoredAsync(player.CharacterId);
            Assert.AreEqual(WorldData.Town, stored.MapId, "she is stored in town");
            Assert.AreEqual(MostHp, stored.Hp, "and alive");
        }

        [TestMethod]
        public async Task ACharacterWhoLeavesTheWorldDeadIsStoredDead()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);
            await using (var alice = Harness.Client(player))
            {
                await alice.EnterWorldAsync(player.Slot);
                await DieAsync(alice);
            }

            var waited = Stopwatch.StartNew();
            while ((await Harness.StoredAsync(player.CharacterId)).Hp != 0 && waited.Elapsed < TimeSpan.FromSeconds(15))
            {
                await Task.Delay(250);
            }

            Assert.AreEqual(0, (await Harness.StoredAsync(player.CharacterId)).Hp, "leaving does not bring her back to life");
        }

        // The harness cannot bring such a character in the usual way: that waits for the map it was stored on.
        [TestMethod]
        public async Task ACharacterStoredDeadComesBackAtItsSavePointBarelyAlive()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 82, 30, c => c.Hp = 0);
            await using var alice = Harness.Client(player);
            await alice.ConnectAsync();
            var entered = alice.Mark();
            await alice.SendAsync($"select {player.Slot}");
            await alice.ExpectAsync("OK");

            await alice.SendAsync("game_start");
            await alice.SendAsync("lbs 0");

            await alice.ExpectSinceAsync(entered, p => p == $"c_map 0 {WorldData.Town} 1", "the town she comes back in");
            var bars = await alice.ExpectSinceAsync(entered, Stat, "her bars on entering");
            Assert.AreEqual((1, 1), (Hp(bars), Mp(bars)), "one point of each");
            Assert.IsFalse(alice.Since(entered).Any(p => p.StartsWith("dlgi #revival", StringComparison.Ordinal)),
                "nobody asks her how to revive");
        }

        [TestMethod]
        public async Task TheLivingCannotReviveThemselvesBackToFullHp()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 82, 24, c => c.Hp = 100);
            var asked = alice.Mark();

            await alice.SendAsync($"#revival^{ReviveHere}");
            await alice.SendAsync($"#revival^{ReviveAtTheSavePoint}");

            Assert.IsTrue(await alice.NoneSinceAsync(asked, p => (Stat(p) && Hp(p) > 100) || TestClient.HeaderOf(p) is "revive" or "c_map",
                TimeSpan.FromSeconds(1)), "nothing is restored and she stays where she is");
        }

        [TestMethod]
        public async Task ARangedSkillReachesAMonsterWithinItsRangeOnly()
        {
            await using var near = await Harness.EnterAsync("Alice", WorldData.Meadow, 60, (short)(60 + RangedSkillRange));
            await using var far = await Harness.EnterAsync("Bob", WorldData.Meadow, 60, (short)(60 + RangedSkillRange + 5));
            var refused = far.Mark();
            var watched = near.Mark();

            await far.SendAsync($"u_s {RangedSkill} {Monster} {WorldData.SturdyMonster}");

            // cancel 2 is the client's cue to stop attacking.
            await far.ExpectSinceAsync(refused, p => p.StartsWith("cancel 2 ", StringComparison.Ordinal), "the refusal");
            Assert.IsTrue(await near.NoneSinceAsync(watched, HitBy(far, WorldData.SturdyMonster), TimeSpan.FromSeconds(1)), "no hit from out of range");

            await near.SendAsync($"u_s {RangedSkill} {Monster} {WorldData.SturdyMonster}");

            var hit = (await far.ExpectAsync(HitBy(near, WorldData.SturdyMonster), "the hit from within range")).Split(' ');
            Assert.AreEqual("201", hit[5], "the ranged skill");
        }

        [TestMethod]
        public async Task AMeleeSkillDoesNotReachAMonsterStandingApart()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 60, 64);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 62, 62);
            var refused = alice.Mark();
            var watched = bob.Mark();

            await alice.SendAsync($"u_s 0 {Monster} {WorldData.SturdyMonster}");

            await alice.ExpectSinceAsync(refused, p => p.StartsWith("cancel 2 ", StringComparison.Ordinal), "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync(watched, HitBy(alice, WorldData.SturdyMonster), TimeSpan.FromSeconds(1)), "no hit from four cells away");
        }

        [TestMethod]
        public async Task ASkillCostsItsMpAndIsRefusedUntilItHasCooledDown()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 60, 61);
            await Harness.TeachAsync(player, WorldData.SlowSkill);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 62, 62);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            var cast = alice.Mark();

            await alice.SendAsync($"u_s {WorldData.SlowSkillCastId} {Monster} {WorldData.SturdyMonster}");

            var hit = (await bob.ExpectAsync(HitBy(alice, WorldData.SturdyMonster), "the hit")).Split(' ');
            Assert.AreEqual(WorldData.SlowSkill.ToString(CultureInfo.InvariantCulture), hit[5]);
            Assert.AreEqual(MostMp - WorldData.SlowSkillMp, Mp(await alice.ExpectSinceAsync(cast, p => Stat(p) && Mp(p) < MostMp, "the MP it cost")));
            var again = alice.Mark();
            var watched = bob.Mark();

            await alice.SendAsync($"u_s {WorldData.SlowSkillCastId} {Monster} {WorldData.SturdyMonster}");

            await alice.ExpectSinceAsync(again, p => p.StartsWith("cancel 2 ", StringComparison.Ordinal), "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync(watched, HitBy(alice, WorldData.SturdyMonster), TimeSpan.FromSeconds(1)), "no second hit");
            Assert.IsFalse(alice.Since(again).Any(p => Stat(p) && Mp(p) < MostMp - WorldData.SlowSkillMp), "and no MP spent on it");
        }

        [TestMethod]
        public async Task ASkillIsRefusedToSomeoneWithoutTheMpForIt()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 60, 61, c => c.Mp = WorldData.SlowSkillMp - 1);
            await Harness.TeachAsync(player, WorldData.SlowSkill);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 62, 62);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            var refused = alice.Mark();
            var watched = bob.Mark();

            await alice.SendAsync($"u_s {WorldData.SlowSkillCastId} {Monster} {WorldData.SturdyMonster}");

            await alice.ExpectSinceAsync(refused, p => p.StartsWith("cancel 2 ", StringComparison.Ordinal), "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync(watched, HitBy(alice, WorldData.SturdyMonster), TimeSpan.FromSeconds(1)), "no hit");
        }

        [TestMethod]
        public async Task AnotherPlayerCannotBeHitOnAnOrdinaryMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 84, 40);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 84, 41);
            var struck = bob.Mark();

            await alice.SendAsync($"u_s 0 {Player} {bob.CharacterId}");

            Assert.IsTrue(await bob.NoneSinceAsync(struck, p => p.StartsWith($"su {Player} {alice.CharacterId} {Player} {bob.CharacterId} ", StringComparison.Ordinal)
                || (Stat(p) && Hp(p) < MostHp), TimeSpan.FromSeconds(2)), "Bob is not hit");
        }
    }
}
