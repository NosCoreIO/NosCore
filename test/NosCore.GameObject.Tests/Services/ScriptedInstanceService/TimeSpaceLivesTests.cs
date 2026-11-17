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
using NosCore.Packets.ClientPackets.Instance;
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

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _instances = World.Freeze<Mock<IScriptedInstanceService>>();
            _mapChange = new Mock<IMapChangeService>();
            _bus = new Mock<IGameEventDispatcher>();
        }

        [TestMethod]
        public async Task ADeathWithALifeLeftRevivesThePlayer()
        {
            await new Spec("A death while the party has another life uses one, and the player stands up in the run")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .WhenAsync(ThePlayerDies)
                .Then(ThePlayerShouldBeTold_RevivesAreLeft, 1)
                .And(TheRunShouldStillBeGoing)
                .AndAsync(TenSecondsPass)
                .Then(ThePlayerShouldBeAlive)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeathWithNoLifeLeftFailsTheRun()
        {
            await new Spec("The death that uses the party's last life fails the run")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)2)
                .AndAsync(ThePlayerDies)
                .AndAsync(TenSecondsPass)
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
                .AndAsync(TenSecondsPass)
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
            await new Spec("Giving up a started time-space with no life left fails it for the others")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)0)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(ThePlayerLeaves)
                .Then(TheRunShouldHaveRunOutOfLives)
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
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .And(ThePlayerHas_Dignity, (short)-995)
                .And(ThePlayerIsLevel_, (byte)70)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(ThePlayerLeaves)
                .Then(ThePlayerShouldHave_Dignity, (short)-1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARunOutOfTimeScoresNothingAndCostsDignity()
        {
            await new Spec("When the clock runs out the score is all zeros and each player inside loses dignity")
                .GivenAsync(ATimeSpaceWith_Lives, (byte)1)
                .AndAsync(TheRunHasStarted)
                .WhenAsync(TheClockRunsOut)
                .Then(TheScoreShouldBeEmpty)
                .And(EachPlayerShouldBeChargedForTheTimeout)
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
        public async Task RunningOutOfTimeCostsTheLevelInDignity()
        {
            await new Spec("A player whose time-space ran out of time loses their level in dignity and is told so")
                .Given(ThePlayerHas_Dignity, (short)100)
                .And(ThePlayerIsLevel_, (byte)3)
                .WhenAsync(TheTimeoutIsCharged)
                .Then(ThePlayerShouldHave_Dignity, (short)97)
                .And(ThePlayerShouldBeToldTheDignityChangedBy_, -3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RunningOutOfTimeCostsAtMostFiftyDignity()
        {
            await new Spec("A player above level 50 whose time-space ran out of time loses 50 dignity")
                .Given(ThePlayerHas_Dignity, (short)100)
                .And(ThePlayerIsLevel_, (byte)70)
                .WhenAsync(TheTimeoutIsCharged)
                .Then(ThePlayerShouldHave_Dignity, (short)50)
                .ExecuteAsync();
        }

        private Task ATimeSpaceWith_Lives(byte lives)
        {
            var room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            room.Sessions = _roomSessions.Object;
            var script = new Script(lives);
            _run = new ScriptedInstanceRun(
                new TimeSpaceRunHost(
                    new GameObject.Services.ScriptedInstanceService.ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, Logic = script },
                    new Lazy<IMapChangeService>(_mapChange.Object), _bus.Object, World.Clock),
                script,
                new Dictionary<int, MapInstance> { [1] = room },
                new Dictionary<short, NpcMonsterDto>(),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            _session.Character.MapInstance = room;
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, room.MapInstanceId);
            _instances.Setup(s => s.GetRun(room.MapInstanceId)).Returns(_run);
            return Task.CompletedTask;
        }

        private Task TheRunHasStarted() => _run.StartAsync();

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

        private Task TheTimeoutIsCharged()
        {
            _session.LastPackets.Clear();
            return World.Create<TimeSpaceTimedOutHandler>().Handle(new ScriptedInstanceTimedOutEvent(_session.Character));
        }

        private void TheScoreShouldBeEmpty()
        {
            var broadcast = _roomSessions.Invocations.SelectMany(call => call.Arguments.OfType<IPacket[]>()).SelectMany(packets => packets);
            PacketAssert.AreEqual(new ScorePacket { EndState = EndStateType.Timeout }, broadcast.OfType<ScorePacket>().Single());
        }

        private void EachPlayerShouldBeChargedForTheTimeout()
        {
            var characterId = _session.Character.CharacterId;
            _bus.Verify(b => b.RaiseAsync(It.Is<ScriptedInstanceTimedOutEvent>(e => IsFor(e, characterId))), Times.Once);
        }

        private void EachPlayerShouldHave_FailedAttempt(int count)
        {
            var characterId = _session.Character.CharacterId;
            _bus.Verify(b => b.RaiseAsync(It.Is<TimeSpaceFailedEvent>(e => e.Character.CharacterId == characterId && e.TimeSpaceId == 1)),
                Times.Exactly(count));
        }

        private static bool IsFor(ScriptedInstanceTimedOutEvent timedOut, long characterId) =>
            timedOut.Character.CharacterId == characterId;

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
