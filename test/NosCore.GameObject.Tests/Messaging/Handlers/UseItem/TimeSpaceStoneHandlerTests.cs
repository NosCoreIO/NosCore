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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using ScriptedInstance = NosCore.GameObject.Services.ScriptedInstanceService.ScriptedInstance;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class TimeSpaceStoneHandlerTests
    {
        private const short GateVNum = 1184;
        private TestWorld World;
        private ClientSession _session;
        private Mock<IScriptedInstanceService> _instances;
        private InventoryItemInstance _gate;
        private ScriptedInstance? _opened;

        private sealed class Script : InstanceScript
        {
            public Script() =>
                Definition = InstanceDefinitionBuilder.Named(624, "Gate 1", "Gate 1").Build();
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _instances = World.Freeze<Mock<IScriptedInstanceService>>();
            _instances.Setup(s => s.LoadAsync(It.IsAny<ScriptedInstance>()))
                .Callback<ScriptedInstance>(entrance => _opened = entrance)
                .ReturnsAsync(false);
            World.UseReal<ITimeSpaceEntryService, TimeSpaceEntryService>();
            _gate = PlaceGate(ItemEffectType.TimeSpaceStone);
        }

        [TestMethod]
        public async Task AGateOpensItsTimeSpaceWhereThePlayerStands()
        {
            _session.Character.PositionX = 12;
            _session.Character.PositionY = 8;

            await UseAsync();

            Assert.IsNotNull(_opened);
            Assert.AreEqual("timespace-624", _opened.Script);
            Assert.AreEqual(ScriptedInstanceType.TimeSpace, _opened.Type);
            Assert.AreEqual(_session.Character.MapId, _opened.MapId);
            Assert.AreEqual((short)12, _opened.PositionX);
            Assert.AreEqual((short)8, _opened.PositionY);
            Assert.AreEqual(GateVNum, _opened.StoneVNum);
            Assert.AreEqual(1, GatesLeft());
        }

        [TestMethod]
        public async Task AGateCannotBeUsedInsideAnInstance()
        {
            _session.Character.MapInstance = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map { MapId = 4201, NameI18NKey = "room", Data = [1, 0, 1, 0, 0] },
                Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);

            await UseAsync();

            Assert.IsNull(_opened);
            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.CanNotBeUsedHere
            }, _session.LastPackets.OfType<SayiPacket>().Last());
        }

        [TestMethod]
        public async Task AnItemWithoutTheStoneEffectOpensNothing()
        {
            _session.Character.InventoryService.Clear();
            _gate = PlaceGate(ItemEffectType.NoEffect);

            await UseAsync();

            Assert.IsNull(_opened);
        }

        [TestMethod]
        public async Task WinningTheTimeSpaceUsesUpTheGate()
        {
            var run = StartRunFromGate();

            run.Succeed();
            await run.TickAsync();

            Assert.AreEqual(0, GatesLeft());
        }

        [TestMethod]
        public async Task FailingTheTimeSpaceKeepsTheGate()
        {
            var run = StartRunFromGate();

            run.Fail();
            await run.TickAsync();

            Assert.AreEqual(1, GatesLeft());
        }

        private ScriptedInstanceRun StartRunFromGate()
        {
            var room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 4201,
                    NameI18NKey = "room",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            room.Sessions = new Mock<ISessionGroup>().Object;
            var script = new Script();
            var run = new ScriptedInstanceRun(
                new TimeSpaceRunHost(
                    new ScriptedInstance { MapId = 1, Logic = script, StoneVNum = GateVNum },
                    new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), new Mock<IGameEventDispatcher>().Object, World.Clock),
                script,
                new Dictionary<int, MapInstance> { [1] = room },
                new Dictionary<short, NpcMonsterDto>(),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            _session.Character.MapInstance = room;
            World.SessionRegistry.UpdateCharacter(_session.Channel?.Id ?? string.Empty, _session.Character.CharacterId, room.MapInstanceId);
            return run;
        }

        private InventoryItemInstance PlaceGate(ItemEffectType effect)
        {
            var gate = new ItemInstance(new Item
            {
                VNum = GateVNum, Type = NoscorePocketType.Main, ItemType = ItemType.Special, Effect = effect, EffectValue = 624
            })
            {
                Amount = 1,
            };
            var placed = InventoryItemInstance.Create(gate, _session.Character.CharacterId);
            placed.Slot = 0;
            placed.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[placed.ItemInstanceId] = placed;
            return placed;
        }

        private int GatesLeft() => _session.Character.InventoryService.CountItem(GateVNum);

        private Task UseAsync() =>
            World.Create<TimeSpaceStoneHandler>().Handle(new ItemUsedEvent(_session, _gate, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
            }));
    }
}
