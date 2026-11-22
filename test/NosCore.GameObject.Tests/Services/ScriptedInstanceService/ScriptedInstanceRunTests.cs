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
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Handlers.TimeSpace;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Shared.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class ScriptedInstanceRunTests
    {
        private TestWorld World;
        private const short Grunt = 58;
        private const short Boss = 59;
        private const short Villager = 60;

        private sealed class Script : InstanceScript
        {
            public Action<IInstanceRun>? Enter { get; init; }
            public Action<IInstanceRun, int>? Cleared { get; init; }
            public Action<IInstanceRun>? Timeout { get; init; }
            public Action<IInstanceRun, int>? Exit { get; init; }
            public List<string> Calls { get; } = [];

            public Script(byte id = 0, params InstanceObjective[] objectives) =>
                Definition = new ScriptedInstanceDefinition { Id = id, Objectives = objectives };

            public Script(ScriptedInstanceDefinition definition) => Definition = definition;

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

            public override void OnDialogClosed(IInstanceRun run, int room, int dialog) =>
                Calls.Add($"closed({room},{dialog})");

            public override void OnItemCollected(IInstanceRun run, int room, short vNum) =>
                Calls.Add($"collected({room},{vNum})");

            public override void OnTimeout(IInstanceRun run)
            {
                Calls.Add("timeout");
                Timeout?.Invoke(run);
            }
        }

        private sealed class TalkingGruntScript : TimeSpaceScript
        {
            public TalkingGruntScript() : base(1, "timespace") =>
                Room(2004, 0, 0).Spawn(Monster(Grunt).At(1, 1).Talk(6028));
        }

        private MapInstance _first;
        private MapInstance _second;
        private IInstanceRunHost? _host;
        private readonly Mock<IGameEventDispatcher> _gameEvents = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Inject(_gameEvents.Object);
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

        private ScriptedInstanceRun Run(IInstanceScript script, TimeSpan? timeout = null) =>
            new(_host ?? World.CreateTimeSpaceRunHost(new ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, PositionX = 5, PositionY = 6, LevelMinimum = 1, Logic = script }),
                script,
                new Dictionary<int, MapInstance> { [1] = _first, [2] = _second },
                new NpcMonsterCatalog([new NpcMonsterDto { NpcMonsterVNum = Grunt, MaxHp = 10, IsHostile = true },
                    new NpcMonsterDto { NpcMonsterVNum = Villager, MaxHp = 10 }]),
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
        public async Task ARoomMessageIsShownOnlyInThatRoomAsync()
        {
            var run = Run(new Script { Enter = r => r.Message(2, Game18NConstString.EnemiesReinforcements) });

            await run.StartAsync();

            Assert.IsTrue(_second.LastPackets.OfType<MsgiPacket>().Any(packet => packet.Message == Game18NConstString.EnemiesReinforcements));
            Assert.IsFalse(_first.LastPackets.OfType<MsgiPacket>().Any(packet => packet.Message == Game18NConstString.EnemiesReinforcements));
        }

        [TestMethod]
        public async Task ARoomServerTextMessageReachesOnlyThePlayersInThatRoomAsync()
        {
            var inside = await PlayerInFirstRoomAsync();
            var elsewhere = await PlayerInRoomAsync(_second);
            Mock.Get(World.GameLanguageLocalizer)
                .Setup(x => x[LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, It.IsAny<RegionType>(), It.IsAny<object[]>()])
                .Returns(new LocalizedString("TIMESPACE_STRIKE_CENTRE_FLOOR", "Centre floor"));
            var run = Run(new Script { Enter = r => r.Message(1, LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, 2) });

            await run.StartAsync();

#pragma warning disable CS0618
            PacketAssert.AreEqual(new MsgPacket { Type = MessageType.Default, Message = "Centre floor" },
                inside.LastPackets.OfType<MsgPacket>().Single());
            Assert.IsFalse(elsewhere.LastPackets.OfType<MsgPacket>().Any());
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
        public async Task KillingAWaveInATimedRoomKeepsItsObjectiveAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            ObjectiveText("Élimine tous les ennemis !");
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.StartRoomClock(1, TimeSpan.FromMinutes(3));
                    r.SetMonsterLocker(1, 1);
                    r.Objective(1, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
                }
            });
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            PacketAssert.AreEqual(new SinfoPacket { Message = "Élimine tous les ennemis !" },
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
        public async Task AnUnlockedPortalIsTakenOffBeforeItIsShownOpenAsync()
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
            _first.LastPackets.Clear();
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            var lines = _first.LastPackets.OfType<GpPacket>().ToList();
            Assert.HasCount(2, lines);
            Assert.AreEqual((PortalType.Closed, true), (lines[0].PortalType, lines[0].IsDisabled));
            Assert.AreEqual((PortalType.Open, false), (lines[1].PortalType, lines[1].IsDisabled));
            Assert.AreEqual(lines[0].PortalId, lines[1].PortalId);
        }

        [TestMethod]
        public async Task UnlockingAnOpenPortalSendsNothingAsync()
        {
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.SetMonsterLocker(1, 1);
                    r.SpawnPortal(1, new Spot(3, 3), 2, new Spot(1, 1), locked: false);
                },
                Cleared = (r, room) => r.UnlockPortal(room, 2)
            });

            await run.StartAsync();
            _first.LastPackets.Clear();
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            Assert.IsFalse(_first.LastPackets.OfType<GpPacket>().Any());
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
            PacketAssert.AreEqual(new ScoreFailedPacket { EndState = EndStateType.Fail },
                _first.LastPackets.OfType<ScoreFailedPacket>().Single());
            Assert.IsFalse(_first.LastPackets.OfType<ScorePacket>().Any());
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
            Assert.IsFalse(_first.LastPackets.OfType<ScorePacket>().Any());
            PacketAssert.AreEqual(new ScoreFailedPacket { EndState = EndStateType.Fail },
                _first.LastPackets.OfType<ScoreFailedPacket>().Single());
        }

        [TestMethod]
        public async Task AnAbandonedRunShowsAFailedScoreAsync()
        {
            var run = Run(new Script { Enter = _ => throw new InvalidOperationException() });

            await run.StartAsync();

            Assert.IsTrue(_first.LastPackets.OfType<ScoreFailedPacket>().Any(s => s.EndState == EndStateType.Fail));
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
        public async Task ClosingADialogOfTheRunTellsTheScriptAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var script = new Script { Enter = r => r.Dialog(1, 6024) };
            var run = Run(script);
            await run.StartAsync();

            await run.DialogClosedAsync(session.Character.CharacterId);

            CollectionAssert.Contains(script.Calls, "closed(1,6024)");
        }

        [TestMethod]
        public async Task TheClockStandsStillWhileATalkIsOpenAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.StartClock(TimeSpan.FromSeconds(60));
                    r.Dialog(1, 6024);
                }
            });
            await run.StartAsync();

            World.Clock.AdvanceSeconds(20);
            await run.TickAsync();
            await run.DialogClosedAsync(session.Character.CharacterId);
            World.Clock.AdvanceSeconds(59);
            await run.TickAsync();

            Assert.IsNull(run.EndState);
            var clock = _first.LastPackets.OfType<EventPacket>().Last(s => s.Type == EventPacketType.TSTimer);
            Assert.AreEqual(600, clock.Parameter);
        }

        [TestMethod]
        public async Task ARoomDoesNotClearWhileItsTalkIsOpenAsync()
        {
            await PlayerInFirstRoomAsync();
            var script = new Script
            {
                Enter = r =>
                {
                    r.SetMonsterLocker(1, 1);
                    r.Dialog(1, 6055);
                }
            };
            var run = Run(script);
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            CollectionAssert.DoesNotContain(script.Calls, "cleared(1)");
        }

        [TestMethod]
        public async Task ARoomHeldByItsTalkClearsWhenTheTalkIsEndedAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var script = new Script
            {
                Enter = r =>
                {
                    r.SetMonsterLocker(1, 1);
                    r.Dialog(1, 6055);
                }
            };
            var run = Run(script);
            await run.StartAsync();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
            await run.DialogClosedAsync(session.Character.CharacterId);

            CollectionAssert.AreEqual(new[] { "enter", "killed(1,58)", "closed(1,6055)", "cleared(1)" },
                script.Calls.Where(c => !c.StartsWith("entered")).ToList());
        }

        [TestMethod]
        public async Task ATalkLeftOpenHoldsTheClockForThirtySecondsAtMostAsync()
        {
            await PlayerInFirstRoomAsync();
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.StartClock(TimeSpan.FromSeconds(60));
                    r.Dialog(1, 6024);
                }
            });
            await run.StartAsync();

            World.Clock.AdvanceSeconds(30);
            await run.TickAsync();
            World.Clock.AdvanceSeconds(59);
            await run.TickAsync();
            Assert.IsNull(run.EndState);

            World.Clock.AdvanceSeconds(1);
            await run.TickAsync();
            Assert.AreEqual(EndStateType.Timeout, run.EndState);
        }

        [TestMethod]
        public async Task ClosingADialogTheRunNeverOpenedTellsTheScriptNothingAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var script = new Script();
            var run = Run(script);
            await run.StartAsync();

            await run.DialogClosedAsync(session.Character.CharacterId);

            Assert.IsFalse(script.Calls.Any(c => c.StartsWith("closed", StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task ATalkNobodyClosedIsShownToALaterEntrantAsync()
        {
            var first = await PlayerInFirstRoomAsync();
            var run = Run(new Script { Enter = r => r.Dialog(1, 6024) });
            await run.EnteredAsync(first, _first.MapInstanceId);
            var later = await PlayerInRoomAsync(_second);
            later.LastPackets.Clear();

            Move(later, _first);
            await run.EnteredAsync(later, _first.MapInstanceId);

            PacketAssert.AreEqual(new NpcReqPacket { Type = VisualType.Player, TargetId = later.Character.CharacterId, Data = 6024 },
                later.LastPackets.OfType<NpcReqPacket>().Single());
        }

        [TestMethod]
        public async Task ATalkOpenedInAnEmptyRoomIsShownToItsFirstEntrantAsync()
        {
            var first = await PlayerInFirstRoomAsync();
            var run = Run(new Script { Enter = r => r.Dialog(2, 6024) });
            await run.EnteredAsync(first, _first.MapInstanceId);
            first.LastPackets.Clear();

            Move(first, _second);
            await run.EnteredAsync(first, _second.MapInstanceId);

            Assert.AreEqual(1, first.LastPackets.OfType<NpcReqPacket>().Count(p => p.Data == 6024));
        }

        [TestMethod]
        public async Task ATalkOpenedOnEntryIsShownOnceAsync()
        {
            var first = await PlayerInFirstRoomAsync();
            var run = Run(new Script { Enter = r => r.Dialog(1, 6024) });

            await run.EnteredAsync(first, _first.MapInstanceId);

            Assert.AreEqual(1, first.LastPackets.OfType<NpcReqPacket>().Count(p => p.Data == 6024));
        }

        [TestMethod]
        public async Task AClosedTalkIsNotShownAgainAsync()
        {
            var first = await PlayerInFirstRoomAsync();
            var run = Run(new Script { Enter = r => r.Dialog(1, 6024) });
            await run.EnteredAsync(first, _first.MapInstanceId);
            await run.DialogClosedAsync(first.Character.CharacterId);
            var later = await PlayerInRoomAsync(_second);
            later.LastPackets.Clear();

            Move(later, _first);
            await run.EnteredAsync(later, _first.MapInstanceId);

            Assert.IsFalse(later.LastPackets.OfType<NpcReqPacket>().Any());
        }

        private static ScriptedInstanceDefinition TwoRooms => new()
        {
            Rooms = [new InstanceRoom(1, 2004, 2, 9), new InstanceRoom(2, 2004, 3, 9)]
        };

        [TestMethod]
        public async Task TheWayOutOfTheFirstRoomAsksWhetherToStartAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var run = Run(new Script(TwoRooms));
            await run.StartAsync();

            Assert.IsTrue(await run.AskToStartAsync(session));

            PacketAssert.AreEqual(new DlgiPacket
            {
                YesPacket = new RStartPacket { Type = RStartPacketType.TimeSpaceInstanceEntry },
                NoPacket = new RStartPacket(),
                Question = Game18NConstString.WantEnterFirstRoom
            }, session.LastPackets.OfType<DlgiPacket>().Single());
        }

        [TestMethod]
        public async Task APacketHandlerOnTheRoomGetsTheStartQuestionsAnswerAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var run = Run(new Script(TwoRooms));
            await run.StartAsync();
            var asked = false;

            await _first.Executor.RunHandlerAsync(async () => asked = await run.AskToStartAsync(session))
                .WaitAsync(TimeSpan.FromSeconds(5));

            Assert.IsTrue(asked);
        }

        [TestMethod]
        public async Task OnlyTheFirstRoomAsksWhetherToStartAsync()
        {
            var session = await PlayerInRoomAsync(_second);
            var run = Run(new Script(TwoRooms));
            await run.StartAsync();

            Assert.IsFalse(await run.AskToStartAsync(session));
            Assert.IsFalse(await run.ConfirmStartAsync(session));
        }

        [TestMethod]
        public async Task OnceStartedTheFirstRoomNoLongerAsksAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var run = Run(new Script(TwoRooms));
            await run.EnteredAsync(session, _first.MapInstanceId);

            Assert.IsTrue(await run.ConfirmStartAsync(session));

            Assert.IsFalse(await run.AskToStartAsync(session));
            PacketAssert.AreEqual(new RsfnPacket { MapIndexX = 2, MapIndexY = 9, Status = 0 },
                session.LastPackets.OfType<RsfnPacket>().Last());
        }

        [TestMethod]
        public async Task ALaterYesStillLetsItsPlayerThroughWithoutRestartingTheClockAsync()
        {
            var first = await PlayerInFirstRoomAsync();
            var second = await PlayerInFirstRoomAsync();
            var run = Run(new Script(TwoRooms) { Enter = r => r.StartClock(TimeSpan.FromSeconds(60)) });
            await run.StartAsync();

            Assert.IsTrue(await run.ConfirmStartAsync(first));
            World.Clock.AdvanceSeconds(10);
            Assert.IsTrue(await run.ConfirmStartAsync(second), "the second answer lets its player through");

            World.Clock.AdvanceSeconds(50);
            await run.TickAsync();
            Assert.AreEqual(EndStateType.Timeout, run.EndState, "the clock started once, with the first yes");
        }

        [TestMethod]
        public async Task TheClockIsOnlyDrawnOnceTheStartIsConfirmedAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var run = Run(new Script(TwoRooms) { Enter = r => r.StartClock(TimeSpan.FromSeconds(60)) });
            await run.EnteredAsync(session, _first.MapInstanceId);
            World.Clock.AdvanceSeconds(20);
            await run.TickAsync();

            Assert.IsFalse(session.LastPackets.Concat(_first.LastPackets).OfType<EventPacket>().Any(p => p.Type == EventPacketType.TSTimer),
                "a drawn clock would be counted down by the client while the run waits");

            await run.ConfirmStartAsync(session);

            PacketAssert.AreEqual(new EventPacket { Type = EventPacketType.TSTimer, Parameter = 600, SecondParameter = 600 },
                _first.LastPackets.OfType<EventPacket>().Single(p => p.Type == EventPacketType.TSTimer));
        }

        [TestMethod]
        public async Task TheClockWaitsForTheStartToBeConfirmedAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var script = new Script(TwoRooms) { Enter = r => r.StartClock(TimeSpan.FromSeconds(60)) };
            var run = Run(script);
            await run.StartAsync();

            World.Clock.AdvanceSeconds(100);
            await run.TickAsync();
            Assert.IsNull(run.EndState);

            await run.ConfirmStartAsync(session);
            World.Clock.AdvanceSeconds(59);
            await run.TickAsync();
            Assert.IsNull(run.EndState);

            World.Clock.AdvanceSeconds(1);
            await run.TickAsync();
            Assert.AreEqual(EndStateType.Timeout, run.EndState);
        }

        [TestMethod]
        public async Task AMonsterSummonedWithATalkShowsItAsync()
        {
            await PlayerInFirstRoomAsync();
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.Dialog(1, 6028);
                    r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1), Dialog: 6028));
                }
            });

            await run.StartAsync();

            var monster = _first.EnumerateMonsters().Single();
            Assert.AreEqual((short)6028, monster.Dialog);
            Assert.AreEqual((short)6028, _first.LastPackets.OfType<InPacket>().Single().InNonPlayerSubPacket.NotNull().Dialog);
        }

        [TestMethod]
        public async Task AMonsterGivenATalkByAScriptShowsItAsync()
        {
            await PlayerInFirstRoomAsync();
            var run = Run(new TalkingGruntScript());

            await run.StartAsync();

            Assert.AreEqual((short)6028, _first.EnumerateMonsters().Single().Dialog);
            Assert.AreEqual((short)6028, _first.LastPackets.OfType<InPacket>().Single().InNonPlayerSubPacket.NotNull().Dialog);
        }

        [TestMethod]
        public async Task ClosingTheTalkTakesItOffTheMonsterThatCarriesItAsync()
        {
            var session = await PlayerInFirstRoomAsync();
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.Dialog(1, 6028);
                    r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1), Dialog: 6028));
                }
            });
            await run.StartAsync();

            await run.DialogClosedAsync(session.Character.CharacterId);

            var monster = _first.EnumerateMonsters().Single();
            Assert.IsNull(monster.Dialog);
            Assert.AreEqual((short)0, monster.GenerateIn().InNonPlayerSubPacket.NotNull().Dialog);
        }

        [TestMethod]
        public async Task ADialogShownToTwoPlayersIsClosedOnceAsync()
        {
            var first = await PlayerInFirstRoomAsync();
            var second = await PlayerInFirstRoomAsync();
            var script = new Script { Enter = r => r.Dialog(1, 6024) };
            var run = Run(script);
            await run.StartAsync();

            await run.DialogClosedAsync(first.Character.CharacterId);
            await run.DialogClosedAsync(second.Character.CharacterId);
            await run.DialogClosedAsync(first.Character.CharacterId);

            Assert.AreEqual(1, script.Calls.Count(c => c == "closed(1,6024)"));
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
        public async Task DismissingARoomTakesAllItsMonstersAwayAsync()
        {
            var run = Run(new Script
            {
                Enter = r =>
                {
                    r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)), new MonsterSpawn(Grunt, new Spot(2, 1)));
                    r.Summon(2, new MonsterSpawn(Grunt, new Spot(1, 1)));
                }
            });
            await run.StartAsync();

            run.Dismiss(1);
            await run.TickAsync();

            Assert.IsFalse(_first.EnumerateMonsters().Any(m => m.IsAlive));
            Assert.AreEqual(0, run.MonstersAlive(1));
            Assert.IsTrue(_second.EnumerateMonsters().Single().IsAlive);
        }

        [TestMethod]
        public async Task AnAllyIsSetAgainstTheMonstersAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.SummonNpcs(1, new NpcSpawn(Villager, new Spot(2, 2), IsAlly: true), new NpcSpawn(Villager, new Spot(3, 2)))
            });

            await run.StartAsync();

            var npcs = _first.EnumerateNpcs().OrderBy(n => n.PositionX).ToList();
            Assert.IsTrue(npcs[0].IsHostile);
            Assert.IsFalse(npcs[1].IsHostile);
        }

        [TestMethod]
        public async Task AnNpcsOwnDeathStepRunsWhenItFallsAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.SummonNpcs(1, new NpcSpawn(Grunt, new Spot(2, 2), IsAlly: true,
                    OnDeath: d => d.Summon(1, new MonsterSpawn(Grunt, new Spot(7, 1)))))
            });
            await run.StartAsync();
            var deathStep = _first.EnumerateNpcs().Single().DeathStep ?? throw new AssertFailedException("No death step.");

            await run.NpcFellAsync(_first.MapInstanceId, deathStep);

            Assert.AreEqual((short)7, _first.EnumerateMonsters().Single().PositionX);
            Assert.AreEqual(1, run.MonstersAlive(1));
        }

        [TestMethod]
        public async Task AFallenNpcThatIsNoEscortRunsItsDeathStepAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.SummonNpcs(1, new NpcSpawn(Grunt, new Spot(2, 2), IsAlly: true,
                    OnDeath: d => d.Summon(1, new MonsterSpawn(Grunt, new Spot(7, 1)))))
            });
            await run.StartAsync();
            World.Freeze<Mock<IScriptedInstanceService>>().Setup(s => s.GetRun(_first.MapInstanceId)).Returns(run);
            var npc = _first.EnumerateNpcs().Single();

            await World.Create<TimeSpaceProgressHandler>().Handle(new EntityDiedEvent(npc, null));

            Assert.AreEqual((short)7, _first.EnumerateMonsters().Single().PositionX);
            Assert.IsNull(run.EndState);
        }

        [TestMethod]
        public async Task APlacedItemLiesOnItsCellForAnyoneAsync()
        {
            var run = Run(new Script { Enter = r => r.PlaceItem(2, 1012, new Spot(3, 1)) });

            await run.StartAsync();

            var item = _second.GetMapItemBundles().Single();
            Assert.AreEqual((short)1012, item.VNum);
            Assert.AreEqual((short)3, item.PositionX);
            Assert.AreEqual((short)1, item.PositionY);
            Assert.IsNull(item.OwnerId);
        }

        [TestMethod]
        public async Task PickingUpTheItemToCollectLetsThePartyOutAsync()
        {
            var script = new Script(0, new InstanceObjective(ObjectiveKind.CollectItem, 1059, 1))
            {
                Enter = r => r.SpawnExit(1, new Spot(3, 3), locked: false),
                Exit = (r, _) => r.Succeed()
            };
            var run = Run(script);
            await run.StartAsync();

            await run.ItemCollectedAsync(_second.MapInstanceId, 1012, 1);
            await run.ExitReachedAsync(_first.MapInstanceId);
            Assert.IsNull(run.EndState);

            await run.ItemCollectedAsync(_second.MapInstanceId, 1059, 1);
            await run.ExitReachedAsync(_first.MapInstanceId);
            Assert.AreEqual(EndStateType.Success, run.EndState);
            CollectionAssert.IsSubsetOf(new[] { "collected(2,1012)", "collected(2,1059)" }, script.Calls);
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
        public async Task AMonstersHitStepRunsOnEachHitItSurvivesAsync()
        {
            var hits = 0;
            var run = Run(new Script { Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1), OnHit: _ => hits++)) });
            await run.StartAsync();
            var monster = _first.EnumerateMonsters().Single();

            await HitAsync(run, monster, 9);
            await HitAsync(run, monster, 8);

            Assert.AreEqual(2, hits);
        }

        [TestMethod]
        public async Task AnHpThresholdRunsOnceAsTheMonsterFallsBelowItAsync()
        {
            var crossed = new List<byte>();
            var run = Run(new Script
            {
                Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1), OnHpBelow:
                [
                    new HpThreshold(75, _ => crossed.Add(75)),
                    new HpThreshold(50, _ => crossed.Add(50))
                ]))
            });
            await run.StartAsync();
            var monster = _first.EnumerateMonsters().Single();

            await HitAsync(run, monster, 8);
            await HitAsync(run, monster, 7);
            await HitAsync(run, monster, 9);
            await HitAsync(run, monster, 3);

            CollectionAssert.AreEqual(new byte[] { 75, 50 }, crossed);
        }

        [TestMethod]
        public async Task AHitThatCrossesTwoThresholdsRunsBothAsync()
        {
            var crossed = new List<byte>();
            var run = Run(new Script
            {
                Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1), OnHpBelow:
                [
                    new HpThreshold(75, _ => crossed.Add(75)),
                    new HpThreshold(50, _ => crossed.Add(50))
                ]))
            });
            await run.StartAsync();

            await HitAsync(run, _first.EnumerateMonsters().Single(), 2);

            CollectionAssert.AreEqual(new byte[] { 75, 50 }, crossed);
        }

        [TestMethod]
        public async Task AThresholdAHitCrossedRunsThoughALaterHitKilledTheMonsterFirstAsync()
        {
            var crossed = new List<byte>();
            var run = Run(new Script
            {
                Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1), OnHpBelow:
                [
                    new HpThreshold(75, _ => crossed.Add(75)),
                    new HpThreshold(50, _ => crossed.Add(50))
                ]))
            });
            await run.StartAsync();
            var monster = _first.EnumerateMonsters().Single();
            monster.Hp = 0;
            monster.IsAlive = false;

            await HitAsync(run, monster, 6);

            CollectionAssert.AreEqual(new byte[] { 75 }, crossed);
        }

        [TestMethod]
        public async Task AHitStepCanActOnTheRunAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1),
                    OnHit: h => h.Summon(1, new MonsterSpawn(Grunt, new Spot(3, 1)))))
            });
            await run.StartAsync();

            await HitAsync(run, _first.EnumerateMonsters().Single(), 9);

            Assert.AreEqual(2, _first.EnumerateMonsters().Count());
        }

        [TestMethod]
        public async Task AHitOnAMonsterWithoutHitStepsLeavesTheRunAloneAsync()
        {
            var run = Run(new Script { Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1))) });
            await run.StartAsync();
            var service = World.Freeze<Mock<IScriptedInstanceService>>();
            var attacker = (await World.CreateSessionAsync()).Character;

            await World.Create<TimeSpaceProgressHandler>()
                .Handle(new EntityDamagedEvent(attacker, _first.EnumerateMonsters().Single(), 1, false, 9));

            service.Verify(s => s.GetRun(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task AKillingHitRunsNoHitStepAsync()
        {
            var service = World.Freeze<Mock<IScriptedInstanceService>>();
            var run = Run(new Script { Enter = r => r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1), OnHit: _ => { })) });
            await run.StartAsync();
            var attacker = (await World.CreateSessionAsync()).Character;

            await World.Create<TimeSpaceProgressHandler>()
                .Handle(new EntityDamagedEvent(attacker, _first.EnumerateMonsters().Single(), 10, true, 0));

            service.Verify(s => s.GetRun(It.IsAny<Guid>()), Times.Never);
        }

        private Mock<IScriptedInstanceService>? _runs;

        private async Task HitAsync(ScriptedInstanceRun run, MonsterComponentBundle monster, int hpLeft)
        {
            if (_runs == null)
            {
                _runs = World.Freeze<Mock<IScriptedInstanceService>>();
                _runs.Setup(s => s.GetRun(_first.MapInstanceId)).Returns(run);
            }

            var attacker = (await World.CreateSessionAsync()).Character;
            await World.Create<TimeSpaceProgressHandler>()
                .Handle(new EntityDamagedEvent(attacker, monster, 1, false, hpLeft));
        }

        [TestMethod]
        public void ACellAroundASpotIsWalkableAndWithinReach()
        {
            var run = Run(new Script());

            var cells = Enumerable.Range(0, 50).Select(_ => run.RandomCell(1, new Spot(1, 1), 1)).ToList();

            Assert.IsTrue(cells.All(c => c.X is >= 0 and <= 2 && c.Y is >= 0 and <= 2));
            Assert.AreEqual(new Spot(30, 30), run.RandomCell(1, new Spot(30, 30), 2));
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
        public async Task AnUnlockedExitIsTakenOffBeforeItIsShownOpenAsync()
        {
            var run = Run(new Script
            {
                Enter = r => r.SpawnExit(1, new Spot(3, 3)),
                Cleared = (r, room) => r.UnlockExit(room)
            });
            await run.StartAsync();
            run.SetMonsterLocker(1, 1);
            _first.LastPackets.Clear();

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            var lines = _first.LastPackets.OfType<GpPacket>().ToList();
            Assert.HasCount(2, lines);
            Assert.AreEqual((PortalType.TsEndClosed, true), (lines[0].PortalType, lines[0].IsDisabled));
            Assert.AreEqual((PortalType.TsEnd, false), (lines[1].PortalType, lines[1].IsDisabled));
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
            Assert.AreEqual(EventTimerState.Running, started.State);

            run.SetMonsterLocker(1, 1);
            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            var stopped = _first.LastPackets.OfType<EventPacket>().Last(s => s.Type == EventPacketType.RedTimer);
            Assert.AreEqual(EventTimerState.Stopped, stopped.State);
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
        public async Task AnAlliedEscortStaysAnAllyInTheNextRoomAsync()
        {
            var run = Run(new Script { Enter = r => r.SummonNpcs(1, new NpcSpawn(Villager, new Spot(2, 2), IsEscort: true, IsAlly: true)) });
            var session = await PlayerInFirstRoomAsync();
            await run.EnteredAsync(session, _first.MapInstanceId);
            _first.EnumerateNpcs().Single().LeaderId = session.Character.CharacterId;

            session.Character.MapInstance = _second;
            session.Character.PositionX = 1;
            session.Character.PositionY = 0;
            await run.EnteredAsync(session, _second.MapInstanceId);

            Assert.IsTrue(_second.EnumerateNpcs().Single().IsHostile);
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
        public async Task AnEscortFallingOnARoomDoesNotWaitForATriggerWaitingOnThatRoomAsync()
        {
            var scriptRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = Run(new Script
            {
                Enter = r =>
                {
                    scriptRunning.SetResult();
                    r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)));
                }
            });
            var start = Task.CompletedTask;

            await _first.Executor.RunAsync(async () =>
            {
                using (ExecutionContext.SuppressFlow())
                {
                    start = Task.Run(run.StartAsync);
                }

                await scriptRunning.Task;
                await run.EscortFellAsync(_first.MapInstanceId).WaitAsync(TimeSpan.FromSeconds(5));
            });
            await start;

            Assert.AreEqual(1, _first.EnumerateMonsters().Count());
            Assert.IsTrue(await EventuallyAsync(() => run.EndState == EndStateType.Fail));
        }

        [TestMethod]
        public async Task AKillOnARoomDoesNotWaitForATriggerWaitingOnThatRoomAsync()
        {
            var scriptRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var cleared = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = Run(new Script
            {
                Enter = r =>
                {
                    scriptRunning.SetResult();
                    r.SetMonsterLocker(1, 1);
                    r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)));
                },
                Cleared = (_, _) => cleared.SetResult()
            });
            var start = Task.CompletedTask;

            await _first.Executor.RunAsync(async () =>
            {
                using (ExecutionContext.SuppressFlow())
                {
                    start = Task.Run(run.StartAsync);
                }

                await scriptRunning.Task;
                await run.MonsterKilledAsync(_first.MapInstanceId, Grunt).WaitAsync(TimeSpan.FromSeconds(5));
            });
            await start;

            await cleared.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(1, _first.EnumerateMonsters().Count());
            Assert.AreEqual(1, run.MonstersKilled);
        }

        [TestMethod]
        public async Task AnExitReachedAfterAnEscortFellComesAfterItAsync()
        {
            var scriptRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = Run(new Script
            {
                Enter = r =>
                {
                    scriptRunning.SetResult();
                    r.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)));
                },
                Exit = (r, _) => r.Succeed()
            });
            var start = Task.CompletedTask;
            var exit = Task.CompletedTask;

            await _first.Executor.RunAsync(async () =>
            {
                using (ExecutionContext.SuppressFlow())
                {
                    start = Task.Run(run.StartAsync);
                }

                await scriptRunning.Task;
                await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
                await run.EscortFellAsync(_first.MapInstanceId);
                using (ExecutionContext.SuppressFlow())
                {
                    exit = Task.Run(() => run.ExitReachedAsync(_first.MapInstanceId));
                }

                await Task.Delay(100);
            });
            await Task.WhenAll(start, exit);

            Assert.AreEqual(EndStateType.Fail, run.EndState);
        }

        [TestMethod]
        public async Task ATriggerRaisedByAnotherOfTheRunsTriggersRunsAfterItAsync()
        {
            var run = Run(new Script { Exit = (r, _) => r.Succeed() });
            await run.StartAsync();

            await run.RunHostAsync(() => run.ExitReachedAsync(_first.MapInstanceId)).WaitAsync(TimeSpan.FromSeconds(5));

            Assert.IsTrue(await EventuallyAsync(() => run.EndState == EndStateType.Success));
        }

        [TestMethod]
        public async Task AClockRunningOutSeenFromARoomsWorkStillEndsTheRunAsATimeoutAsync()
        {
            var run = Run(new Script { Enter = r => r.StartClock(TimeSpan.FromMinutes(1)) });
            await run.StartAsync();
            World.Clock.AdvanceSeconds(60);

            await _first.Executor.RunAsync(run.TickAsync);

            Assert.IsTrue(await EventuallyAsync(() => _first.LastPackets.OfType<ScoreFailedPacket>().Any()));
            Assert.AreEqual(EndStateType.Timeout, run.EndState);
            PacketAssert.AreEqual(new ScoreFailedPacket { EndState = EndStateType.Timeout },
                _first.LastPackets.OfType<ScoreFailedPacket>().Single());
        }

        [TestMethod]
        public async Task AnNpcFallingDuringARoomsWorkIsStillCountedAsync()
        {
            var run = Run(new Script { Enter = r => r.SummonNpcs(1, new NpcSpawn(Grunt, new Spot(2, 2))) });
            await run.StartAsync();
            var npc = _first.EnumerateNpcs().Single();

            await _first.Executor.RunAsync(() => run.NpcFellAsync(_first.MapInstanceId, npc.VisualId));

            Assert.IsTrue(await EventuallyAsync(() => run.NpcsLost == 1));
        }

        [TestMethod]
        public async Task KillsDuringARoomsWorkReachTheScriptInTheOrderTheyHappenedAsync()
        {
            var script = new Script();
            var run = Run(script);
            await run.StartAsync();

            await _first.Executor.RunAsync(async () =>
            {
                await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);
                await run.MonsterKilledAsync(_first.MapInstanceId, Boss);
            });

            Assert.IsTrue(await EventuallyAsync(() => script.Calls.Count == 3));
            CollectionAssert.AreEqual(new[] { "enter", $"killed(1,{Grunt})", $"killed(1,{Boss})" }, script.Calls);
        }

        // What a room's own work raises reaches the run once that work is over, so it is waited for.
        private static async Task<bool> EventuallyAsync(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++)
            {
                await Task.Delay(50);
            }

            return condition();
        }

        [TestMethod]
        public async Task TheScoreCarriesThePointsAsync()
        {
            var run = Run(new Script { Enter = r => r.SetMonsterLocker(1, 1), Cleared = (r, _) => r.Succeed() });
            var session = await PlayerInFirstRoomAsync();
            session.Character.Level = 1;
            await run.EnteredAsync(session, _first.MapInstanceId);

            await run.MonsterKilledAsync(_first.MapInstanceId, Grunt);

            var score = session.LastPackets.OfType<ScorePacket>().Single();
            Assert.AreEqual(4, score.Points);
        }
    }
}
