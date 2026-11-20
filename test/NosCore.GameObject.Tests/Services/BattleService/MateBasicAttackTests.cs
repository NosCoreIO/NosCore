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
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class MateBasicAttackTests
    {
        private const short PetVNum = 709;
        private const short PetSkill = 790;
        private const short PetHitEffect = 200;
        private const short PetBasicCooldown = 14;

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _map;
        private MateComponentBundle _pet;
        private MonsterComponentBundle _monster;
        private readonly List<HitRequest> _hits = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _map = _session.Character.MapInstance;

            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = PetSkill, CastId = 0, Cooldown = 80, Range = 3, Effect = 791, AttackAnimation = 12, SkillType = 5 },
            });
            World.Inject(new List<NpcMonsterSkillDto>
            {
                new() { NpcMonsterSkillId = 1, NpcMonsterVNum = PetVNum, SkillVNum = PetSkill },
            });
            World.Inject(new List<BCardDto>
            {
                new() { SkillVNum = PetSkill, Type = BCardType.CardType.Buff, SubType = 11, FirstData = 100, SecondData = 1 },
            });
            World.UseReal<INpcCombatCatalog, NpcCombatCatalog>();
            World.UseReal<ISkillResolver, SkillResolver>();
            World.Freeze<Mock<ITargetResolver>>()
                .Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<SkillInfo>()))
                .Returns<IAliveEntity, IAliveEntity, SkillInfo>((_, target, _) => new[] { target });
            World.Freeze<Mock<IHitQueue>>()
                .Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .Callback<HitRequest>(_hits.Add)
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 10, SuPacketHitMode.SuccessAttack, false));
            World.Freeze<Mock<IBattleStatsProvider>>()
                .Setup(s => s.GetStats(It.IsAny<IAliveEntity>()))
                .Returns(new CombatStats());
            World.UseReal<IBattleService, GameObject.Services.BattleService.BattleService>();

            var pet = TestMates.Pet(_session.Character.CharacterId, vnum: PetVNum);
            pet.NpcMonster.BasicSkill = PetHitEffect;
            pet.NpcMonster.BasicCooldown = PetBasicCooldown;
            pet.NpcMonster.BasicRange = 1;
            pet.NpcMonster.AttackClass = 1;
            _session.Character.Mates[pet.MateTransportId] = pet;
            await pet.SpawnAsync(_session.Character, _map, 5, 5);
            _pet = pet.Entity.NotNull();

            _monster = _map.SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = 2, MaxHp = 1000 }, 6, 5, false);
        }

        private Task<bool> ThePetSwingsAtTheMonsterAsync() =>
            World.Create<MateAttackService>().BasicAttackAsync(_pet, _monster);

        [TestMethod]
        public async Task APetsBasicAttackIsNotItsFirstSkill()
        {
            Assert.IsTrue(await ThePetSwingsAtTheMonsterAsync());

            var hit = _hits.Single();
            Assert.AreEqual((short)0, hit.Skill.SkillVnum);
            Assert.AreEqual(0, hit.Skill.BCards.Count);
        }

        [TestMethod]
        public async Task APetsBasicHitShowsItsOwnEffectAndCooldown()
        {
            await ThePetSwingsAtTheMonsterAsync();

            PacketAssert.AreEqual(new SuPacket
            {
                VisualType = VisualType.Npc,
                VisualId = _pet.VisualId,
                TargetVisualType = VisualType.Monster,
                TargetId = _monster.VisualId,
                SkillVnum = 0,
                SkillCooldown = PetBasicCooldown,
                AttackAnimation = 11,
                SkillEffect = PetHitEffect,
                TargetIsAlive = true,
                HpPercentage = (byte)(_monster.Hp * 100 / _monster.MaxHp),
                Damage = 10,
                HitMode = SuPacketHitMode.SuccessAttack,
                TargetCurrentHp = _monster.Hp,
                TargetMaxHp = _monster.MaxHp,
            }, _map.LastPackets.OfType<SuPacket>().Single());
        }
    }
}
