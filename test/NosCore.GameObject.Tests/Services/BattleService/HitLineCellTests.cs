//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class HitLineCellTests
    {
        private const byte Melee = 0;
        private const byte Ranged = 1;
        private const byte Magic = 2;
        private const byte Dash = 5;

        private TestWorld World;
        private ClientSession _caster;
        private ClientSession _ally;
        private MapInstance _map;
        private MonsterComponentBundle _target;
        private MonsterComponentBundle _neighbour;
        private SkillInfo _skill = Skill(Melee, SkillReach.TargetedSkill, TargetHitType.SingleTargetHit);
        private CombatStats _stats = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _caster = await World.CreateSessionAsync();
            _ally = await World.CreateSessionAsync();
            _map = World.GetBaseMap(TestWorld.TestMapId);
            _caster.Character.PositionX = 10;
            _caster.Character.PositionY = 10;
            _ally.Character.PositionX = 11;
            _ally.Character.PositionY = 11;
            _target = _map.SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = 2, MaxHp = 1000 }, 12, 10, false);
            _neighbour = _map.SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = 2, MaxHp = 1000 }, 13, 10, false);

            World.Freeze<Mock<ISkillResolver>>()
                .Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), It.IsAny<long>()))
                .Returns(() => _skill);
            var targets = World.Freeze<Mock<ITargetResolver>>();
            targets.Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<SkillInfo>()))
                .Returns<IAliveEntity, IAliveEntity, SkillInfo>((_, target, skill) =>
                    skill.HitsSeveral ? new IAliveEntity[] { target, _neighbour } : new[] { target });
            targets.Setup(r => r.ResolveArea(It.IsAny<IAliveEntity>(), It.IsAny<MapInstance>(), It.IsAny<short>(), It.IsAny<short>(),
                    It.IsAny<SkillInfo>()))
                .Returns(new IAliveEntity[] { _target, _neighbour });
            World.Freeze<Mock<IHitQueue>>()
                .Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 10, SuPacketHitMode.SuccessAttack, false));
            World.Freeze<Mock<IBattleStatsProvider>>()
                .Setup(s => s.GetStats(It.IsAny<IAliveEntity>()))
                .Returns(() => _stats);
        }

        private static SkillInfo Skill(byte attackClass, byte targetType, TargetHitType hitType, byte targetRange = 0) => new(
            SkillVnum: 830, CastId: 3, Cooldown: 20, AttackAnimation: 13, CastEffect: 0, Effect: 4316,
            Type: attackClass, HitType: hitType, Range: 8, TargetRange: targetRange, TargetType: targetType,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        private Task<bool> CastingOnAsync(IAliveEntity target) =>
            World.Create<GameObject.Services.BattleService.BattleService>()
                .Hit(_caster.Character, target, new HitArguments { SkillId = _skill.CastId });

        private SuPacket Blow(IAliveEntity target, SuPacketHitMode hitMode, uint damage = 10, short x = 0, short y = 0) => new()
        {
            VisualType = _caster.Character.VisualType,
            VisualId = _caster.Character.VisualId,
            TargetVisualType = target.VisualType,
            TargetId = target.VisualId,
            SkillVnum = _skill.SkillVnum,
            SkillCooldown = _skill.Cooldown,
            AttackAnimation = _skill.AttackAnimation,
            SkillEffect = _skill.Effect,
            PositionX = x,
            PositionY = y,
            TargetIsAlive = true,
            HpPercentage = (byte)(target.Hp * 100 / target.MaxHp),
            Damage = damage,
            HitMode = hitMode,
            TargetCurrentHp = target.Hp,
            TargetMaxHp = target.MaxHp,
        };

        private SuPacket LineOn(IAliveEntity target) =>
            _map.LastPackets.OfType<SuPacket>().Single(su => su.TargetVisualType == target.VisualType && su.TargetId == target.VisualId);

        [TestMethod]
        public async Task ASingleTargetSkillNamesNoCellAndNoAttackClass()
        {
            _skill = Skill(Magic, SkillReach.TargetedSkill, TargetHitType.SingleTargetHit);

            Assert.IsTrue(await CastingOnAsync(_target));

            PacketAssert.AreEqual(Blow(_target, SuPacketHitMode.SuccessAttack), LineOn(_target));
        }

        [TestMethod]
        public async Task EveryBlowOfASkillAroundItsTargetNamesNoCell()
        {
            _skill = Skill(Ranged, SkillReach.TargetedSkill, TargetHitType.SingleTargetHitCombo, targetRange: 2);

            await CastingOnAsync(_target);

            PacketAssert.AreEqual(Blow(_target, SuPacketHitMode.SuccessAttack), LineOn(_target));
            PacketAssert.AreEqual(Blow(_neighbour, SuPacketHitMode.SuccessAttack), LineOn(_neighbour));
        }

        [TestMethod]
        public async Task ADashNamesTheCellItsCasterStandsOnForItsTargetAlone()
        {
            _skill = Skill(Dash, SkillReach.TargetedSkill, TargetHitType.SingleTargetHitCombo, targetRange: 1);

            await CastingOnAsync(_target);

            var caster = _caster.Character;
            PacketAssert.AreEqual(Blow(_target, SuPacketHitMode.SuccessAttack, x: caster.PositionX, y: caster.PositionY), LineOn(_target));
            PacketAssert.AreEqual(Blow(_neighbour, SuPacketHitMode.SuccessAttack), LineOn(_neighbour));
        }

        [TestMethod]
        public async Task ABystanderOfALuckyBlowIsNamedWithNoCell()
        {
            _stats = new CombatStats() with { SurroundingDamageChance = 100, SurroundingDamageRadius = 2 };
            _skill = Skill(Melee, SkillReach.TargetedSkill, TargetHitType.SingleTargetHit);

            await CastingOnAsync(_target);

            PacketAssert.AreEqual(Blow(_neighbour, SuPacketHitMode.AttackedInAoe), LineOn(_neighbour));
        }

        [TestMethod]
        public async Task ABlowAroundTheCasterNamesNoCell()
        {
            _skill = Skill(Melee, SkillReach.SelfSkill, TargetHitType.SingleTargetHitCombo, targetRange: 3);

            await CastingOnAsync(_caster.Character);

            PacketAssert.AreEqual(Blow(_caster.Character, SuPacketHitMode.NoDamageFail, damage: 0), LineOn(_caster.Character));
            PacketAssert.AreEqual(Blow(_target, SuPacketHitMode.AttackedInAoe), LineOn(_target));
            PacketAssert.AreEqual(Blow(_neighbour, SuPacketHitMode.AttackedInAoe), LineOn(_neighbour));
        }

        [TestMethod]
        public async Task ABuffOnTheCasterNamesNoCellAndNoAttackClass()
        {
            _skill = Skill(Melee, SkillReach.SelfSkill, TargetHitType.SingleTargetHit);

            Assert.IsTrue(await CastingOnAsync(_caster.Character));

            PacketAssert.AreEqual(Blow(_caster.Character, SuPacketHitMode.NoDamageSuccess, damage: 0), LineOn(_caster.Character));
        }

        [TestMethod]
        public async Task AGroupBuffNamesNoAttackClassForTheFriendsItReaches()
        {
            _skill = Skill(Melee, SkillReach.SelfSkill, TargetHitType.SingleAoeTargetHit, targetRange: 3);

            await CastingOnAsync(_caster.Character);

            PacketAssert.AreEqual(Blow(_ally.Character, SuPacketHitMode.AttackedInAoe, damage: 0), LineOn(_ally.Character));
        }

        [TestMethod]
        public async Task AGroundSkillNamesItsCastCell()
        {
            _skill = Skill(Magic, SkillReach.GroundSkill, TargetHitType.ZoneHit, targetRange: 3);

            Assert.IsTrue(await World.Create<GameObject.Services.BattleService.BattleService>()
                .HitArea(_caster.Character, 12, 11, new HitArguments { SkillId = _skill.CastId }));

            PacketAssert.AreEqual(Blow(_target, SuPacketHitMode.AttackedInAoe, x: 12, y: 11), LineOn(_target));
            PacketAssert.AreEqual(Blow(_neighbour, SuPacketHitMode.AttackedInAoe, x: 12, y: 11), LineOn(_neighbour));
        }
    }
}
