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
    // Each test holds a shipped time-space to what its packet captures show.
    [TestClass]
    public class TimeSpaceCaptureTests
    {
        private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "instances");

        private IInstanceScript _script = null!;
        private readonly RecordingRun _run = new() { Players = 1 };

        private async Task LoadAsync(string name)
        {
            _script = await new CsxInstanceScriptLoader(Folder).LoadAsync(name)
                ?? throw new AssertFailedException($"{name}.csx did not load.");
            _script.OnFirstEnter(_run);
        }

        private int Room(short vNum, byte x, byte y) =>
            _script.Definition.Rooms.Single(r => r.VNum == vNum && r.IndexX == x && r.IndexY == y).Key;

        private void RunTimers()
        {
            while (_run.Scheduled.Count > 0)
            {
                var next = _run.Scheduled.OrderBy(s => s.At).First();
                _run.Scheduled.Remove(next);
                _run.Elapsed = next.At;
                next.Step(_run);
            }
        }

        private List<string> CallsSince(int count) => _run.Calls.Skip(count).ToList();

        private void Fall(short vNum, short x, short y)
        {
            var monster = _run.Summoned.First(m => m.VNum == vNum && m.At == new Spot(x, y));
            monster.OnDeath?.Invoke(_run);
        }

        [TestMethod]
        public async Task TheMonsterHuntBringsAWaveEvery30SecondsForThreeMinutes()
        {
            await LoadAsync("timespace-27");
            var hunt = Room(2101, 2, 9);
            Assert.IsFalse(_run.Calls.Any(c => c.StartsWith($"summon({hunt},", StringComparison.Ordinal)));

            _script.OnRoomEntered(_run, hunt);
            RunTimers();

            CollectionAssert.Contains(_run.Calls, $"roomClock({hunt},180s)");
            Assert.AreEqual(6, _run.Calls.Count(c => c.StartsWith($"summon({hunt},", StringComparison.Ordinal)));
            Assert.AreEqual(46, _run.Calls.Where(c => c.StartsWith($"summon({hunt},", StringComparison.Ordinal))
                .Sum(c => c.Split(',').Length - 1));
            CollectionAssert.Contains(_run.Calls, $"message({hunt},QuickMissionComplete)");
            CollectionAssert.Contains(_run.Calls, "addClock(300s)");
        }

        [TestMethod]
        public async Task TheMonsterHuntGivesNoTimeWhenNobodyIsThereAsItEnds()
        {
            await LoadAsync("timespace-27");
            var hunt = Room(2101, 2, 9);
            _script.OnRoomEntered(_run, hunt);
            _run.Players = 0;

            RunTimers();

            CollectionAssert.DoesNotContain(_run.Calls, "addClock(300s)");
        }

        [TestMethod]
        public async Task TheKovoltHeroSculptureCallsMonstersAndTheDoorOpensOnceTheyFall()
        {
            await LoadAsync("timespace-23");
            var hall = Room(4003, 9, 3);
            var lastStand = Room(4003, 9, 4);
            var before = _run.Calls.Count;

            _script.OnButtonUsed(_run, hall, 1);

            CollectionAssert.AreEqual(new[]
            {
                "message(ObjectHasBeenMoved)",
                $"summon({hall},121,121,121,121,121,121,121,121,121)",
                $"objective({hall},TIMESPACE_GOAL_KILL_ALL_ENEMIES)"
            }, CallsSince(before));

            Fall(121, 8, 9);
            Fall(121, 7, 6);
            _script.OnRoomCleared(_run, hall);

            CollectionAssert.AreEqual(new[]
            {
                $"summon({hall},121,121)",
                $"summon({hall},121,121)",
                $"unlock({hall}->{lastStand})",
                "message(ADoorHasOpened)",
                "message(QuickMissionComplete)"
            }, CallsSince(before + 3));
        }

        [TestMethod]
        public async Task BecomingAWarriorYardEndsWithThreeAsgobas()
        {
            await LoadAsync("timespace-17");
            var yard = Room(4211, 2, 9);
            var before = _run.Calls.Count;

            Fall(58, 14, 8);
            Fall(58, 6, 14);
            Fall(66, 20, 17);

            CollectionAssert.AreEqual(new[] { $"summon({yard},58)", $"summon({yard},66)", $"summon({yard},66,66)" }, CallsSince(before));
        }

        [TestMethod]
        public async Task TheDemonBattleCornerHoldsTwoDemonsThatEachCallAnother()
        {
            await LoadAsync("timespace-26");
            var corner = Room(5207, 3, 9);
            CollectionAssert.Contains(_run.Calls, $"summon({corner},145,145)");
            var before = _run.Calls.Count;

            Fall(145, 8, 13);
            Fall(145, 10, 10);

            CollectionAssert.AreEqual(new[] { $"summon({corner},148)", $"summon({corner},148)" }, CallsSince(before));
        }

        [TestMethod]
        public async Task TheStrangeButtonSummonsItsMonsters()
        {
            await LoadAsync("timespace-10");
            var buttonRoom = Room(4109, 4, 11);
            Assert.IsFalse(_run.Calls.Any(c => c.StartsWith($"summon({buttonRoom},", StringComparison.Ordinal)));
            var before = _run.Calls.Count;

            _script.OnButtonUsed(_run, buttonRoom, 1);

            CollectionAssert.AreEqual(new[]
            {
                $"summon({buttonRoom},136,136,19)",
                $"objective({buttonRoom},TIMESPACE_GOAL_BUTTON_SUMMONED_ENEMIES)",
                "message(NewGoal)"
            }, CallsSince(before));
        }

        [TestMethod]
        public async Task TheBlackKnightSecondStandStartsAsItIsEntered()
        {
            await LoadAsync("timespace-19");
            var secondStand = Room(4205, 2, 8);
            var before = _run.Calls.Count;

            _script.OnRoomEntered(_run, secondStand);

            CollectionAssert.AreEqual(new[]
            {
                $"objective({secondStand},TIMESPACE_GOAL_SURVIVE_ONE_MINUTE)",
                $"dialog({secondStand},6102)",
                $"summon({secondStand},140,13)",
                $"roomClock({secondStand},60s)",
                $"summon({secondStand},141,13,140,66,66)",
                "message(EnemiesReinforcements)"
            }, CallsSince(before));
        }

        [TestMethod]
        public async Task HiddenSchemesBarracksHoldTheirKnightsBeforeTheTalk()
        {
            await LoadAsync("timespace-18");

            CollectionAssert.Contains(_run.Calls, $"summon({Room(4102, 1, 10)},{string.Join(",", Enumerable.Repeat(338, 17))})");
            CollectionAssert.Contains(_run.Calls, $"summon({Room(4102, 1, 11)},{string.Join(",", Enumerable.Repeat(338, 15))})");
        }

        [TestMethod]
        public async Task TheKovoltRiotGalleryKovoltCallsAnotherAsItFalls()
        {
            await LoadAsync("timespace-09");
            var gallery = Room(4005, 7, 5);
            var before = _run.Calls.Count;

            Fall(108, 1, 9);

            CollectionAssert.AreEqual(new[] { $"summon({gallery},108)" }, CallsSince(before));
        }

        [TestMethod]
        public async Task TheTutorialTimedRoomClockStartsOnEntryAndStopsWhenCleared()
        {
            await LoadAsync("timespace-00");
            var timed = Room(4101, 3, 7);
            var before = _run.Calls.Count;

            _script.OnRoomEntered(_run, timed);
            _script.OnRoomCleared(_run, timed);

            var calls = CallsSince(before);
            CollectionAssert.AreEqual(new[] { $"dialog({timed},8010)", $"roomClock({timed},120s)" }, calls.Take(2).ToList());
            Assert.AreEqual($"stopRoomClock({timed})", calls[^1]);
        }
    }
}
