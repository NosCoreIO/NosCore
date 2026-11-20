//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.ServerPackets.Player;
using NodaTime;
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
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceLivesTests
    {
        private TestWorld World;
        private ClientSession _session;
        private ScriptedInstanceRun _run;
        private Mock<IScriptedInstanceService> _instances;
        private Mock<IMapChangeService> _mapChange;
        private Mock<IGameEventDispatcher> _bus;
        private readonly Mock<ISessionGroup> _roomSessions = new();

        private sealed class Script : InstanceScript
        {
            public Script(byte lives) =>
                Definition = InstanceDefinitionBuilder.Named(1, "timespace", "timespace").WithLives(lives).Build();

            public override void OnFirstEnter(IInstanceRun run) => run.StartClock(TimeSpan.FromSeconds(60));
        }

        private sealed class KeyScript : TimeSpaceScript
        {
            public KeyScript() : base(1, "timespace")
            {
                Lives(1);
                Room(2004, 0, 0).KeyAnywhere(1057);
            }
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _instances = World.Freeze<Mock<IScriptedInstanceService>>();
            _mapChange = new Mock<IMapChangeService>();
            _bus = new Mock<IGameEventDispatcher>();
            World.Inject(new Lazy<IMapChangeService>(_mapChange.Object));
            World.Inject(_bus.Object);
        }

        [TestMethod]
        public async Task ADeathWithALifeLeftRevivesThePlayer()
        {
            await new Spec("A death while the party has another life uses one, costs no dignity, and the player stands up in the run")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .WhenAsync(ThePlayerDies)
                .Then(ThePlayerShouldBeTold_RevivesAreLeft, 1)
                .And(TheRunShouldStillBeGoing)
                .And(ThePlayerShouldHave_Dignity, (short)0)
                .AndAsync(ThePlayerAnswersTheRevival)
                .Then(ThePlayerShouldBeAlive)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenPlayerIsAskedToContinueTheMission()
        {
            await new Spec("A player who falls with a life left is shown their empty bars and asked to continue the mission")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .And(ThePlayerIsLevel_, (byte)15)
                .WhenAsync(ThePlayerDies)
                .Then(ThePlayerShouldBeAskedToContinueWith_, Game18NConstString.MissionContinueLessThanLevel10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHigherLevelPlayerIsWarnedOfTheHalfHpAndMp()
        {
            await new Spec("Above level 20 the question says the mission continues with half the HP and MP")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .And(ThePlayerIsLevel_, (byte)76)
                .WhenAsync(ThePlayerDies)
                .Then(ThePlayerShouldBeAskedToContinueWith_, Game18NConstString.MissionContinueHalfHpMp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TimeAloneDoesNotBringAFallenPlayerBack()
        {
            await new Spec("A fallen player stays down until they answer the question")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .AndAsync(ThePlayerDies)
                .WhenAsync(TenSecondsPass)
                .Then(ThePlayerShouldStillBeDown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnsweringBringsThePlayerBackWithHalfTheirHpAndMp()
        {
            await new Spec("Answering the question brings the player back where they fell, with half their HP and MP")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .AndAsync(ThePlayerDies)
                .WhenAsync(ThePlayerAnswersTheRevival)
                .Then(ThePlayerShouldBeAlive)
                .And(ThePlayerShouldHaveHalfTheirHpAndMp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeathWithNoLifeLeftFailsTheRun()
        {
            await new Spec("The death that uses the party's last life fails the run")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .AndAsync(ThePlayerDies)
                .AndAsync(ThePlayerAnswersTheRevival)
                .WhenAsync(ThePlayerDies)
                .Then(TheRunShouldHaveRunOutOfLives)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATimeSpaceWithOneLifeFailsOnTheFirstDeath()
        {
            await new Spec("A time-space with a single life has nothing to revive with, so the first death fails it")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .WhenAsync(ThePlayerDies)
                .Then(TheRunShouldHaveRunOutOfLives)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ThePartySharesItsLives()
        {
            await new Spec("Lives belong to the party, so a second player's death after the first used the spare life fails the run")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .AndAsync(ThePlayerDies)
                .AndAsync(ThePlayerAnswersTheRevival)
                .WhenAsync(AnotherPlayerInTheRunDies)
                .Then(TheRunShouldHaveRunOutOfLives)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingAStartedRunCostsDignityAndALife()
        {
            await new Spec("Giving up a started time-space costs the player's level in dignity and one of the party's lives")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .And(ThePlayerIsLevel_, (byte)12)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(ThePlayerLeaves)
                .Then(ThePlayerShouldHave_Dignity, (short)-12)
                .And(ThePlayerShouldBeToldTheDignityChangedBy_, -12)
                .And(TheRunShouldStillBeGoing)
                .And(ThePlayerShouldBeSentBack)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingWithNoLifeLeftFailsTheRun()
        {
            await new Spec("Giving up a started time-space with no life left fails it, and the failure alone charges the dignity")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)0)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(ThePlayerLeaves)
                .Then(TheRunShouldHaveRunOutOfLives)
                .And(EachPlayerShouldHave_FailedAttempt, 1)
                .And(ThePlayerShouldHave_Dignity, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingBeforeTheRunStartsCostsNothing()
        {
            await new Spec("Leaving a time-space that has not started costs nothing")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)0)
                .WhenAsync(ThePlayerLeaves)
                .Then(ThePlayerShouldHave_Dignity, (short)0)
                .And(TheRunShouldStillBeGoing)
                .And(ThePlayerShouldBeSentBack)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingAnEndedRunCostsNothing()
        {
            await new Spec("Leaving a time-space that is already over costs nothing")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)0)
                .AndAsync(TheRunHasStarted)
                .And(TheRunIsWon)
                .WhenAsync(ThePlayerLeaves)
                .Then(ThePlayerShouldHave_Dignity, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DignityStopsAtTheFloor()
        {
            await new Spec("Giving up never takes dignity below the lowest band's floor")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .And(ThePlayerHas_Dignity, (short)-995)
                .And(ThePlayerIsLevel_, (byte)70)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(ThePlayerLeaves)
                .Then(ThePlayerShouldHave_Dignity, (short)-1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARunOutOfTimeScoresNothingAndFails()
        {
            await new Spec("When the clock runs out the score is all zeros and the run fails for each player inside")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(TheClockRunsOut)
                .Then(TheFailedScoreShouldShow_, EndStateType.Timeout)
                .And(EachPlayerShouldHave_FailedAttempt, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARunOutOfLivesScoresNothingAndShowsTheLivesAreSpent()
        {
            await new Spec("When the last life is used the score is all zeros and names the lives as spent")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(ThePlayerDies)
                .Then(TheFailedScoreShouldShow_, EndStateType.LivesExhausted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADoubledClockLastsTwiceAsLong()
        {
            await new Spec("A run on a doubled clock is still going when the normal time is up, and fails once the doubled time is")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .And(TheRunIsOnADoubledClock)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(TheClockRunsOut)
                .Then(TheRunShouldStillBeGoing)
                .AndAsync(TheClockRunsOut)
                .Then(EachPlayerShouldHave_FailedAttempt, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADoubledClockDoublesTimeTaken()
        {
            await new Spec("Thirty seconds taken from a doubled clock costs a minute, so the run fails when the normal time is up")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .And(TheRunIsOnADoubledClock)
                .AndAsync(TheRunHasStarted)
                .And(TheRunLoses_Seconds, 30)
                .WhenAsync(TheClockRunsOut)
                .Then(EachPlayerShouldHave_FailedAttempt, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RunningOutOfTimeCountsAFailedAttempt()
        {
            await new Spec("A run that ran out of time counts as a failed attempt for each player inside")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(TheClockRunsOut)
                .Then(EachPlayerShouldHave_FailedAttempt, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RunningOutOfLivesCountsAFailedAttempt()
        {
            await new Spec("A run that ran out of lives counts as a failed attempt for each player inside")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)0)
                .WhenAsync(ThePlayerDies)
                .Then(TheRunShouldHaveRunOutOfLives)
                .And(EachPlayerShouldHave_FailedAttempt, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWonRunIsNotAFailedAttempt()
        {
            await new Spec("A run that was won counts no failed attempt")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .AndAsync(TheRunHasStarted)
                .When(TheRunIsWon)
                .AndAsync(TenSecondsPass)
                .Then(EachPlayerShouldHave_FailedAttempt, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayersStayTwoMinutesAfterRunningOutOfTime()
        {
            await new Spec("A run that ran out of time keeps its players for two minutes before sending them back")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .AndAsync(TheRunHasStarted)
                .AndAsync(TheClockRunsOut)
                .WhenAsync(TenSecondsPass)
                .Then(ThePlayerShouldNotBeSentBack)
                .AndAsync(TwoMinutesPass)
                .Then(ThePlayerShouldBeSentBack)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedRunCostsTheLevelInDignity()
        {
            await new Spec("A player whose time-space failed, by time or by lives, loses their level in dignity and is told so")
                .Given(ThePlayerHas_Dignity, (short)100)
                .And(ThePlayerIsLevel_, (byte)3)
                .WhenAsync(TheFailureIsCharged)
                .Then(ThePlayerShouldHave_Dignity, (short)97)
                .And(ThePlayerShouldBeToldTheDignityChangedBy_, -3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedRunCostsAtMostFiftyDignity()
        {
            await new Spec("A player above level 50 whose time-space failed loses 50 dignity")
                .Given(ThePlayerHas_Dignity, (short)100)
                .And(ThePlayerIsLevel_, (byte)70)
                .WhenAsync(TheFailureIsCharged)
                .Then(ThePlayerShouldHave_Dignity, (short)50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARandomCellIsAWalkableCellOfTheRoom()
        {
            await new Spec("A room too small to keep clear of its doors still gives a walkable cell")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .Then(ARandomCellShouldBeWalkable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARandomCellFindsTheOnlyWalkableCell()
        {
            await new Spec("A room whose only walkable cell is off the corner still gives that cell")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .And(OnlyCell_2_3IsWalkable)
                .Then(ARandomCellShouldBe_, 2, 3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKeyPlacedAnywhereLiesOnAWalkableCellOfTheRoom()
        {
            await new Spec("A key placed anywhere in a room whose only walkable cell is (2, 3) lies on that cell")
                .GivenAsync(ATimeSpaceRunOf_, (IInstanceScript)new KeyScript())
                .And(OnlyCell_2_3IsWalkable)
                .WhenAsync(TheRunHasStarted)
                .Then(TheKeyShouldLieAt_, 2, 3)
                .ExecuteAsync();
        }

        private void TheKeyShouldLieAt_(int x, int y)
        {
            var key = _session.Character.MapInstance.Buttons[1];
            Assert.AreEqual(ButtonKind.Key, key.Kind);
            Assert.AreEqual((short)x, key.PositionX);
            Assert.AreEqual((short)y, key.PositionY);
            Assert.IsTrue(_session.Character.MapInstance.Map.IsWalkable(key.PositionX, key.PositionY));
        }

        private void OnlyCell_2_3IsWalkable()
        {
            var data = _session.Character.MapInstance.Map.Data;
            for (var i = 4; i < data.Length; i++)
            {
                data[i] = 1;
            }

            data[4 + 3 * 4 + 2] = 0;
        }

        private void ARandomCellShouldBe_(int x, int y) => Assert.AreEqual(new Spot((short)x, (short)y), _run.RandomCell(1));

        private void ARandomCellShouldBeWalkable()
        {
            var cell = _run.RandomCell(1);
            Assert.IsTrue(_session.Character.MapInstance.Map.IsWalkable(cell.X, cell.Y));
        }

        private Task ATimeSpaceWith_Lives(byte lives) => ATimeSpaceRunOf_(new Script(lives));

        private Task ATimeSpaceRunOf_(IInstanceScript script)
        {
            var room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            room.Sessions = _roomSessions.Object;
            _run = new ScriptedInstanceRun(
                World.CreateTimeSpaceRunHost(new GameObject.Services.ScriptedInstanceService.ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, Logic = script }),
                script,
                new Dictionary<int, MapInstance> { [1] = room },
                new NosCore.GameObject.Services.BattleService.NpcMonsterCatalog([]),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            GameObject.Services.MapChangeService.MapChangeService.MovePlayer(_session, _session.Character.MapInstance, room, null, null);
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, room.MapInstanceId);
            _instances.Setup(s => s.GetRun(room.MapInstanceId)).Returns(_run);
            return Task.CompletedTask;
        }

        private Task TheRunHasStarted() => _run.StartAsync();

        private void TheRunIsOnADoubledClock() => _run.ClockScale = 2;

        private void TheRunLoses_Seconds(int seconds) => _run.AddClockTime(TimeSpan.FromSeconds(-seconds));

        private void TheRunIsWon() => _run.Succeed();

        private void ThePlayerHas_Dignity(short dignity)
        {
            _session.Character.Dignity = dignity;
        }

        private void ThePlayerIsLevel_(byte level)
        {
            _session.Character.Level = level;
        }

        private async Task ThePlayerDies()
        {
            await TheRunHasStarted();
            _session.LastPackets.Clear();
            _session.Character.IsAlive = false;
            _session.Character.Hp = 0;
            await _run.PlayerDownAsync(_session.Character);
        }

        private async Task AnotherPlayerInTheRunDies()
        {
            var other = await World.CreateSessionAsync();
            other.Character.MapInstance = _session.Character.MapInstance;
            other.Character.IsAlive = false;
            other.Character.Hp = 0;
            await _run.PlayerDownAsync(other.Character);
        }

        private Task ThePlayerAnswersTheRevival()
        {
            _session.LastPackets.Clear();
            return _run.RevivalAnsweredAsync(_session.Character);
        }

        private void ThePlayerShouldBeAskedToContinueWith_(Game18NConstString question)
        {
            PacketAssert.AreEqual(_session.Character.GenerateStat(), _session.LastPackets.OfType<StatPacket>().Last());
            PacketAssert.AreEqual(new DlgiPacket
            {
                YesPacket = new RevivalPacket { Type = RevivalType.AtSavePoint },
                NoPacket = new RevivalPacket { Type = RevivalType.AtSavePoint },
                Question = question,
            }, _session.LastPackets.OfType<DlgiPacket>().Single());
        }

        private void ThePlayerShouldStillBeDown()
        {
            Assert.IsFalse(_session.Character.IsAlive);
            Assert.IsTrue(_run.IsDown(_session.Character.CharacterId));
        }

        private void ThePlayerShouldHaveHalfTheirHpAndMp()
        {
            Assert.AreEqual(Math.Max(1, _session.Character.MaxHp / 2), _session.Character.Hp);
            Assert.AreEqual(Math.Max(1, _session.Character.MaxMp / 2), _session.Character.Mp);
        }

        private async Task TenSecondsPass()
        {
            World.Clock.Advance(Duration.FromSeconds(10));
            await _run.TickAsync();
        }

        private async Task TheClockRunsOut()
        {
            _session.LastPackets.Clear();
            World.Clock.Advance(Duration.FromSeconds(60));
            await _run.TickAsync();
        }

        private async Task TwoMinutesPass()
        {
            World.Clock.Advance(Duration.FromSeconds(110));
            await _run.TickAsync();
        }

        private Task TheFailureIsCharged()
        {
            _session.LastPackets.Clear();
            return World.Create<TimeSpaceFailedHandler>().Handle(new TimeSpaceFailedEvent(_session.Character, 1));
        }

        private void TheFailedScoreShouldShow_(EndStateType state)
        {
            var broadcast = _roomSessions.Invocations.SelectMany(call => call.Arguments.OfType<IPacket[]>()).SelectMany(packets => packets);
            PacketAssert.AreEqual(new ScoreFailedPacket { EndState = state }, broadcast.OfType<ScoreFailedPacket>().Single());
        }

        private void EachPlayerShouldHave_FailedAttempt(int count)
        {
            var characterId = _session.Character.CharacterId;
            _bus.Verify(b => b.RaiseAsync(It.Is<TimeSpaceFailedEvent>(e => e.Character.CharacterId == characterId && e.TimeSpaceId == 1)),
                Times.Exactly(count));
        }

        private void ThePlayerShouldNotBeSentBack()
        {
            _mapChange.Verify(m => m.ChangeMapAsync(_session, It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);
        }

        private async Task ThePlayerLeaves()
        {
            _session.LastPackets.Clear();
            await World.Create<InstanceExitService>().LeaveAsync(_session);
        }

        private void ThePlayerShouldBeTold_RevivesAreLeft(int lives)
        {
            var told = _session.LastPackets.OfType<MsgiPacket>()
                .Single(packet => packet.Message == Game18NConstString.YouCanBeRevivedMoreTime);
            Assert.AreEqual(lives, Convert.ToInt32(told.Game18NArguments.Single()));
        }

        private void TheRunShouldStillBeGoing()
        {
            Assert.IsNull(_run.EndState);
        }

        private void TheRunShouldHaveRunOutOfLives()
        {
            Assert.AreEqual(EndStateType.LivesExhausted, _run.EndState);
        }

        private void ThePlayerShouldBeAlive()
        {
            Assert.IsTrue(_session.Character.IsAlive);
            Assert.IsFalse(_run.IsDown(_session.Character.CharacterId));
        }

        private void ThePlayerShouldHave_Dignity(short dignity)
        {
            Assert.AreEqual(dignity, _session.Character.Dignity);
        }

        private void ThePlayerShouldBeToldTheDignityChangedBy_(int change)
        {
            var say = _session.LastPackets.OfType<SayiPacket>().Single(packet => packet.Message == Game18NConstString.DignityReduced);
            Assert.AreEqual(change, Convert.ToInt32(say.Game18NArguments.Single()));
        }

        private void ThePlayerShouldBeSentBack()
        {
            _mapChange.Verify(m => m.ChangeMapAsync(_session, It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Once);
        }
    }
}
