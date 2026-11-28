//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.I18N;
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

        public HashSet<int> Cleared { get; } = [];

        public bool WasCleared(int room) => Cleared.Contains(room);

        public int Players { get; set; }

        public bool Decided { get; set; }

        public int PlayersIn(int room) => Players;

        public bool OneSideStanding(int room) => Decided;

        public void SetPvp(int room, bool isPvp) => Calls.Add($"pvp({room},{isPvp})");

        public List<MonsterSpawn> Summoned { get; } = [];

        public void Summon(int room, params MonsterSpawn[] monsters)
        {
            Summoned.AddRange(monsters);
            Calls.Add($"summon({room},{string.Join(",", monsters.Select(m => m.VNum))})");
        }

        public void Hunt(int room, short vNum) => Calls.Add($"hunt({room},{vNum})");

        public void Dismiss(int room, short vNum) => Calls.Add($"dismiss({room},{vNum})");

        public void Dismiss(int room) => Calls.Add($"dismiss({room})");

        public void SetRates(int room, int experience, int drops) => Calls.Add($"rates({room},{experience},{drops})");

        public List<(int From, int To, bool Locked)> Portals { get; } = [];

        public HashSet<(int From, int To)> Unlocked { get; } = [];

        public List<(int Room, bool Locked)> Exits { get; } = [];

        public HashSet<int> UnlockedExits { get; } = [];

        public void SpawnPortal(int fromRoom, Spot at, int toRoom, Spot arrival, bool locked = true)
        {
            Portals.Add((fromRoom, toRoom, locked));
            Calls.Add($"portal({fromRoom}->{toRoom},{(locked ? "locked" : "open")})");
        }

        public void SpawnTeamPortal(int room, Spot at, Spot arrival) =>
            Calls.Add($"teamPortal({room},{at.X},{at.Y}->{arrival.X},{arrival.Y})");

        public void UnlockPortal(int fromRoom, int toRoom)
        {
            Unlocked.Add((fromRoom, toRoom));
            Relocked.Remove((fromRoom, toRoom));
            Calls.Add($"unlock({fromRoom}->{toRoom})");
        }

        public HashSet<(int From, int To)> Relocked { get; } = [];

        public void LockPortal(int fromRoom, int toRoom)
        {
            Unlocked.Remove((fromRoom, toRoom));
            Relocked.Add((fromRoom, toRoom));
            Calls.Add($"lock({fromRoom}->{toRoom})");
        }

        public void SpawnExit(int room, Spot at, bool locked = true)
        {
            Exits.Add((room, locked));
            Calls.Add($"exit({room},{(locked ? "locked" : "open")})");
        }

        public void UnlockExit(int room)
        {
            UnlockedExits.Add(room);
            Calls.Add($"unlockExit({room})");
        }

        public List<NpcSpawn> SummonedNpcs { get; } = [];

        public void SummonNpcs(int room, params NpcSpawn[] npcs)
        {
            SummonedNpcs.AddRange(npcs);
            Calls.Add($"npcs({room},{string.Join(",", npcs.Select(m => m.VNum))})");
        }

        public void AddClockTime(TimeSpan extra) => Calls.Add($"addClock({extra.TotalSeconds}s)");

        private readonly Dictionary<int, RoomClock> _roomClocks = [];
        private int _roomClockStarts;

        public void StartRoomClock(int room, TimeSpan length)
        {
            _roomClocks[room] = new RoomClock(++_roomClockStarts, Elapsed + length);
            Calls.Add($"roomClock({room},{length.TotalSeconds}s)");
        }

        public void StopRoomClock(int room)
        {
            _roomClocks.Remove(room);
            Calls.Add($"stopRoomClock({room})");
        }

        public RoomClock? RoomClockOf(int room) => _roomClocks.TryGetValue(room, out var clock) ? clock : null;

        public List<(int Room, int Dialog)> Dialogs { get; } = [];

        public void Dialog(int room, int dialog)
        {
            Dialogs.Add((room, dialog));
            Calls.Add($"dialog({room},{dialog})");
        }

        public void SetMonsterLocker(int room, short count) => Calls.Add($"monsterLocker({room},{count})");

        public void SetMonsterLocker(int room, short count, short countedVNum) =>
            Calls.Add($"monsterLocker({room},{count},{countedVNum})");

        public int LockerLeft { get; set; }

        int IInstanceRun.LockerLeft(int room) => LockerLeft;

        public int ClockScale { get; set; } = 1;

        private short _randomCells;

        public List<int> RandomCellRooms { get; } = [];

        public Spot RandomCell(int room)
        {
            RandomCellRooms.Add(room);
            return new(10, (short)(5 + _randomCells++));
        }

        private int _cellsAround;

        // Walks the cells of the square's east edge, so each one stays within the radius.
        public Spot RandomCell(int room, Spot around, short radius) =>
            new((short)(around.X + radius), (short)(around.Y - radius + _cellsAround++ % (2 * radius + 1)));

        public HashSet<int> ButtonsOn { get; } = [];

        public HashSet<(int Room, int Button)> Used { get; } = [];

        public bool IsButtonOn(int room, int button) => ButtonsOn.Contains(button) || Used.Contains((room, button));

        public Dictionary<(int Room, int Button), int> SwitchedOn { get; } = [];

        public int TimesSwitchedOn(int room, int button) =>
            SwitchedOn.TryGetValue((room, button), out var times) ? times : IsButtonOn(room, button) ? 1 : 0;

        public bool IsPortalOpen(int fromRoom, int toRoom) =>
            !Relocked.Contains((fromRoom, toRoom))
            && (Unlocked.Contains((fromRoom, toRoom)) || Portals.Any(p => p.From == fromRoom && p.To == toRoom && !p.Locked));

        public List<(int Room, int Button)> Buttons { get; } = [];

        public void PlaceButton(int room, int button, Spot at, short offVNum, short onVNum, bool toggles = false, ButtonKind kind = ButtonKind.Lever)
        {
            Buttons.Add((room, button));
            Calls.Add($"button({room},{button},{at.X},{at.Y},{offVNum},{onVNum},{toggles})");
        }

        public void RemoveButton(int room, int button) => Calls.Add($"removebutton({room},{button})");

        public void PlaceNpc(int room, short vNum, Spot at) => Calls.Add($"npc({room},{vNum},{at.X},{at.Y})");

        public List<(int Room, short VNum)> Items { get; } = [];

        public void PlaceItem(int room, short vNum, Spot at)
        {
            Items.Add((room, vNum));
            Calls.Add($"item({room},{vNum},{at.X},{at.Y})");
        }

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

        public void Message(Game18NConstString message, long value) => Calls.Add($"message({message},{value})");

        public void Message(LanguageKey message, params object[] arguments) =>
            Calls.Add($"message({message}{string.Concat(arguments.Select(a => $",{a}"))})");

        public void Message(int room, Game18NConstString message) => Calls.Add($"message({room},{message})");

        public void Message(int room, LanguageKey message, params object[] arguments) =>
            Calls.Add($"message({room},{message}{string.Concat(arguments.Select(a => $",{a}"))})");

        public void Objective(int room, LanguageKey objective) => Calls.Add($"objective({room},{objective})");

        public void Succeed() => Calls.Add("succeed");

        public void Fail() => Calls.Add("fail");
    }

    [TestClass]
    public class CsxInstanceScriptTests
    {
        private static CsxInstanceScriptLoader Loader() =>
            new(Path.Combine(AppContext.BaseDirectory, "instances"));

        private static async Task<IInstanceScript> LoadAsync(string name) =>
            await Loader().LoadAsync(name) ?? throw new AssertFailedException($"{name}.csx did not load.");

        [TestMethod]
        public async Task AScriptThatDoesNotExistLoadsAsNothing()
        {
            Assert.IsNull(await Loader().LoadAsync("no-such-script"));
        }

        [TestMethod]
        public async Task TheScriptDescribesItsRooms()
        {
            var script = await LoadAsync("timespace-sample");

            Assert.AreEqual(2, script.Definition.Rooms.Count);
            Assert.AreEqual(20, script.Definition.StartX);
            Assert.AreEqual(33, script.Definition.StartY);
        }

        [TestMethod]
        public async Task EnteringOpensTheFirstRoom()
        {
            var script = await LoadAsync("timespace-sample");
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
            var script = await LoadAsync("timespace-sample");
            var run = new RecordingRun();

            script.OnRoomCleared(run, 1);

            CollectionAssert.AreEqual(new[] { "unlock(1->2)", "summon(2,60)", "monsterLocker(2,1)" }, run.Calls);
        }

        [TestMethod]
        public async Task ClearingTheLastRoomEndsTheInstance()
        {
            var script = await LoadAsync("timespace-sample");
            var run = new RecordingRun();

            script.OnRoomCleared(run, 2);

            CollectionAssert.AreEqual(new[] { "message(TimeSpaceCompleted,1)", "succeed" }, run.Calls);
        }

        [TestMethod]
        public async Task TheClockRunningOutIsLeftToEndTheRunAsATimeout()
        {
            var script = await LoadAsync("timespace-sample");
            var run = new RecordingRun();

            script.OnTimeout(run);

            CollectionAssert.AreEqual(Array.Empty<string>(), run.Calls);
        }

        [TestMethod]
        public async Task InstantCombatRunsItsFifteenMinutesFromTheFirstWave()
        {
            var script = await LoadAsync("instant-combat");
            var run = new RecordingRun();

            script.OnFirstEnter(run);
            var timeline = new List<string>();
            foreach (var (at, step) in run.Scheduled.OrderBy(s => s.At))
            {
                var before = run.Calls.Count;
                step(run);
                timeline.AddRange(run.Calls.Skip(before).Select(call => $"{at.TotalSeconds} {call}"));
            }

            CollectionAssert.AreEqual(new[]
            {
                "14 message(MonstersAreApproaching)",
                "21 message(MonstersAreApproaching)",
                "24 wave(1,0)",
                "24 message(MonsterHaveAppeared)",
                "144 message(MonsterWillAppearsInSeconds,40)",
                "154 drop(1,0)",
                "169 message(MonstersAreApproaching)",
                "184 wave(1,1)",
                "204 message(BattleEndInMinutes,12)",
                "294 message(MonsterWillAppearsInSeconds,40)",
                "304 drop(1,1)",
                "319 message(MonstersAreApproaching)",
                "324 message(BattleEndInMinutes,10)",
                "334 wave(1,2)",
                "444 message(MonsterWillAppearsInSeconds,40)",
                "454 drop(1,2)",
                "469 message(MonstersAreApproaching)",
                "484 wave(1,3)",
                "594 message(MonsterWillAppearsInSeconds,40)",
                "604 drop(1,3)",
                "619 message(MonstersAreApproaching)",
                "624 message(BattleEndInMinutes,5)",
                "634 wave(1,4)",
                "684 message(BattleEndInMinutes,4)",
                "744 message(BattleEndInMinutes,3)",
                "804 message(BattleEndInMinutes,2)",
                "864 message(BattleEndInMinutes,1)",
                "894 message(InstantCombatThirtySecondsRemaining)",
                "924 fail",
            }, timeline);
        }

        [TestMethod]
        public async Task InstantCombatIsWonOnceTheLastWaveIsOutAndEveryMonsterIsDead()
        {
            var script = await LoadAsync("instant-combat");
            var run = new RecordingRun { Elapsed = TimeSpan.FromSeconds(633) };

            script.OnMonsterKilled(run, 1, 333);
            Assert.AreEqual(0, run.Calls.Count);

            run.Elapsed = TimeSpan.FromSeconds(634);
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
            var script = await LoadAsync("ice-breaker");
            var run = new RecordingRun { Players = 1 };

            script.OnFirstEnter(run);

            CollectionAssert.AreEqual(new[] { "succeed" }, run.Calls);
            Assert.AreEqual(0, run.Scheduled.Count);
        }

        [TestMethod]
        public async Task AnIcebreakerOpensPvpAfterItsWarningsAndEndsAfterTenMinutes()
        {
            var script = await LoadAsync("ice-breaker");
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
            var script = await LoadAsync("ice-breaker");
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
        public async Task ARainbowBattlePlacesItsCrystalsAndEndsAfterSevenMinutes()
        {
            var script = await LoadAsync("rainbow-battle");
            var run = new RecordingRun { Players = 10 };

            script.OnFirstEnter(run);
            Assert.AreEqual(7, run.Calls.Count(c => c.StartsWith("npc(", StringComparison.Ordinal)));
            Assert.AreEqual("npc(1,922,59,40)", run.Calls[0]);
            CollectionAssert.AreEqual(new[] { "countdown(1,420s)", "message(BattleStarted)" }, run.Calls.Skip(7).ToArray());
            CollectionAssert.AreEqual(new[] { 390d, 420 }, run.Scheduled.Select(s => s.At.TotalSeconds).OrderBy(s => s).ToArray());

            while (run.Scheduled.Count > 0)
            {
                RunNextStep(run);
            }

            CollectionAssert.AreEqual(new[] { "message(TheBattleEndsShortly,30)", "succeed" }, run.Calls.Skip(9).ToArray());
            Assert.AreEqual(2, script.Definition.Bases.Count);
            Assert.AreEqual(10, script.Definition.TeamSizeMinimum);
            Assert.AreEqual(30, script.Definition.TeamSizeMaximum);
        }

        [TestMethod]
        public async Task ALandOfDeathHuntsEveryFourMinutesFromHalfTime()
        {
            var script = await LoadAsync("land-of-death");
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
            var script = await LoadAsync("land-of-death");
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
            var script = await LoadAsync("land-of-death");
            var run = new RecordingRun { Elapsed = TimeSpan.FromMinutes(34.5) };

            script.OnFirstEnter(run);

            Assert.AreEqual("clock(25.5m)", run.Calls.Single());
            Assert.AreEqual(34d, run.Scheduled.Min(s => s.At.TotalMinutes));
        }

        [TestMethod]
        public async Task ALandOfDeathClosesWhenItsClockRunsOut()
        {
            var script = await LoadAsync("land-of-death");
            var run = new RecordingRun();

            script.OnTimeout(run);

            CollectionAssert.AreEqual(new[] { "fail" }, run.Calls);
        }

        [TestMethod]
        public async Task TheShipBringsTheCaptainAboardAndOnlyHim()
        {
            var room = (await LoadAsync("act4-ship")).Definition.Rooms.Single();

            Assert.AreEqual(148, room.VNum);
            CollectionAssert.AreEquivalent(new short[] { 540, 613 }, room.MapNpcs.ToArray());
        }

        [TestMethod]
        public async Task TheShipWarnsItsPassengersThenSails()
        {
            var script = await LoadAsync("act4-ship");
            var run = new RecordingRun();
            script.OnFirstEnter(run);

            foreach (var (_, step) in run.Scheduled.OrderBy(s => s.At))
            {
                step(run);
            }

            CollectionAssert.AreEqual(new[] { 21d, 24, 24.5, 24 + 50 / 60d, 25 },
                run.Scheduled.Select(s => s.At.TotalMinutes).OrderBy(m => m).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                "message(ShipLeaveInMinutes,4)",
                "message(ShipLeaveInOneMinute)",
                "message(ShipLeaveInThirtySeconds)",
                "message(ShipLeaveInTenSeconds)",
                "message(RaiseTheAnchor)",
                "succeed"
            }, run.Calls);
        }

        [TestMethod]
        public async Task BoardingLateOnlyKeepsTheWarningsStillAhead()
        {
            var script = await LoadAsync("act4-ship");
            var run = new RecordingRun { Elapsed = TimeSpan.FromMinutes(24.2) };

            script.OnFirstEnter(run);

            CollectionAssert.AreEqual(new[] { 24.5, 24 + 50 / 60d, 25 },
                run.Scheduled.Select(s => s.At.TotalMinutes).OrderBy(m => m).ToArray());
        }

        [TestMethod]
        public async Task TheMortazDesertShipCarriesItsSailorAboard()
        {
            var room = (await LoadAsync("act5-ship")).Definition.Rooms.Single();

            Assert.AreEqual(148, room.VNum);
            CollectionAssert.AreEquivalent(new short[] { 1350 }, room.MapNpcs.ToArray());
        }

        [TestMethod]
        public async Task TheMortazDesertShipWarnsItsPassengersThenSails()
        {
            var script = await LoadAsync("act5-ship");
            var run = new RecordingRun();
            script.OnFirstEnter(run);

            foreach (var (_, step) in run.Scheduled.OrderBy(s => s.At))
            {
                step(run);
            }

            CollectionAssert.AreEqual(new[] { 60d, 90, 110, 120 },
                run.Scheduled.Select(s => s.At.TotalSeconds).OrderBy(m => m).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                "message(ShipLeaveInOneMinute)",
                "message(ShipLeaveInThirtySeconds)",
                "message(ShipLeaveInTenSeconds)",
                "message(RaiseTheAnchor)",
                "succeed"
            }, run.Calls);
        }

        [TestMethod]
        public async Task TheRaidIsOpenedByItsSealForATeamInItsLevelRange()
        {
            var definition = (await LoadAsync("raid-mother-cuby")).Definition;

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
            var script = await LoadAsync("talent-arena");
            var run = new RecordingRun();

            script.OnFirstEnter(run);

            CollectionAssert.AreEqual(new[] { "countdown(1,15s)" }, run.Calls);
            Assert.AreEqual(TimeSpan.FromSeconds(15), run.Scheduled.Single().At);
            Assert.AreEqual(2, script.Definition.Sides.Count);
        }

        [TestMethod]
        public async Task ABoutOpensFiveSecondsAfterTheFightersAreCalled()
        {
            var script = await LoadAsync("talent-arena");
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
            var script = await LoadAsync("talent-arena");
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
            var script = await LoadAsync("talent-arena");
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
            var script = await LoadAsync("talent-arena");
            var run = new RecordingRun();
            script.OnFirstEnter(run);

            RunNextStep(run);

            CollectionAssert.AreEqual(new[] { "countdown(1,15s)", "call(1,False)", "succeed" }, run.Calls);
            Assert.AreEqual(0, run.Scheduled.Count);
        }

        [TestMethod]
        public async Task TheRaidOpensOnItsEntranceWithTheBossWaitingInTheSquare()
        {
            var script = await LoadAsync("raid-mother-cuby");
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
            var script = await LoadAsync("raid-mother-cuby");
            var run = new RecordingRun();

            script.OnMonsterKilled(run, 1, 283);
            script.OnMonsterKilled(run, 1, 282);

            CollectionAssert.AreEqual(new[] { "succeed" }, run.Calls);
        }

        [TestMethod]
        [DataRow("raid-ginseng", 1, 1128, 40, 59, 4685)]
        [DataRow("raid-glacerus", 17, 5512, 80, 99, 4772)]
        [DataRow("raid-grenigas", 15, 5922, 85, 99, 4698)]
        [DataRow("raid-zenas", 23, 5883, 85, 99, 9844)]
        public async Task EachRaidIsOpenedByItsOwnSeal(string name, int raid, int seal, int minimum, int maximum, int box)
        {
            var definition = (await LoadAsync(name)).Definition;

            Assert.AreEqual(raid, definition.Id);
            Assert.AreEqual(seal, definition.RequiredItems.Single().VNum);
            Assert.AreEqual(minimum, definition.LevelMinimum);
            Assert.AreEqual(maximum, definition.LevelMaximum);
            Assert.AreEqual(box, definition.GiftItems.Single().VNum);
        }

        [TestMethod]
        public async Task TheGinsengFarmStaysShutUntilTheLeversAndTheMonsterGoalAreDone()
        {
            var script = await LoadAsync("raid-ginseng");
            var run = new RecordingRun { LockerLeft = 3 };
            for (var lever = 1; lever <= 7; lever++)
            {
                run.ButtonsOn.Add(lever);
                script.OnButtonUsed(run, 1, lever);
            }

            run.LockerLeft = 0;
            script.OnRoomCleared(run, 1);

            CollectionAssert.AreEqual(new[] { "unlock(1->2)" }, run.Calls);
        }

        [TestMethod]
        public async Task OnlyTheFieryGiantsHoldTheGrenigasLavaFields()
        {
            var run = new RecordingRun();

            (await LoadAsync("raid-grenigas")).OnFirstEnter(run);

            CollectionAssert.IsSubsetOf(new[]
            {
                "portal(1->2,locked)", "portal(2->3,locked)", "monsterLocker(1,2,1925)", "monsterLocker(2,3,1925)"
            }, run.Calls);
        }

        [TestMethod]
        [DataRow(1, 2, "unlock(1->2)")]
        [DataRow(2, 3, "unlock(2->3)")]
        public async Task EachGrenigasFieldOpensOnceItsLeversAndGiantsAreDone(int room, int levers, string unlock)
        {
            var script = await LoadAsync("raid-grenigas");
            var run = new RecordingRun { LockerLeft = 1 };
            for (var lever = 1; lever <= levers; lever++)
            {
                run.ButtonsOn.Add(lever);
                script.OnButtonUsed(run, room, lever);
            }

            Assert.AreEqual(0, run.Calls.Count);

            run.LockerLeft = 0;
            script.OnRoomCleared(run, room);

            CollectionAssert.AreEqual(new[] { unlock }, run.Calls);
        }

        [TestMethod]
        public async Task TheGrenigasFieldStaysShutWhileALeverIsUp()
        {
            var script = await LoadAsync("raid-grenigas");
            var run = new RecordingRun();
            run.ButtonsOn.Add(1);

            script.OnRoomCleared(run, 1);

            Assert.AreEqual(0, run.Calls.Count);
        }

        [TestMethod]
        public async Task KillingGrenigasWinsTheRaid()
        {
            var run = new RecordingRun();

            (await LoadAsync("raid-grenigas")).OnMonsterKilled(run, 3, 1058);

            CollectionAssert.AreEqual(new[] { "succeed" }, run.Calls);
        }

        [TestMethod]
        public async Task TheZenasTeamIsSplitBetweenTheTwoEndsOfTheHall()
        {
            var definition = (await LoadAsync("raid-zenas")).Definition;

            CollectionAssert.AreEqual(new[] { new Spot(90, 103), new Spot(245, 102), new Spot(90, 103), new Spot(245, 102) },
                Enumerable.Range(0, 4).Select(definition.StartOf).ToList());
        }

        [TestMethod]
        public async Task TheFirstZenasTestStartsThirtySecondsInWithGuardsAtBothEnds()
        {
            var run = new RecordingRun();

            (await LoadAsync("raid-zenas")).OnFirstEnter(run);

            Assert.AreEqual("clock(60m)", run.Calls[0]);
            Assert.AreEqual(1, run.Summoned.Count(m => m.VNum == 2504));
            Assert.AreEqual(92, run.Summoned.Count(m => m.VNum is >= 2506 and <= 2509));
            CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) }, run.Scheduled.Select(s => s.At).ToList());

            run.Scheduled[0].Step(run);
            run.Scheduled[1].Step(run);

            Assert.AreEqual("message(ZenasFirstTestWillStart)", run.Calls[6]);
            Assert.AreEqual(92 + 42, run.Summoned.Count(m => m.VNum is >= 2506 and <= 2508 || m.VNum == 2509));
        }

        [TestMethod]
        public async Task TheWestGuardsFallingOpensThePortalAndCallsLuciferThirtySecondsLater()
        {
            var run = new RecordingRun();
            (await LoadAsync("raid-zenas")).OnFirstEnter(run);
            run.Scheduled[1].Step(run);
            var west = run.Summoned.Where(m => m.OnDeath != null).ToList();
            Assert.AreEqual(21, west.Count);
            Assert.IsTrue(west.All(m => m.At.X < 150));
            run.Calls.Clear();

            foreach (var guard in west.Take(20))
            {
                guard.OnDeath!(run);
            }

            Assert.AreEqual(0, run.Calls.Count);

            run.Elapsed = TimeSpan.FromSeconds(55);
            west[20].OnDeath!(run);

            CollectionAssert.AreEqual(new[] { "teamPortal(1,91,149->88,175)", "message(ZenasFirstTestPassed)" }, run.Calls);
            var lucifer = run.Scheduled.Last();
            Assert.AreEqual(TimeSpan.FromSeconds(85), lucifer.At);
            lucifer.Step(run);
            CollectionAssert.AreEqual(new[] { "message(ArchangelLuciferZenas13thWingAppeared)", "summon(1,2553)" }, run.Calls.TakeLast(2).ToList());
        }

        [TestMethod]
        public async Task LuciferFallingClearsTheGuardsAndLeadsToThePrayer()
        {
            var script = await LoadAsync("raid-zenas");
            var run = new RecordingRun { Elapsed = TimeSpan.FromSeconds(90) };

            script.OnMonsterKilled(run, 1, 2553);

            CollectionAssert.AreEqual(new[]
            {
                "dismiss(1,2506)", "dismiss(1,2507)", "dismiss(1,2508)", "dismiss(1,2509)", "throw(1,8)", "message(ZenasSecondTestPassed)"
            }, run.Calls);
            CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(95), TimeSpan.FromSeconds(120) }, run.Scheduled.Select(s => s.At).ToList());
            run.Scheduled[0].Step(run);
            Assert.AreEqual("teamPortal(1,160,247->160,217)", run.Calls.Last());
        }

        [TestMethod]
        public async Task ThePrayerLastsAMinuteUnderSevenWavesThenOpensTheWayToZenas()
        {
            var script = await LoadAsync("raid-zenas");
            var run = new RecordingRun { Elapsed = TimeSpan.FromSeconds(90) };
            script.OnMonsterKilled(run, 1, 2553);
            run.Elapsed = TimeSpan.FromSeconds(120);
            run.Scheduled[1].Step(run);
            run.Scheduled.RemoveRange(0, 2);

            CollectionAssert.IsSubsetOf(new[] { "npcs(1,2550,2551,2549)", "npcs(1,2505,2505,2505,2505,2505,2505,2505)", "roomClock(1,60s)" }, run.Calls);
            Assert.IsTrue(run.SummonedNpcs.Where(n => n.VNum is 2505 or 2549).All(n => n.IsAlly));
            Assert.IsFalse(run.SummonedNpcs.Single(n => n.VNum == 2550).IsAlly);
            CollectionAssert.AreEqual(new[] { 126, 134, 142, 150, 158, 166, 174, 180, 186 },
                run.Scheduled.Select(s => (int)s.At.TotalSeconds).ToList());

            run.Calls.Clear();
            foreach (var (_, step) in run.Scheduled)
            {
                step(run);
            }

            Assert.AreEqual(7, run.Calls.Count(c => c == "summon(1,2506,2507,2508)"));
            CollectionAssert.AreEqual(new[] { "stopRoomClock(1)", "message(ZenasAllTestPassed)", "teamPortal(1,160,203->159,170)" },
                run.Calls.TakeLast(3).ToList());
        }

        [TestMethod]
        public async Task KillingZenasThrowsTheLootAndWinsTheRaid()
        {
            var run = new RecordingRun();

            (await LoadAsync("raid-zenas")).OnMonsterKilled(run, 1, 2504);

            CollectionAssert.AreEqual(new[] { "throw(1,11)", "succeed" }, run.Calls);
        }

        [TestMethod]
        public async Task AGinsengDecoyLeverDoesNotCount()
        {
            var script = await LoadAsync("raid-ginseng");
            var run = new RecordingRun();
            for (var lever = 2; lever <= 19; lever++)
            {
                run.ButtonsOn.Add(lever);
                script.OnButtonUsed(run, 1, lever);
            }

            Assert.AreEqual(0, run.Calls.Count);
        }

        [TestMethod]
        public async Task TheGinsengRaidPlacesItsLeversAndWaitsForEightKills()
        {
            var script = await LoadAsync("raid-ginseng");
            var run = new RecordingRun();

            script.OnFirstEnter(run);

            CollectionAssert.AreEqual(new[] { "clock(120m)", "portal(1->2,locked)", "monsterLocker(1,8)" }, run.Calls.Take(3).ToList());
            Assert.AreEqual(19, run.Calls.Count(c => c.StartsWith("button(1,")));
            Assert.IsTrue(run.Calls.Contains("summon(2,284)"));
        }

        [TestMethod]
        public async Task PullingTheGlacerusLeverOpensTheDen()
        {
            var script = await LoadAsync("raid-glacerus");
            var run = new RecordingRun();

            script.OnButtonUsed(run, 1, 1);
            script.OnMonsterKilled(run, 2, 2049);

            CollectionAssert.AreEqual(new[] { "unlock(1->2)", "succeed" }, run.Calls);
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
            var first = await loader.LoadAsync("timespace-sample");
            var second = await loader.LoadAsync("timespace-sample");

            Assert.AreSame(first, second);
        }

        [TestMethod]
        [DataRow("act4-morcos", (short)563, (short)882)]
        [DataRow("act4-hatus", (short)577, (short)185)]
        [DataRow("act4-calvina", (short)629, (short)942)]
        [DataRow("act4-berios", (short)624, (short)999)]
        public async Task AGlacernonRaidOpensItsBossDoorAtHalfTime(string name, short boss, short gift)
        {
            var script = await LoadAsync(name);
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
            var script = await LoadAsync("act4-hatus");
            var run = new RecordingRun { Elapsed = TimeSpan.FromMinutes(45) };

            script.OnFirstEnter(run);

            Assert.AreEqual("clock(15m)", run.Calls.First());
            Assert.IsTrue(run.Scheduled.All(s => s.At >= TimeSpan.FromMinutes(45) || s.At == TimeSpan.FromMinutes(30)));
        }

        [TestMethod]
        public async Task TheGlacernonRaidBossThrowsItsLootAndEndsTheRaid()
        {
            var script = await LoadAsync("act4-morcos");
            var run = new RecordingRun();

            script.OnMonsterKilled(run, 2, 562);
            Assert.AreEqual(0, run.Calls.Count);
            script.OnMonsterKilled(run, 2, 563);

            CollectionAssert.AreEqual(new[] { "message(CongratulationsKilledTheBoss)", "throw(2,5)", "succeed" }, run.Calls);
        }
    }
}
