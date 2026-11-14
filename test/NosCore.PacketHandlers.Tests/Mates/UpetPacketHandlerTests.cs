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
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.PacketHandlers.Mates;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Mates
{
    [TestClass]
    public class UpetPacketHandlerTests
    {
        private const short PetSkill = 664;
        private const short CostlyPetSkill = 663;
        private const short MonsterSkill = 1100;
        private const short SomeoneElsesSkill = 1673;
        private const int StartingMp = 10;

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
            _session = await World.CreateSessionAsync();
            _map = _session.Character.MapInstance;
            _battleService = World.Freeze<Mock<IBattleService>>();
            _battleService.Setup(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(true);
            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = PetSkill, CastId = 2, SkillType = 5, Range = 1, Cooldown = 350, MpCost = 4 },
                new() { SkillVNum = CostlyPetSkill, CastId = 3, SkillType = 5, Range = 1, Cooldown = 700, MpCost = 40 },
                new() { SkillVNum = MonsterSkill, CastId = 1, SkillType = 4, Range = 1 },
                new() { SkillVNum = SomeoneElsesSkill, CastId = 3, SkillType = 5, Range = 1 }
            });
            World.Inject(new List<NpcMonsterSkillDto>
            {
                new() { NpcMonsterSkillId = 1, NpcMonsterVNum = 333, SkillVNum = CostlyPetSkill },
                new() { NpcMonsterSkillId = 2, NpcMonsterVNum = 333, SkillVNum = PetSkill },
                new() { NpcMonsterSkillId = 3, NpcMonsterVNum = 333, SkillVNum = MonsterSkill },
                new() { NpcMonsterSkillId = 4, NpcMonsterVNum = 806, SkillVNum = SomeoneElsesSkill }
            });
            _handler = World.UseReal<IMateAttackService, MateAttackService>()
                .UseReal<INpcCombatCatalog, NpcCombatCatalog>()
                .UseReal<IPetSkillBarService, PetSkillBarService>()
                .UseReal<IPetSkillService, PetSkillService>()
                .Create<UpetPacketHandler>();

            _pet = TestMates.Pet(_session.Character.CharacterId);
            _pet.NpcMonster.BasicRange = 1;
            _pet.NpcMonster.BasicCooldown = 10;
            _session.Character.Mates[_pet.MateTransportId] = _pet;
            await _pet.SpawnAsync(_session.Character, _map, 5, 5);

            _map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 9100, MapId = 0, MapX = 6, MapY = 5, VNum = 2 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 2, MaxHp = 100, Level = 1 } });
            _monster = _map.EnumerateMonsters().Single(m => m.VisualId == 9100);
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

        [TestMethod]
        public async Task APetCastsASkillOfItsBar()
        {
            await new Spec("A team pet hits a monster with the skill of its bar the order names, spends the skill's mana and tells its owner")
                .WhenAsync(ThePetIsOrderedToUse_, PetSkill)
                .Then(ThePetShouldHitTheMonsterWith_, PetSkill)
                .And(ThePetShouldHave_Mp, StartingMp - 4)
                .And(TheOwnerShouldSeeThePetsMana)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillThatIsNotOnThePetsBarIsRefused()
        {
            await new Spec("A pet uses neither another creature's skill nor one of its own that its bar does not hold")
                .WhenAsync(ThePetIsOrderedToUse_, SomeoneElsesSkill)
                .AndAsync(ThePetIsOrderedToUse_, MonsterSkill)
                .Then(TheBattleServiceIsAsked_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetSkillNeedsEnoughMana()
        {
            await new Spec("A skill costing more mana than the pet has is not cast")
                .WhenAsync(ThePetIsOrderedToUse_, CostlyPetSkill)
                .Then(TheBattleServiceIsAsked_Times, 0)
                .And(ThePetShouldHave_Mp, StartingMp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetSkillWaitsForItsCooldown()
        {
            await new Spec("A pet skill cannot be cast again before its own cooldown is over, whatever the basic attack's is")
                .GivenAsync(ThePetIsOrderedToUse_, PetSkill)
                .And(TimePasses_Milliseconds, 34000)
                .WhenAsync(ThePetIsOrderedToUse_, PetSkill)
                .Then(TheBattleServiceIsAsked_Times, 1)
                .And(ThePetShouldHave_Mp, StartingMp - 4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetSkillIsBackAfterItsCooldown()
        {
            await new Spec("A pet skill can be cast again once its cooldown is over")
                .GivenAsync(ThePetIsOrderedToUse_, PetSkill)
                .And(TimePasses_Milliseconds, 35000)
                .WhenAsync(ThePetIsOrderedToUse_, PetSkill)
                .Then(TheBattleServiceIsAsked_Times, 2)
                .And(ThePetShouldHave_Mp, StartingMp - 8)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheOwnerIsToldWhenTheSlotIsReadyAgain()
        {
            await new Spec("Once a pet skill has cooled down, the slot of the bar it sits in is handed to the map to tell the owner, once")
                .GivenAsync(ThePetIsOrderedToUse_, PetSkill)
                .And(TimePasses_Milliseconds, 34000)
                .And(NoSlotIsReadyYet)
                .When(TimePasses_Milliseconds, 1000)
                .Then(TheMapShouldTellTheOwnerSlot_IsReady, (short)1)
                .And(NoSlotIsReadyYet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetSkillDoesNotReachAMonsterFarFromThePet()
        {
            await new Spec("A monster beyond the skill's range from the pet is not hit")
                .Given(TheMonsterStandsAt_, (short)15)
                .WhenAsync(ThePetIsOrderedToUse_, PetSkill)
                .Then(TheBattleServiceIsAsked_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARefusedCastCostsNothing()
        {
            await new Spec("A cast the fight refuses spends no mana and starts no cooldown")
                .Given(TheFightRefusesTheNextCast)
                .AndAsync(ThePetIsOrderedToUse_, PetSkill)
                .WhenAsync(ThePetIsOrderedToUse_, PetSkill)
                .Then(TheBattleServiceIsAsked_Times, 2)
                .And(ThePetShouldHave_Mp, StartingMp - 4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetSkillOnlyGoesForAMonster()
        {
            await new Spec("A pet skill aimed at a player is ignored")
                .WhenAsync(ThePetIsOrderedToUseItsSkillOnItsOwner)
                .Then(TheBattleServiceIsAsked_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetLeftAtHomeUsesNoSkill()
        {
            await new Spec("A pet that is not in the team casts nothing")
                .Given(ThePetIsNotInTheTeam)
                .WhenAsync(ThePetIsOrderedToUse_, PetSkill)
                .Then(TheBattleServiceIsAsked_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SomeoneElsesPetTakesNoOrder()
        {
            await new Spec("A pet skill ordered by a player who does not own the pet is ignored")
                .WhenAsync(AnotherPlayerOrdersThePetToUseItsSkill)
                .Then(TheBattleServiceIsAsked_Times, 0)
                .ExecuteAsync();
        }

        private void TheMonsterStandsAt_(short x) => _monster.PositionX = x;

        private void ThePetIsNotInTheTeam() => _pet.IsTeamMember = false;

        private void TheFightRefusesTheNextCast() =>
            _battleService.SetupSequence(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(false)
                .ReturnsAsync(true);

        private Task ThePetIsOrderedToUse_(short skillVNum) => _handler.ExecuteAsync(new UpetPacket
        {
            MateTransportId = _pet.MateTransportId,
            TargetType = VisualType.Monster,
            TargetId = _monster.VisualId,
            SkillVNum = skillVNum
        }, _session);

        private Task ThePetIsOrderedToUseItsSkillOnItsOwner() => _handler.ExecuteAsync(new UpetPacket
        {
            MateTransportId = _pet.MateTransportId,
            TargetType = VisualType.Player,
            TargetId = _session.Character.VisualId,
            SkillVNum = PetSkill
        }, _session);

        private async Task AnotherPlayerOrdersThePetToUseItsSkill()
        {
            var other = await World.CreateSessionAsync();
            await _handler.ExecuteAsync(new UpetPacket
            {
                MateTransportId = _pet.MateTransportId,
                TargetType = VisualType.Monster,
                TargetId = _monster.VisualId,
                SkillVNum = PetSkill
            }, other);
        }

        private void ThePetShouldHitTheMonsterWith_(short skill) =>
            _battleService.Verify(b => b.Hit(
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Npc && e.VisualId == _pet.MateTransportId),
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Monster && e.VisualId == _monster.VisualId),
                It.Is<HitArguments>(a => a.Skill != null && a.Skill.SkillVNum == skill)), Times.Once);

        private List<(long OwnerId, short Slot)> ReadySlots()
        {
            var ready = new List<(long OwnerId, short Slot)>();
            new SkillResetSystem().TakeReadyPetSlots(_map.EcsWorld, World.Clock.GetCurrentInstant(), ready);
            return ready;
        }

        private void NoSlotIsReadyYet() => Assert.AreEqual(0, ReadySlots().Count);

        private void TheMapShouldTellTheOwnerSlot_IsReady(short slot) =>
            CollectionAssert.AreEqual(new[] { (_session.Character.CharacterId, slot) }, ReadySlots());

        private void ThePetShouldHave_Mp(int mp)
        {
            Assert.AreEqual(mp, _pet.Entity.NotNull().Mp);
            Assert.AreEqual(mp, _pet.Mp);
        }

        private void TheOwnerShouldSeeThePetsMana() =>
            Assert.IsTrue(_session.LastPackets.OfType<PstPacket>().Any(p => p.VisualId == _pet.MateTransportId));

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
            var other = await World.CreateSessionAsync();
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
