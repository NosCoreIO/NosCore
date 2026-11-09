//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
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
        private const short RaidMap = 2510;

        private sealed class Raid : InstanceScript
        {
            public Raid() => Definition = InstanceDefinitionBuilder.Named(0, "raid", "raid")
                .ForLevels(30, 49)
                .ForTeams(2, 3)
                .Requiring(Seal, 1)
                .StartingAt(1, 1)
                .WithRoom(RaidMap, out _)
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

        private readonly List<ClientSession> _players = [];
        private ScriptedInstances.ScriptedInstanceService _instances = null!;
        private ServiceUnderTest _raids = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            for (var i = 0; i < 4; i++)
            {
                var session = await TestHelpers.Instance.GenerateSessionAsync();
                session.Character.Level = 40;
                TestHelpers.Instance.SessionRegistry.Register(new SessionInfo
                {
                    ChannelId = session.Channel!.Id,
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
            _instances = new ScriptedInstances.ScriptedInstanceService(
                [new ScriptedInstanceDto { ScriptedInstanceId = 7, MapId = 0, PositionX = 2, PositionY = 2, Type = ScriptedInstanceType.Raid, Script = "raid" }],
                [new MapDto { MapId = RaidMap, NameI18NKey = "raidMap", Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0] }],
                [new NpcMonsterDto { NpcMonsterVNum = Boss, MaxHp = 10 }],
                new Lazy<IMapInstanceGeneratorService>(TestHelpers.Instance.MapInstanceGeneratorService),
                TestHelpers.Instance.MapInstanceRegistry, loader.Object, TestHelpers.Instance.SessionRegistry,
                new Lazy<IMapChangeService>(TestHelpers.Instance.MapChangeService), TestHelpers.Instance.Clock,
                NullLogger<ScriptedInstances.ScriptedInstanceService>.Instance);
            _raids = new ServiceUnderTest(_instances, TestHelpers.Instance.MapChangeService,
                TestHelpers.Instance.SessionRegistry, TestHelpers.Instance.SessionGroupFactory, new IdService<Group>(1),
                TestHelpers.Instance.GenerateItemProvider(), NullLogger<ServiceUnderTest>.Instance);
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
        public async Task AWipedTeamFails()
        {
            await new Spec("The raid fails once every member is dead, and nobody is rewarded")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .AndAsync(Player_DiesAsync, 1)
                .WhenAsync(Player_DiesAsync, 0)
                .Then(TheRaidEnded_, EndStateType.Fail)
                .And(NobodyHasTheBox)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OneDeathIsNotAWipe()
        {
            await new Spec("A member dying while another still stands does not end the raid")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .WhenAsync(Player_DiesAsync, 1)
                .Then(TheRaidIsStillRunning)
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

        [TestMethod]
        public async Task TheLastDisconnectTearsTheRaidDown()
        {
            await new Spec("When the last member disconnects the raid's rooms are removed")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .AndAsync(Player_DisconnectsAsync, 1)
                .WhenAsync(Player_DisconnectsAsync, 0)
                .Then(TheRaidRoomsAreGone)
                .ExecuteAsync();
        }

        private ClientSession Leader => _players[0];

        private Group Team => Leader.Character.Group;

        private ScriptedInstances.ScriptedInstanceRun? _run;

        private async Task Player_UsesTheSealAsync(int player)
        {
            var character = _players[player].Character;
            var seal = character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                TestHelpers.Instance.GenerateItemProvider().Create(Seal, 1), character.CharacterId))!.First();
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

        private Task TheBossDiesAsync() => _run!.MonsterKilledAsync(_run.Rooms.Values.First(), Boss);

        private async Task Player_DiesAsync(int player)
        {
            var character = _players[player].Character;
            await character.MapInstance.Executor.RunAsync(() =>
            {
                character.IsAlive = false;
                return Task.CompletedTask;
            });
            await _run!.MemberDiedAsync(character.CharacterId);
        }

        private async Task FortyMinutesPassAsync()
        {
            TestHelpers.Instance.Clock.Advance(Duration.FromMinutes(41));
            await _run!.TickAsync();
        }

        private async Task Player_DisconnectsAsync(int player)
        {
            var session = _players[player];
            session.Character.MapInstance.Sessions.Remove(session.Channel!);
            await _raids.DisconnectAsync(session);
            TestHelpers.Instance.SessionRegistry.Unregister(session.Channel!.Id);
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
            Assert.IsNotNull(_run);
            var room = _run!.Rooms.Values.First();
            foreach (var player in _players.Take(2))
            {
                Assert.AreEqual(room, player.Character.MapInstance.MapInstanceId);
            }
        }

        private void TheRaidEnded_(EndStateType state) => Assert.AreEqual(state, _run!.EndState);

        private void TheRaidIsStillRunning() => Assert.IsNull(_run!.EndState);

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

        private void TheRaidRoomsAreGone()
        {
            foreach (var room in _run!.Rooms.Values)
            {
                Assert.IsNull(_instances.GetRun(room));
                Assert.IsNull(TestHelpers.Instance.MapInstanceRegistry.GetById(room));
            }
        }
    }
}
