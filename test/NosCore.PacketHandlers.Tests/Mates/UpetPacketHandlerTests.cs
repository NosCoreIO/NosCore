//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.PacketHandlers.Mates;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Mates
{
    [TestClass]
    public class UpetPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private MapInstance _map;
        private Mock<IBattleService> _battleService;
        private UpetPacketHandler _handler;
        private Mate _pet;
        private MonsterComponentBundle _monster;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.GenerateSessionAsync();
            _map = _session.Character.MapInstance;
            _battleService = new Mock<IBattleService>();
            _battleService.Setup(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(true);
            _handler = new UpetPacketHandler(
                new MateAttackService(_battleService.Object, World.DistanceCalculator, World.Clock),
                World.SessionRegistry,
                new Mock<ILogger<UpetPacketHandler>>().Object,
                World.LogLanguageLocalizer);

            _pet = TestMates.Pet(_session.Character.CharacterId);
            _pet.NpcMonster.BasicRange = 1;
            _pet.NpcMonster.BasicCooldown = 10;
            _session.Character.Mates[_pet.MateTransportId] = _pet;
            await _pet.SpawnAsync(_session.Character, _map, 5, 5);

            _map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 9100, MapId = 0, MapX = 6, MapY = 5, VNum = 2 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 2, MaxHp = 100, Level = 1 } });
            _monster = _map.Monsters.Single(m => m.VisualId == 9100);
        }

        [TestMethod]
        public async Task APetAttacksAMonsterBesideIt()
        {
            await new Spec("A pet's basic attack on a monster within its basic range goes through")
                .WhenAsync(ThePetAttacks)
                .Then(TheBattleServiceIsAsked_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetCannotAttackAgainBeforeItsBasicCooldown()
        {
            await new Spec("A second basic attack before the creature's basic cooldown is over is ignored")
                .WhenAsync(ThePetAttacks)
                .AndAsync(ThePetAttacks)
                .Then(TheBattleServiceIsAsked_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetAttacksAgainOnceItsBasicCooldownIsOver()
        {
            await new Spec("A basic attack after the creature's basic cooldown goes through again")
                .WhenAsync(ThePetAttacks)
                .And(TimePasses_Milliseconds, 1100)
                .AndAsync(ThePetAttacks)
                .Then(TheBattleServiceIsAsked_Times, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetCannotAttackBeyondItsBasicRange()
        {
            await new Spec("A basic attack on a monster beyond the creature's basic range from the pet is ignored")
                .Given(TheMonsterStandsAt_, (short)15)
                .WhenAsync(ThePetAttacks)
                .Then(TheBattleServiceIsAsked_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetCannotAttackATargetOnAnotherMap()
        {
            await new Spec("A basic attack on a player standing on another map is ignored")
                .WhenAsync(ThePetAttacksAPlayerOnAnotherMap)
                .Then(TheBattleServiceIsAsked_Times, 0)
                .ExecuteAsync();
        }

        private void TheMonsterStandsAt_(short x) => _monster.PositionX = x;

        private void TimePasses_Milliseconds(int ms) =>
            World.Clock.Advance(NodaTime.Duration.FromMilliseconds(ms));

        private Task ThePetAttacks() => _handler.ExecuteAsync(new UpetPacket
        {
            MateTransportId = _pet.MateTransportId,
            TargetType = VisualType.Monster,
            TargetId = _monster.VisualId
        }, _session);

        private async Task ThePetAttacksAPlayerOnAnotherMap()
        {
            var other = await World.GenerateSessionAsync();
            other.Character.MapInstance = World.GetBaseMap(1);
            other.Character.PositionX = 5;
            other.Character.PositionY = 5;
            await _handler.ExecuteAsync(new UpetPacket
            {
                MateTransportId = _pet.MateTransportId,
                TargetType = VisualType.Player,
                TargetId = other.Character.VisualId
            }, _session);
        }

        private void TheBattleServiceIsAsked_Times(int times) =>
            _battleService.Verify(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()),
                Times.Exactly(times));
    }
}
