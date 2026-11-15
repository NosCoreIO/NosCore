//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
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

            public Script(byte id = 0) => Definition = new ScriptedInstanceDefinition { Id = id };

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
        private IInstanceRunHost? _host;
        private readonly Mock<Wolverine.IMessageBus> _bus = new();

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
            new(_host ?? new TimeSpaceRunHost(
                    new ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, PositionX = 5, PositionY = 6, Logic = script },
                    new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), _bus.Object, World.Clock),
                script,
                new Dictionary<int, MapInstance> { [1] = _first, [2] = _second },
                new Dictionary<short, NpcMonsterDto> { [Grunt] = new() { NpcMonsterVNum = Grunt, MaxHp = 10 } },
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(), World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(),
                NullLogger.Instance, World.LogLanguageLocalizer,
                timeout ?? TimeSpan.FromSeconds(30));

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
        public async Task TheFirstPlayerToFinishEnteringStartsTheScriptAsync()
        {
            var script = new Script();
            var run = Run(script);
            var session = await World.CreateSessionAsync();

            await run.EnteredAsync(session, _first.MapInstanceId);
            await run.EnteredAsync(session, _second.MapInstanceId);

            CollectionAssert.AreEqual(new[] { "enter" }, script.Calls);
        }

        [TestMethod]
        public async Task AMessageWithANumberCarriesItAsItsArgumentAsync()
        {
            var run = Run(new Script { Enter = r => r.Message(Game18NConstString.TimeSpaceCompleted, 30) });

            await run.StartAsync();

            var told = _first.LastPackets.OfType<MsgiPacket>().Single(packet => packet.Message == Game18NConstString.TimeSpaceCompleted);
            Assert.AreEqual(4, told.ArgumentType);
            Assert.AreEqual(30L, Convert.ToInt64(told.Game18NArguments.First()));
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
            await _second.Executor.RunAsync(() => Task.CompletedTask);

            Assert.AreEqual(EndStateType.Fail, run.EndState);
            Assert.AreEqual(0, _second.EnumerateMonsters().Count());
        }

        [TestMethod]
        public async Task AnEffectThatNeverFinishesBeforeASuccessFailsTheRunAsync()
        {
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.Summon(2, new MonsterSpawn(Grunt, new Spot(1, 1)));
                    r.Succeed();
                }
            }, TimeSpan.FromMilliseconds(200));
            var release = new TaskCompletionSource();
            var holder = _second.Executor.RunAsync(() => release.Task);

            await run.StartAsync();
            release.SetResult();
            await holder;

            Assert.AreEqual(EndStateType.Fail, run.EndState);
            var scores = _first.LastPackets.OfType<NosCore.Packets.ClientPackets.Instance.ScorePacket>().ToList();
            Assert.AreEqual(1, scores.Count);
            Assert.AreEqual(EndStateType.Fail, scores[0].EndState);
        }

        [TestMethod]
        public async Task AScriptThatSucceedsAndThenThrowsFailsTheRunAsync()
        {
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.Succeed();
                    throw new InvalidOperationException();
                }
            });

            await run.StartAsync();

            Assert.AreEqual(EndStateType.Fail, run.EndState);
            Assert.IsTrue(_first.LastPackets.OfType<NosCore.Packets.ClientPackets.Instance.ScorePacket>()
                .All(s => s.EndState == EndStateType.Fail));
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
    
        private sealed class Host : IInstanceRunHost
        {
            public List<EndStateType> Endings { get; } = [];

            public string Name => "test";

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) =>
                wave == 1 ? [new WaveMonster(Grunt, 3, false)] : [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public Task EndedAsync(ScriptedInstanceRun run, EndStateType state)
            {
                Endings.Add(state);
                return Task.CompletedTask;
            }

            public Task ReturnAsync(NosCore.GameObject.Networking.ClientSession.ClientSession session) =>
                Task.CompletedTask;
        }

        private async Task<NosCore.GameObject.Networking.ClientSession.ClientSession> PlayerInFirstRoomAsync()
        {
            var session = await World.CreateSessionAsync();
            session.Character.MapInstance = _first;
            World.SessionRegistry.UpdateCharacter(session.Channel?.Id ?? string.Empty, session.Character.CharacterId, _first.MapInstanceId);
            return session;
        }

        [TestMethod]
        public async Task ASucceededTimeSpaceTellsEachPlayerItWasWonAsync()
        {
            var run = Run(new Script(42) { Enter = r => r.Succeed() });
            var session = await PlayerInFirstRoomAsync();

            await run.StartAsync();

            var characterId = session.Character.CharacterId;
            var published = _bus.Invocations.Select(i => i.Arguments[0]).OfType<ScriptedInstanceSucceededEvent>().Single();
            Assert.AreEqual(characterId, published.Character.CharacterId);
            Assert.AreEqual(ScriptedInstanceType.TimeSpace, published.Type);
            Assert.AreEqual(42, published.InstanceId);
        }

        [TestMethod]
        public async Task AFailedTimeSpaceIsNotWonAsync()
        {
            var run = Run(new Script(42) { Enter = r => r.Fail() });
            await PlayerInFirstRoomAsync();

            await run.StartAsync();

            Assert.IsFalse(_bus.Invocations.Any(i => i.Arguments[0] is ScriptedInstanceSucceededEvent));
        }

        [TestMethod]
        public async Task TwoPlayersApartAreTwoSidesAsync()
        {
            var run = Run(new Script());
            await PlayerInFirstRoomAsync();
            await PlayerInFirstRoomAsync();

            Assert.AreEqual(2, run.PlayersIn(1));
            Assert.IsFalse(run.OneSideStanding(1));
        }

        [TestMethod]
        public async Task AFallenPlayerIsOutOfTheFightAsync()
        {
            var run = Run(new Script());
            await PlayerInFirstRoomAsync();
            var fallen = await PlayerInFirstRoomAsync();

            await run.PlayerDownAsync(fallen.Character);

            Assert.IsTrue(run.IsDown(fallen.Character.CharacterId));
            Assert.IsTrue(run.OneSideStanding(1));
        }

        [TestMethod]
        public async Task AGroupLeftStandingIsOneSideAsync()
        {
            var run = Run(new Script());
            var first = await PlayerInFirstRoomAsync();
            var second = await PlayerInFirstRoomAsync();
            var group = new NosCore.GameObject.Services.GroupService.Group(
                NosCore.Data.Enumerations.Group.GroupType.Group, World.SessionGroupFactory);
            first.Character.JoinGroup(group);
            second.Character.JoinGroup(group);
            Assert.IsTrue(run.OneSideStanding(1));

            await PlayerInFirstRoomAsync();
            Assert.IsFalse(run.OneSideStanding(1));
        }

        [TestMethod]
        public async Task AnEndingClosesPvpAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.SetPvp(1, true)
            });
            await run.StartAsync();
            Assert.IsTrue(_first.IsPvp);

            await run.StartAsync();
            run.Fail();
            await run.TickAsync();

            Assert.IsFalse(_first.IsPvp);
        }

        [TestMethod]
        public async Task AHostThatStartsTheScriptItselfIsNotStartedByEntryAsync()
        {
            var script = new Script();
            _host = new SelfStartingHost();
            var run = Run(script);
            var session = await World.CreateSessionAsync();

            await run.EnteredAsync(session, _first.MapInstanceId);
            Assert.AreEqual(0, script.Calls.Count);

            await run.StartAsync();
            CollectionAssert.AreEqual(new[] { "enter" }, script.Calls);
        }

        private sealed class SelfStartingHost : IInstanceRunHost
        {
            public string Name => "test";

            public bool StartsOnFirstEntry => false;

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public Task EndedAsync(ScriptedInstanceRun run, EndStateType state) => Task.CompletedTask;

            public Task ReturnAsync(NosCore.GameObject.Networking.ClientSession.ClientSession session) =>
                Task.CompletedTask;
        }

        [TestMethod]
        public async Task AHunterIsSetOnTheLastPlayerToArriveAsync()
        {
            var run = Run(new Script());
            var first = await PlayerInFirstRoomAsync();
            var last = await PlayerInFirstRoomAsync();
            last.Character.PositionX = 3;
            last.Character.PositionY = 2;
            await run.EnteredAsync(first, _first.MapInstanceId);
            await run.EnteredAsync(last, _first.MapInstanceId);

            run.Hunt(1, Grunt);
            await run.TickAsync();

            var hunter = _first.EnumerateMonsters().Single();
            Assert.AreEqual((short)3, hunter.PositionX);
            Assert.AreEqual((short)2, hunter.PositionY);
            Assert.AreEqual(last.Character.VisualId, World.Create<AggroService>().Current(hunter).TargetVisualId);
        }

        [TestMethod]
        public async Task ADismissedMonsterLeavesTheRoomAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)))
            });
            await run.StartAsync();

            run.Dismiss(1, Grunt);
            await run.TickAsync();

            Assert.IsFalse(_first.EnumerateMonsters().Single().IsAlive);
        }

        [TestMethod]
        public async Task RatesApplyToTheirRoomOnlyAsync()
        {
            var run = Run(new Script { Enter = r => r.SetRates(1, 3, 2) });

            await run.StartAsync();

            Assert.AreEqual(3, _first.XpRate);
            Assert.AreEqual(2, _first.DropRate);
            Assert.AreEqual(1, _second.XpRate);
        }

        [TestMethod]
        public async Task ARunJoiningAnEventLateStartsPartWayThroughItsTimelineAsync()
        {
            _host = new LateHost();
            var script = new Script { Enter = r => r.At(TimeSpan.FromMinutes(3), s => s.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)))) };
            var run = Run(script);

            await run.StartAsync();
            Assert.AreEqual(TimeSpan.FromMinutes(5), run.Elapsed);

            await run.TickAsync();
            await run.TickAsync();
            Assert.AreEqual(1, _first.EnumerateMonsters().Count());
        }

        private sealed class LateHost : IInstanceRunHost
        {
            public string Name => "test";

            public TimeSpan AlreadyElapsed => TimeSpan.FromMinutes(5);

            public IEnumerable<WaveMonster> WaveMonsters(byte wave) => [];

            public Task DropWaveAsync(MapInstance room, byte wave) => Task.CompletedTask;

            public Task EndedAsync(ScriptedInstanceRun run, EndStateType state) => Task.CompletedTask;

            public Task ReturnAsync(NosCore.GameObject.Networking.ClientSession.ClientSession session) =>
                Task.CompletedTask;
        }

        [TestMethod]
        public async Task AScheduledStepRunsOnceItsTimeHasComeAsync()
        {
            var script = new Script { Enter = r => r.At(TimeSpan.FromSeconds(5), s => s.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)))) };
            var run = Run(script);
            await run.StartAsync();

            World.Clock.AdvanceSeconds(4);
            await run.TickAsync();
            Assert.AreEqual(0, _first.EnumerateMonsters().Count());

            World.Clock.AdvanceSeconds(1);
            await run.TickAsync();
            await run.TickAsync();
            Assert.AreEqual(1, _first.EnumerateMonsters().Count());
        }

        [TestMethod]
        public async Task AWaveBringsInTheHostsMonstersAndCountsThemAsync()
        {
            _host = new Host();
            var run = Run(new Script { Enter = r => r.SummonWave(2, 1) });
            await run.StartAsync();

            Assert.AreEqual(3, _second.EnumerateMonsters().Count());
            Assert.AreEqual(3, run.MonstersAlive(2));

            await run.MonsterKilledAsync(_second.MapInstanceId, Grunt);

            Assert.AreEqual(2, run.MonstersAlive(2));
            Assert.AreEqual(0, run.MonstersAlive(1));
        }

        [TestMethod]
        public async Task TheEndingGoesToTheHostAndCancelsWhatIsStillScheduledAsync()
        {
            var host = new Host();
            _host = host;
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.At(TimeSpan.FromSeconds(5), s => s.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1))));
                    r.Succeed();
                }
            });
            await run.StartAsync();

            World.Clock.AdvanceSeconds(5);
            await run.TickAsync();

            CollectionAssert.AreEqual(new[] { EndStateType.Success }, host.Endings);
            Assert.AreEqual(0, _first.EnumerateMonsters().Count());
        }
    }
}
