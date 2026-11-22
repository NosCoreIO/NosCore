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
                .Where(name => name != "timespace-sample")
                .Select(name => new object[] { name! });

        private static async Task<IInstanceScript> LoadAsync(string name) =>
            await new CsxInstanceScriptLoader(Folder).LoadAsync(name)
            ?? throw new AssertFailedException($"{name}.csx did not load.");

        [TestMethod]
        [DynamicData(nameof(TimeSpaces))]
        public async Task ATimeSpaceScriptCarriesTheIdItIsNamedAfter(string name)
        {
            var script = await LoadAsync(name);

            Assert.AreEqual($"timespace-{script.Definition.Id:00}", name);
            Assert.IsTrue(script.Definition.LevelMinimum > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(script.Definition.Title));
            Assert.IsTrue(script.Definition.Lives > 0);
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

            foreach (var (room, vNum) in run.Items.ToList())
            {
                script.OnItemCollected(run, room, vNum);
            }

            foreach (var npc in run.SummonedNpcs.ToList())
            {
                npc.OnDeath?.Invoke(run);
            }

            for (var fallen = 0; fallen < run.Summoned.Count; fallen++)
            {
                run.Summoned[fallen].OnDeath?.Invoke(run);
            }

            Assert.IsTrue(run.Calls.Any(call => call.StartsWith("clock(", StringComparison.Ordinal)));
        }

        [TestMethod]
        [DataRow("timespace-33", 6, 7, "unlock(5->8)")]
        [DataRow("timespace-33", 10, 9, "unlock(8->11)")]
        [DataRow("timespace-35", 6, 5, "unlock(4->7)")]
        public async Task ACrossingOpensOnceBothItsSideRoomsAreCleared(string name, int first, int second, string unlock)
        {
            var script = await LoadAsync(name);
            var run = new RecordingRun();
            script.OnFirstEnter(run);

            script.OnRoomCleared(run, first);
            run.Cleared.Add(first);
            var afterFirst = run.Calls.Contains(unlock);
            script.OnRoomCleared(run, second);

            Assert.IsFalse(afterFirst);
            Assert.IsTrue(run.Calls.Contains(unlock));
        }

        [TestMethod]
        [DataRow("timespace-33")]
        [DataRow("timespace-34")]
        [DataRow("timespace-35")]
        [DataRow("timespace-36")]
        [DataRow("timespace-38")]
        [DataRow("timespace-44")]
        [DataRow("timespace-46")]
        [DataRow("timespace-518")]
        [DataRow("timespace-628")]
        public async Task EnteringTheFirstRoomStartsTheMission(string name)
        {
            var script = await LoadAsync(name);
            var run = new RecordingRun();
            script.OnFirstEnter(run);
            var first = script.Definition.Rooms[0].Key;

            script.OnRoomEntered(run, first);

            Assert.AreEqual("message(MissionStartWhenEnterFirstRoom)", run.Calls[^1]);
            Assert.IsTrue(run.Portals.Any(p => p.From == first && !p.Locked));
        }

        [TestMethod]
        public async Task PickingUpTheArtifactInTheDarkTrapWinsTheTreasureRoom()
        {
            var script = await LoadAsync("timespace-34");
            var run = new RecordingRun();
            script.OnFirstEnter(run);
            const int trap = 16;

            script.OnItemCollected(run, 4, 1001);
            var boxWins = run.Calls.Contains("succeed");
            script.OnItemCollected(run, trap, 1059);

            Assert.IsFalse(boxWins);
            Assert.AreEqual("succeed", run.Calls[^1]);
            CollectionAssert.Contains(run.Items, (trap, (short)1059));
            Assert.AreEqual(new InstanceObjective(ObjectiveKind.CollectItem, 1059, 1), script.Definition.Objectives.Single());
        }

        [TestMethod]
        public async Task TheDarkTrapHoldsItsDoorsUntilItsClockRunsOut()
        {
            var script = await LoadAsync("timespace-34");
            var run = new RecordingRun { Players = 1 };
            script.OnFirstEnter(run);
            const int trap = 16;

            script.OnRoomEntered(run, trap);
            script.OnRoomCleared(run, trap);
            var openedOnClearing = run.Unlocked.Any(p => p.From == trap);
            RunTimers(run);

            Assert.IsFalse(openedOnClearing);
            CollectionAssert.IsSubsetOf(new[] { (trap, 11), (trap, 17), (trap, 15) }, run.Unlocked.ToList());
            CollectionAssert.Contains(run.Calls, "message(TIMESPACE_PORTAL_HAS_OPENED)");
        }

        [TestMethod]
        [DataRow(119, true)]
        [DataRow(121, false)]
        public async Task TheMimicRoomGivesItsBonusOnlyWithinItsTwoMinutes(int clearedAfter, bool bonus)
        {
            var script = await LoadAsync("timespace-34");
            var run = new RecordingRun { Players = 1 };
            script.OnFirstEnter(run);
            const int bonusRoom = 9;

            script.OnRoomEntered(run, bonusRoom);
            foreach (var (at, step) in run.Scheduled.Where(s => s.At <= TimeSpan.FromSeconds(clearedAfter)).OrderBy(s => s.At).ToList())
            {
                run.Scheduled.Remove((at, step));
                run.Elapsed = at;
                step(run);
            }

            run.Elapsed = TimeSpan.FromSeconds(clearedAfter);
            script.OnRoomCleared(run, bonusRoom);

            Assert.AreEqual(bonus, run.Calls.Contains("addClock(180s)"));
            Assert.IsTrue(run.Unlocked.Any(p => p.From == bonusRoom));
        }

        [TestMethod]
        public async Task ThePyjamaHuntRoomOpensOnlyItsWayBackAndAddsTwoMinutes()
        {
            var script = await LoadAsync("timespace-518");
            var run = new RecordingRun();
            script.OnFirstEnter(run);
            const int eastWing = 16;
            const int huntRoom = 17;
            const int exitRoom = 18;

            script.OnRoomEntered(run, huntRoom);
            script.OnRoomCleared(run, huntRoom);

            Assert.IsTrue(run.Unlocked.Contains((huntRoom, eastWing)));
            Assert.IsFalse(run.Unlocked.Contains((huntRoom, exitRoom)));
            CollectionAssert.AreEqual(
                new[] { $"unlock({huntRoom}->{eastWing})", "message(ADoorHasOpened)", "addClock(120s)", "message(QuickMissionComplete)" },
                run.Calls.TakeLast(4).ToList());
        }

        [TestMethod]
        public async Task ThePyjamaExitIsOpenFromTheStart()
        {
            var script = await LoadAsync("timespace-518");
            var run = new RecordingRun();
            script.OnFirstEnter(run);

            Assert.AreEqual((18, false), run.Exits.Single());
            Assert.IsEmpty(script.Definition.Objectives);
        }

        [TestMethod]
        public async Task TheKovoltsFightBesideThePlayer()
        {
            var script = await LoadAsync("timespace-36");
            var run = new RecordingRun();

            script.OnFirstEnter(run);

            Assert.IsTrue(run.SummonedNpcs.Where(n => n.VNum is 116 or 122 or 329).All(n => n.IsAlly));
            Assert.IsFalse(run.SummonedNpcs.Where(n => n.VNum == 319).Any(n => n.IsAlly));
        }

        [TestMethod]
        public async Task AFallenKovoltBringsAKenkoRaider()
        {
            var script = await LoadAsync("timespace-36");
            var run = new RecordingRun();
            script.OnFirstEnter(run);

            run.SummonedNpcs.Select(n => n.OnDeath).OfType<Action<IInstanceRun>>().Single()(run);

            Assert.AreEqual("summon(7,152)", run.Calls[^1]);
        }

        [TestMethod]
        [DataRow(12, 13, 5)]
        [DataRow(14, 15, 6)]
        public async Task AHoldoutTakesItsLeftoverMonstersAwayWhenItsClockEnds(int holdout, int next, int perWave)
        {
            var script = await LoadAsync("timespace-44");
            var run = new RecordingRun { Players = 1 };
            script.OnFirstEnter(run);

            script.OnRoomEntered(run, holdout);
            RunTimers(run);

            var wave = $"summon({holdout},{string.Join(",", Enumerable.Repeat(154, perWave))})";
            Assert.AreEqual(4, run.Calls.Count(c => c == wave));
            var unlock = run.Calls.IndexOf($"unlock({holdout}->{next})");
            Assert.IsTrue(unlock >= 0 && run.Calls.IndexOf($"dismiss({holdout})") > unlock);
        }

        [TestMethod]
        public async Task TheHeartSendsSpearmenThreeTimesAndRaidersUntilItsClockEnds()
        {
            var script = await LoadAsync("timespace-44");
            var run = new RecordingRun { Players = 1 };
            script.OnFirstEnter(run);
            const int heart = 16;

            script.OnRoomEntered(run, heart);
            script.OnDialogClosed(run, heart, 6173);
            RunTimers(run);

            Assert.AreEqual(3, run.Calls.Count(c => c == $"summon({heart},154,154,154,154)"));
            Assert.AreEqual(4, run.Calls.Count(c => c == $"summon({heart},155,155)"));
            Assert.IsTrue(run.UnlockedExits.Contains(heart));
            CollectionAssert.AreEqual(new[] { $"dismiss({heart})", $"stopRoomClock({heart})" }, run.Calls.TakeLast(2).ToList());
        }

        private static void RunTimers(RecordingRun run)
        {
            while (run.Scheduled.Count > 0)
            {
                var next = run.Scheduled.OrderBy(s => s.At).First();
                run.Scheduled.Remove(next);
                run.Elapsed = next.At;
                next.Step(run);
            }
        }

        [TestMethod]
        [DynamicData(nameof(TimeSpaces))]
        public async Task ReachingTheExitWinsTheTimeSpace(string name)
        {
            var script = await LoadAsync(name);
            var run = new RecordingRun();

            script.OnFirstEnter(run);
            script.OnExitReached(run, script.Definition.Rooms[^1].Key);

            Assert.AreEqual("succeed", run.Calls[^1]);
        }

        [TestMethod]
        [DynamicData(nameof(TimeSpaces))]
        public async Task TheWayOutCanBeReachedFromTheFirstRoom(string name)
        {
            var script = await LoadAsync(name);
            var run = new RecordingRun();
            script.OnFirstEnter(run);
            var portals = run.Portals.ToList();
            var exits = run.Exits.ToList();

            var reached = new HashSet<int> { script.Definition.Rooms[0].Key };
            var triggered = new HashSet<int>();
            var won = false;
            while (reached.Except(triggered).Select(r => (int?)r).FirstOrDefault() is { } room)
            {
                triggered.Add(room);
                var before = run.Calls.Count;
                var summoned = run.Summoned.Count;
                var dialogs = run.Dialogs.Count;
                script.OnRoomEntered(run, room);
                foreach (var (_, dialog) in run.Dialogs.Skip(dialogs).Where(d => d.Room == room).ToList())
                {
                    script.OnDialogClosed(run, room, dialog);
                }

                script.OnRoomCleared(run, room);
                run.Cleared.Add(room);
                foreach (var (_, button) in run.Buttons.Where(b => b.Room == room).ToList())
                {
                    run.Used.Add((room, button));
                    script.OnButtonUsed(run, room, button);
                }

                foreach (var (_, vNum) in run.Items.Where(i => i.Room == room).ToList())
                {
                    script.OnItemCollected(run, room, vNum);
                }

                for (var step = 0; step < run.Scheduled.Count; step++)
                {
                    run.Scheduled[step].Step(run);
                }

                run.Scheduled.Clear();
                for (var fallen = summoned; fallen < run.Summoned.Count; fallen++)
                {
                    run.Summoned[fallen].OnDeath?.Invoke(run);
                }

                won |= run.Calls.Skip(before).Contains("succeed");
                foreach (var (from, to, _) in portals.Where(p => !p.Locked || run.Unlocked.Contains((p.From, p.To))))
                {
                    if (reached.Contains(from))
                    {
                        reached.Add(to);
                    }
                }
            }

            won |= exits.Any(e => reached.Contains(e.Room) && (!e.Locked || run.UnlockedExits.Contains(e.Room)));
            Assert.IsTrue(won, $"{name}: no open exit and no win from rooms {string.Join(",", reached)}");
        }
    }
}
