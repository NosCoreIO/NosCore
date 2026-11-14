//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Algorithm.RarityPointService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.PacketHandlers.Mates;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using Item = NosCore.GameObject.Services.ItemGenerationService.Item.Item;

namespace NosCore.PacketHandlers.Tests.Mates
{
    [TestClass]
    public class UpsPacketHandlerTests
    {
        private TestWorld World;
        private const short PartnerSpecialist = 900;
        private const byte SkillSet = 9;
        private const short AttackSkill = 1315;
        private const short CostlySkill = 1316;
        private const int NearMonsterId = 200;
        private const int FarMonsterId = 201;

        private ClientSession _session;
        private MapInstance _map;
        private Mock<IBattleService> _battleService;
        private ItemGenerationService _items;
        private PartnerSpecialistService _specialists;
        private UpsPacketHandler _handler;
        private Mate _partner;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            _session.Character.MapInstance = _map;

            var monster = new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 };
            _map.LoadMonsters(new List<MapMonsterDto>
            {
                new() { MapMonsterId = NearMonsterId, MapId = 0, MapX = 8, MapY = 5, VNum = 1 },
                new() { MapMonsterId = FarMonsterId, MapId = 0, MapX = 40, MapY = 40, VNum = 1 },
            }, new List<NpcMonsterDto> { monster });

            _battleService = World.Freeze<Mock<IBattleService>>();
            _battleService.Setup(s => s.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(true);
            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = AttackSkill, CastId = 0, SkillType = 5, UpgradeType = SkillSet, Range = 12, TargetRange = 3, Cooldown = 60, MpCost = 4 },
                new() { SkillVNum = CostlySkill, CastId = 1, SkillType = 5, UpgradeType = SkillSet, Range = 12, Cooldown = 300, MpCost = 50 }
            });
            _items = new ItemGenerationService(new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = PartnerSpecialist, ItemType = ItemType.Specialist,
                    EquipmentSlot = EquipmentType.Sp, ItemSubType = 4, Morph = 2333, PartnerSkillSet = SkillSet
                }
            }, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());
            _specialists = World.Create<PartnerSpecialistService>();
            _handler = new UpsPacketHandler(_specialists);
        }

        [TestMethod]
        public async Task ATransformedPartnerCastsTheSkillInTheSlot()
        {
            await new Spec("A transformed partner hits a monster with the skill in the slot it was told to use, and spends its mana")
                .GivenAsync(APartnerWearingItsCardIsOut)
                .AndAsync(ThePartnerIsTransformed)
                .WhenAsync(TheOwnerCastsSlot_OnMonster_, 0, NearMonsterId)
                .Then(ThePartnerShouldHitMonster_With_, NearMonsterId, AttackSkill)
                .And(ThePartnerShouldHave_Mp, 6)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APartnerThatIsNotTransformedHasNoSkills()
        {
            await new Spec("A partner that is not transformed has no skill bar to cast from")
                .GivenAsync(APartnerWearingItsCardIsOut)
                .WhenAsync(TheOwnerCastsSlot_OnMonster_, 0, NearMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillNeedsEnoughMana()
        {
            await new Spec("A skill costing more mana than the partner has is not cast")
                .GivenAsync(APartnerWearingItsCardIsOut)
                .AndAsync(ThePartnerIsTransformed)
                .WhenAsync(TheOwnerCastsSlot_OnMonster_, 1, NearMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMonsterOutOfReachIsRefused()
        {
            await new Spec("A monster out of the skill's reach is not hit")
                .GivenAsync(APartnerWearingItsCardIsOut)
                .AndAsync(ThePartnerIsTransformed)
                .WhenAsync(TheOwnerCastsSlot_OnMonster_, 0, FarMonsterId)
                .Then(NothingShouldBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillWaitsForItsCooldown()
        {
            await new Spec("A partner skill cannot be cast again before its cooldown is over")
                .GivenAsync(APartnerWearingItsCardIsOut)
                .AndAsync(ThePartnerIsTransformed)
                .AndAsync(TheOwnerCastsSlot_OnMonster_, 0, NearMonsterId)
                .And(_SecondsPass, 5)
                .WhenAsync(TheOwnerCastsSlot_OnMonster_, 0, NearMonsterId)
                .Then(ThePartnerShouldHaveHit_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillIsBackAfterItsCooldown()
        {
            await new Spec("A partner skill can be cast again once its cooldown is over")
                .GivenAsync(APartnerWearingItsCardIsOut)
                .AndAsync(ThePartnerIsTransformed)
                .AndAsync(TheOwnerCastsSlot_OnMonster_, 0, NearMonsterId)
                .And(_SecondsPass, 6)
                .WhenAsync(TheOwnerCastsSlot_OnMonster_, 0, NearMonsterId)
                .Then(ThePartnerShouldHaveHit_Times, 2)
                .ExecuteAsync();
        }

        private async Task APartnerWearingItsCardIsOut()
        {
            _partner = TestMates.Partner(_session.Character.CharacterId);
            _session.Character.Mates[_partner.MateTransportId] = _partner;
            await _partner.SpawnAsync(_session.Character, _map, 5, 5);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(PartnerSpecialist, 1), _session.Character.CharacterId),
                NoscorePocketType.Equipment, 0);
            await new PartnerEquipmentService(_specialists).WearAsync(_session.Character, 0, 1);
        }

        private async Task ThePartnerIsTransformed()
        {
            await _specialists.ToggleAsync(_session.Character);
            _SecondsPass(5);
            await _specialists.TransformAsync(_session.Character);
            Assert.IsNotNull(_partner.SpecialistMorph);
        }

        private void _SecondsPass(int seconds) => World.Clock.Advance(Duration.FromSeconds(seconds));

        private Task TheOwnerCastsSlot_OnMonster_(int slot, int monsterId) =>
            _handler.ExecuteAsync(new UpsPacket
            {
                MateTransportId = _partner.MateTransportId,
                TargetType = VisualType.Monster,
                TargetId = monsterId,
                SkillSlot = slot
            }, _session);

        private void ThePartnerShouldHitMonster_With_(int monsterId, short skill) =>
            _battleService.Verify(s => s.Hit(
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Npc && e.VisualId == _partner.MateTransportId),
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Monster && e.VisualId == monsterId),
                It.Is<HitArguments>(a => a.Skill != null && a.Skill.SkillVNum == skill)), Times.Once);

        private void ThePartnerShouldHave_Mp(int mp)
        {
            Assert.AreEqual(mp, _partner.Entity.NotNull().Mp);
            Assert.AreEqual(mp, _partner.CurrentMp());
        }

        private void ThePartnerShouldHaveHit_Times(int times) =>
            _battleService.Verify(s => s.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(),
                It.IsAny<HitArguments>()), Times.Exactly(times));

        private void NothingShouldBeHit() => ThePartnerShouldHaveHit_Times(0);
    }
}
