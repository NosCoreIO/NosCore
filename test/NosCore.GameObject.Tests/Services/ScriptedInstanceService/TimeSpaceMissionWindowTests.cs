//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceMissionWindowTests
    {
        private const short Boss = 109;
        private const short Seal = 1048;
        private const short Artifact = 1059;

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _room;
        private ScriptedInstanceRun _run;
        private readonly Mock<ISessionGroup> _roomSessions = new();

        private sealed class Script : InstanceScript
        {
            private readonly bool _withExit;

            public Script(ScriptedInstanceDefinition definition, bool withExit)
            {
                Definition = definition;
                _withExit = withExit;
            }

            public bool WonByPickUp { get; init; }

            public override void OnItemCollected(IInstanceRun run, int room, short vNum)
            {
                if (WonByPickUp)
                {
                    run.Succeed();
                }
            }

            public override void OnFirstEnter(IInstanceRun run)
            {
                run.StartClock(TimeSpan.FromSeconds(60));
                if (_withExit)
                {
                    run.SpawnExit(1, new Spot(2, 2));
                }
            }
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public async Task EnteringShowsTheLivesAndTheWayOut()
        {
            await new Spec("Entering a time-space with an exit shows its lives and that the exit is the goal")
                .Given(ATimeSpaceWith_LivesAndAnExit, (byte)3)
                .WhenAsync(ThePlayerEnters)
                .Then(ThePlayerShouldBeShown_, new MinfoPacket { GoToExit = true, MaxLives = 3 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringShowsTheObjectivesStillToDo()
        {
            await new Spec("Entering a time-space whose goal is a kill and two seals shows both counters at zero, and no exit")
                .Given(ATimeSpaceWith_LivesKillingTheBossAndUsingTwoSeals, (byte)4)
                .WhenAsync(ThePlayerEnters)
                .Then(ThePlayerShouldBeShown_, new MinfoPacket
                {
                    KillMonster = new MinfoObjectiveSubPacket { Vnum = Boss, Current = 0, Total = 1 },
                    InteractObjects = new MinfoObjectiveSubPacket { Vnum = Seal, Current = 0, Total = 2 },
                    MaxLives = 4
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKillTowardsTheObjectiveMovesItsCounter()
        {
            await new Spec("Killing the monster an objective counts shows the new count to the run")
                .Given(ATimeSpaceWith_LivesKillingTheBossAndUsingTwoSeals, (byte)4)
                .AndAsync(ThePlayerEnters)
                .WhenAsync(TheBossIsKilled)
                .Then(TheRunShouldBeShown_, new MinfoPacket
                {
                    KillMonster = new MinfoObjectiveSubPacket { Vnum = Boss, Current = 1, Total = 1 },
                    InteractObjects = new MinfoObjectiveSubPacket { Vnum = Seal, Current = 0, Total = 2 },
                    MaxLives = 4
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpTheItemToCollectMovesItsCounter()
        {
            await new Spec("Picking up the item a time-space asks for shows it collected to the run")
                .Given(ATimeSpaceWith_LivesCollectingTheArtifact, (byte)2)
                .AndAsync(ThePlayerEnters)
                .WhenAsync(TheArtifactIsPickedUp)
                .Then(TheRunShouldBeShown_, new MinfoPacket
                {
                    CollectItem = new MinfoObjectiveSubPacket { Vnum = Artifact, Current = 1, Total = 1 },
                    MaxLives = 2
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APickUpThatWinsTheRunStillShowsTheItemCollected()
        {
            await new Spec("Picking up the item that wins the run shows it collected before the run ends")
                .Given(ATimeSpaceWith_LivesWonByPickingUpTheArtifact, (byte)2)
                .AndAsync(ThePlayerEnters)
                .WhenAsync(TheArtifactIsPickedUp)
                .Then(TheRunShouldBeShown_, new MinfoPacket
                {
                    CollectItem = new MinfoObjectiveSubPacket { Vnum = Artifact, Current = 1, Total = 1 },
                    MaxLives = 2
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKillThatCountsForNothingLeavesTheWindowAlone()
        {
            await new Spec("Killing a monster no objective counts sends no new window")
                .Given(ATimeSpaceWith_LivesKillingTheBossAndUsingTwoSeals, (byte)4)
                .AndAsync(ThePlayerEnters)
                .WhenAsync(AnotherMonsterIsKilled)
                .Then(TheRunShouldBeShownNoWindow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALostLifeTakesAHeartAway()
        {
            await new Spec("A death that uses one of the party's lives shows one life fewer")
                .Given(ATimeSpaceWith_LivesAndAnExit, (byte)3)
                .AndAsync(ThePlayerEnters)
                .WhenAsync(ThePlayerDies)
                .Then(TheRunShouldBeShown_, new MinfoPacket { GoToExit = true, MaxLives = 2 })
                .ExecuteAsync();
        }

        private void ATimeSpaceWith_LivesAndAnExit(byte lives) =>
            ATimeSpace(InstanceDefinitionBuilder.Named(1, "timespace", "timespace").WithLives(lives).Build(), true);

        private void ATimeSpaceWith_LivesKillingTheBossAndUsingTwoSeals(byte lives) =>
            ATimeSpace(InstanceDefinitionBuilder.Named(1, "timespace", "timespace").WithLives(lives)
                .WithObjective(ObjectiveKind.KillMonster, Boss, 1)
                .WithObjective(ObjectiveKind.UseObject, Seal, 2)
                .Build(), false);

        private void ATimeSpaceWith_LivesCollectingTheArtifact(byte lives) =>
            ATimeSpace(InstanceDefinitionBuilder.Named(1, "timespace", "timespace").WithLives(lives)
                .WithObjective(ObjectiveKind.CollectItem, Artifact, 1)
                .Build(), false);

        private void ATimeSpaceWith_LivesWonByPickingUpTheArtifact(byte lives) =>
            ATimeSpace(InstanceDefinitionBuilder.Named(1, "timespace", "timespace").WithLives(lives)
                .WithObjective(ObjectiveKind.CollectItem, Artifact, 1)
                .Build(), false, wonByPickUp: true);

        private Task TheArtifactIsPickedUp() => _run.ItemCollectedAsync(_room.MapInstanceId, Artifact, 1);

        private void ATimeSpace(ScriptedInstanceDefinition definition, bool withExit, bool wonByPickUp = false)
        {
            _room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            _room.Sessions = _roomSessions.Object;
            var script = new Script(definition, withExit) { WonByPickUp = wonByPickUp };
            _run = new ScriptedInstanceRun(
                World.CreateTimeSpaceRunHost(new GameObject.Services.ScriptedInstanceService.ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, Logic = script }),
                script,
                new Dictionary<int, MapInstance> { [1] = _room },
                new NosCore.GameObject.Services.BattleService.NpcMonsterCatalog([]),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            _session.Character.MapInstance = _room;
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, _room.MapInstanceId);
        }

        private async Task ThePlayerEnters()
        {
            await _run.EnteredAsync(_session, _room.MapInstanceId);
            _roomSessions.Invocations.Clear();
        }

        private Task TheBossIsKilled() => _run.MonsterKilledAsync(_room.MapInstanceId, Boss);

        private Task AnotherMonsterIsKilled() => _run.MonsterKilledAsync(_room.MapInstanceId, 333);

        private Task ThePlayerDies()
        {
            _session.Character.IsAlive = false;
            _session.Character.Hp = 0;
            return _run.PlayerDownAsync(_session.Character);
        }

        private IEnumerable<MinfoPacket> WindowsShownToTheRun() =>
            _roomSessions.Invocations.SelectMany(call => call.Arguments.OfType<IPacket[]>()).SelectMany(packets => packets).OfType<MinfoPacket>();

        private void ThePlayerShouldBeShown_(MinfoPacket expected) =>
            PacketAssert.AreEqual(expected, _session.LastPackets.OfType<MinfoPacket>().Single());

        private void TheRunShouldBeShown_(MinfoPacket expected) =>
            PacketAssert.AreEqual(expected, WindowsShownToTheRun().Last());

        private void TheRunShouldBeShownNoWindow() => Assert.IsFalse(WindowsShownToTheRun().Any());
    }
}
