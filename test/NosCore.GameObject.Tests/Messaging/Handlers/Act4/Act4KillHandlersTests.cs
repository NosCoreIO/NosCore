//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Act4;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FactionWarService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Act4
{
    [TestClass]
    public class Act4KillHandlersTests
    {
        private const short Monster = 2;

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _glacernon;
        private Mock<IFactionWarService> _factionWar;
        private PlayerComponentBundle? _credited;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _glacernon = _session.Character.MapInstance;
            _glacernon.IsAct4 = true;
            _glacernon.MapInstanceType = MapInstanceType.BaseMapInstance;
            _factionWar = World.Freeze<Mock<IFactionWarService>>();
            _factionWar.Setup(s => s.RecordMonsterKillAsync(It.IsAny<PlayerComponentBundle>()))
                .Callback<PlayerComponentBundle>(killer => _credited = killer)
                .ReturnsAsync(true);
        }

        [TestMethod]
        public async Task KillingAMonsterInGlacernonMovesTheKillersSide()
        {
            await KillAsync(Spawn(Monster), _session.Character);

            Assert.AreEqual(_session.Character.CharacterId, _credited.NotNull().CharacterId);
        }

        [TestMethod]
        public async Task APetsKillCountsForItsOwner()
        {
            var pet = TestMates.Pet(_session.Character.CharacterId);
            _session.Character.Mates[pet.MateTransportId] = pet;
            await pet.SpawnAsync(_session.Character, _glacernon, 5, 5);

            await KillAsync(Spawn(Monster), pet.Entity.NotNull());

            Assert.AreEqual(_session.Character.CharacterId, _credited.NotNull().CharacterId);
        }

        [TestMethod]
        public async Task AKillOutsideGlacernonMovesNoSide()
        {
            _glacernon.IsAct4 = false;

            await KillAsync(Spawn(Monster), _session.Character);

            _factionWar.Verify(s => s.RecordMonsterKillAsync(It.IsAny<PlayerComponentBundle>()), Times.Never);
        }

        [TestMethod]
        public async Task AKillInsideAGlacernonInstanceMovesNoSide()
        {
            _glacernon.MapInstanceType = MapInstanceType.Act4Instance;

            await KillAsync(Spawn(Monster), _session.Character);

            _factionWar.Verify(s => s.RecordMonsterKillAsync(It.IsAny<PlayerComponentBundle>()), Times.Never);
        }

        [TestMethod]
        public async Task AMonsterWithNoKillerMovesNoSide()
        {
            await KillAsync(Spawn(Monster), null);

            _factionWar.Verify(s => s.RecordMonsterKillAsync(It.IsAny<PlayerComponentBundle>()), Times.Never);
        }

        [TestMethod]
        public async Task KillingMukrajuMovesNoSideButReportsTheGuardian()
        {
            var mukraju = Spawn(FactionWarService.Mukraju);

            await KillAsync(mukraju, _session.Character);

            _factionWar.Verify(s => s.RecordMonsterKillAsync(It.IsAny<PlayerComponentBundle>()), Times.Never);
            _factionWar.Verify(s => s.GuardianDiedAsync(It.IsAny<MonsterComponentBundle>()), Times.Once);
        }

        [TestMethod]
        public async Task AnotherMonsterIsNoGuardian()
        {
            await KillAsync(Spawn(Monster), _session.Character);

            _factionWar.Verify(s => s.GuardianDiedAsync(It.IsAny<MonsterComponentBundle>()), Times.Never);
        }

        [TestMethod]
        public async Task MukrajuOutsideGlacernonIsNoGuardian()
        {
            _glacernon.IsAct4 = false;

            await KillAsync(Spawn(FactionWarService.Mukraju), _session.Character);

            _factionWar.Verify(s => s.GuardianDiedAsync(It.IsAny<MonsterComponentBundle>()), Times.Never);
        }

        private MonsterComponentBundle Spawn(short vnum) =>
            _glacernon.SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = vnum, MaxHp = 1000 }, 6, 5, false);

        private async Task KillAsync(MonsterComponentBundle monster, IAliveEntity? killer)
        {
            var died = new EntityDiedEvent(monster, killer);
            await World.Create<MonsterKillFactionHandler>().Handle(died);
            await World.Create<GuardianDiedHandler>().Handle(died);
        }
    }
}
