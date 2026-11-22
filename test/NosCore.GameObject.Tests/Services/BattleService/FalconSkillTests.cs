//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.SkillService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class FalconSkillTests
    {
        private const long SpyOutCastId = 3;
        private const long ArrowCastId = 4;
        private const short HuntingFalconAttackVNum = 1249;

        private static readonly IReadOnlyList<BCardDto> SpyOut =
            new[] { new BCardDto { Type = BCardType.CardType.FalconSkill, SubType = 41 } };

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _map;
        private MonsterComponentBundle _monster;
        private Mock<IBattleService> _battle;
        private readonly List<IAliveEntity> _struck = new();

        private PlayerComponentBundle Caster => _session.Character;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _map = _session.Character.MapInstance;
            _monster = _map.SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 1000 }, 5, 5, false);

            var skills = World.Freeze<Mock<ISkillResolver>>();
            skills.Setup(s => s.Resolve(It.IsAny<IAliveEntity>(), SpyOutCastId)).Returns(Skill(1117, SpyOut));
            skills.Setup(s => s.Resolve(It.IsAny<IAliveEntity>(), ArrowCastId)).Returns(Skill(1114, Array.Empty<BCardDto>()));
            World.Freeze<Mock<ITargetResolver>>()
                .Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<SkillInfo>()))
                .Returns<IAliveEntity, IAliveEntity, SkillInfo>((_, target, _) => new[] { target });
            World.Freeze<Mock<IHitQueue>>()
                .Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 10, SuPacketHitMode.SuccessAttack, false));
            World.Freeze<Mock<IBattleStatsProvider>>()
                .Setup(s => s.GetStats(It.IsAny<IAliveEntity>()))
                .Returns(new CombatStats());
            World.Freeze<Mock<IBuffService>>();

            World.Freeze<Mock<ISkillCatalog>>()
                .Setup(s => s.GetSkill(HuntingFalconAttackVNum))
                .Returns(new SkillDto { SkillVNum = HuntingFalconAttackVNum, Cooldown = 600 });
            World.Freeze<Mock<ISkillCastService>>()
                .Setup(s => s.Cooldown(It.IsAny<PlayerComponentBundle>(), 600))
                .Returns(Duration.FromSeconds(60));
            _battle = World.Freeze<Mock<IBattleService>>();
            _battle.Setup(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(),
                    It.Is<HitArguments>(a => a.Skill != null && a.Skill.SkillVNum == HuntingFalconAttackVNum)))
                .Callback<IAliveEntity, IAliveEntity, HitArguments>((_, target, _) => _struck.Add(target))
                .ReturnsAsync(true);
        }

        private static SkillInfo Skill(short vnum, IReadOnlyList<BCardDto> cards) =>
            new(vnum, vnum, 0, 0, 0, 0, 0, TargetHitType.SingleTargetHit, 13, 0, SkillReach.TargetedSkill, 0, 0, 0, cards);

        [TestMethod]
        public async Task SpyOutSendsTheFalconAfterItsTarget()
        {
            await new Spec("Spy Out sets the caster's falcon on the entity it was cast on")
                .WhenAsync(TheCasterUses_OnTheMonster, SpyOutCastId)
                .Then(TheFalconShouldPursueTheMonster)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherSkillLeavesTheFalconAlone()
        {
            await new Spec("A skill without the pursuit effect sets the falcon on nobody")
                .WhenAsync(TheCasterUses_OnTheMonster, ArrowCastId)
                .Then(TheFalconShouldPursueNobody)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheFalconAttacksThePursuedEntityFromTheAir()
        {
            await new Spec("The falcon icon makes the falcon attack the entity it pursues, whatever the distance")
                .Given(TheFalconPursuesTheMonster)
                .And(TheCasterStandsAt_, (short)60, (short)60)
                .WhenAsync(TheCasterUsesTheFalconIcon)
                .Then(TheFalconShouldHaveStruck_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutAPursuedEntityTheIconDoesNothing()
        {
            await new Spec("The falcon icon does nothing while the falcon pursues nobody")
                .WhenAsync(TheCasterUsesTheFalconIcon)
                .Then(TheFalconShouldHaveStruck_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheFalconWaitsForItsAttackToCoolDown()
        {
            await new Spec("The falcon only attacks again once its attack's cooldown has passed")
                .Given(TheFalconPursuesTheMonster)
                .AndAsync(TheCasterUsesTheFalconIcon)
                .AndAsync(TheCasterUsesTheFalconIcon)
                .And(_SecondsPass, 60)
                .WhenAsync(TheCasterUsesTheFalconIcon)
                .Then(TheFalconShouldHaveStruck_Times, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadQuarryIsLetGo()
        {
            await new Spec("Once the pursued entity is dead the falcon pursues nobody and attacks nothing")
                .Given(TheFalconPursuesTheMonster)
                .And(TheMonsterIsDead)
                .WhenAsync(TheCasterUsesTheFalconIcon)
                .Then(TheFalconShouldHaveStruck_Times, 0)
                .And(TheFalconShouldPursueNobody)
                .ExecuteAsync();
        }

        private async Task TheCasterUses_OnTheMonster(long castId) =>
            Assert.IsTrue(await World.Create<GameObject.Services.BattleService.BattleService>()
                .Hit(Caster, _monster, new HitArguments { SkillId = castId }));

        private void TheFalconPursuesTheMonster() => Caster.SendFalconAfter(_monster);

        private void TheCasterStandsAt_(short x, short y)
        {
            var caster = Caster;
            caster.PositionX = x;
            caster.PositionY = y;
        }

        private void TheMonsterIsDead() => _monster.IsAlive = false;

        private void _SecondsPass(int seconds) => World.Clock.Advance(Duration.FromSeconds(seconds));

        private Task TheCasterUsesTheFalconIcon() => World.Create<FalconService>().StrikeAsync(Caster);

        private void TheFalconShouldPursueTheMonster()
        {
            Assert.AreEqual(VisualType.Monster, Caster.FalconTargetType);
            Assert.AreEqual(_monster.VisualId, Caster.FalconTargetId);
        }

        private void TheFalconShouldPursueNobody() => Assert.AreEqual(0, Caster.FalconTargetId);

        private void TheFalconShouldHaveStruck_Times(int times)
        {
            Assert.AreEqual(times, _struck.Count);
            Assert.IsTrue(_struck.TrueForAll(t => t.VisualType == VisualType.Monster && t.VisualId == _monster.VisualId));
        }
    }
}
