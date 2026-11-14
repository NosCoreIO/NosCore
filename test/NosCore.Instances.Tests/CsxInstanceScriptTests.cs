//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Instances.Abstractions;
using NosCore.Instances.Scripting;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Instances.Tests
{
    // A script is only as good as what it can be held to. This drives the shipped file
    // against a recording run, so instance content is testable the way the rest is.
    public class RecordingRun : IInstanceRun
    {
        public List<string> Calls { get; } = [];

        public TimeSpan Elapsed { get; set; }

        public List<(TimeSpan At, Action<IInstanceRun> Step)> Scheduled { get; } = [];

        public int Alive { get; set; }

        public void At(TimeSpan elapsed, Action<IInstanceRun> step) => Scheduled.Add((elapsed, step));

        public void SummonWave(int room, byte wave) => Calls.Add($"wave({room},{wave})");

        public void DropWave(int room, byte wave) => Calls.Add($"drop({room},{wave})");

        public int MonstersAlive(int room) => Alive;

        public int Players { get; set; }

        public bool Decided { get; set; }

        public int PlayersIn(int room) => Players;

        public bool OneSideStanding(int room) => Decided;

        public void SetPvp(int room, bool isPvp) => Calls.Add($"pvp({room},{isPvp})");

        public void Summon(int room, params MonsterSpawn[] monsters) =>
            Calls.Add($"summon({room},{string.Join(",", monsters.Select(m => m.VNum))})");

        public void SpawnPortal(int fromRoom, Spot at, int toRoom, Spot arrival, bool locked = true) =>
            Calls.Add($"portal({fromRoom}->{toRoom},{(locked ? "locked" : "open")})");

        public void UnlockPortal(int fromRoom, int toRoom) => Calls.Add($"unlock({fromRoom}->{toRoom})");

        public void SetMonsterLocker(int room, short count) => Calls.Add($"monsterLocker({room},{count})");

        public void StartClock(TimeSpan limit) => Calls.Add($"clock({limit.TotalMinutes}m)");

        public void Message(Game18NConstString message) => Calls.Add($"message({message})");

        public void Succeed() => Calls.Add("succeed");

        public void Fail() => Calls.Add("fail");
    }

    [TestClass]
    public class CsxInstanceScriptTests
    {
        private static CsxInstanceScriptLoader Loader() =>
            new(Path.Combine(AppContext.BaseDirectory, "instances"));

        [TestMethod]
        public async Task TheScriptDescribesItsRooms()
        {
            var script = await Loader().LoadAsync("timespace-01");

            Assert.AreEqual(2, script.Definition.Rooms.Count);
            Assert.AreEqual(20, script.Definition.StartX);
            Assert.AreEqual(33, script.Definition.StartY);
        }

        [TestMethod]
        public async Task EnteringOpensTheFirstRoom()
        {
            var script = await Loader().LoadAsync("timespace-01");
            var run = new RecordingRun();

            script.OnFirstEnter(run);

            CollectionAssert.AreEqual(new[]
            {
                "clock(10m)",
                "message(EliminateAllMonsters)",
                "summon(1,58,58,59)",
                "monsterLocker(1,3)",
                "portal(1->2,locked)"
            }, run.Calls);
        }

        [TestMethod]
        public async Task ClearingTheFirstRoomOpensTheWay()
        {
            var script = await Loader().LoadAsync("timespace-01");
            var run = new RecordingRun();

            script.OnRoomCleared(run, 1);

            CollectionAssert.AreEqual(new[] { "unlock(1->2)", "summon(2,60)", "monsterLocker(2,1)" }, run.Calls);
        }

        [TestMethod]
        public async Task ClearingTheLastRoomEndsTheInstance()
        {
            var script = await Loader().LoadAsync("timespace-01");
            var run = new RecordingRun();

            script.OnRoomCleared(run, 2);

            CollectionAssert.AreEqual(new[] { "message(TimeSpaceCompleted)", "succeed" }, run.Calls);
        }

        [TestMethod]
        public async Task TheClockRunningOutFailsTheRun()
        {
            var script = await Loader().LoadAsync("timespace-01");
            var run = new RecordingRun();

            script.OnTimeout(run);

            CollectionAssert.AreEqual(new[] { "fail" }, run.Calls);
        }

        [TestMethod]
        public async Task InstantCombatBringsItsWavesInOverTime()
        {
            var script = await Loader().LoadAsync("instant-combat");
            var run = new RecordingRun();

            script.OnFirstEnter(run);
            foreach (var (_, step) in run.Scheduled.OrderBy(s => s.At))
            {
                step(run);
            }

            CollectionAssert.AreEqual(new[] { "wave(1,0)", "wave(1,1)", "wave(1,2)", "wave(1,3)", "wave(1,4)" },
                run.Calls.Where(c => c.StartsWith("wave")).ToList());
            CollectionAssert.AreEqual(new[] { "drop(1,0)", "drop(1,1)", "drop(1,2)", "drop(1,3)", "drop(1,4)" },
                run.Calls.Where(c => c.StartsWith("drop")).ToList());
            Assert.AreEqual("fail", run.Calls[^1]);
        }

        [TestMethod]
        public async Task InstantCombatIsWonOnceTheLastWaveIsOutAndEveryMonsterIsDead()
        {
            var script = await Loader().LoadAsync("instant-combat");
            var run = new RecordingRun { Elapsed = TimeSpan.FromSeconds(729) };

            script.OnMonsterKilled(run, 1, 333);
            Assert.AreEqual(0, run.Calls.Count);

            run.Elapsed = TimeSpan.FromSeconds(730);
            run.Alive = 1;
            script.OnMonsterKilled(run, 1, 333);
            Assert.AreEqual(0, run.Calls.Count);

            run.Alive = 0;
            script.OnMonsterKilled(run, 1, 333);
            CollectionAssert.AreEqual(new[]
            {
                "message(InstantCombatSuccess)",
                "message(ReturnToStartingPositionTenSeconds)",
                "succeed"
            }, run.Calls);
        }

        [TestMethod]
        public async Task AnIcebreakerWithOnePlayerIsWonAtOnce()
        {
            var script = await Loader().LoadAsync("ice-breaker");
            var run = new RecordingRun { Players = 1 };

            script.OnFirstEnter(run);

            CollectionAssert.AreEqual(new[] { "succeed" }, run.Calls);
            Assert.AreEqual(0, run.Scheduled.Count);
        }

        [TestMethod]
        public async Task AnIcebreakerOpensPvpAfterItsWarningsAndEndsAfterTenMinutes()
        {
            var script = await Loader().LoadAsync("ice-breaker");
            var run = new RecordingRun { Players = 2 };

            script.OnFirstEnter(run);
            foreach (var (_, step) in run.Scheduled.OrderBy(s => s.At).ToList())
            {
                step(run);
            }

            CollectionAssert.AreEqual(new[] { 0d, 6, 13, 14, 584, 614 },
                run.Scheduled.Take(6).Select(s => s.At.TotalSeconds).OrderBy(s => s).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                "message(PvpWillBeAvailable)",
                "message(PvpWillBeAvailable)",
                "message(PvpWillBeAvailable)",
                "message(BeginAttack)",
                "pvp(1,True)",
                "message(IcebreakerWillEndInThirtySeconds)",
                "message(IcebreakerTimeElapsed)",
                "fail"
            }, run.Calls);
        }

        [TestMethod]
        public async Task AnIcebreakerIsWonOnceOneSideIsLeftStanding()
        {
            var script = await Loader().LoadAsync("ice-breaker");
            var run = new RecordingRun { Players = 2, Elapsed = TimeSpan.FromSeconds(14) };
            script.OnFirstEnter(run);
            var beginAttack = run.Scheduled.Single(s => s.At == TimeSpan.FromSeconds(14)).Step;

            beginAttack(run);
            var check = run.Scheduled.Single(s => s.At == TimeSpan.FromSeconds(15)).Step;
            Assert.IsFalse(run.Calls.Contains("succeed"));

            run.Decided = true;
            check(run);
            Assert.AreEqual("succeed", run.Calls[^1]);
        }

        [TestMethod]
        public async Task ABrokenScriptSaysSoAtLoad()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(Path.Combine(dir, "broken.csx"), "this is not C#;");

            var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => new CsxInstanceScriptLoader(dir).LoadAsync("broken"));

            StringAssert.Contains(ex.Message, "did not compile");
        }

        [TestMethod]
        public async Task AScriptFixedAfterAFailedLoadLoadsOnTheNextTry()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "fixed.csx");
            await File.WriteAllTextAsync(path, "this is not C#;");
            var loader = new CsxInstanceScriptLoader(dir);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => loader.LoadAsync("fixed"));

            await File.WriteAllTextAsync(path, "public class Fixed : InstanceScript; return new Fixed();");

            Assert.IsNotNull(await loader.LoadAsync("fixed"));
        }

        [TestMethod]
        [DataRow("../outside")]
        [DataRow("sub/inside")]
        [DataRow("..")]
        [DataRow("")]
        public async Task OnlyScriptsDirectlyInTheFolderAreLoaded(string name)
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dir = Path.Combine(root, "instances");
            Directory.CreateDirectory(Path.Combine(dir, "sub"));
            const string valid = "class S : InstanceScript { } return new S();";
            await File.WriteAllTextAsync(Path.Combine(root, "outside.csx"), valid);
            await File.WriteAllTextAsync(Path.Combine(dir, "sub", "inside.csx"), valid);

            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => new CsxInstanceScriptLoader(dir).LoadAsync(name));
        }

        [TestMethod]
        public async Task CompilingIsPaidOnceThenCached()
        {
            var loader = Loader();
            var first = await loader.LoadAsync("timespace-01");
            var second = await loader.LoadAsync("timespace-01");

            Assert.AreSame(first, second);
        }
    }
}
