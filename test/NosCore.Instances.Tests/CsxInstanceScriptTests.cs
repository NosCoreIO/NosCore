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

            CollectionAssert.AreEqual(new[] { "unlock(1->2)", "summon(2,200)", "monsterLocker(2,1)" }, run.Calls);
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
            var first = await loader.LoadAsync("timespace-01");
            var second = await loader.LoadAsync("timespace-01");

            Assert.AreSame(first, second);
        }
    }
}
