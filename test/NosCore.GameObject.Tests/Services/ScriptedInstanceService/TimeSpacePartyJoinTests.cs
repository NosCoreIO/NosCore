//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpacePartyJoinTests
    {
        private const short TimeSpaceId = 16;
        private const short SeedOfPower = 1012;

        private TestWorld World;
        private ClientSession _leader;
        private ClientSession _member;
        private GameObject.Services.ScriptedInstanceService.ScriptedInstance _entrance;
        private Mock<IScriptedInstanceService> _scripted;
        private Mock<IMapChangeService> _mapChange;
        private MapInstance _firstRoom;
        private ScriptedInstanceRun _run;

        private sealed class Script : InstanceScript
        {
            public Script(short id) => Definition = InstanceDefinitionBuilder.Named(id, "timespace", "timespace")
                .WithRoom(2004, out _).WithRoom(2004, out _).StartingAt(10, 8).Requiring(SeedOfPower, 2).Build();
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _leader = await World.CreateSessionAsync();
            _member = await World.CreateSessionAsync();
            _entrance = Entrance(TimeSpaceId);
            _scripted = World.Freeze<Mock<IScriptedInstanceService>>();
            _scripted.Setup(s => s.LoadAsync(It.IsAny<GameObject.Services.ScriptedInstanceService.ScriptedInstance>())).ReturnsAsync(true);
            _mapChange = World.Freeze<Mock<IMapChangeService>>();
            _firstRoom = Room();
        }

        private static GameObject.Services.ScriptedInstanceService.ScriptedInstance Entrance(short id) =>
            new() { ScriptedInstanceId = id, Logic = new Script(id), LevelMinimum = 1 };

        private MapInstance Room() =>
            World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);

        [TestMethod]
        public async Task TheStoneAsksToJoinAPartyMemberInsideAsync()
        {
            await new Spec("A party member inside this time-space makes the stone ask whether to join them")
                .Given(TheLeaderIsInARunOf_, TimeSpaceId)
                .And(TheMemberIsInTheLeadersParty)
                .WhenAsync(TheMemberOpensTheStone)
                .Then(TheMemberShouldBeAskedToJoinTheLeader)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SayingNoOpensTheUsualWindowAsync()
        {
            await new Spec("Answering no to the join question opens the time-space window")
                .Given(TheLeaderIsInARunOf_, TimeSpaceId)
                .And(TheMemberIsInTheLeadersParty)
                .WhenAsync(TheMemberDeclinesToJoin)
                .Then(TheMemberShouldSeeTheWindow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherTimeSpaceDoesNotAskAsync()
        {
            await new Spec("A party member in another time-space does not make this stone ask")
                .Given(TheLeaderIsInARunOf_, (short)(TimeSpaceId + 1))
                .And(TheMemberIsInTheLeadersParty)
                .WhenAsync(TheMemberOpensTheStone)
                .Then(TheMemberShouldSeeTheWindow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SomeoneOutsideThePartyDoesNotAskAsync()
        {
            await new Spec("A player outside the party who is inside the time-space does not make the stone ask")
                .Given(TheLeaderIsInARunOf_, TimeSpaceId)
                .WhenAsync(TheMemberOpensTheStone)
                .Then(TheMemberShouldSeeTheWindow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoiningPaysTheSeedsAndLandsInTheFirstRoomAsync()
        {
            await new Spec("Joining spends the member's own seeds, says so, and puts them in the run's first room")
                .Given(TheLeaderIsInARunOf_, TimeSpaceId)
                .And(TheMemberIsInTheLeadersParty)
                .And(TheMemberCarries_Seeds, 3)
                .WhenAsync(TheMemberJoins)
                .Then(TheMemberShouldBeMovedToTheFirstRoom_Times, 1)
                .And(TheMemberShouldCarry_Seeds, 1)
                .And(TheMemberShouldBeToldTwoSeedsWereUsed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoiningWithoutTheSeedsIsRefusedAsync()
        {
            await new Spec("Without the seeds the member is told what is missing and stays where they are")
                .Given(TheLeaderIsInARunOf_, TimeSpaceId)
                .And(TheMemberIsInTheLeadersParty)
                .And(TheMemberCarries_Seeds, 1)
                .WhenAsync(TheMemberJoins)
                .Then(TheMemberShouldBeMovedToTheFirstRoom_Times, 0)
                .And(TheMemberShouldCarry_Seeds, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task JoiningAfterLeavingThePartyIsRefusedAsync()
        {
            await new Spec("A join answer naming a player who is not in the party moves nobody")
                .Given(TheLeaderIsInARunOf_, TimeSpaceId)
                .And(TheMemberCarries_Seeds, 2)
                .WhenAsync(TheMemberJoins)
                .Then(TheMemberShouldBeMovedToTheFirstRoom_Times, 0)
                .And(TheMemberShouldCarry_Seeds, 2)
                .ExecuteAsync();
        }

        private void TheLeaderIsInARunOf_(short timeSpaceId)
        {
            var script = new Script(timeSpaceId);
            var entrance = new GameObject.Services.ScriptedInstanceService.ScriptedInstance { ScriptedInstanceId = timeSpaceId, Logic = script, LevelMinimum = 1 };
            _run = new ScriptedInstanceRun(World.CreateTimeSpaceRunHost(entrance), script,
                new Dictionary<int, MapInstance> { [1] = _firstRoom, [2] = Room() },
                new NpcMonsterCatalog([]), new RandomProvider(), World.Clock, World.SessionRegistry,
                World.Create<AggroService>(), World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(),
                NullLogger.Instance, World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            _leader.Character.MapInstance = _firstRoom;
            World.SessionRegistry.UpdateCharacter(_leader.Channel?.Id ?? string.Empty, _leader.Character.CharacterId, _firstRoom.MapInstanceId);
            _scripted.Setup(s => s.GetRun(_firstRoom.MapInstanceId)).Returns(_run);
        }

        private void TheMemberIsInTheLeadersParty()
        {
            var party = new Group(GroupType.Group, World.SessionGroupFactory);
            _leader.Character.JoinGroup(party);
            _member.Character.JoinGroup(party);
        }

        private void TheMemberCarries_Seeds(int amount) =>
            _member.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(SeedOfPower, (short)amount), _member.Character.CharacterId));

        private Task TheMemberOpensTheStone() => World.Create<TimeSpaceEntryService>().ShowAsync(_member, _entrance);

        private Task TheMemberDeclinesToJoin() =>
            World.Create<TimeSpaceEntryService>().ShowAsync(_member, _entrance, offerToJoin: false);

        private Task TheMemberJoins() =>
            World.Create<TimeSpaceEntryService>().JoinAsync(_member, _entrance, _leader.Character.VisualId);

        private void TheMemberShouldBeAskedToJoinTheLeader() =>
            PacketAssert.AreEqual(new DlgiPacket
            {
                YesPacket = new WreqPacket { Value = WreqPacketType.TeamTimeSpaceEntry, Param = _leader.Character.VisualId },
                NoPacket = new WreqPacket { Value = WreqPacketType.DailyTeamTimeSpaceEntry, Param = 1 },
                Question = Game18NConstString.WantJoinParty
            }, _member.LastPackets.OfType<DlgiPacket>().Single());

        private void TheMemberShouldSeeTheWindow()
        {
            Assert.AreEqual(1, _member.LastPackets.OfType<RbrPacket>().Count());
            Assert.IsFalse(_member.LastPackets.OfType<DlgiPacket>().Any());
        }

        private void TheMemberShouldBeMovedToTheFirstRoom_Times(int times) =>
            _mapChange.Verify(m => m.ChangeMapInstanceAsync(_member, _firstRoom.MapInstanceId, 10, 8), Times.Exactly(times));

        private void TheMemberShouldCarry_Seeds(int amount) =>
            Assert.AreEqual(amount, _member.Character.InventoryService.CountItem(SeedOfPower));

        private void TheMemberShouldBeToldTwoSeedsWereUsed() =>
            PacketAssert.AreEqual(_member.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.YouHaveUsed, 2, SeedOfPower, (short)2),
                _member.LastPackets.OfType<SayiPacket>().Single(p => p.Message == Game18NConstString.YouHaveUsed));
    }
}
