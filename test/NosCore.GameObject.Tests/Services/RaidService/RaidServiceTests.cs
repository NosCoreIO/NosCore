//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Helpers;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScriptedInstances = NosCore.GameObject.Services.ScriptedInstanceService;
using ServiceUnderTest = NosCore.GameObject.Services.RaidService.RaidService;

namespace NosCore.GameObject.Tests.Services.RaidService
{
    [TestClass]
    public class RaidServiceTests
    {
        private const short Seal = 1012;
        private const short Box = 1013;
        private const short Boss = 282;

        private sealed class Raid : InstanceScript
        {
            public Raid() => Definition = InstanceDefinitionBuilder.Named(0, "raid", "raid")
                .ForLevels(30, 49)
                .ForTeams(2, 3)
                .Requiring(Seal, 1)
                .StartingAt(1, 1)
                .WithRoom(TestWorld.TestMapId, out _)
                .WithReward(Box, 1)
                .Build();

            public override void OnFirstEnter(IInstanceRun run) => run.StartClock(TimeSpan.FromMinutes(40));

            public override void OnMonsterKilled(IInstanceRun run, int room, short vNum)
            {
                if (vNum == Boss)
                {
                    run.Succeed();
                }
            }

            public override void OnTimeout(IInstanceRun run) => run.Fail();
        }

        private TestWorld World;
        private readonly List<ClientSession> _players = [];
        private ServiceUnderTest _raids;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            for (var i = 0; i < 4; i++)
            {
                var session = await World.CreateSessionAsync();
                session.Character.Level = 40;
                World.SessionRegistry.Register(new SessionInfo
                {
                    ChannelId = session.Channel.NotNull().Id,
                    SessionId = session.SessionId,
                    Sender = session,
                    AccountName = session.Account.Name,
                    Disconnect = () => Task.CompletedTask,
                    CharacterId = session.Character.CharacterId,
                    MapInstanceId = session.Character.MapInstance.MapInstanceId,
                });
                _players.Add(session);
            }

            var loader = new Mock<IInstanceScriptLoader>();
            loader.Setup(s => s.LoadAsync("raid")).ReturnsAsync(new Raid());
            World.Inject(loader.Object);
            World.Inject(new List<ScriptedInstanceDto>
            {
                new() { ScriptedInstanceId = 7, MapId = 1, PositionX = 2, PositionY = 2, Type = ScriptedInstanceType.Raid, Script = "raid" }
            });
            World.Inject(new List<NpcMonsterDto> { new() { NpcMonsterVNum = Boss, MaxHp = 10 } });
            World.Inject(new Lazy<IMapInstanceGeneratorService>(World.MapInstanceGeneratorService));
            World.Inject(new Lazy<IMapChangeService>(World.MapChangeService));
            World.Inject<ScriptedInstances.IScriptedInstanceService>(World.Create<ScriptedInstances.ScriptedInstanceService>());
            _raids = World.Create<ServiceUnderTest>();
        }

        [TestMethod]
        public async Task ASealOpensATeamLedByWhoeverUsedIt()
        {
            await new Spec("Using a raid seal makes its user the leader of a team for that raid")
                .WhenAsync(Player_UsesTheSealAsync, 0)
                .Then(Player_LeadsTheTeam, 0)
                .And(TheSealIsUsedUp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnInvitedPlayerJoinsTheTeam()
        {
            await new Spec("A player the leader invited joins the team on answering")
                .GivenAsync(Player_UsesTheSealAsync, 0)
                .AndAsync(TheLeaderInvites_Async, 1)
                .WhenAsync(Player_AnswersTheLeaderAsync, 1)
                .Then(Player_IsInTheTeam, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NobodyJoinsWithoutAnInvitation()
        {
            await new Spec("A player who was not invited cannot join")
                .GivenAsync(Player_UsesTheSealAsync, 0)
                .WhenAsync(Player_AnswersTheLeaderAsync, 1)
                .Then(Player_IsNotInTheTeam, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMemberWhoLeavesIsOnTheirOwnAgain()
        {
            await new Spec("A member who leaves goes back to being alone and the team keeps its leader")
                .GivenAsync(ATeamOf_Async, 3)
                .WhenAsync(Player_LeavesAsync, 2)
                .Then(Player_IsNotInTheTeam, 2)
                .And(Player_LeadsTheTeam, 0)
                .And(TheTeamHas_Members, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKickedMemberIsOutOfTheTeam()
        {
            await new Spec("The leader can kick a member before the raid starts")
                .GivenAsync(ATeamOf_Async, 3)
                .WhenAsync(TheLeaderKicks_Async, 1)
                .Then(Player_IsNotInTheTeam, 1)
                .And(TheTeamHas_Members, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALeaderAloneCannotStart()
        {
            await new Spec("The raid does not start below its team size")
                .GivenAsync(ATeamOf_Async, 1)
                .WhenAsync(TheLeaderStartsAsync)
                .Then(TheRaidHasNotStarted)
                .And(TheLeaderWasTold_, Game18NConstString.TooFewRaidMember)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMemberOutOfTheLevelRangeHoldsTheStart()
        {
            await new Spec("The raid does not start while a member is outside its level range")
                .GivenAsync(ATeamOf_Async, 2)
                .And(Player_IsLevel_, 1, (byte)20)
                .WhenAsync(TheLeaderStartsAsync)
                .Then(TheRaidHasNotStarted)
                .And(TheLeaderWasTold_, Game18NConstString.LevelDoesNotMatch)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StartingTakesTheWholeTeamIn()
        {
            await new Spec("Starting moves every member into the raid's first room")
                .GivenAsync(ATeamOf_Async, 2)
                .WhenAsync(TheLeaderStartsAsync)
                .Then(EveryMemberIsInTheRaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KillingTheBossRewardsEveryMember()
        {
            await new Spec("Killing the boss wins the raid and gives every member its box")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .WhenAsync(TheBossDiesAsync)
                .Then(TheRaidEnded_, EndStateType.Success)
                .And(EveryMemberHasTheBox)
                .And(TheTeamIsDissolved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenMemberStandsUpTwentySecondsLater()
        {
            await new Spec("A member who falls for the first time is revived with full HP twenty seconds later")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .AndAsync(Player_FallsAsync, 1)
                .WhenAsync(TwentySecondsPassAsync)
                .Then(Player_IsStandingWithFullHp, 1)
                .And(Player_WasTold_, 1, Game18NConstString.MessageRevivedInTwentySecondWithAnotherDeathAvailable)
                .And(TheRaidIsStillRunning)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenMemberWaitsBeforeStandingUp()
        {
            await new Spec("A fallen member is not revived before twenty seconds have passed")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .AndAsync(Player_FallsAsync, 1)
                .WhenAsync(TenSecondsPassAsync)
                .Then(Player_IsStillDown, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASecondFallIsTheLastWarning()
        {
            await new Spec("A member falling a second time is warned the next fall takes them out")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .AndAsync(Player_FallsAndStandsUpAsync, 1)
                .WhenAsync(Player_FallsAsync, 1)
                .Then(Player_WasTold_, 1, Game18NConstString.MessageRevivedInTwentySecondWithNoMoreDeathsAvailable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AThirdFallTakesAMemberOutOfTheRaid()
        {
            await new Spec("A member falling a third time has used all their lives and leaves the team and the raid")
                .GivenAsync(AStartedRaidOf_Async, 3)
                .AndAsync(Player_FallsAndStandsUpAsync, 2)
                .AndAsync(Player_FallsAndStandsUpAsync, 2)
                .WhenAsync(Player_FallsAsync, 2)
                .Then(Player_WasTold_, 2, Game18NConstString.FinishedLives)
                .And(Player_IsNotInTheTeam, 2)
                .And(Player_IsOutOfTheRaidAndAlive, 2)
                .And(TheRaidIsStillRunning)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RunningOutOfTimeFails()
        {
            await new Spec("The raid fails when its clock runs out")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .WhenAsync(FortyMinutesPassAsync)
                .Then(TheRaidEnded_, EndStateType.Fail)
                .And(NobodyHasTheBox)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADisconnectedMemberLeavesTheTeam()
        {
            await new Spec("A member who disconnects inside the raid is taken out of the team")
                .GivenAsync(AStartedRaidOf_Async, 3)
                .WhenAsync(Player_DisconnectsAsync, 2)
                .Then(Player_IsNotInTheTeam, 2)
                .And(TheRaidIsStillRunning)
                .ExecuteAsync();
        }

        private ClientSession Leader => _players[0];

        private Group Team => Leader.Character.Group;

        private ScriptedInstances.ScriptedInstanceRun? _run;

        private async Task Player_UsesTheSealAsync(int player)
        {
            var character = _players[player].Character;
            var seal = character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(Seal, 1), character.CharacterId)).NotNull().First();
            await _raids.OpenAsync(_players[player], seal);
        }

        private Task TheLeaderInvites_Async(int player) =>
            _raids.InviteAsync(Leader, _players[player].Character.CharacterId);

        private Task Player_AnswersTheLeaderAsync(int player) =>
            _raids.JoinAsync(_players[player], Leader.Character.CharacterId);

        private Task Player_LeavesAsync(int player) => _raids.LeaveAsync(_players[player]);

        private Task TheLeaderKicks_Async(int player) => _raids.KickAsync(Leader, _players[player].Character.CharacterId);

        private async Task ATeamOf_Async(int size)
        {
            await Player_UsesTheSealAsync(0);
            for (var i = 1; i < size; i++)
            {
                await TheLeaderInvites_Async(i);
                await Player_AnswersTheLeaderAsync(i);
            }
        }

        private async Task TheLeaderStartsAsync()
        {
            await _raids.StartAsync(Leader);
            _run = Team.RaidRun;
        }

        private async Task AStartedRaidOf_Async(int size)
        {
            await ATeamOf_Async(size);
            await TheLeaderStartsAsync();
            Assert.IsNotNull(_run);
        }

        private ScriptedInstances.ScriptedInstanceRun Run => _run.NotNull();

        private Task TheBossDiesAsync() => Run.MonsterKilledAsync(Run.Rooms.Values.First(), Boss);

        private async Task Player_FallsAsync(int player)
        {
            var character = _players[player].Character;
            await character.MapInstance.Executor.RunAsync(() =>
            {
                character.Hp = 0;
                character.IsAlive = false;
                return Task.CompletedTask;
            });
            await Run.PlayerDownAsync(character);
        }

        private async Task Player_FallsAndStandsUpAsync(int player)
        {
            await Player_FallsAsync(player);
            await TwentySecondsPassAsync();
        }

        private async Task TenSecondsPassAsync()
        {
            World.Clock.Advance(Duration.FromSeconds(10));
            await Run.TickAsync();
        }

        private async Task TwentySecondsPassAsync()
        {
            World.Clock.Advance(Duration.FromSeconds(20));
            await Run.TickAsync();
        }

        private async Task FortyMinutesPassAsync()
        {
            World.Clock.Advance(Duration.FromMinutes(41));
            await Run.TickAsync();
        }

        private async Task Player_DisconnectsAsync(int player)
        {
            var session = _players[player];
            var channel = session.Channel.NotNull();
            session.Character.MapInstance.Sessions.Remove(channel);
            await _raids.DisconnectAsync(session);
            World.SessionRegistry.Unregister(channel.Id);
        }

        private void Player_IsLevel_(int player, byte level) => _players[player].Character.Level = level;

        private void Player_LeadsTheTeam(int player)
        {
            var team = _players[player].Character.Group;
            Assert.IsNotNull(team.Raid);
            Assert.AreEqual(_players[player].Character.CharacterId, team.Leader?.VisualId);
        }

        private void TheSealIsUsedUp() =>
            Assert.AreEqual(0, Leader.Character.InventoryService.CountItem(Seal));

        private void Player_IsInTheTeam(int player) =>
            Assert.AreSame(Team, _players[player].Character.Group);

        private void Player_IsNotInTheTeam(int player)
        {
            Assert.IsFalse(Team.IsMember(_players[player].Character.CharacterId));
            Assert.IsNull(_players[player].Character.Group.Raid);
        }

        private void TheTeamHas_Members(int count) => Assert.AreEqual(count, Team.Count);

        private void TheRaidHasNotStarted() => Assert.IsNull(Team.RaidRun);

        private void TheLeaderWasTold_(Game18NConstString message) =>
            Assert.IsTrue(Leader.LastPackets.OfType<MsgiPacket>().Any(s => s.Message == message));

        private void EveryMemberIsInTheRaid()
        {
            var room = Run.Rooms.Values.First();
            foreach (var player in _players.Take(2))
            {
                Assert.AreEqual(room, player.Character.MapInstance.MapInstanceId);
            }
        }

        private void TheRaidEnded_(EndStateType state) => Assert.AreEqual(state, Run.EndState);

        private void TheRaidIsStillRunning() => Assert.IsNull(Run.EndState);

        private void Player_IsStandingWithFullHp(int player)
        {
            var character = _players[player].Character;
            Assert.IsTrue(character.IsAlive);
            Assert.AreEqual(character.MaxHp, character.Hp);
            Assert.IsFalse(Run.IsDown(character.CharacterId));
        }

        private void Player_IsStillDown(int player)
        {
            Assert.IsFalse(_players[player].Character.IsAlive);
            Assert.IsTrue(Run.IsDown(_players[player].Character.CharacterId));
        }

        private void Player_WasTold_(int player, Game18NConstString message) =>
            Assert.IsTrue(_players[player].LastPackets.OfType<MsgiPacket>().Any(s => s.Message == message));

        private void Player_IsOutOfTheRaidAndAlive(int player)
        {
            var character = _players[player].Character;
            Assert.IsFalse(Run.Rooms.Values.Contains(character.MapInstance.MapInstanceId));
            Assert.IsTrue(character.IsAlive);
        }

        private void EveryMemberHasTheBox()
        {
            foreach (var player in _players.Take(2))
            {
                Assert.AreEqual(1, player.Character.InventoryService.CountItem(Box));
            }
        }

        private void NobodyHasTheBox() =>
            Assert.IsTrue(_players.All(s => s.Character.InventoryService.CountItem(Box) == 0));

        private void TheTeamIsDissolved()
        {
            foreach (var player in _players.Take(2))
            {
                Assert.IsNull(player.Character.Group.Raid);
            }
        }
    }
}
