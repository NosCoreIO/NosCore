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
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.FactionWarService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using FactionWar = NosCore.GameObject.Services.FactionWarService.FactionWarService;
using Act4RaidType = NosCore.Packets.Enumerations.Act4RaidType;

namespace NosCore.GameObject.Tests.Services.Act4RaidService
{
    [TestClass]
    public class Act4RaidServiceTests
    {
        private const short StartX = 7;
        private const short StartY = 9;

        private TestWorld World;
        private ClientSession _session;
        private Mock<IFactionWarService> _factionWar;
        private Mock<IScriptedInstanceService> _instances;
        private Mock<IMapChangeService> _mapChange;
        private Mock<IInstanceScript> _script;
        private readonly List<ScriptedInstanceRun> _runs = new();
        private GameObject.Services.Act4RaidService.Act4RaidService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var gateMap = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = FactionWar.RaidGateMapId,
                    NameI18NKey = "gate",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.BaseMapInstance);
            _session.Character.MapInstance = gateMap;
            _session.Character.PositionX = FactionWar.RaidGateX;
            _session.Character.PositionY = FactionWar.RaidGateY;
            _session.Character.Faction = FactionType.Angel;
            _session.Character.Level = 70;
            _session.Character.Family = new Family { FamilyId = 3 };

            _factionWar = World.Freeze<Mock<IFactionWarService>>();
            _factionWar.Setup(s => s.OpenRaid(FactionType.Angel)).Returns(Act4RaidType.Morcos);
            _factionWar.Setup(s => s.RaidClosesAt(FactionType.Angel))
                .Returns(World.Clock.GetCurrentInstant() + Duration.FromMinutes(40));

            _script = new Mock<IInstanceScript>();
            _script.SetupGet(s => s.Definition).Returns(new ScriptedInstanceDefinition
            {
                LevelMinimum = 50, StartX = StartX, StartY = StartY,
            });
            _instances = World.Freeze<Mock<IScriptedInstanceService>>();
            _instances.Setup(s => s.LoadScriptAsync(GameObject.Services.Act4RaidService.Act4RaidService.Script(Act4RaidType.Morcos)))
                .ReturnsAsync(_script.Object);
            _instances.Setup(s => s.InstantiateAsync(_script.Object, MapInstanceType.Act4Instance, It.IsAny<IInstanceRunHost>()))
                .ReturnsAsync((IInstanceScript script, MapInstanceType _, IInstanceRunHost host) => NewRun(script, host));
            _instances.Setup(s => s.GetRun(It.IsAny<Guid>()))
                .Returns((Guid room) => _runs.FirstOrDefault(r => r.Rooms.Values.Contains(room)));
            _mapChange = World.Freeze<Mock<IMapChangeService>>();
            _service = World.Create<GameObject.Services.Act4RaidService.Act4RaidService>();
        }

        [TestMethod]
        public async Task AFamilyMemberAtTheOpenGateEntersTheRaid()
        {
            Assert.IsTrue(await _service.UseGateAsync(_session));

            var room = _runs.Single().Rooms.Values.Single();
            _mapChange.Verify(s => s.ChangeMapInstanceAsync(_session, room, StartX, StartY), Times.Once);
            PacketAssert.AreEqual(new DgPacket { RaidType = Act4RaidType.Morcos, Urgency = 1, Seconds = 2400 },
                _session.LastPackets.OfType<DgPacket>().Single());
        }

        [TestMethod]
        public async Task TheLastHalfHourIsUrgent()
        {
            _factionWar.Setup(s => s.RaidClosesAt(FactionType.Angel))
                .Returns(World.Clock.GetCurrentInstant() + Duration.FromMinutes(20));

            await _service.UseGateAsync(_session);

            PacketAssert.AreEqual(new DgPacket { RaidType = Act4RaidType.Morcos, Urgency = 2, Seconds = 1200 },
                _session.LastPackets.OfType<DgPacket>().Single());
        }

        [TestMethod]
        public async Task TheSameFamilyJoinsItsRunningRun()
        {
            var other = await World.CreateSessionAsync();
            other.Character.MapInstance = _session.Character.MapInstance;
            other.Character.PositionX = FactionWar.RaidGateX;
            other.Character.PositionY = FactionWar.RaidGateY;
            other.Character.Faction = FactionType.Angel;
            other.Character.Level = 70;
            other.Character.Family = new Family { FamilyId = 3 };

            await _service.UseGateAsync(_session);
            await _service.UseGateAsync(other);

            var room = _runs.Single().Rooms.Values.Single();
            _mapChange.Verify(s => s.ChangeMapInstanceAsync(other, room, StartX, StartY), Times.Once);
        }

        [TestMethod]
        public async Task AnotherFamilyGetsItsOwnRun()
        {
            var other = await World.CreateSessionAsync();
            other.Character.MapInstance = _session.Character.MapInstance;
            other.Character.PositionX = FactionWar.RaidGateX;
            other.Character.PositionY = FactionWar.RaidGateY;
            other.Character.Faction = FactionType.Angel;
            other.Character.Level = 70;
            other.Character.Family = new Family { FamilyId = 4 };

            await _service.UseGateAsync(_session);
            await _service.UseGateAsync(other);

            Assert.AreEqual(2, _runs.Count);
            _mapChange.Verify(s => s.ChangeMapInstanceAsync(other, _runs[1].Rooms.Values.Single(), StartX, StartY), Times.Once);
        }

        [TestMethod]
        public async Task APlayerWithoutAFamilyIsTurnedAway()
        {
            _session.Character.Family = null;

            Assert.IsTrue(await _service.UseGateAsync(_session));

            Assert.AreEqual(Game18NConstString.FamilyMemberOnlyEnterDungeon, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
            NoOneEntered();
        }

        [TestMethod]
        public async Task APlayerUnderTheRaidsLevelIsTurnedAway()
        {
            _session.Character.Level = 49;

            Assert.IsTrue(await _service.UseGateAsync(_session));

            PacketAssert.AreEqual(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.TooLowLevelToEntry,
                ArgumentType = 4,
                Game18NArguments = { 50 },
            }, _session.LastPackets.OfType<MsgiPacket>().Single());
            NoOneEntered();
        }

        [TestMethod]
        public async Task ARaidWithoutAScriptLetsNobodyIn()
        {
            _instances.Setup(s => s.LoadScriptAsync(It.IsAny<string>())).ReturnsAsync((IInstanceScript?)null);

            Assert.IsTrue(await _service.UseGateAsync(_session));

            NoOneEntered();
        }

        [TestMethod]
        public async Task ARunThatCannotStartClosesThePortal()
        {
            _instances.Setup(s => s.InstantiateAsync(It.IsAny<IInstanceScript>(), It.IsAny<MapInstanceType>(), It.IsAny<IInstanceRunHost>()))
                .ReturnsAsync((ScriptedInstanceRun?)null);

            Assert.IsTrue(await _service.UseGateAsync(_session));

            Assert.AreEqual(Game18NConstString.PortalHasClosed, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
            NoOneEntered();
        }

        [TestMethod]
        public async Task TheGateIsShutWhileTheSidesRaidIsClosed()
        {
            _factionWar.Setup(s => s.OpenRaid(FactionType.Angel)).Returns((Act4RaidType?)null);

            Assert.IsFalse(await _service.UseGateAsync(_session));

            NoOneEntered();
        }

        [TestMethod]
        public async Task TheGateIsOutOfReachFromAfar()
        {
            _session.Character.PositionX = FactionWar.RaidGateX + 3;

            Assert.IsFalse(await _service.UseGateAsync(_session));

            NoOneEntered();
        }

        [TestMethod]
        public async Task TheGateIsOnlyOnTheRaidMap()
        {
            _session.Character.MapInstance = World.GetBaseMap(TestWorld.TestMapId);

            Assert.IsFalse(await _service.UseGateAsync(_session));

            NoOneEntered();
        }

        private void NoOneEntered() => _mapChange.Verify(s => s.ChangeMapInstanceAsync(It.IsAny<ClientSession>(),
            It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);

        private ScriptedInstanceRun NewRun(IInstanceScript script, IInstanceRunHost host)
        {
            var room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 4202,
                    NameI18NKey = "raid",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.Act4Instance);
            var run = new ScriptedInstanceRun(host, script,
                new Dictionary<int, MapInstance> { [0] = room },
                new NpcMonsterCatalog([]),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
            _runs.Add(run);
            return run;
        }
    }
}
