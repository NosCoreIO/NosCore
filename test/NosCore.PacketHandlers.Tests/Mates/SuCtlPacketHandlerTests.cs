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
        private const short PetSkillVNum = 700;
        private const int PetCastId = 3;
        private const int NearMonsterId = 200;
        private const int FarMonsterId = 201;
        private const short LongReachSkillVNum = 701;

        private SuCtlPacketHandler _handler = null!;
        private ClientSession _session = null!;
        private ClientSession _otherSession = null!;
        private MapInstance _map = null!;
        private Mock<IBattleService> _battleService = null!;
        private Mock<INpcCombatCatalog> _catalog = null!;
        private Mate _pet = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _otherSession = await TestHelpers.Instance.GenerateSessionAsync();
            _map = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            _session.Character.MapInstance = _map;
            _otherSession.Character.MapInstance = _map;

            var monster = new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 };
            _map.LoadMonsters(new List<MapMonsterDto>
            {
                new() { MapMonsterId = NearMonsterId, MapId = 0, MapX = 6, MapY = 5, VNum = 1 },
                new() { MapMonsterId = FarMonsterId, MapId = 0, MapX = 0, MapY = 0, VNum = 1 },
            }, new List<NpcMonsterDto> { monster });

            _battleService = new Mock<IBattleService>();
            _battleService.Setup(s => s.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(true);
            _catalog = new Mock<INpcCombatCatalog>();
            _catalog.Setup(c => c.GetSkills(It.IsAny<short>())).Returns(new List<NpcMonsterSkillDto>
            {
                new() { NpcMonsterVNum = 333, SkillVNum = PetSkillVNum }
            });
            var skills = new List<SkillDto>
            {
                new() { SkillVNum = PetSkillVNum, CastId = PetCastId, Range = 2, TargetType = 0, Cooldown = 10 },
                new() { SkillVNum = LongReachSkillVNum, CastId = 0, Range = 20, TargetType = 0, Cooldown = 10 }
            };
            _handler = new SuCtlPacketHandler(MateControl.Service(_battleService.Object, _catalog.Object, skills));
        }

        [TestMethod]
        public async Task AnOwnPetHitsAMonsterWithItsSkill()
        {
            await new Spec("An own pet hits a monster in reach with a skill it knows, through the battle service")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerOrdersCast_OnMonster_, PetCastId, NearMonsterId)
                .Then(ThePetShouldHitMonster_WithCast_, NearMonsterId, PetCastId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherPlayersPetIsIgnored()
        {
            await new Spec("A suctl naming another player's pet does not attack")
                .GivenAsync(APetOf_IsOut, "other player")
                .WhenAsync(TheOwnerOrdersCast_OnMonster_, PetCastId, NearMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillThePetDoesNotKnowIsRefused()
        {
            await new Spec("A cast id outside the pet's own skills is refused, not swapped for its basic attack")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerOrdersCast_OnMonster_, 9, NearMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMonsterOutOfReachIsRefused()
        {
            await new Spec("A monster beyond the skill's range is not hit")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerOrdersCast_OnMonster_, PetCastId, FarMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnknownMonsterIsRefused()
        {
            await new Spec("A target id that is not a monster on the map is refused")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerOrdersCast_OnMonster_, PetCastId, 999)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlayerIsNotATarget()
        {
            await new Spec("A pet cannot be ordered at a player")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerOrdersCastOnTheOtherPlayer)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillWaitsForItsCooldown()
        {
            await new Spec("A pet's skill cannot be cast again before its cooldown is over")
                .GivenAsync(APetOf_IsOut, "owner")
                .AndAsync(TheOwnerOrdersCast_OnMonster_, PetCastId, NearMonsterId)
                .WhenAsync(TheOwnerOrdersCast_OnMonster_, PetCastId, NearMonsterId)
                .Then(ThePetShouldHaveHit_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillIsBackAfterItsCooldown()
        {
            await new Spec("A pet's skill can be cast again once its cooldown is over")
                .GivenAsync(APetOf_IsOut, "owner")
                .AndAsync(TheOwnerOrdersCast_OnMonster_, PetCastId, NearMonsterId)
                .And(_SecondsPass, 1)
                .WhenAsync(TheOwnerOrdersCast_OnMonster_, PetCastId, NearMonsterId)
                .Then(ThePetShouldHaveHit_Times, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CastZeroIsCheckedOnTheSkillTheResolverCasts()
        {
            await new Spec("Cast 0 is the pet's first skill, so a far monster is out of that skill's reach")
                .GivenAsync(APetOf_IsOut, "owner")
                .And(ThePetsSecondSkillHasCastZeroAndAMuchLongerRange)
                .WhenAsync(TheOwnerOrdersCast_OnMonster_, 0, FarMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        private void ThePetsSecondSkillHasCastZeroAndAMuchLongerRange()
        {
            _catalog.Setup(c => c.GetSkills(It.IsAny<short>())).Returns(new List<NpcMonsterSkillDto>
            {
                new() { NpcMonsterVNum = 333, SkillVNum = PetSkillVNum },
                new() { NpcMonsterVNum = 333, SkillVNum = LongReachSkillVNum }
            });
        }

        private async Task APetOf_IsOut(string whose)
        {
            var owner = whose == "owner" ? _session : _otherSession;
            _pet = TestMates.Pet(owner.Character.CharacterId);
            owner.Character.Mates[_pet.MateTransportId] = _pet;
            await _pet.SpawnAsync(owner.Character, _map, 5, 5);
        }

        private void _SecondsPass(int seconds)
        {
            TestHelpers.Instance.Clock.Advance(Duration.FromSeconds(seconds));
        }

        private Task TheOwnerOrdersCast_OnMonster_(int castId, int monsterId) =>
            _handler.ExecuteAsync(new SuCtlPacket
            {
                CastId = castId,
                MateTransportId = _pet.MateTransportId,
                TargetType = VisualType.Monster,
                TargetId = monsterId
            }, _session);

        private Task TheOwnerOrdersCastOnTheOtherPlayer() =>
            _handler.ExecuteAsync(new SuCtlPacket
            {
                CastId = PetCastId,
                MateTransportId = _pet.MateTransportId,
                TargetType = VisualType.Player,
                TargetId = _otherSession.Character.VisualId
            }, _session);

        private void ThePetShouldHitMonster_WithCast_(int monsterId, int castId)
        {
            _battleService.Verify(s => s.Hit(
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Npc && e.VisualId == _pet.MateTransportId),
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Monster && e.VisualId == monsterId),
                It.Is<HitArguments>(a => a.SkillId == castId)), Times.Once);
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
