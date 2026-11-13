//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class ScriptedInstanceRunTests
    {
        private TestWorld World;
        private const short Grunt = 58;

        private sealed class Script : InstanceScript
        {
            public Action<IInstanceRun>? Enter { get; init; }
            public Action<IInstanceRun, int>? Cleared { get; init; }
            public Action<IInstanceRun>? Timeout { get; init; }
            public List<string> Calls { get; } = [];

            public Script() => Definition = new ScriptedInstanceDefinition();

            public override void OnFirstEnter(IInstanceRun run)
            {
                Calls.Add("enter");
                Enter?.Invoke(run);
            }

            public override void OnMonsterKilled(IInstanceRun run, int room, short vNum) =>
                Calls.Add($"killed({room},{vNum})");

            public override void OnRoomCleared(IInstanceRun run, int room)
            {
                Calls.Add($"cleared({room})");
                Cleared?.Invoke(run, room);
            }

            public override void OnTimeout(IInstanceRun run)
            {
                Calls.Add("timeout");
                Timeout?.Invoke(run);
            }
        }

        private MapInstance _first;
        private MapInstance _second;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _first = Room();
            _second = Room();
        }

        private MapInstance Room() =>
            World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);

        private ScriptedInstanceRun Run(Script script, TimeSpan? timeout = null) =>
            new(new ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, PositionX = 5, PositionY = 6, Logic = script },
                script,
                new Dictionary<int, MapInstance> { [1] = _first, [2] = _second },
                new Dictionary<short, NpcMonsterDto> { [Grunt] = new() { NpcMonsterVNum = Grunt, MaxHp = 10 } },
                World.Clock, World.SessionRegistry,
                new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), NullLogger.Instance, timeout ?? TimeSpan.FromSeconds(30));

        [TestMethod]
        public async Task TheFirstEntryRunsTheScriptOnlyOnceAsync()
        {
            var script = new Script();
            var run = Run(script);

            await run.StartAsync();
            await run.StartAsync();

            CollectionAssert.AreEqual(new[] { "enter" }, script.Calls);
        }

        [TestMethod]
        public async Task SummonedMonstersAppearInTheirRoomAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.Summon(2, new MonsterSpawn(Grunt, new Spot(1, 1)), new MonsterSpawn(Grunt, new Spot(2, 1)))
            });

            await run.StartAsync();

            Assert.AreEqual(0, _first.EnumerateMonsters().Count());
            Assert.AreEqual(2, _second.EnumerateMonsters().Count());
            Assert.AreEqual(2, _second.EnumerateMonsters().Select(s => s.VisualId).Distinct().Count());
            Assert.IsTrue(_second.EnumerateMonsters().All(s => s.IsHostile));
        }

        [TestMethod]
        public async Task ASummonWaitsForWorkAlreadyOnTheRoomAsync()
        {
            var run = Run(new Script { Enter = r => r.Summon(2, new MonsterSpawn(Grunt, new Spot(1, 1))) });
            var release = new TaskCompletionSource();
            var entered = new TaskCompletionSource();
            var holder = _second.Executor.RunAsync(async () =>
            {
                entered.SetResult();
                await release.Task;
            });
            await entered.Task;

            var start = run.StartAsync();
            await Task.Delay(50);
            var countWhileHeld = _second.EnumerateMonsters().Count();
            release.SetResult();
            await Task.WhenAll(holder, start);

            Assert.AreEqual(0, countWhileHeld);
            Assert.AreEqual(1, _second.EnumerateMonsters().Count());
        }

        [TestMethod]
        public async Task AFailingStepDoesNotCostTheOthersAsync()
        {
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)));
                    r.Summon(2, new MonsterSpawn(Grunt, new Spot(1, 1)));
                }
            });
            await _first.Executor.CloseAsync();

            await run.StartAsync();

            Assert.AreEqual(0, _first.EnumerateMonsters().Count());
            Assert.AreEqual(1, _second.EnumerateMonsters().Count());
        }

        [TestMethod]
        public async Task TheRoomClearsWhenItsLockerRunsOutAsync()
        {
            var script = new Script { Enter = r => r.SetMonsterLocker(1, 2) };
            var run = Run(script);
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            CollectionAssert.AreEqual(new[]
            {
                "enter", "killed(1,58)", "killed(1,58)", "cleared(1)", "killed(1,58)"
            }, script.Calls);
        }

        [TestMethod]
        public async Task AKillInAnotherRoomDoesNotOpenThisOneAsync()
        {
            var script = new Script { Enter = r => r.SetMonsterLocker(1, 1) };
            var run = Run(script);
            await run.StartAsync();

            await run.MonsterKilledAsync(_second.MapInstanceId, Grunt);

            CollectionAssert.DoesNotContain(script.Calls, "cleared(1)");
        }

        [TestMethod]
        public async Task ALockedPortalOpensWhenTheScriptSaysSoAsync()
        {
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.SetMonsterLocker(1, 1);
                    r.SpawnPortal(1, new Spot(3, 3), 2, new Spot(1, 1));
                },
                Cleared = (r, room) => r.UnlockPortal(room, 2)
            });

            await run.StartAsync();
            var portal = _first.Portals.Single();
            Assert.AreEqual(PortalType.Closed, portal.Type);
            Assert.AreEqual(_second.MapInstanceId, portal.DestinationMapInstanceId);

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            Assert.AreEqual(PortalType.Open, portal.Type);
        }

        [TestMethod]
        public async Task TheClockRunningOutAsksTheScriptAsync()
        {
            var script = new Script
            {
                Enter = r => r.StartClock(TimeSpan.FromMinutes(1)),
                Timeout = r => r.Fail()
            };
            var run = Run(script);
            await run.StartAsync();

            World.Clock.AdvanceSeconds(59);
            await run.TickAsync();
            Assert.IsNull(run.EndState);

            World.Clock.AdvanceSeconds(1);
            await run.TickAsync();
            Assert.AreEqual(EndStateType.Fail, run.EndState);
        }

        [TestMethod]
        public async Task AScriptSilentOnTimeoutStillEndsTheRunAsync()
        {
            var run = Run(new Script { Enter = r => r.StartClock(TimeSpan.FromSeconds(10)) });
            await run.StartAsync();

            World.Clock.Advance(Duration.FromSeconds(10));
            await run.TickAsync();

            Assert.AreEqual(EndStateType.Timeout, run.EndState);
        }

        [TestMethod]
        public async Task AScriptThatNeverReturnsFailsTheRunAndIsShutOutAsync()
        {
            using var release = new System.Threading.ManualResetEventSlim();
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)));
                    release.Wait();
                    r.Summon(1, new MonsterSpawn(Grunt, new Spot(2, 1)));
                    r.Succeed();
                }
            }, TimeSpan.FromMilliseconds(200));

            await run.StartAsync();
            release.Set();
            await Task.Delay(100);

            Assert.AreEqual(EndStateType.Fail, run.EndState);
            Assert.AreEqual(0, _first.EnumerateMonsters().Count());
        }

        [TestMethod]
        public async Task AScriptThatThrowsFailsTheRunAsync()
        {
            var run = Run(new Script { Enter = _ => throw new InvalidOperationException() });

            await run.StartAsync();

            Assert.AreEqual(EndStateType.Fail, run.EndState);
        }

        [TestMethod]
        public async Task AnEffectThatNeverFinishesFailsTheRunAsync()
        {
            var run = Run(new Script { Enter = r => r.Summon(2, new MonsterSpawn(Grunt, new Spot(1, 1))) },
                TimeSpan.FromMilliseconds(200));
            var release = new TaskCompletionSource();
            var holder = _second.Executor.RunAsync(() => release.Task);

            await run.StartAsync();
            release.SetResult();
            await holder;

            Assert.AreEqual(EndStateType.Fail, run.EndState);
        }

        [TestMethod]
        public async Task AnAbandonedRunShowsAFailedScoreAsync()
        {
            var run = Run(new Script { Enter = _ => throw new InvalidOperationException() });

            await run.StartAsync();

            Assert.IsTrue(_first.LastPackets.OfType<NosCore.Packets.ClientPackets.Instance.ScorePacket>()
                .Any(s => s.EndState == EndStateType.Fail));
        }

        [TestMethod]
        public async Task TheFirstEndingStandsAsync()
        {
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.Succeed();
                    r.Fail();
                }
            });

            await run.StartAsync();

            Assert.AreEqual(EndStateType.Success, run.EndState);
        }
    }
}
