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

        public void Throw(int room, Spot around, params ItemPile[] piles) => Calls.Add($"throw({room},{piles.Length})");

        public int MonstersAlive(int room) => Alive;

        public int Players { get; set; }

        public bool Decided { get; set; }

        public int PlayersIn(int room) => Players;

        public bool OneSideStanding(int room) => Decided;

        public void SetPvp(int room, bool isPvp) => Calls.Add($"pvp({room},{isPvp})");

        public void Summon(int room, params MonsterSpawn[] monsters) =>
            Calls.Add($"summon({room},{string.Join(",", monsters.Select(m => m.VNum))})");

        public void Hunt(int room, short vNum) => Calls.Add($"hunt({room},{vNum})");

        public void Dismiss(int room, short vNum) => Calls.Add($"dismiss({room},{vNum})");

        public void SetRates(int room, int experience, int drops) => Calls.Add($"rates({room},{experience},{drops})");

        public void SpawnPortal(int fromRoom, Spot at, int toRoom, Spot arrival, bool locked = true) =>
            Calls.Add($"portal({fromRoom}->{toRoom},{(locked ? "locked" : "open")})");

        public void UnlockPortal(int fromRoom, int toRoom) => Calls.Add($"unlock({fromRoom}->{toRoom})");

        public void SetMonsterLocker(int room, short count) => Calls.Add($"monsterLocker({room},{count})");

        public void StartClock(TimeSpan limit) => Calls.Add($"clock({limit.TotalMinutes}m)");

        public void ShowCountdown(int room, TimeSpan length) => Calls.Add($"countdown({room},{length.TotalSeconds}s)");

        public Queue<bool> Fighters { get; } = new();

        public int Standing { get; set; } = 2;

        public bool CallFighters(int room)
        {
            var called = Fighters.Count > 0 && Fighters.Dequeue();
            Calls.Add($"call({room},{called})");
            return called;
        }

        public void OpenBout(int room) => Calls.Add($"open({room})");

        public int FightersStanding(int room) => Standing;

        public void EndBout(int room) => Calls.Add($"endBout({room})");

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
        public async Task ALandOfDeathHuntsEveryFourMinutesFromHalfTime()
        {
            var script = await Loader().LoadAsync("land-of-death");
            var run = new RecordingRun();

            script.OnFirstEnter(run);

            Assert.AreEqual("clock(60m)", run.Calls.Single());
            CollectionAssert.AreEqual(
                new[] { 30d, 31, 34, 35, 38, 39, 42, 43, 46, 47, 50, 51, 54, 55, 55, 58, 59 },
                run.Scheduled.Select(s => s.At.TotalMinutes).OrderBy(m => m).ToArray());
        }

        [TestMethod]
        public async Task ALandOfDeathDevilTriplesRatesUntilItLeaves()
        {
            var script = await Loader().LoadAsync("land-of-death");
            var run = new RecordingRun();
            script.OnFirstEnter(run);
            run.Calls.Clear();

            run.Scheduled.Single(s => s.At == TimeSpan.FromMinutes(30)).Step(run);
            run.Scheduled.Single(s => s.At == TimeSpan.FromMinutes(31)).Step(run);

            CollectionAssert.AreEqual(new[]
            {
                "rates(1,3,3)",
                "hunt(1,443)",
                "message(FlyingFireDevilAppears)",
                "rates(1,1,1)",
                "dismiss(1,443)",
                "message(FlyingFireDevilVanished)"
            }, run.Calls);
        }

        [TestMethod]
        public async Task ALandFirstReachedLateOnlyKeepsWhatIsLeft()
        {
            var script = await Loader().LoadAsync("land-of-death");
            var run = new RecordingRun { Elapsed = TimeSpan.FromMinutes(34.5) };

            script.OnFirstEnter(run);

            Assert.AreEqual("clock(25.5m)", run.Calls.Single());
            Assert.AreEqual(34d, run.Scheduled.Min(s => s.At.TotalMinutes));
        }

        [TestMethod]
        public async Task ALandOfDeathClosesWhenItsClockRunsOut()
        {
            var script = await Loader().LoadAsync("land-of-death");
            var run = new RecordingRun();

            script.OnTimeout(run);

            CollectionAssert.AreEqual(new[] { "fail" }, run.Calls);
        }

        [TestMethod]
        public async Task TheRaidIsOpenedByItsSealForATeamInItsLevelRange()
        {
            var definition = (await Loader().LoadAsync("raid-mother-cuby")).Definition;

            Assert.AreEqual(1127, definition.RequiredItems.Single().VNum);
            Assert.AreEqual(30, definition.LevelMinimum);
            Assert.AreEqual(49, definition.LevelMaximum);
            Assert.AreEqual(5, definition.TeamSizeMinimum);
            Assert.AreEqual(15, definition.TeamSizeMaximum);
            Assert.AreEqual(4684, definition.GiftItems.Single().VNum);
        }

        private static void RunNextStep(RecordingRun run)
        {
            var next = run.Scheduled.OrderBy(s => s.At).First();
            run.Scheduled.Remove(next);
            run.Elapsed = next.At;
            next.Step(run);
        }

        [TestMethod]
        public async Task TheArenaGivesFifteenSecondsBeforeTheFirstBout()
        {
            var script = await Loader().LoadAsync("talent-arena");
            var run = new RecordingRun();

            script.OnFirstEnter(run);

            CollectionAssert.AreEqual(new[] { "countdown(1,15s)" }, run.Calls);
            Assert.AreEqual(TimeSpan.FromSeconds(15), run.Scheduled.Single().At);
            Assert.AreEqual(2, script.Definition.Sides.Count);
        }

        [TestMethod]
        public async Task ABoutOpensFiveSecondsAfterTheFightersAreCalled()
        {
            var script = await Loader().LoadAsync("talent-arena");
            var run = new RecordingRun();
            run.Fighters.Enqueue(true);
            script.OnFirstEnter(run);

            while (!run.Calls.Contains("open(1)"))
            {
                RunNextStep(run);
            }

            CollectionAssert.AreEqual(new[] { "countdown(1,15s)", "call(1,True)", "countdown(1,305s)", "open(1)" }, run.Calls);
            Assert.AreEqual(TimeSpan.FromSeconds(20), run.Elapsed);
        }

        [TestMethod]
        public async Task AFallCallsTheNextFightersThreeSecondsLater()
        {
            var script = await Loader().LoadAsync("talent-arena");
            var run = new RecordingRun();
            run.Fighters.Enqueue(true);
            run.Fighters.Enqueue(true);
            script.OnFirstEnter(run);
            RunNextStep(run);
            RunNextStep(run);

            run.Standing = 1;
            RunNextStep(run);
            RunNextStep(run);

            Assert.AreEqual(TimeSpan.FromSeconds(20), run.Elapsed);
            Assert.AreEqual("call(1,True)", run.Calls[^2]);
        }

        [TestMethod]
        public async Task ABoutStillGoingAfterFiveMinutesEndsWithoutAWinner()
        {
            var script = await Loader().LoadAsync("talent-arena");
            var run = new RecordingRun();
            run.Fighters.Enqueue(true);
            script.OnFirstEnter(run);

            while (!run.Calls.Contains("endBout(1)"))
            {
                RunNextStep(run);
            }

            Assert.AreEqual(TimeSpan.FromSeconds(15 + 305), run.Elapsed);
            RunNextStep(run);
            CollectionAssert.AreEqual(new[] { "call(1,False)", "succeed" }, run.Calls.TakeLast(2).ToList());
        }

        [TestMethod]
        public async Task TheMatchEndsOnceASideHasNobodyLeft()
        {
            var script = await Loader().LoadAsync("talent-arena");
            var run = new RecordingRun();
            script.OnFirstEnter(run);

            RunNextStep(run);

            CollectionAssert.AreEqual(new[] { "countdown(1,15s)", "call(1,False)", "succeed" }, run.Calls);
            Assert.AreEqual(0, run.Scheduled.Count);
        }

        [TestMethod]
        public async Task TheRaidOpensOnItsEntranceWithTheBossWaitingInTheSquare()
        {
            var script = await Loader().LoadAsync("raid-mother-cuby");
            var run = new RecordingRun();

            script.OnFirstEnter(run);

            CollectionAssert.AreEqual(new[]
            {
                "clock(40m)",
                "portal(1->2,open)",
                "summon(2,282,283,283,283,283,283,283,283,283)",
                "summon(2,167,167,167,167,167,167,167,167)",
                "summon(2,168,168,168,168,168,168,168)",
                "summon(2,169,169,169,169,169,169,169)",
                "summon(2,170,170,170,170,170,170,170)"
            }, run.Calls);
        }

        [TestMethod]
        public async Task OnlyTheBossFallingWinsTheRaid()
        {
            var script = await Loader().LoadAsync("raid-mother-cuby");
            var run = new RecordingRun();

            script.OnMonsterKilled(run, 1, 283);
            script.OnMonsterKilled(run, 1, 282);

            CollectionAssert.AreEqual(new[] { "succeed" }, run.Calls);
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

        [TestMethod]
        [DataRow("act4-morcos", (short)563, (short)882)]
        [DataRow("act4-hatus", (short)577, (short)185)]
        [DataRow("act4-calvina", (short)629, (short)942)]
        [DataRow("act4-berios", (short)624, (short)999)]
        public async Task AGlacernonRaidOpensItsBossDoorAtHalfTime(string name, short boss, short gift)
        {
            var script = await Loader().LoadAsync(name);
            var run = new RecordingRun();

            script.OnFirstEnter(run);

            Assert.AreEqual("clock(60m)", run.Calls.First());
            Assert.AreEqual($"summon(2,{boss})", run.Calls.Last());
            Assert.AreEqual(30d, run.Scheduled.Min(s => s.At.TotalMinutes));
            Assert.AreEqual(21, run.Scheduled.Count);
            Assert.AreEqual(60, script.Definition.LevelMinimum);
            Assert.AreEqual(gift, script.Definition.GiftItems.Single().VNum);
            Assert.AreEqual(50, script.Definition.GiftItems.Single().Design);

            run.Calls.Clear();
            run.Scheduled.First(s => s.At == TimeSpan.FromMinutes(30)).Step(run);
            Assert.IsTrue(run.Calls.Contains("portal(1->2,open)"));
        }

        [TestMethod]
        public async Task AGlacernonRaidReachedLateKeepsOnlyTheGuardsStillToCome()
        {
            var script = await Loader().LoadAsync("act4-hatus");
            var run = new RecordingRun { Elapsed = TimeSpan.FromMinutes(45) };

            script.OnFirstEnter(run);

            Assert.AreEqual("clock(15m)", run.Calls.First());
            Assert.IsTrue(run.Scheduled.All(s => s.At >= TimeSpan.FromMinutes(45) || s.At == TimeSpan.FromMinutes(30)));
        }

        [TestMethod]
        public async Task TheGlacernonRaidBossThrowsItsLootAndEndsTheRaid()
        {
            var script = await Loader().LoadAsync("act4-morcos");
            var run = new RecordingRun();

            script.OnMonsterKilled(run, 2, 562);
            Assert.AreEqual(0, run.Calls.Count);
            script.OnMonsterKilled(run, 2, 563);

            CollectionAssert.AreEqual(new[] { "message(CongratulationsKilledTheBoss)", "throw(2,5)", "succeed" }, run.Calls);
        }
    }
}
