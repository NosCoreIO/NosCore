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

        private MapInstance _first = null!;
        private MapInstance _second = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _first = Room();
            _second = Room();
        }

        private static MapInstance Room() =>
            TestHelpers.Instance.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);

        private ScriptedInstanceRun Run(Script script) =>
            new(new ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, PositionX = 5, PositionY = 6, Logic = script },
                script,
                new Dictionary<int, MapInstance> { [1] = _first, [2] = _second },
                new Dictionary<short, NpcMonsterDto> { [Grunt] = new() { NpcMonsterVNum = Grunt, MaxHp = 10 } },
                TestHelpers.Instance.Clock, TestHelpers.Instance.SessionRegistry,
                new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), NullLogger.Instance);

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

            Assert.AreEqual(0, _first.Monsters.Count);
            Assert.AreEqual(2, _second.Monsters.Count);
            Assert.AreEqual(2, _second.Monsters.Select(s => s.VisualId).Distinct().Count());
            Assert.IsTrue(_second.Monsters.All(s => s.IsHostile));
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

            TestHelpers.Instance.Clock.AdvanceSeconds(59);
            await run.TickAsync();
            Assert.IsNull(run.EndState);

            TestHelpers.Instance.Clock.AdvanceSeconds(1);
            await run.TickAsync();
            Assert.AreEqual(EndStateType.Fail, run.EndState);
        }

        [TestMethod]
        public async Task AScriptSilentOnTimeoutStillEndsTheRunAsync()
        {
            var run = Run(new Script { Enter = r => r.StartClock(TimeSpan.FromSeconds(10)) });
            await run.StartAsync();

            TestHelpers.Instance.Clock.Advance(Duration.FromSeconds(10));
            await run.TickAsync();

            Assert.AreEqual(EndStateType.Timeout, run.EndState);
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
