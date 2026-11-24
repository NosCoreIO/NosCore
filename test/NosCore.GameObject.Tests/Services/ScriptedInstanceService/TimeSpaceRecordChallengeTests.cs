//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceRecordChallengeTests
    {
        private const long Cost = 850;

        private TestWorld World;
        private ClientSession _session;
        private Mock<IScriptedInstanceService> _instances;
        private Mock<IMapChangeService> _mapChange;
        private GameObject.Services.ScriptedInstanceService.ScriptedInstance _entrance;
        private Script _script;
        private ScriptedInstanceRun _run;

        private sealed class Script : InstanceScript
        {
            public readonly int Room;

            public Script(long? recordCost)
            {
                var builder = InstanceDefinitionBuilder.Named(8, "timespace", "timespace")
                    .StartingAt(14, 10)
                    .WithRoom(4100, out Room);
                Definition = (recordCost is { } cost ? builder.WithRecordCost(cost) : builder).Build();
            }
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _instances = World.Freeze<Mock<IScriptedInstanceService>>();
            _mapChange = World.Freeze<Mock<IMapChangeService>>();
            TheStoneCosts_(Cost);
        }

        [TestMethod]
        public async Task TheRecordButtonAsksTheCost()
        {
            await new Spec("Pressing the record button asks whether to pay the stone's cost to challenge its record")
                .WhenAsync(TheRecordButtonIsPressed)
                .Then(ThePlayerShouldBeAskedToPay_, Cost)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStoneWithoutACostIgnoresTheRecordButton()
        {
            await new Spec("A stone that names no record cost asks nothing")
                .Given(TheStoneCosts_, (long?)null)
                .WhenAsync(TheRecordButtonIsPressed)
                .Then(ThePlayerShouldNotBeAsked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AcceptingPaysTheCostAndEnters()
        {
            await new Spec("Accepting the cost takes the gold and enters the time-space")
                .Given(ThePlayerHas_Gold, 1000L)
                .WhenAsync(TheCostIsAccepted)
                .Then(ThePlayerShouldHave_Gold, 150L)
                .And(ThePlayerShouldBeToldTheGoldLeft_, 150L)
                .And(TheFirstRoomShouldBeEntered_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TooLittleGoldIsRefused()
        {
            await new Spec("Accepting with less gold than the cost keeps the gold and does not enter")
                .Given(ThePlayerHas_Gold, 849L)
                .WhenAsync(TheCostIsAccepted)
                .Then(ThePlayerShouldHave_Gold, 849L)
                .And(ThePlayerShouldBeRefusedFor_, Game18NConstString.NotEnoughGold)
                .And(TheFirstRoomShouldBeEntered_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANormalEntryCostsNothing()
        {
            await new Spec("Entering without the record button takes no gold")
                .Given(ThePlayerHas_Gold, 1000L)
                .WhenAsync(TheTimeSpaceIsEntered)
                .Then(ThePlayerShouldHave_Gold, 1000L)
                .And(TheFirstRoomShouldBeEntered_Times, 1)
                .ExecuteAsync();
        }

        private void TheStoneCosts_(long? cost)
        {
            _script = new Script(cost);
            _entrance = new GameObject.Services.ScriptedInstanceService.ScriptedInstance
            {
                MapId = 1, PositionX = 8, PositionY = 45, Logic = _script
            };
            _instances.Setup(s => s.LoadAsync(_entrance)).ReturnsAsync(true);
            _run = CreateRun();
            _instances.Setup(s => s.InstantiateAsync(_entrance)).ReturnsAsync(_run);
        }

        private void ThePlayerHas_Gold(long gold) => _session.Character.Gold = gold;

        private Task TheRecordButtonIsPressed() =>
            World.Create<TimeSpaceEntryService>().AskRecordChallengeAsync(_session, _entrance);

        private Task TheCostIsAccepted() =>
            World.Create<TimeSpaceEntryService>().EnterAsync(_session, _entrance, recordChallenge: true);

        private Task TheTimeSpaceIsEntered() =>
            World.Create<TimeSpaceEntryService>().EnterAsync(_session, _entrance);

        private void ThePlayerShouldBeAskedToPay_(long cost) =>
            PacketAssert.AreEqual(new QnaiPacket
            {
                YesPacket = new TreqPacket { X = 8, Y = 45, StartPress = true, RecordPress = true, GoldAccepted = true },
                Question = Game18NConstString.GoldRequiredToChallengeRecord,
                ArgumentType = 4,
                Game18NArguments = { cost }
            }, _session.LastPackets.OfType<QnaiPacket>().Single());

        private void ThePlayerShouldNotBeAsked() => Assert.IsFalse(_session.LastPackets.OfType<QnaiPacket>().Any());

        private void ThePlayerShouldHave_Gold(long gold) => Assert.AreEqual(gold, _session.Character.Gold);

        private void ThePlayerShouldBeToldTheGoldLeft_(long gold) =>
            PacketAssert.AreEqual(new GoldPacket { Gold = gold }, _session.LastPackets.OfType<GoldPacket>().Last());

        private void ThePlayerShouldBeRefusedFor_(Game18NConstString message) =>
            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = message
            }, _session.LastPackets.OfType<SayiPacket>().Last());

        private void TheFirstRoomShouldBeEntered_Times(int times) =>
            _mapChange.Verify(m => m.ChangeMapInstanceAsync(_session, _run.Rooms[_script.Room], 14, 10), Times.Exactly(times));

        private ScriptedInstanceRun CreateRun()
        {
            var room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 4100,
                    NameI18NKey = "room",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            room.Sessions = new Mock<ISessionGroup>().Object;
            return new ScriptedInstanceRun(
                World.CreateTimeSpaceRunHost(_entrance),
                _script,
                new Dictionary<int, MapInstance> { [_script.Room] = room },
                new NpcMonsterCatalog([]),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
        }
    }
}
