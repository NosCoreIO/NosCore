//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.TimeSpacePenaltyService;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.TimeSpace;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceScoreTests
    {
        private const short TimeSpaceId = 7;
        private const byte LevelMinimum = 10;
        private const short Grunt = 58;
        private const short Villager = 59;
        private const long Gold = 7462;

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _room;
        private ScriptedInstanceRun _run;
        private readonly Mock<IGameEventDispatcher> _gameEvents = new();

        private sealed class Script : InstanceScript
        {
            private readonly bool _succeeds;

            public Script(bool succeeds)
            {
                _succeeds = succeeds;
                Definition = new ScriptedInstanceDefinition
                {
                    Id = TimeSpaceId,
                    Gold = Gold,
                    DrawItems = [new InstanceGift(1, 1), new InstanceGift(2, 1), new InstanceGift(3, 1)]
                };
            }

            public override void OnFirstEnter(IInstanceRun run)
            {
                run.Summon(1, new MonsterSpawn(Grunt, new Spot(1, 1)), new MonsterSpawn(Grunt, new Spot(2, 1)));
                run.SummonNpcs(1, new NpcSpawn(Villager, new Spot(1, 2)), new NpcSpawn(Villager, new Spot(2, 2)));
            }

            public override void OnExitReached(IInstanceRun run, int room)
            {
                if (_succeeds)
                {
                    run.Succeed();
                }
                else
                {
                    run.Fail();
                }
            }
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.Level = LevelMinimum;
        }

        [TestMethod]
        public async Task AFirstClearShowsEveryCategoryPerfect()
        {
            await new Spec("A first clear that kills every monster and keeps every NPC alive scores a perfect first clear")
                .GivenAsync(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 2)
                .And(TheClearIsRecordedWhenItSucceeds)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldBe, new ScorePacket
                {
                    Points = 8,
                    Gold = (int)Gold,
                    DrawRewardCount = 3,
                    MonstersKilled = 2,
                    RoomsExplored = 1,
                    NpcsLeft = 2,
                    Perfect = [true, true, true],
                    ClearType = TimeSpaceClearType.FirstClear,
                    Unknown = 15
                })
                .And(TheClearShouldBeRecorded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARepeatedClearIsMarkedClearedBefore()
        {
            await new Spec("A time-space the character cleared before scores as a repeat")
                .Given(TheCharacterHasClearedItBefore)
                .AndAsync(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldBe, new ScorePacket
                {
                    Points = 8,
                    Gold = (int)Gold,
                    DrawRewardCount = 3,
                    MonstersKilled = 2,
                    RoomsExplored = 1,
                    NpcsLeft = 2,
                    Perfect = [true, true, true],
                    ClearType = TimeSpaceClearType.ClearedBefore,
                    Unknown = 15
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALoserModeClearHasNoDraw()
        {
            await new Spec("A Loser Mode clear scores as Loser Mode with no draw, even on a first clear")
                .GivenAsync(ATimeSpaceThatSucceeds)
                .And(TheRunIsInLoserMode)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldBe, new ScorePacket
                {
                    Points = 8,
                    Gold = (int)Gold,
                    DrawRewardCount = 0,
                    MonstersKilled = 2,
                    RoomsExplored = 1,
                    NpcsLeft = 2,
                    Perfect = [true, true, true],
                    ClearType = TimeSpaceClearType.LoserMode,
                    Unknown = 15
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenNpcAndASurvivingMonsterAreNotPerfect()
        {
            await new Spec("A clear that leaves a monster alive and loses an NPC counts the NPCs left and marks only the rooms perfect")
                .GivenAsync(ATimeSpaceThatSucceeds)
                .AndAsync(_MonstersAreKilled, 1)
                .AndAsync(AnNpcFalls)
                .WhenAsync(TheExitIsReached)
                .Then(TheScoreShouldBe, new ScorePacket
                {
                    Points = 4,
                    Gold = (int)Gold,
                    DrawRewardCount = 3,
                    MonstersKilled = 1,
                    RoomsExplored = 1,
                    NpcsLeft = 1,
                    Perfect = [false, true, false],
                    ClearType = TimeSpaceClearType.FirstClear,
                    Unknown = 15
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedRunShowsTheFailedScore()
        {
            await new Spec("A failed run shows only its end state and no cleared score")
                .GivenAsync(ATimeSpaceThatFails)
                .AndAsync(_MonstersAreKilled, 2)
                .WhenAsync(TheExitIsReached)
                .Then(TheFailedScoreShouldBe, EndStateType.Fail)
                .And(NoClearedScoreShouldBeSent)
                .ExecuteAsync();
        }

        private void TheCharacterHasClearedItBefore() =>
            _session.Character.CompletedTimeSpaces = ImmutableHashSet.Create(TimeSpaceId);

        private void TheRunIsInLoserMode() => _run.IsLoserMode = true;

        private void TheClearIsRecordedWhenItSucceeds()
        {
            var handler = World.Create<TimeSpaceSucceededHandler>();
            _gameEvents.Setup(e => e.RaiseAsync(It.IsAny<ScriptedInstanceSucceededEvent>()))
                .Returns((ScriptedInstanceSucceededEvent evt) => handler.Handle(evt));
        }

        private Task ATimeSpaceThatSucceeds() => ATimeSpaceAsync(true);

        private Task ATimeSpaceThatFails() => ATimeSpaceAsync(false);

        private async Task ATimeSpaceAsync(bool succeeds)
        {
            _room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            var script = new Script(succeeds);
            _run = new ScriptedInstanceRun(
                new TimeSpaceRunHost(
                    new GameObject.Services.ScriptedInstanceService.ScriptedInstance
                    {
                        ScriptedInstanceId = 1, MapId = 1, LevelMinimum = LevelMinimum, Logic = script
                    },
                    new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), _gameEvents.Object,
                    World.Create<TimeSpacePenaltyService>()),
                script,
                new Dictionary<int, MapInstance> { [1] = _room },
                new NpcMonsterCatalog(
                [
                    new NpcMonsterDto { NpcMonsterVNum = Grunt, MaxHp = 10, IsHostile = true },
                    new NpcMonsterDto { NpcMonsterVNum = Villager, MaxHp = 10 }
                ]),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            _session.Character.MapInstance = _room;
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, _room.MapInstanceId);
            await _run.EnteredAsync(_session, _room.MapInstanceId);
        }

        private async Task _MonstersAreKilled(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await _run.MonsterKilledAsync(_room.MapInstanceId, Grunt);
            }
        }

        private Task AnNpcFalls()
        {
            var scriptedInstances = World.Freeze<Mock<IScriptedInstanceService>>();
            scriptedInstances.Setup(s => s.GetRun(_room.MapInstanceId)).Returns(_run);
            return World.Create<TimeSpaceProgressHandler>().Handle(new EntityDiedEvent(_room.EnumerateNpcs().First(), null));
        }

        private Task TheExitIsReached() => _run.ExitReachedAsync(_room.MapInstanceId);

        private void TheScoreShouldBe(ScorePacket expected) =>
            PacketAssert.AreEqual(expected, _session.LastPackets.OfType<ScorePacket>().Single());

        private void TheFailedScoreShouldBe(EndStateType state) =>
            PacketAssert.AreEqual(new ScoreFailedPacket { EndState = state },
                _room.LastPackets.OfType<ScoreFailedPacket>().Single());

        private void NoClearedScoreShouldBeSent() =>
            Assert.IsFalse(_room.LastPackets.OfType<ScorePacket>().Any() || _session.LastPackets.OfType<ScorePacket>().Any());

        private void TheClearShouldBeRecorded() =>
            Assert.IsTrue(_session.Character.CompletedTimeSpaces?.Contains(TimeSpaceId));
    }
}
