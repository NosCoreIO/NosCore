//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Instances.Abstractions;
using NosCore.Instances.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Instances.Tests
{
    [TestClass]
    public class TimeSpaceScriptsTests
    {
        private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "instances");

        public static IEnumerable<object[]> TimeSpaces() =>
            Directory.GetFiles(Folder, "timespace-*.csx")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => name != "timespace-01")
                .Select(name => new object[] { name! });

        private static async Task<IInstanceScript> LoadAsync(string name) =>
            await new CsxInstanceScriptLoader(Folder).LoadAsync(name)
            ?? throw new AssertFailedException($"{name}.csx did not load.");

        [TestMethod]
        [DynamicData(nameof(TimeSpaces))]
        public async Task ATimeSpaceScriptCarriesTheIdItIsNamedAfter(string name)
        {
            var script = await LoadAsync(name);

            Assert.AreEqual($"timespace-{script.Definition.Id}", name);
            Assert.IsTrue(script.Definition.LevelMinimum > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(script.Definition.Title));
        }

        [TestMethod]
        [DynamicData(nameof(TimeSpaces))]
        public async Task EveryTriggerOfATimeSpaceScriptRuns(string name)
        {
            var script = await LoadAsync(name);
            var run = new RecordingRun();

            script.OnFirstEnter(run);
            foreach (var room in script.Definition.Rooms)
            {
                script.OnRoomEntered(run, room.Key);
                script.OnRoomCleared(run, room.Key);
                for (var button = 1; button <= 9; button++)
                {
                    script.OnButtonUsed(run, room.Key, button);
                }
            }

            for (var fallen = 0; fallen < run.Summoned.Count; fallen++)
            {
                run.Summoned[fallen].OnDeath?.Invoke(run);
            }

            Assert.IsTrue(run.Calls.Any(call => call.StartsWith("clock(", StringComparison.Ordinal)));
        }

        [TestMethod]
        [DynamicData(nameof(TimeSpaces))]
        public async Task ReachingTheExitWinsTheTimeSpace(string name)
        {
            var script = await LoadAsync(name);
            var run = new RecordingRun();

            script.OnFirstEnter(run);
            Assert.IsTrue(run.Calls.Any(call => call.StartsWith("exit(", StringComparison.Ordinal)));
            script.OnExitReached(run, script.Definition.Rooms[^1].Key);

            Assert.AreEqual("succeed", run.Calls[^1]);
        }
    }
}
