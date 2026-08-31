using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Instances.Abstractions;
using NosCore.Instances.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NosCore.Instances.Tests
{
    // A script is only as good as what it can be held to. This drives the dropped-in file
    // against a recording run, so instance content is testable the way the rest is.
    public class RecordingRun : IInstanceRun
    {
        public List<string> Calls { get; } = [];
        public int CurrentRoom { get; set; }
        public TimeSpan Elapsed { get; set; }

        public void Summon(int room, params MonsterSpawn[] monsters) =>
            Calls.Add($"summon({room},{string.Join(",", Array.ConvertAll(monsters, m => m.VNum + (m.IsBoss ? "!" : "")))})");
        public void SummonNpc(int room, short vNum, Spot at) => Calls.Add($"npc({room},{vNum})");
        public void SpawnPortal(int fromRoom, int toRoom, Spot at, bool locked = true) =>
            Calls.Add($"portal({fromRoom}->{toRoom},{(locked ? "locked" : "open")})");
        public void UnlockPortal(int fromRoom, int toRoom) => Calls.Add($"unlock({fromRoom}->{toRoom})");
        public void SetMonsterLocker(int room, short count) => Calls.Add($"monsterLocker({room},{count})");
        public void SetButtonLocker(int room, short count) => Calls.Add($"buttonLocker({room},{count})");
        public void StartClock(TimeSpan limit) => Calls.Add($"clock({limit.TotalMinutes}m)");
        public void StopClock() => Calls.Add("stopClock");
        public void Message(string text) => Calls.Add($"message({text})");
        public void Effect(int room, short effectId) => Calls.Add($"effect({room},{effectId})");
        public void Succeed() => Calls.Add("succeed");
        public void Fail() => Calls.Add("fail");
    }

    [TestClass]
    public class CsxInstanceScriptTests
    {
        private static CsxInstanceScriptLoader Loader() =>
            new(Path.Combine(Directory.GetCurrentDirectory(), "instances"));

        [TestMethod]
        public async Task ADroppedInScriptOpensTheInstance()
        {
            var script = await Loader().LoadAsync("timespace-01");
            var run = new RecordingRun();

            script.OnFirstEnter(run);

            CollectionAssert.AreEqual(new[]
            {
                "clock(20m)",
                "message(Clear each room to open the way.)",
                "summon(0,58,58,59)",
                "monsterLocker(0,3)",
                "portal(0->1,locked)"
            }, run.Calls);
        }

        [TestMethod]
        public async Task ClearingTheFirstRoomOpensTheWay()
        {
            var script = await Loader().LoadAsync("timespace-01");
            var run = new RecordingRun();

            script.OnRoomCleared(run, 0);

            CollectionAssert.AreEqual(new[] { "unlock(0->1)", "effect(0,5000)" }, run.Calls);
        }

        [TestMethod]
        public async Task KillingTheBossEndsTheInstance()
        {
            var script = await Loader().LoadAsync("timespace-01");
            var run = new RecordingRun();

            script.OnRoomCleared(run, 1);
            script.OnMonsterKilled(run, 200);

            CollectionAssert.AreEqual(new[]
            {
                "summon(1,200!)", "monsterLocker(1,1)", "message(The way out is open.)", "succeed"
            }, run.Calls);
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
        public async Task CompilingIsPaidOnceThenCached()
        {
            var loader = Loader();
            var cold = Stopwatch.StartNew();
            await loader.LoadAsync("timespace-01");
            cold.Stop();

            var warm = Stopwatch.StartNew();
            await loader.LoadAsync("timespace-01");
            warm.Stop();

            Console.WriteLine($"COLD {cold.ElapsedMilliseconds}ms  WARM {warm.ElapsedMilliseconds}ms");
            Assert.IsTrue(warm.ElapsedMilliseconds <= 5, "a second entry must not recompile");
        }
    }
}
