//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.I18N;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Instances.Tests
{
    [TestClass]
    public class TimeSpaceScriptTests
    {
        private const short Grunt = 58;
        private const short Boss = 59;

        private readonly Declared _script = new();
        private readonly RecordingRun _run = new();
        private readonly IRoom _first;
        private readonly IRoom _second;

        public TimeSpaceScriptTests()
        {
            _first = _script.NewRoom();
            _second = _script.NewRoom();
        }

        private sealed class Declared : TimeSpaceScript
        {
            public Declared() : base(1, "Declared")
            {
            }

            public IRoom NewRoom() => Room(4100, 0, 0);

            public static IMonster Mob(short vNum) => Monster(vNum);

            public static INpc Guide(short vNum) => Npc(vNum);

            public void WithClock(TimeSpan limit) => Clock(limit);

            public void RunAfter(TimeSpan elapsed, Action<RunEvent> step) => After(elapsed, step);

            public void WhenTimeRunsOut(Action<RunEvent> step) => OnTimeout(step);
        }

        private IInstanceScript Script => _script;

        [TestMethod]
        public void ARoomHoldsItsMonstersFromTheStart()
        {
            new Spec("The run's clock starts, then the room's monsters come in where they were placed")
                .Given(TheRunHasAClockOf_Minutes, 10)
                .And(TheSecondRoomHolds_At_, Grunt, new Spot(3, 4))
                .When(TheRunStarts)
                .Then(TheCallsShouldBe_, new[] { "clock(10m)", "summon(2,58)" })
                .And(Monster_ShouldStandAt_, 0, new Spot(3, 4))
                .Execute();
        }

        [TestMethod]
        public void EnteringARoomRunsOnlyItsOwnSteps()
        {
            new Spec("Only the room that is entered runs its steps")
                .Given(TheSecondRoomOpensDialog_OnEntry, 6000)
                .When(TheFirstRoomIsEntered)
                .And(TheSecondRoomIsEntered)
                .Then(TheCallsShouldBe_, new[] { "dialog(2,6000)" })
                .Execute();
        }

        [TestMethod]
        public void ClosingADialogRunsTheStepsOfThatDialog()
        {
            new Spec("Closing a dialog brings in what that dialog was holding back, and closing another does nothing")
                .Given(ClosingDialog_InTheFirstRoomSpawns_, 6000, Grunt)
                .When(Dialog_IsClosedInTheFirstRoom, 6001)
                .And(Dialog_IsClosedInTheFirstRoom, 6000)
                .Then(TheCallsShouldBe_, new[] { "summon(1,58)", "objective(1,TIMESPACE_GOAL_KILL_ALL_ENEMIES)" })
                .Execute();
        }

        [TestMethod]
        public void ClearingARoomOpensTheDoorsItNames()
        {
            new Spec("A cleared room opens the portal it names and says so")
                .Given(ClearingTheFirstRoomOpensItsPortal)
                .And(TheRunStarts)
                .When(TheFirstRoomIsCleared)
                .Then(TheCallsShouldEndWith_, new[] { "unlock(1->2)", "message(ADoorHasOpened)", "addClock(100s)" })
                .Execute();
        }

        [TestMethod]
        public void ALeverRunsItsStepsWhenItIsUsed()
        {
            new Spec("Each lever of a room has its own id and its own steps")
                .Given(TheFirstRoomHasTwoLevers)
                .And(TheRunStarts)
                .When(Button_OfTheFirstRoomIsUsed, 2)
                .Then(TheCallsShouldEndWith_, new[] { "message(NewGoal)" })
                .And(TheCallsShouldInclude_, "button(1,1,9,9,1000,1045,False)")
                .And(TheCallsShouldInclude_, "button(1,2,3,3,1000,1045,False)")
                .Execute();
        }

        [TestMethod]
        public void AMonstersDeathRunsInTheRoomItFellIn()
        {
            new Spec("A monster's death brings the next one into the room it stood in")
                .Given(TheSecondRoomHoldsAGruntWhoseDeathBringsTheBoss)
                .And(TheRunStarts)
                .When(Monster_Falls, 0)
                .Then(TheCallsShouldEndWith_, new[] { "summon(2,59)" })
                .Execute();
        }

        [TestMethod]
        public void AMonsterTakingAHitRunsItsHitSteps()
        {
            new Spec("A monster's hit steps run in its room")
                .Given(TheFirstRoomHoldsAGruntThatCallsForHelpWhenHit)
                .And(TheRunStarts)
                .When(Monster_IsHit, 0)
                .Then(TheCallsShouldEndWith_, new[] { "message(EnemiesReinforcements)" })
                .Execute();
        }

        [TestMethod]
        public void HpThresholdsAreHandedOverFromTheHighest()
        {
            new Spec("A monster's HP thresholds reach the run highest first, each with its own step")
                .Given(TheFirstRoomHoldsABossWithThresholdsAt50And75)
                .And(TheRunStarts)
                .When(EveryThresholdOfMonster_IsCrossed, 0)
                .Then(TheThresholdsOfMonster_ShouldBe_, 0, new byte[] { 75, 50 })
                .And(TheCallsShouldEndWith_, new[] { "summon(1,58)", "summon(1,58,58)" })
                .Execute();
        }

        [TestMethod]
        public void ReachingTheExitRunsItsStepsThenWins()
        {
            new Spec("An exit's own steps run before the run is won")
                .Given(TheSecondRoomHasAnExitThatSaysGoodbye)
                .And(TheRunStarts)
                .When(TheExitOfTheSecondRoomIsReached)
                .Then(TheCallsShouldEndWith_, new[] { "message(TimeSpaceCompleted)", "succeed" })
                .And(TheCallsShouldInclude_, "exit(2,locked)")
                .Execute();
        }

        [TestMethod]
        public void AnExitCanBeUnlocked()
        {
            new Spec("A locked exit opens when a step unlocks it")
                .Given(ClearingTheSecondRoomUnlocksItsExit)
                .And(TheRunStarts)
                .When(TheSecondRoomIsCleared)
                .Then(TheCallsShouldEndWith_, new[] { "unlockExit(2)" })
                .Execute();
        }

        [TestMethod]
        public void ARoomHasOneExitAtMost()
        {
            _first.Exit(new Spot(1, 1));

            Assert.ThrowsExactly<InvalidOperationException>(() => _first.Exit(new Spot(2, 2)));
        }

        [TestMethod]
        public void ARunTimerActsWhenItsTimeComes()
        {
            new Spec("A run timer is set for its time after the start and can act in a room")
                .Given(OneMinuteInTheBossComesToTheSecondRoom)
                .And(TheRunStarts)
                .When(TheTimersRun)
                .Then(TheTimersShouldHaveBeenAt_Seconds, new[] { 60d })
                .And(TheCallsShouldEndWith_, new[] { "summon(2,59)", "message(EnemiesReinforcements)" })
                .Execute();
        }

        [TestMethod]
        public void RunningOutOfTimeRunsTheTimeoutSteps()
        {
            new Spec("Running out of time runs the steps set for it")
                .Given(RunningOutOfTimeSaysSo)
                .When(TheRunTimesOut)
                .Then(TheCallsShouldBe_, new[] { "message(QuickMissionComplete)", "fail" })
                .Execute();
        }

        [TestMethod]
        public void ARoomClockBringsWavesWhileSomeoneIsInside()
        {
            new Spec("A room clock brings a wave at once and every 30 seconds, then opens the way when it runs out")
                .Given(TheFirstRoomHoldsOutForAMinuteWithWavesEvery30Seconds)
                .And(_PlayersAreInTheRoom, 1)
                .And(TheRunStarts)
                .When(Dialog_IsClosedInTheFirstRoom, 6000)
                .And(TheTimersRun)
                .Then(TheCallsShouldBe_, new[]
                {
                    "portal(1->2,locked)",
                    "roomClock(1,60s)", "message(EnemiesReinforcements)", "summon(1,58)",
                    "message(EnemiesReinforcements)", "summon(1,58)",
                    "unlock(1->2)", "stopRoomClock(1)"
                })
                .Execute();
        }

        [TestMethod]
        public void AWaveIsSkippedWhileTheRoomIsEmpty()
        {
            new Spec("Nobody in the room means no wave, but the clock still runs out")
                .Given(TheFirstRoomHoldsOutForAMinuteWithWavesEvery30Seconds)
                .And(_PlayersAreInTheRoom, 0)
                .And(TheRunStarts)
                .When(Dialog_IsClosedInTheFirstRoom, 6000)
                .And(TheTimersRun)
                .Then(TheCallsShouldBe_, new[] { "portal(1->2,locked)", "roomClock(1,60s)", "unlock(1->2)", "stopRoomClock(1)" })
                .Execute();
        }

        [TestMethod]
        public void EachRunKeepsItsOwnRoomClock()
        {
            var other = new RecordingRun { Players = 1 };
            _first.OnDialogClosed(6000, r => r.StartRoomClock(TimeSpan.FromSeconds(60)))
                .Every(TimeSpan.FromSeconds(30), r => r.Say(Game18NConstString.EnemiesReinforcements));
            _run.Players = 1;

            Script.OnDialogClosed(_run, 1, 6000);
            Script.OnDialogClosed(other, 1, 6000);
            RunTimers(_run);
            var wave = other.Scheduled.Single(s => s.At == TimeSpan.FromSeconds(30));
            wave.Step(other);

            Assert.AreEqual(2, other.Calls.Count(c => c == "message(EnemiesReinforcements)"));
        }

        [TestMethod]
        public void ARestartedRoomClockIsNotEndedByTheClockItReplaced()
        {
            _first.OnDialogClosed(6000, r => r.StartRoomClock(TimeSpan.FromSeconds(30)))
                .OnDialogClosed(6001, r => r.StartRoomClock(TimeSpan.FromSeconds(30)))
                .OnClockOut(r => r.Say(Game18NConstString.ADoorHasOpened));

            Script.OnDialogClosed(_run, 1, 6000);
            _run.Elapsed = TimeSpan.FromSeconds(10);
            Script.OnDialogClosed(_run, 1, 6001);
            var ran = RunTimers(_run);

            CollectionAssert.AreEqual(new[] { 30d, 40d }, ran);
            Assert.AreEqual(1, _run.Calls.Count(c => c == "message(ADoorHasOpened)"));
            CollectionAssert.AreEqual(new[] { "stopRoomClock(1)" }, _run.Calls.Where(c => c.StartsWith("stopRoomClock")).ToList());
            Assert.IsNull(_run.RoomClockEnd(1));
        }

        [TestMethod]
        public void AWaveFirstReachedAfterTheClockRanOutDoesNotRun()
        {
            _first.OnDialogClosed(6000, r => r.StartRoomClock(TimeSpan.FromSeconds(3)))
                .Every(TimeSpan.FromSeconds(1), r => r.Say(Game18NConstString.EnemiesReinforcements))
                .OnClockOut(r => r.Say(Game18NConstString.ADoorHasOpened));
            _run.Players = 1;

            Script.OnDialogClosed(_run, 1, 6000);
            _run.Elapsed = TimeSpan.FromSeconds(4);
            foreach (var (_, step) in _run.Scheduled.OrderBy(s => s.At).ToList())
            {
                step(_run);
            }

            Assert.AreEqual(1, _run.Calls.Count(c => c == "message(EnemiesReinforcements)"));
            Assert.AreEqual("stopRoomClock(1)", _run.Calls[^1]);
        }

        [TestMethod]
        public void ASecondPortalToTheSameRoomIsRefused()
        {
            _first.Portal(new Spot(17, 9), _second, new Spot(0, 9));

            Assert.ThrowsExactly<InvalidOperationException>(() => _first.Portal(new Spot(9, 17), _second, new Spot(9, 0)));
        }

        [TestMethod]
        public void AnywherePlacesEachCopyOnItsOwn()
        {
            new Spec("Copies sent anywhere each get a cell drawn for them")
                .Given(TheFirstRoomHolds_GruntsAnywhere, 3)
                .When(TheRunStarts)
                .Then(TheMonstersShouldStandAt_, new[] { new Spot(10, 5), new Spot(10, 6), new Spot(10, 7) })
                .Execute();
        }

        [TestMethod]
        public void AroundPicksACellNearTheSpot()
        {
            new Spec("A monster placed around a spot asks for a cell within that radius")
                .Given(TheFirstRoomHoldsAGruntWithin_CellsOf_, (short)2, new Spot(20, 20))
                .When(TheRunStarts)
                .Then(TheMonstersShouldStandAt_, new[] { new Spot(22, 18) })
                .Execute();
        }

        [TestMethod]
        public void CountAndBonusApplyToEveryCopy()
        {
            new Spec("Two bonus copies stand on the same spot")
                .Given(TheFirstRoomHoldsTwoBonusGruntsAt_, new Spot(4, 4))
                .When(TheRunStarts)
                .Then(TheMonstersShouldStandAt_, new[] { new Spot(4, 4), new Spot(4, 4) })
                .And(EveryMonsterShouldBeABonus)
                .Execute();
        }

        [TestMethod]
        public void AMonsterIsAValueToBuildOn()
        {
            var grunt = Declared.Mob(Grunt);
            _first.Spawn(grunt.At(1, 1), grunt.At(2, 2).Bonus());

            Script.OnFirstEnter(_run);

            CollectionAssert.AreEqual(new[] { (new Spot(1, 1), false), (new Spot(2, 2), true) },
                _run.Summoned.Select(s => (s.At, s.IsBonus)).ToArray());
        }

        [TestMethod]
        public void AMonsterWithoutAPlaceIsRefused()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => _first.Spawn(Declared.Mob(Grunt)));
        }

        [TestMethod]
        public void AStoppedRoomClockBringsNoMoreWavesAndNeverRunsOut()
        {
            new Spec("Clearing the room takes its clock down, so the second wave and the clock's end never come")
                .Given(TheFirstRoomHoldsOutForAMinuteWithWavesEvery30Seconds)
                .And(ClearingTheFirstRoomStopsItsClock)
                .And(_PlayersAreInTheRoom, 1)
                .And(TheRunStarts)
                .When(Dialog_IsClosedInTheFirstRoom, 6000)
                .And(TheFirstRoomIsCleared)
                .And(TheTimersRun)
                .Then(TheCallsShouldBe_, new[]
                {
                    "portal(1->2,locked)",
                    "roomClock(1,60s)", "message(EnemiesReinforcements)", "summon(1,58)",
                    "stopRoomClock(1)"
                })
                .Execute();
        }

        [TestMethod]
        public void AStepCanWaitBeforeActingInItsRoom()
        {
            new Spec("Thirty seconds after the room is entered, the grunt comes in")
                .Given(EnteringTheFirstRoomBringsAGruntAfter_Seconds, 30)
                .When(TheFirstRoomIsEntered)
                .And(TheTimersRun)
                .Then(TheCallsShouldBe_, new[] { "summon(1,58)" })
                .And(TheTimersShouldHaveBeenAt_Seconds, new[] { 30d })
                .Execute();
        }

        [TestMethod]
        public void AStepCanDismissSpeakToItsRoomAndBringNpcs()
        {
            new Spec("A cleared room sends its monsters away, tells only its own players and brings a guide")
                .Given(ClearingTheFirstRoomDismissesSpeaksAndBringsAGuide)
                .When(TheFirstRoomIsCleared)
                .Then(TheCallsShouldBe_, new[] { "dismiss(1,58)", "dismiss(1,59)", "message(1,ADoorHasOpened)", "npcs(1,311)" })
                .Execute();
        }

        [TestMethod]
        public void AStepCanActOnlyWhenSomeoneIsInside()
        {
            new Spec("Clearing an empty room adds no time; clearing it with someone inside does")
                .Given(ClearingTheFirstRoomAddsTimeIfAnyoneIsInside)
                .And(_PlayersAreInTheRoom, 0)
                .When(TheFirstRoomIsCleared)
                .And(_PlayersAreInTheRoom, 1)
                .And(TheFirstRoomIsCleared)
                .Then(TheCallsShouldBe_, new[] { "addClock(300s)" })
                .Execute();
        }

        [TestMethod]
        public void ASculptureAndALeverAreDrawnWithTheirOwnVnums()
        {
            new Spec("A sculpture and a button are placed with the vnums they were given")
                .Given(TheFirstRoomHasASculptureAndAButton)
                .When(TheRunStarts)
                .Then(TheCallsShouldBe_, new[] { "button(1,1,10,16,1048,1049,False)", "button(1,2,15,15,1051,1052,False)" })
                .Execute();
        }

        [TestMethod]
        public void NpcsStandWhereTheyArePut()
        {
            _first.Spawn(Declared.Guide(311).At(16, 12), Declared.Guide(318).At(14, 14));

            Script.OnFirstEnter(_run);

            CollectionAssert.AreEqual(new[] { "npcs(1,311,318)" }, _run.Calls);
        }

        private void TheRunHasAClockOf_Minutes(int minutes) => _script.WithClock(TimeSpan.FromMinutes(minutes));

        private void TheSecondRoomHolds_At_(short vNum, Spot at) => _second.Spawn(Declared.Mob(vNum).At(at.X, at.Y));

        private void TheSecondRoomOpensDialog_OnEntry(int dialog) => _second.OnEnter(r => r.Dialog(dialog));

        private void ClosingDialog_InTheFirstRoomSpawns_(int dialog, short vNum) =>
            _first.OnDialogClosed(dialog, r => r.Spawn(Declared.Mob(vNum).At(1, 1))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES));

        private void ClearingTheFirstRoomOpensItsPortal()
        {
            var door = _first.Portal(new Spot(17, 9), _second, new Spot(0, 9));
            _first.OnCleared(r => r.Unlock(door).Say(Game18NConstString.ADoorHasOpened).AddTime(TimeSpan.FromSeconds(100)));
        }

        private void TheFirstRoomHasTwoLevers()
        {
            _first.Lever(new Spot(9, 9)).OnUse(r => r.Say(Game18NConstString.ADoorHasOpened));
            _first.Lever(new Spot(3, 3)).OnUse(r => r.Say(Game18NConstString.NewGoal));
        }

        private void TheSecondRoomHoldsAGruntWhoseDeathBringsTheBoss() =>
            _second.Spawn(Declared.Mob(Grunt).At(1, 1).OnDeath(r => r.Spawn(Declared.Mob(Boss).At(5, 5))));

        private void TheFirstRoomHoldsAGruntThatCallsForHelpWhenHit() =>
            _first.Spawn(Declared.Mob(Grunt).At(1, 1).OnHit(r => r.Say(Game18NConstString.EnemiesReinforcements)));

        private void TheFirstRoomHoldsABossWithThresholdsAt50And75() =>
            _first.Spawn(Declared.Mob(Boss).At(1, 1)
                .OnHpBelow(50, r => r.Spawn(Declared.Mob(Grunt).At(2, 2).Count(2)))
                .OnHpBelow(75, r => r.Spawn(Declared.Mob(Grunt).At(2, 2))));

        private void TheSecondRoomHasAnExitThatSaysGoodbye() =>
            _second.Exit(new Spot(28, 15)).OnReached(r => r.Say(Game18NConstString.TimeSpaceCompleted));

        private void ClearingTheSecondRoomUnlocksItsExit()
        {
            var exit = _second.Exit(new Spot(28, 15));
            _second.OnCleared(r => r.Unlock(exit));
        }

        private void OneMinuteInTheBossComesToTheSecondRoom() =>
            _script.RunAfter(TimeSpan.FromMinutes(1), r => r.In(_second).Spawn(Declared.Mob(Boss).At(5, 5))
                .Say(Game18NConstString.EnemiesReinforcements));

        private void RunningOutOfTimeSaysSo() => _script.WhenTimeRunsOut(r => r.Say(Game18NConstString.QuickMissionComplete).Fail());

        private void TheFirstRoomHoldsOutForAMinuteWithWavesEvery30Seconds()
        {
            var door = _first.Portal(new Spot(17, 9), _second, new Spot(0, 9));
            _first.OnDialogClosed(6000, r => r.StartRoomClock(TimeSpan.FromSeconds(60)))
                .Every(TimeSpan.FromSeconds(30), r => r
                    .Say(Game18NConstString.EnemiesReinforcements)
                    .Spawn(Declared.Mob(Grunt).At(5, 5)))
                .OnClockOut(r => r.Unlock(door));
        }

        private void TheFirstRoomHolds_GruntsAnywhere(int copies) => _first.Spawn(Declared.Mob(Grunt).Anywhere().Count(copies));

        private void TheFirstRoomHoldsAGruntWithin_CellsOf_(short radius, Spot spot) =>
            _first.Spawn(Declared.Mob(Grunt).Around(spot.X, spot.Y, radius));

        private void TheFirstRoomHoldsTwoBonusGruntsAt_(Spot spot) =>
            _first.Spawn(Declared.Mob(Grunt).At(spot.X, spot.Y).Bonus().Count(2));

        private void ClearingTheFirstRoomStopsItsClock() => _first.OnCleared(r => r.StopRoomClock());

        private void EnteringTheFirstRoomBringsAGruntAfter_Seconds(int seconds) =>
            _first.OnEnter(r => r.After(TimeSpan.FromSeconds(seconds), r => r.Spawn(Declared.Mob(Grunt).At(1, 1))));

        private void ClearingTheFirstRoomDismissesSpeaksAndBringsAGuide() =>
            _first.OnCleared(r => r.Dismiss(Grunt, Boss).SayToRoom(Game18NConstString.ADoorHasOpened).Spawn(Declared.Guide(311).At(1, 1)));

        private void ClearingTheFirstRoomAddsTimeIfAnyoneIsInside() =>
            _first.OnCleared(r => r.IfAnyoneInside(r => r.AddTime(TimeSpan.FromSeconds(300))));

        private void TheFirstRoomHasASculptureAndAButton()
        {
            _first.Sculpture(new Spot(10, 16), 1048, 1049);
            _first.Lever(new Spot(15, 15), 1051, 1052);
        }

        private void _PlayersAreInTheRoom(int players) => _run.Players = players;

        private void TheRunStarts() => Script.OnFirstEnter(_run);

        private void TheFirstRoomIsEntered() => Script.OnRoomEntered(_run, 1);

        private void TheSecondRoomIsEntered() => Script.OnRoomEntered(_run, 2);

        private void Dialog_IsClosedInTheFirstRoom(int dialog) => Script.OnDialogClosed(_run, 1, dialog);

        private void TheFirstRoomIsCleared() => Script.OnRoomCleared(_run, 1);

        private void TheSecondRoomIsCleared() => Script.OnRoomCleared(_run, 2);

        private void Button_OfTheFirstRoomIsUsed(int button) => Script.OnButtonUsed(_run, 1, button);

        private void Monster_Falls(int index) => _run.Summoned[index].OnDeath?.Invoke(_run);

        private void Monster_IsHit(int index) => _run.Summoned[index].OnHit?.Invoke(_run);

        private void EveryThresholdOfMonster_IsCrossed(int index)
        {
            foreach (var threshold in _run.Summoned[index].OnHpBelow ?? [])
            {
                threshold.Step(_run);
            }
        }

        private void TheExitOfTheSecondRoomIsReached() => Script.OnExitReached(_run, 2);

        private void TheRunTimesOut() => Script.OnTimeout(_run);

        private readonly List<double> _timersRan = [];

        private void TheTimersRun()
        {
            _timersRan.AddRange(RunTimers(_run));
        }

        private static List<double> RunTimers(RecordingRun run)
        {
            var ran = new List<double>();
            while (run.Scheduled.Count > 0)
            {
                var next = run.Scheduled.OrderBy(s => s.At).First();
                run.Scheduled.Remove(next);
                run.Elapsed = next.At;
                next.Step(run);
                ran.Add(next.At.TotalSeconds);
            }

            return ran;
        }

        private void TheCallsShouldBe_(string[] calls)
        {
            CollectionAssert.AreEqual(calls, _run.Calls);
            _run.Calls.Clear();
        }

        private void TheCallsShouldEndWith_(string[] calls) =>
            CollectionAssert.AreEqual(calls, _run.Calls.TakeLast(calls.Length).ToList());

        private void TheCallsShouldInclude_(string call) => CollectionAssert.Contains(_run.Calls, call);

        private void Monster_ShouldStandAt_(int index, Spot at) => Assert.AreEqual(at, _run.Summoned[index].At);

        private void TheMonstersShouldStandAt_(Spot[] spots) =>
            CollectionAssert.AreEqual(spots, _run.Summoned.Select(s => s.At).ToArray());

        private void EveryMonsterShouldBeABonus() => Assert.IsTrue(_run.Summoned.All(s => s.IsBonus));

        private void TheThresholdsOfMonster_ShouldBe_(int index, byte[] percents) =>
            CollectionAssert.AreEqual(percents, (_run.Summoned[index].OnHpBelow ?? []).Select(t => t.Percent).ToArray());

        private void TheTimersShouldHaveBeenAt_Seconds(double[] seconds) => CollectionAssert.AreEqual(seconds, _timersRan);
    }
}
