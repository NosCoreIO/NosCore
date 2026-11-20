//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
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
    public class SuCtlPacketHandlerTests
    {
        private TestWorld World;
        private const short PetSkillVNum = 700;
        private const int PetCastId = 3;
        private const int NearMonsterId = 200;
        private const int FarMonsterId = 201;

        private SuCtlPacketHandler _handler;
        private ClientSession _session;
        private ClientSession _otherSession;
        private MapInstance _map;
        private Mock<IBattleService> _battleService;
        private Mate _pet;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _otherSession = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            _session.Character.MapInstance = _map;
            _otherSession.Character.MapInstance = _map;

            var monster = new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 };
            _map.LoadMonsters(new List<MapMonsterDto>
            {
                new() { MapMonsterId = NearMonsterId, MapId = 0, MapX = 6, MapY = 5, VNum = 1 },
                new() { MapMonsterId = FarMonsterId, MapId = 0, MapX = 0, MapY = 0, VNum = 1 },
            }, new NosCore.GameObject.Services.BattleService.NpcMonsterCatalog(new List<NpcMonsterDto> { monster }));

            _battleService = World.Freeze<Mock<IBattleService>>();
            _battleService.Setup(s => s.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(true);
            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = PetSkillVNum, CastId = PetCastId, Range = 20, TargetType = 0, Cooldown = 10 }
            });
            World.Inject(new List<NpcMonsterSkillDto>
            {
                new() { NpcMonsterSkillId = 1, NpcMonsterVNum = 333, SkillVNum = PetSkillVNum }
            });
            _handler = World.UseReal<IMateControlService, MateControlService>()
                .UseReal<IMateAttackService, MateAttackService>()
                .UseReal<INpcCombatCatalog, NpcCombatCatalog>()
                .Create<SuCtlPacketHandler>();
        }

        [TestMethod]
        public async Task AnOwnPetHitsAMonsterWithItsBasicAttack()
        {
            await new Spec("An own pet sent at a monster in reach hits it with its basic attack, through the battle service")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerSendsThePetAtMonster_, NearMonsterId)
                .Then(ThePetShouldHitMonster_WithItsBasicAttack, NearMonsterId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheFirstFieldOfTheOrderPicksNoSkill()
        {
            await new Spec("An order whose first field matches one of the pet's skills still sends the pet in with its basic attack")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerSendsThePetAtMonster_WithFirstField_, NearMonsterId, PetCastId)
                .Then(ThePetShouldHitMonster_WithItsBasicAttack, NearMonsterId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetWithoutSkillsStillAttacks()
        {
            await new Spec("A pet that knows no skill still hits the monster it is sent at")
                .GivenAsync(APetWithoutSkillsIsOut)
                .WhenAsync(TheOwnerSendsThePetAtMonster_, NearMonsterId)
                .Then(ThePetShouldHitMonster_WithItsBasicAttack, NearMonsterId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherPlayersPetIsIgnored()
        {
            await new Spec("A suctl naming another player's pet does not attack")
                .GivenAsync(APetOf_IsOut, "other player")
                .WhenAsync(TheOwnerSendsThePetAtMonster_, NearMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMonsterOutOfReachIsRefused()
        {
            await new Spec("A monster beyond the pet's basic range is not hit, however far its skills reach")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerSendsThePetAtMonster_, FarMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnknownMonsterIsRefused()
        {
            await new Spec("A target id that is not a monster on the map is refused")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerSendsThePetAtMonster_, 999)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlayerIsNotATarget()
        {
            await new Spec("A pet cannot be sent at a player")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerSendsThePetAtTheOtherPlayer)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAttackWaitsForTheBasicCooldown()
        {
            await new Spec("A pet cannot hit again before its basic cooldown is over")
                .GivenAsync(APetOf_IsOut, "owner")
                .AndAsync(TheOwnerSendsThePetAtMonster_, NearMonsterId)
                .WhenAsync(TheOwnerSendsThePetAtMonster_, NearMonsterId)
                .Then(ThePetShouldHaveHit_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAttackIsBackAfterTheBasicCooldown()
        {
            await new Spec("A pet hits again once its basic cooldown is over")
                .GivenAsync(APetOf_IsOut, "owner")
                .AndAsync(TheOwnerSendsThePetAtMonster_, NearMonsterId)
                .And(_SecondsPass, 1)
                .WhenAsync(TheOwnerSendsThePetAtMonster_, NearMonsterId)
                .Then(ThePetShouldHaveHit_Times, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEscortHitsAMonsterItsLeaderPoints()
        {
            await new Spec("An escort following the player hits the monster the player sends it at with its basic attack")
                .Given(AnEscortLedBy_, "owner")
                .WhenAsync(TheOwnerSendsTheEscortAtMonster_, NearMonsterId)
                .Then(TheEscortShouldHitMonster_WithItsBasicAttack, NearMonsterId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEscortLedByAnotherPlayerIgnoresTheOrder()
        {
            await new Spec("A suctl naming an escort that follows another player does not attack")
                .Given(AnEscortLedBy_, "other player")
                .WhenAsync(TheOwnerSendsTheEscortAtMonster_, NearMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        private NosCore.GameObject.Ecs.NpcComponentBundle _escort;

        private void AnEscortLedBy_(string whose)
        {
            var leader = whose == "owner" ? _session : _otherSession;
            _escort = _map.SummonNpc(new NpcMonsterDto { NpcMonsterVNum = 318, MaxHp = 10, Speed = 10, BasicRange = 1 }, 5, 5);
            _escort.IsEscort = true;
            _escort.LeaderId = leader.Character.CharacterId;
        }

        private Task TheOwnerSendsTheEscortAtMonster_(int monsterId) =>
            _handler.ExecuteAsync(new SuCtlPacket
            {
                MateTransportId = _escort.VisualId,
                TargetType = VisualType.Monster,
                TargetId = monsterId
            }, _session);

        private Task APetOf_IsOut(string whose) => SpawnAsync(whose, TestMates.Pet((whose == "owner" ? _session : _otherSession).Character.CharacterId));

        private Task APetWithoutSkillsIsOut() => SpawnAsync("owner", TestMates.Pet(_session.Character.CharacterId, vnum: 334));

        private async Task SpawnAsync(string whose, Mate pet)
        {
            var owner = whose == "owner" ? _session : _otherSession;
            _pet = pet;
            _pet.NpcMonster.BasicRange = 1;
            _pet.NpcMonster.BasicCooldown = 10;
            owner.Character.Mates[_pet.MateTransportId] = _pet;
            await _pet.SpawnAsync(owner.Character, _map, 5, 5);
        }

        private void _SecondsPass(int seconds)
        {
            World.Clock.Advance(Duration.FromSeconds(seconds));
        }

        private Task TheOwnerSendsThePetAtMonster_(int monsterId) => TheOwnerSendsThePetAtMonster_WithFirstField_(monsterId, 0);

        private Task TheOwnerSendsThePetAtMonster_WithFirstField_(int monsterId, int firstField) =>
            _handler.ExecuteAsync(new SuCtlPacket
            {
                CastId = firstField,
                MateTransportId = _pet.MateTransportId,
                TargetType = VisualType.Monster,
                TargetId = monsterId
            }, _session);

        private Task TheOwnerSendsThePetAtTheOtherPlayer() =>
            _handler.ExecuteAsync(new SuCtlPacket
            {
                MateTransportId = _pet.MateTransportId,
                TargetType = VisualType.Player,
                TargetId = _otherSession.Character.VisualId
            }, _session);

        private void ThePetShouldHitMonster_WithItsBasicAttack(int monsterId) =>
            ShouldHitWithABasicAttack(_pet.MateTransportId, monsterId);

        private void TheEscortShouldHitMonster_WithItsBasicAttack(int monsterId) =>
            ShouldHitWithABasicAttack(_escort.VisualId, monsterId);

        private void ShouldHitWithABasicAttack(long attackerId, int monsterId)
        {
            _battleService.Verify(s => s.Hit(
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Npc && e.VisualId == attackerId),
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Monster && e.VisualId == monsterId),
                It.Is<HitArguments>(a => a.BasicAttack && a.Skill == null)), Times.Once);
        }

        private void ThePetShouldHaveHit_Times(int times)
        {
            _battleService.Verify(s => s.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(),
                It.IsAny<HitArguments>()), Times.Exactly(times));
        }

        private void NothingShouldBeHit()
        {
            ThePetShouldHaveHit_Times(0);
        }
    }
}
