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
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class MonsterBasicAttackTests
    {
        private const short Fireball = 1028;
        private const short Paralysis = 194;

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _map;
        private MonsterComponentBundle _vampie;
        private readonly List<HitRequest> _hits = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _map = World.GetBaseMap(TestWorld.TestMapId);

            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = Fireball, Cooldown = 50, Range = 9, Effect = 1029, AttackAnimation = 12, SkillType = 1 },
            });
            World.Inject(new List<BCardDto>
            {
                new() { SkillVNum = Fireball, Type = BCardType.CardType.Buff, SubType = 11, FirstData = 80, SecondData = Paralysis },
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

            _vampie = _map.SpawnMonster(new NpcMonsterDto
            {
                NpcMonsterVNum = 58, MaxHp = 1000, BasicSkill = Fireball, BasicCooldown = 12, BasicRange = 4, AttackClass = 2,
            }, _session.Character.PositionX, (short)(_session.Character.PositionY + 1), false);
        }

        private Task<bool> TheVampieSwingsAtThePlayerAsync() =>
            World.Create<GameObject.Services.BattleService.BattleService>()
                .Hit(_vampie, _session.Character, new HitArguments { BasicAttack = true });

        [TestMethod]
        public async Task AMonsterWhoseEffectNumbersASkillStillHitsWithNoSkill()
        {
            Assert.IsTrue(await TheVampieSwingsAtThePlayerAsync());

            var hit = _hits.Single();
            Assert.AreEqual((short)0, hit.Skill.SkillVnum);
            Assert.AreEqual(0, hit.Skill.BCards.Count);
        }

        [TestMethod]
        public async Task AMonstersBasicHitShowsItsOwnEffectAndCooldown()
        {
            await TheVampieSwingsAtThePlayerAsync();

            var player = _session.Character;
            PacketAssert.AreEqual(new SuPacket
            {
                VisualType = VisualType.Monster,
                VisualId = _vampie.VisualId,
                TargetVisualType = VisualType.Player,
                TargetId = player.VisualId,
                SkillVnum = 0,
                SkillCooldown = 12,
                AttackAnimation = 11,
                SkillEffect = Fireball,
                TargetIsAlive = true,
                HpPercentage = (byte)(player.Hp * 100 / player.MaxHp),
                Damage = 10,
                HitMode = SuPacketHitMode.SuccessAttack,
                TargetCurrentHp = player.Hp,
                TargetMaxHp = player.MaxHp,
            }, _map.LastPackets.OfType<SuPacket>().Single());
        }
    }
}
