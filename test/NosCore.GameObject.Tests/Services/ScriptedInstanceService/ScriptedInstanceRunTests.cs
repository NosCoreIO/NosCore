//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Shared.Enumerations;
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
        private const short Boss = 59;

        private sealed class Script : InstanceScript
        {
            public Action<IInstanceRun>? Enter { get; init; }
            public Action<IInstanceRun, int>? Cleared { get; init; }
            public Action<IInstanceRun>? Timeout { get; init; }
            public Action<IInstanceRun, int>? Exit { get; init; }
            public List<string> Calls { get; } = [];

            public Script(byte id = 0, params InstanceObjective[] objectives) =>
                Definition = new ScriptedInstanceDefinition { Id = id, Objectives = objectives };

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

            public override void OnRoomEntered(IInstanceRun run, int room) => Calls.Add($"entered({room})");

            public override void OnExitReached(IInstanceRun run, int room)
            {
                Calls.Add($"exit({room})");
                Exit?.Invoke(run, room);
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
        private readonly Mock<IGameEventDispatcher> _gameEvents = new();

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
                    new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), _gameEvents.Object, World.Clock),
                script,
                new Dictionary<int, MapInstance> { [1] = _first, [2] = _second },
                new Dictionary<short, NpcMonsterDto> { [Grunt] = new() { NpcMonsterVNum = Grunt, MaxHp = 10 } },
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(), World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(),
                NullLogger.Instance, World.LogLanguageLocalizer, World.GameLanguageLocalizer,
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

            Assert.AreEqual(1, script.Calls.Count(s => s == "enter"));
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
        public async Task AServerTextMessageReachesEachPlayerInTheirLanguageAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            session.Account.Language = RegionType.FR;
            Mock.Get(World.GameLanguageLocalizer)
                .Setup(x => x[LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, RegionType.FR, It.Is<object[]>(a => a.Length == 1 && (int)a[0] == 2)])
                .Returns(new LocalizedString("TIMESPACE_STRIKE_CENTRE_FLOOR", "Centre de grève, étage 2"));
            var run = Run(new Script { Enter = r => r.Message(LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, 2) });

            await run.StartAsync();

#pragma warning disable CS0618
            PacketAssert.AreEqual(new MsgPacket { Type = MessageType.Default, Message = "Centre de grève, étage 2" },
                session.LastPackets.OfType<MsgPacket>().Single());
#pragma warning restore CS0618
        }

        [TestMethod]
        public async Task ARoomObjectiveShowsInThePanelOfThePlayersInTheRoomAsync()
        {
            var inside = await PlayerInFirstRoomAsync();
            var elsewhere = await PlayerInRoomAsync(_second);
            ObjectiveText("Élimine tous les ennemis !");
            var run = Run(new Script { Enter = r => r.Objective(1, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES) });

            await run.StartAsync();

            PacketAssert.AreEqual(new SinfoPacket { Message = "Élimine tous les ennemis !" },
                inside.LastPackets.OfType<SinfoPacket>().Single());
            Assert.IsFalse(elsewhere.LastPackets.OfType<SinfoPacket>().Any());
        }

        [TestMethod]
        public async Task ClearingTheRoomEmptiesTheObjectivePanelAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            ObjectiveText("Élimine tous les ennemis !");
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.SetMonsterLocker(1, 1);
                    r.Objective(1, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
                }
            });
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            PacketAssert.AreEqual(new SinfoPacket { Message = string.Empty },
                session.LastPackets.OfType<SinfoPacket>().Last());
        }

        [TestMethod]
        public async Task TheObjectiveFollowsThePlayerBetweenRoomsAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            ObjectiveText("Élimine tous les ennemis !");
            var run = Run(new Script { Enter = r => r.Objective(1, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES) });
            await run.EnteredAsync(session, _first.MapInstanceId);

            Move(session, _second);
            await run.EnteredAsync(session, _second.MapInstanceId);
            PacketAssert.AreEqual(new SinfoPacket { Message = string.Empty },
                session.LastPackets.OfType<SinfoPacket>().Last());

            Move(session, _first);
            await run.EnteredAsync(session, _first.MapInstanceId);
            PacketAssert.AreEqual(new SinfoPacket { Message = "Élimine tous les ennemis !" },
                session.LastPackets.OfType<SinfoPacket>().Last());
            Assert.AreEqual(3, session.LastPackets.OfType<SinfoPacket>().Count());
        }

        private void ObjectiveText(string text) =>
            Mock.Get(World.GameLanguageLocalizer)
                .Setup(x => x[LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES, It.IsAny<RegionType>()])
                .Returns(new LocalizedString("TIMESPACE_GOAL_KILL_ALL_ENEMIES", text));

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
        public async Task AFailingStepIsCountedAsAScriptedInstanceFailureAsync()
        {
            using var failures = new FailureCounter("scripted_instance");
            var run = Run(new Script { Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1))) });
            await _first.Executor.CloseAsync();

            await run.StartAsync();

            Assert.IsGreaterThanOrEqualTo(1, failures.Count);
        }

        [TestMethod]
        public async Task AScriptThatThrowsIsCountedAsAScriptFailureAsync()
        {
            using var failures = new FailureCounter("script");
            var run = Run(new Script { Enter = _ => throw new InvalidOperationException() });

            await run.StartAsync();

            Assert.IsGreaterThanOrEqualTo(1, failures.Count);
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
        public async Task ALockerForOneMonsterIgnoresTheOthersAsync()
        {
            var script = new Script { Enter = r => r.SetMonsterLocker(1, 1, Boss) };
            var run = Run(script);
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
            Assert.AreEqual(1, run.LockerLeft(1));

            await run.MonsterKilledAsync(_first.MapInstanceId, Boss);

            CollectionAssert.Contains(script.Calls, "cleared(1)");
            Assert.AreEqual(0, run.LockerLeft(1));
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
        public async Task APortalReadsOpenOnceItsUnlockHasRunAsync()
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
            Assert.IsFalse(run.IsPortalOpen(1, 2));

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            Assert.IsTrue(run.IsPortalOpen(1, 2));
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

        private Task<NosCore.GameObject.Networking.ClientSession.ClientSession> PlayerInFirstRoomAsync() =>
            PlayerInRoomAsync(_first);

        private async Task<NosCore.GameObject.Networking.ClientSession.ClientSession> PlayerInRoomAsync(MapInstance room)
        {
            var session = await World.CreateSessionAsync();
            Move(session, room);
            return session;
        }

        private void Move(NosCore.GameObject.Networking.ClientSession.ClientSession session, MapInstance room)
        {
            session.Character.MapInstance = room;
            World.SessionRegistry.UpdateCharacter(session.Channel?.Id ?? string.Empty, session.Character.CharacterId, room.MapInstanceId);
        }

        [TestMethod]
        public async Task ASucceededTimeSpaceTellsEachPlayerItWasWonAsync()
        {
            var run = Run(new Script(42) { Enter = r => r.Succeed() });
            var session = await PlayerInFirstRoomAsync();

            await run.StartAsync();

            var characterId = session.Character.CharacterId;
            var published = _gameEvents.Invocations.Select(i => i.Arguments[0]).OfType<ScriptedInstanceSucceededEvent>().Single();
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

            Assert.IsFalse(_gameEvents.Invocations.Any(i => i.Arguments[0] is ScriptedInstanceSucceededEvent));
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
        public async Task AnEscortJoinsThePlayerWhoComesNearAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.SummonNpcs(1, new NpcSpawn(Grunt, new Spot(2, 2), IsEscort: true))
            });
            var player = await PlayerInFirstRoomAsync();
            player.Character.PositionX = 1;
            player.Character.PositionY = 1;
            await run.EnteredAsync(player, _first.MapInstanceId);

            await run.TickAsync();

            var escort = _first.EnumerateNpcs().Single();
            Assert.AreEqual(player.Character.CharacterId, escort.LeaderId);
            var joined = player.LastPackets.OfType<CtlPacket>().Single();
            Assert.AreEqual(escort.VisualId, joined.PetId);
            Assert.AreEqual(player.Character.CharacterId, escort.GenerateIn().InNonPlayerSubPacket?.Owner);
        }

        [TestMethod]
        public async Task AnOrdinaryNpcNeverJoinsAPlayerAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.SummonNpcs(1, new NpcSpawn(Grunt, new Spot(2, 2)))
            });
            var player = await PlayerInFirstRoomAsync();
            player.Character.PositionX = 1;
            player.Character.PositionY = 1;
            await run.EnteredAsync(player, _first.MapInstanceId);

            await run.TickAsync();

            Assert.AreEqual(0, _first.EnumerateNpcs().Single().LeaderId);
            Assert.IsFalse(player.LastPackets.OfType<CtlPacket>().Any());
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

        [TestMethod]
        public async Task BonusKillsInARowScoreMoreThanOthersAsync()
        {
            var run = Run(new Script());
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
            Assert.AreEqual(4, run.Points);
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt, isBonus: true);
            Assert.AreEqual(15, run.Points);
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt, isBonus: true);
            Assert.AreEqual(33, run.Points);
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
            Assert.AreEqual(37, run.Points);
        }

        [TestMethod]
        public async Task ABonusMonsterFlashesAndRemembersItIsABonusAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1), IsBonus: true), new MonsterSpawn(Grunt, new Spot(2, 1)))
            });

            await run.StartAsync();

            var monsters = _first.EnumerateMonsters().OrderBy(s => s.PositionX).ToList();
            Assert.IsTrue(monsters[0].IsBonus);
            Assert.AreEqual(826, monsters[0].Effect);
            Assert.IsFalse(monsters[1].IsBonus);
            Assert.AreEqual(0, monsters[1].Effect);
        }

        [TestMethod]
        public async Task AMonstersOwnDeathStepRunsWhenItFallsAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1),
                    OnDeath: d => d.Summon(1, new MonsterSpawn(Grunt, new Spot(3, 1)))))
            });
            await run.StartAsync();
            var first = _first.EnumerateMonsters().Single();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt, first.IsBonus, first.DeathStep);

            Assert.IsTrue(_first.EnumerateMonsters().Any(s => s.PositionX == 3));
            Assert.AreEqual(1, run.MonstersAlive(1));
        }

        [TestMethod]
        public async Task ARoomWithoutALockerClearsWithItsLastMonsterAsync()
        {
            var script = new Script
            {
                Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)), new MonsterSpawn(Grunt, new Spot(2, 1)))
            };
            var run = Run(script);
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
            CollectionAssert.DoesNotContain(script.Calls, "cleared(1)");
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            Assert.AreEqual(1, script.Calls.Count(s => s == "cleared(1)"));
        }

        [TestMethod]
        public async Task AReinforcementKeepsTheRoomFromClearingAsync()
        {
            var script = new Script
            {
                Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)))
            };
            var run = Run(script);
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt,
                deathStep: d => d.Summon(1, new MonsterSpawn(Grunt, new Spot(2, 1))));

            CollectionAssert.DoesNotContain(script.Calls, "cleared(1)");
        }

        [TestMethod]
        public async Task EachRoomIsEnteredOnceAsync()
        {
            var script = new Script();
            var run = Run(script);
            var session = await World.CreateSessionAsync();

            await run.EnteredAsync(session, _first.MapInstanceId);
            await run.EnteredAsync(session, _second.MapInstanceId);
            await run.EnteredAsync(session, _first.MapInstanceId);

            CollectionAssert.AreEqual(new[] { "enter", "entered(1)", "entered(2)" }, script.Calls);
        }

        [TestMethod]
        public async Task TheExitOpensAndAsksTheScriptWhenReachedAsync()
        {
            var script = new Script
            {
                Enter = r => r.SpawnExit(1, new Spot(3, 3)),
                Cleared = (r, room) => r.UnlockExit(room),
                Exit = (r, _) => r.Succeed()
            };
            var run = Run(script);
            await run.StartAsync();
            var exit = _first.Portals.Single();
            Assert.AreEqual(PortalType.TsEndClosed, exit.Type);

            run.SetMonsterLocker(1, 1);
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
            Assert.AreEqual(PortalType.TsEnd, exit.Type);

            await run.ExitReachedAsync(_first.MapInstanceId);
            Assert.AreEqual(EndStateType.Success, run.EndState);
        }

        [TestMethod]
        public async Task TheExitHoldsThePartyUntilTheMonsterToKillFallsAsync()
        {
            var run = Run(new Script(0, new InstanceObjective(ObjectiveKind.KillMonster, Boss, 1))
            {
                Enter = r => r.SpawnExit(1, new Spot(3, 3), locked: false),
                Exit = (r, _) => r.Succeed()
            });
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
            await run.ExitReachedAsync(_first.MapInstanceId);
            Assert.IsNull(run.EndState);
            Assert.IsTrue(_first.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == Game18NConstString.HaveIncompleteMission));

            await run.MonsterKilledAsync(_first.MapInstanceId, Boss);
            await run.ExitReachedAsync(_first.MapInstanceId);
            Assert.AreEqual(EndStateType.Success, run.EndState);
        }

        [TestMethod]
        public async Task TheExitHoldsThePartyUntilEnoughObjectsAreUsedAsync()
        {
            var run = Run(new Script(0, new InstanceObjective(ObjectiveKind.UseObject, 1055, 2))
            {
                Enter = r =>
                {
                    r.SpawnExit(1, new Spot(3, 3), locked: false);
                    r.PlaceButton(1, 1, new Spot(2, 2), 1055, 1056, kind: ButtonKind.Sculpture);
                    r.PlaceButton(2, 1, new Spot(2, 2), 1055, 1056, kind: ButtonKind.Sculpture);
                },
                Exit = (r, _) => r.Succeed()
            });
            await run.StartAsync();

            await run.UseButtonAsync(_first.MapInstanceId, 1, 1);
            await run.ExitReachedAsync(_first.MapInstanceId);
            Assert.IsNull(run.EndState);

            await run.UseButtonAsync(_second.MapInstanceId, 1, 1);
            await run.ExitReachedAsync(_first.MapInstanceId);
            Assert.AreEqual(EndStateType.Success, run.EndState);
        }

        [TestMethod]
        public async Task AddedTimeRaisesTheClockAndItsLimitAsync()
        {
            var extended = Run(new Script
            {
                Enter = r =>
                {
                    r.StartClock(TimeSpan.FromMinutes(2));
                    r.AddClockTime(TimeSpan.FromSeconds(80));
                }
            });
            await extended.StartAsync();

            var clock = _first.LastPackets.OfType<EventPacket>().Last(s => s.Type == EventPacketType.TSTimer);
            Assert.AreEqual(2000, clock.Parameter);
            Assert.AreEqual(2000, clock.SecondParameter);
        }

        [TestMethod]
        public async Task TheRoomClockStartsAndStopsAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.StartRoomClock(1, TimeSpan.FromMinutes(3)),
                Cleared = (r, room) => r.StopRoomClock(room)
            });
            await run.StartAsync();
            var started = _first.LastPackets.OfType<EventPacket>().Last(s => s.Type == EventPacketType.RedTimer);
            Assert.AreEqual(1800, started.Parameter);
            Assert.IsFalse(started.IsDisabled);

            run.SetMonsterLocker(1, 1);
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            var stopped = _first.LastPackets.OfType<EventPacket>().Last(s => s.Type == EventPacketType.RedTimer);
            Assert.IsTrue(stopped.IsDisabled);
        }

        [TestMethod]
        public async Task ADialogOpensForThePlayersInTheRoomAsync()
        {
            var run = Run(new Script { Enter = r => r.Dialog(1, 6111) });
            var session = await PlayerInFirstRoomAsync();

            await run.StartAsync();

            var request = session.LastPackets.OfType<NpcReqPacket>().Single();
            Assert.AreEqual(VisualType.Player, request.Type);
            Assert.AreEqual(session.Character.CharacterId, request.TargetId);
            Assert.AreEqual(6111L, request.Data);
        }

        [TestMethod]
        public async Task SummonedNpcsStandInTheRoomAsync()
        {
            var run = Run(new Script { Enter = r => r.SummonNpcs(1, new NpcSpawn(Grunt, new Spot(2, 2))) });

            await run.StartAsync();

            var npc = _first.EnumerateNpcs().Single();
            Assert.AreEqual(Grunt, npc.VNum);
            Assert.AreEqual(1, run.NpcsSummoned);
        }

        [TestMethod]
        public async Task ALedEscortFollowsItsLeaderIntoTheNextRoomAsync()
        {
            var run = Run(new Script { Enter = r => r.SummonNpcs(1, new NpcSpawn(Grunt, new Spot(2, 2), IsEscort: true)) });
            var session = await PlayerInFirstRoomAsync();
            await run.EnteredAsync(session, _first.MapInstanceId);
            var escort = _first.EnumerateNpcs().Single();
            escort.LeaderId = session.Character.CharacterId;
            escort.Hp = 4;

            session.Character.MapInstance = _second;
            session.Character.PositionX = 1;
            session.Character.PositionY = 0;
            await run.EnteredAsync(session, _second.MapInstanceId);

            Assert.AreEqual(0, _first.EnumerateNpcs().Count());
            var moved = _second.EnumerateNpcs().Single();
            Assert.AreEqual(session.Character.CharacterId, moved.LeaderId);
            Assert.IsTrue(moved.IsEscort);
            Assert.AreEqual(4, moved.Hp);
            Assert.AreEqual((short)1, moved.PositionX);
        }

        [TestMethod]
        public async Task AnEscortNobodyLeadsStaysInItsRoomAsync()
        {
            var run = Run(new Script { Enter = r => r.SummonNpcs(1, new NpcSpawn(Grunt, new Spot(2, 2), IsEscort: true)) });
            var session = await PlayerInFirstRoomAsync();
            await run.EnteredAsync(session, _first.MapInstanceId);

            session.Character.MapInstance = _second;
            await run.EnteredAsync(session, _second.MapInstanceId);

            Assert.AreEqual(1, _first.EnumerateNpcs().Count());
            Assert.AreEqual(0, _second.EnumerateNpcs().Count());
        }

        [TestMethod]
        public async Task TheRunFailsWhenTheEscortFallsAsync()
        {
            var run = Run(new Script());
            await run.StartAsync();

            await run.EscortFellAsync(_first.MapInstanceId);

            Assert.AreEqual(EndStateType.Fail, run.EndState);
        }

        [TestMethod]
        public async Task TheScoreCarriesThePointsAsync()
        {
            var run = Run(new Script { Enter = r => r.SetMonsterLocker(1, 1), Cleared = (r, _) => r.Succeed() });
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            var score = _first.LastPackets.OfType<NosCore.Packets.ClientPackets.Instance.ScorePacket>().Single();
            Assert.AreEqual(4, score.Points);
        }
    }
}
