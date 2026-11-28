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
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.Inventory;
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
        private const short BoxesWon = 2;
        private const byte HudValue = 25;

        private sealed class Raid : InstanceScript
        {
            private readonly int _lair;

            public Raid(bool inAct6)
            {
                var builder = InstanceDefinitionBuilder.Named(0, "raid", "raid")
                    .ForLevels(30, 49)
                    .ForTeams(2, 3)
                    .Requiring(Seal, 1)
                    .StartingAt(1, 1)
                    .WithRoom(TestWorld.TestMapId, out _lair)
                    .WithReward(Box, BoxesWon)
                    .WithRaidHudValue(HudValue);
                Definition = (inAct6 ? builder.InAct6() : builder).Build();
            }

            public override void OnFirstEnter(IInstanceRun run)
            {
                run.StartClock(TimeSpan.FromMinutes(40));
                run.SetMonsterLocker(_lair, 1, Boss);
                run.Summon(_lair, new MonsterSpawn(Boss, new Spot(3, 3), IsBoss: true));
            }

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
        private bool _inAct6;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            for (var i = 0; i < 4; i++)
            {
                var session = await World.CreateSessionAsync();
                session.Character.Level = 40;
                session.Character.PositionX = 2;
                session.Character.PositionY = 2;
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
            loader.Setup(s => s.LoadAsync("raid")).ReturnsAsync(() => new Raid(_inAct6));
            World.Inject(loader.Object);
            World.Inject(new List<ScriptedInstanceDto>
            {
                new() { ScriptedInstanceId = 7, MapId = TestWorld.TestMapId, PositionX = 2, PositionY = 2, Type = ScriptedInstanceType.Raid, Script = "raid" }
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
        public async Task EveryMemberSeesTheTeamWindow()
        {
            await new Spec("Every member's raid team window lists the team, leader first")
                .GivenAsync(ATeamOf_Async, 2)
                .Then(Player_SeesTheTeam_, 0, new[] { 0, 1 })
                .And(Player_SeesTheTeam_, 1, new[] { 0, 1 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallIsCountedOnTheTeamWindow()
        {
            await new Spec("The team window counts each member's falls in the run")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .WhenAsync(Player_FallsAsync, 1)
                .Then(Player_ShowsFalls_, 1, 1)
                .And(Player_ShowsFalls_, 0, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AListedTeamIsOnTheBoard()
        {
            await new Spec("A team its leader listed is on the raid board")
                .GivenAsync(Player_UsesTheSealAsync, 0)
                .AndAsync(TheLeaderListsTheTeamAsync)
                .WhenAsync(Player_OpensTheBoardAsync, 1)
                .Then(Player_SeesTheLeaderOnTheBoard, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnlistedTeamIsOffTheBoard()
        {
            await new Spec("Taking a team off the board hides it")
                .GivenAsync(Player_UsesTheSealAsync, 0)
                .AndAsync(TheLeaderListsTheTeamAsync)
                .AndAsync(TheLeaderUnlistsTheTeamAsync)
                .WhenAsync(Player_OpensTheBoardAsync, 1)
                .Then(Player_SeesAnEmptyBoard, 1)
                .And(TheLeaderWasTold_, Game18NConstString.RemovedFromRaidList)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AListedTeamTakesAnyoneFromTheBoard()
        {
            await new Spec("A player joins a listed team from the board without an invitation")
                .GivenAsync(Player_UsesTheSealAsync, 0)
                .AndAsync(TheLeaderListsTheTeamAsync)
                .WhenAsync(Player_JoinsFromTheBoardAsync, 1)
                .Then(Player_IsInTheTeam, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnlistedTeamTakesNobodyFromTheBoard()
        {
            await new Spec("A team that is not on the board cannot be joined from it")
                .GivenAsync(Player_UsesTheSealAsync, 0)
                .WhenAsync(Player_JoinsFromTheBoardAsync, 1)
                .Then(Player_IsNotInTheTeam, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStartedTeamLeavesTheBoard()
        {
            await new Spec("A team is off the board once its raid has started")
                .GivenAsync(ATeamOf_Async, 2)
                .AndAsync(TheLeaderListsTheTeamAsync)
                .AndAsync(TheLeaderStartsAsync)
                .WhenAsync(Player_OpensTheBoardAsync, 2)
                .Then(Player_SeesAnEmptyBoard, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheLeaderAtTheGateIsAskedToOpenTheRaid()
        {
            await new Spec("Stepping on the raid's gate asks its team leader whether to open the raid")
                .GivenAsync(ATeamOf_Async, 2)
                .WhenAsync(Player_StepsOnTheGateAsync, 0)
                .Then(Player_IsAskedToOpenTheRaid, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyTheLeaderOpensTheRaidFromTheGate()
        {
            await new Spec("A member who is not the leader is told the leader must lead the raid")
                .GivenAsync(ATeamOf_Async, 2)
                .WhenAsync(Player_StepsOnTheGateAsync, 1)
                .Then(Player_WasTold_, 1, Game18NConstString.LeaderMustLeadRaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheGateNeedsATeam()
        {
            await new Spec("Stepping on the gate without a raid team says a team comes first")
                .WhenAsync(Player_StepsOnTheGateAsync, 3)
                .Then(Player_WasTold_, 3, Game18NConstString.CreateTeamFirst)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheRaidStartsOnlyFromItsGate()
        {
            await new Spec("A leader away from the gate cannot start the raid")
                .GivenAsync(ATeamOf_Async, 2)
                .And(TheLeaderWalksAwayFromTheGate)
                .WhenAsync(TheLeaderStartsAsync)
                .Then(TheRaidHasNotStarted)
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
        public async Task AnInvitationLapsesWhenItsTeamIsGone()
        {
            await new Spec("An invitation is for the team it was sent from, not the next team its sender leads")
                .GivenAsync(Player_UsesTheSealAsync, 0)
                .AndAsync(TheLeaderInvites_Async, 1)
                .AndAsync(Player_LeavesAsync, 0)
                .AndAsync(Player_UsesTheSealAsync, 0)
                .WhenAsync(Player_AnswersTheLeaderAsync, 1)
                .Then(Player_IsNotInTheTeam, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATeamOfTwoIsLeftThroughTheRaidOnDisconnect()
        {
            await new Spec("A disconnecting member of a two-member raid team does not disband it as a party")
                .GivenAsync(ATeamOf_Async, 2)
                .WhenAsync(Player_DropsAsAGroupMemberAsync, 1)
                .Then(TheTeamHas_Members, 2)
                .And(Player_IsInTheTeam, 1)
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
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARewardSplitOverTwoStacksShowsBothSlots()
        {
            await new Spec("A reward that tops up a stack and spills into a new slot shows both slots")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .And(Player_Holds_Boxes, 1, 998)
                .WhenAsync(TheBossDiesAsync)
                .Then(Player_IsShownSlot_With_, 1, 0, 999)
                .And(Player_IsShownSlot_With_, 1, 1, 1)
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

        private Task Player_StepsOnTheGateAsync(int player) => _raids.UseGateAsync(_players[player]);

        private void TheLeaderWalksAwayFromTheGate() => Leader.Character.PositionX = 9;

        private void Player_IsAskedToOpenTheRaid(int player) =>
            Assert.IsTrue(_players[player].LastPackets.OfType<QnaiPacket>()
                .Any(s => s.Question == Game18NConstString.AskCreateRaidMap && s.YesPacket is MkRaidPacket));

        private Task TheLeaderListsTheTeamAsync() => _raids.ListAsync(Leader);

        private Task TheLeaderUnlistsTheTeamAsync() => _raids.UnlistAsync(Leader);

        private Task Player_OpensTheBoardAsync(int player) => _raids.ShowBoardAsync(_players[player]);

        private Task Player_JoinsFromTheBoardAsync(int player) =>
            _raids.JoinListedAsync(_players[player], Leader.Character.Name);

        private void Player_SeesTheTeam_(int player, int[] members)
        {
            var window = _players[player].LastPackets.OfType<RdlstPacket>().Last();
            CollectionAssert.AreEqual(members.Select(s => _players[s].Character.CharacterId).ToList(),
                window.Members.NotNull().Select(s => s.NotNull().CharacterId).ToList());
        }

        private void Player_ShowsFalls_(int player, int falls)
        {
            var id = _players[player].Character.CharacterId;
            var window = Leader.LastPackets.OfType<RdlstPacket>().Last();
            Assert.AreEqual(falls, window.Members.NotNull().Single(s => s.NotNull().CharacterId == id).NotNull().Deaths);
        }

        private void Player_SeesTheLeaderOnTheBoard(int player)
        {
            var board = _players[player].LastPackets.OfType<ServerRlPacket>().Last();
            Assert.AreEqual(Leader.Character.Name, board.Teams.NotNull().Single().NotNull().LeaderName);
        }

        private void Player_SeesAnEmptyBoard(int player) =>
            Assert.AreEqual(0, _players[player].LastPackets.OfType<ServerRlPacket>().Last().Teams.NotNull().Count);

        private Task TheLeaderKicks_Async(int player) => _raids.KickAsync(Leader, _players[player].Character.CharacterId);

        [TestMethod]
        public async Task EveryMemberSeesTheStartBannerAndTheCounters()
        {
            await new Spec("Entering the raid shows each member the start banner, the boss to kill and the team's lives")
                .GivenAsync(ATeamOf_Async, 2)
                .WhenAsync(TheTeamEntersTheRaidAsync)
                .Then(Player_SawTheBanner_, 0, RaidBannerType.Started)
                .And(Player_SawTheBanner_, 1, RaidBannerType.Started)
                .And(Player_LastSawTheCounters_, 1, new RaidmbfPacket { MonsterGoal = 1, MonstersLeft = 1, LivesLeft = 2, Lives = 2, Unknown = HudValue })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KillingTheBossCountsItAndShowsTheEndBanner()
        {
            await new Spec("Killing the boss brings its counter to zero and shows every member the end banner")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .WhenAsync(TheBossDiesAsync)
                .Then(Player_LastSawTheCounters_, 1, new RaidmbfPacket { MonsterGoal = 1, MonstersLeft = 0, LivesLeft = 2, Lives = 2, Unknown = HudValue })
                .And(Player_SawTheBanner_, 0, RaidBannerType.BossDefeated)
                .And(Player_SawTheBanner_, 1, RaidBannerType.BossDefeated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EachFallCostsTheTeamALife()
        {
            await new Spec("Each fall takes one of the team's lives, and standing up again gives none back")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .AndAsync(Player_FallsAndStandsUpAsync, 1)
                .WhenAsync(Player_FallsAsync, 0)
                .Then(Player_LastSawTheCounters_, 1, new RaidmbfPacket { MonsterGoal = 1, MonstersLeft = 1, LivesLeft = 0, Lives = 2, Unknown = HudValue })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ArrivingShowsTheBossBar()
        {
            await new Spec("A member arriving in the boss's room is shown its HP bar")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .WhenAsync(Player_ArrivesAsync, 1)
                .Then(Player_SawTheBossBar_Times, 1, 1)
                .And(Player_LastSawTheBossBarAt_Hp, 1, 10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheBossBarFollowsItsHp()
        {
            await new Spec("The boss's HP bar is shown to its room every second with its HP")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .And(TheRoomIsWatchedBy_, 1)
                .AndAsync(TheBossIsDownTo_Hp, 4)
                .WhenAsync(ASecondPassesAsync)
                .Then(Player_SawTheBossBar_Times, 1, 1)
                .And(Player_LastSawTheBossBarAt_Hp, 1, 4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallenBossShowsItsBarOnceMoreEmpty()
        {
            await new Spec("A boss that has fallen shows its bar one last time with no HP, then no more")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .And(TheRoomIsWatchedBy_, 1)
                .AndAsync(TheBossFallsAsync)
                .WhenAsync(ASecondPassesAsync)
                .AndAsync(ASecondPassesAsync)
                .Then(Player_SawTheBossBar_Times, 1, 1)
                .And(Player_LastSawTheBossBarAt_Hp, 1, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringTheRaidShowsTheTeamHud()
        {
            await new Spec("A member's first room closes the team window and shows the team's HUD with every member's HP and MP")
                .GivenAsync(ATeamOf_Async, 2)
                .WhenAsync(TheTeamEntersTheRaidAsync)
                .Then(Player_LastSaw_, 1, new IPacket[]
                {
                    new RaidPacket { Type = 1, Value = 0 }, new RdlstPacket(), new RaidPacket { Type = 5, Value = 0 },
                    new RaidPacket { Type = 4 }, new RaidMemberStatsPacket()
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryMemberSeesTheTeamsHpAndMpEverySecond()
        {
            await new Spec("Every second, each member inside is shown every member's HP and MP in percent, in team order")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .AndAsync(EveryMemberIsAtFullHpAndMpAsync)
                .AndAsync(Player_IsAtHalfHpAndAQuarterMpAsync, 1)
                .WhenAsync(ASecondPassesAsync)
                .Then(Player_LastSawTheTeamAt_, 0, new[] { "100.100", "50.25" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheTeamsHpAndMpStayShownUntilTheMembersAreSentBack()
        {
            await new Spec("After the win the members are still shown the team's HP and MP until they are sent back")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .AndAsync(EveryMemberIsAtFullHpAndMpAsync)
                .AndAsync(TheBossDiesAsync)
                .And(Player_ForgetsWhatTheySaw, 0)
                .WhenAsync(ASecondPassesAsync)
                .Then(Player_LastSawTheTeamAt_, 0, new[] { "100.100", "100.100" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheEndOfTheRaidTakesEachMemberOutOfTheTeamAsTheyAreSentBack()
        {
            await new Spec("When the members are sent back after the win, each leaves the team and has the raid HUD cleared")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .AndAsync(TheBossDiesAsync)
                .And(Player_ForgetsWhatTheySaw, 0)
                .WhenAsync(TenSecondsPassAsync)
                .Then(Player_LastSaw_, 0, new IPacket[] { new RaidPacket { Type = 2, Value = -1 }, HudCleared })
                .And(Player_IsOutOfTheRaidAndAlive, 0)
                .And(TheTeamIsDissolved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheTeamStaysTogetherUntilItIsSentBack()
        {
            await new Spec("The win itself does not break up the team")
                .GivenAsync(AStartedRaidOf_Async, 2)
                .WhenAsync(TheBossDiesAsync)
                .Then(Player_IsInTheTeam, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAct6TeamUsesTheAct6TeamWindow()
        {
            await new Spec("An Act 6 raid's team is shown with rdlstf and raidf")
                .Given(TheRaidIsInAct6)
                .WhenAsync(ATeamOf_Async, 2)
                .Then(Player_LastSawTheAct6TeamLedByTheLeader, 1)
                .And(Player_SawTheAct6TeamWindow, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringAnAct6RaidShowsItsTeamHud()
        {
            await new Spec("An Act 6 raid closes the team window before the move and shows the members' HP and MP with raidfhp and raidfmp")
                .Given(TheRaidIsInAct6)
                .AndAsync(ATeamOf_Async, 2)
                .And(Player_ForgetsWhatTheySaw, 1)
                .WhenAsync(TheTeamEntersTheRaidAsync)
                .Then(Player_FirstSaw_, 1, new RaidfPacket { Type = 1, Value = 0 })
                .And(Player_LastSaw_, 1, new IPacket[]
                {
                    new RdlstfPacket(), new RaidfPacket { Type = 5, Value = 0 }, new RaidfPacket { Type = 4 },
                    new RaidfhpPacket(), new RaidfmpPacket()
                })
                .And(Player_SawNoOrdinaryRaidPacket, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheEndOfAnAct6RaidClearsItsTeamWithRaidf()
        {
            await new Spec("When an Act 6 raid's members are sent back, the team is closed with raidf")
                .Given(TheRaidIsInAct6)
                .AndAsync(AStartedRaidOf_Async, 2)
                .AndAsync(TheBossDiesAsync)
                .And(Player_ForgetsWhatTheySaw, 0)
                .WhenAsync(TenSecondsPassAsync)
                .Then(Player_LastSaw_, 0, new IPacket[] { new RaidfPacket { Type = 2, Value = -1 }, HudCleared })
                .ExecuteAsync();
        }

        private static readonly EventPacket HudCleared = new()
        {
            Type = EventPacketType.ClearTimeSpaceUI,
            State = EventTimerState.Running,
            Parameter = -1,
            SecondParameter = -1
        };

        private void TheRaidIsInAct6() => _inAct6 = true;

        private void Player_ForgetsWhatTheySaw(int player) => _players[player].LastPackets.Clear();

        private async Task EveryMemberIsAtFullHpAndMpAsync()
        {
            foreach (var character in _players.Take(2).Select(s => s.Character))
            {
                await character.MapInstance.Executor.RunAsync(() =>
                {
                    character.Hp = character.MaxHp;
                    character.Mp = character.MaxMp;
                    return Task.CompletedTask;
                });
            }
        }

        private async Task Player_IsAtHalfHpAndAQuarterMpAsync(int player)
        {
            var character = _players[player].Character;
            await character.MapInstance.Executor.RunAsync(() =>
            {
                character.Hp = (character.MaxHp + 1) / 2;
                character.Mp = (character.MaxMp + 3) / 4;
                return Task.CompletedTask;
            });
        }

        // The raid team and HUD packets a player was shown, in order.
        private List<IPacket> RaidPackets(int player) =>
            _players[player].LastPackets
                .Where(p => p is RaidPacket or RaidfPacket or RdlstPacket or RdlstfPacket or RaidMemberStatsPacket
                    or RaidfhpPacket or RaidfmpPacket or EventPacket { Type: EventPacketType.ClearTimeSpaceUI })
                .OfType<IPacket>()
                .ToList();

        // A team state or a cleared HUD must match field for field; a list packet only has to come in its place.
        private static void AssertSame(IPacket expected, IPacket actual)
        {
            if (expected is RaidPacket or RaidfPacket or EventPacket)
            {
                PacketAssert.AreEqual(expected, actual);
                return;
            }

            Assert.AreEqual(expected.GetType(), actual.GetType());
        }

        private void Player_LastSaw_(int player, IPacket[] expected)
        {
            var seen = RaidPackets(player);
            Assert.IsTrue(seen.Count >= expected.Length);
            var last = seen.Skip(seen.Count - expected.Length).ToList();
            for (var i = 0; i < expected.Length; i++)
            {
                AssertSame(expected[i], last[i]);
            }
        }

        private void Player_FirstSaw_(int player, IPacket expected) => AssertSame(expected, RaidPackets(player).First());

        private void Player_SawNoOrdinaryRaidPacket(int player) =>
            Assert.IsFalse(RaidPackets(player).Any(p => p is RaidPacket or RdlstPacket or RaidMemberStatsPacket));

        private void Player_LastSawTheAct6TeamLedByTheLeader(int player) =>
            Player_LastSaw_(player, new IPacket[]
            {
                new RaidfPacket { Type = 2, Value = Leader.Character.CharacterId },
                new RaidfPacket { Type = 1, Value = 1 },
                new RdlstfPacket()
            });

        private void Player_SawTheAct6TeamWindow(int player)
        {
            Assert.IsTrue(_players[player].LastPackets.OfType<RdlstfPacket>().Any());
            Assert.IsFalse(_players[player].LastPackets.OfType<RdlstPacket>().Any());
        }

        private void Player_LastSawTheTeamAt_(int player, string[] percents)
        {
            var stats = _players[player].LastPackets.OfType<RaidMemberStatsPacket>().Last();
            CollectionAssert.AreEqual(_players.Take(percents.Length).Select(s => s.Character.CharacterId).ToList(),
                stats.Members.NotNull().Select(m => m.NotNull().CharacterId).ToList());
            Assert.AreEqual(string.Join(" ", percents),
                string.Join(" ", stats.Members.NotNull().Select(m => $"{m.NotNull().HpPercent}.{m.NotNull().MpPercent}")));
        }

        // The arrival a map change raises is not dispatched here, so the test raises it.
        private async Task TheTeamEntersTheRaidAsync()
        {
            await TheLeaderStartsAsync();
            foreach (var player in Enumerable.Range(0, 2))
            {
                await Player_ArrivesAsync(player);
            }
        }

        private Task Player_ArrivesAsync(int player) =>
            Run.EnteredAsync(_players[player], _players[player].Character.MapInstance.MapInstanceId);

        private void TheRoomIsWatchedBy_(int player)
        {
            var watcher = _players[player];
            var room = new Mock<ISessionGroup>();
            room.Setup(s => s.Broadcast(It.IsAny<IPacket[]>()))
                .Callback<IPacket[]>(Receive).Returns(Task.CompletedTask);
            room.Setup(s => s.Broadcast(It.IsAny<IPacket[]>(), It.IsAny<ISessionMatcher>()))
                .Callback<IPacket[], ISessionMatcher>((packets, _) => Receive(packets)).Returns(Task.CompletedTask);
            watcher.Character.MapInstance.Sessions = room.Object;

            void Receive(IPacket[] packets)
            {
                foreach (var packet in packets)
                {
                    watcher.LastPackets.Enqueue(packet);
                }
            }
        }

        private void Player_SawTheBanner_(int player, RaidBannerType type) =>
            Assert.IsTrue(_players[player].LastPackets.OfType<RaidbfPacket>().Any(p => p.Type == type && p.Unknown == HudValue));

        private void Player_LastSawTheCounters_(int player, RaidmbfPacket expected) =>
            PacketAssert.AreEqual(expected, _players[player].LastPackets.OfType<RaidmbfPacket>().Last());

        private void Player_LastSawTheBossBarAt_Hp(int player, int hp)
        {
            var bar = _players[player].LastPackets.OfType<RbossPacket>().Last();
            Assert.AreEqual(hp, bar.CurrentHp);
            Assert.AreEqual(10, bar.MaxHp);
            Assert.AreEqual(Boss, bar.VNum);
        }

        private void Player_SawTheBossBar_Times(int player, int times) =>
            Assert.AreEqual(times, _players[player].LastPackets.OfType<RbossPacket>().Count());

        private Task TheBossIsDownTo_Hp(int hp) => OnTheBossAsync(boss => boss.Hp = hp);

        private Task TheBossFallsAsync() => OnTheBossAsync(boss =>
        {
            boss.Hp = 0;
            boss.IsAlive = false;
        });

        private async Task OnTheBossAsync(Action<MonsterComponentBundle> change)
        {
            var room = Leader.Character.MapInstance;
            await room.Executor.RunAsync(() =>
            {
                change(room.EnumerateMonsters().Single(m => m.VNum == Boss));
                return Task.CompletedTask;
            });
        }

        private async Task ASecondPassesAsync()
        {
            World.Clock.Advance(Duration.FromSeconds(1));
            await Run.TickAsync();
        }

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

        private Task Player_DropsAsAGroupMemberAsync(int player) =>
            World.Create<GroupDisconnectHandler>().HandleDisconnectAsync(_players[player]);

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
                Assert.AreEqual(BoxesWon, player.Character.InventoryService.CountItem(Box));
            }
        }

        private void Player_Holds_Boxes(int player, int amount)
        {
            var character = _players[player].Character;
            character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(Box, (short)amount), character.CharacterId));
        }

        private void Player_IsShownSlot_With_(int player, int slot, int amount)
        {
            var shown = _players[player].LastPackets.OfType<IvnPacket>().SelectMany(p => p.IvnSubPackets ?? [])
                .OfType<IvnSubPacket>().Last(i => i.Slot == slot && i.VNum == Box);
            Assert.AreEqual(amount, shown.RareAmount);
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
