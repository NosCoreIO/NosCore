//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using FakeEntity = NosCore.GameObject.Tests.Services.BattleService.BattleServiceTests.FakeEntity;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class CastRefusalTests
    {
        private const long CastId = 4;

        private TestWorld World;
        private ClientSession _caster;
        private Mock<ISkillResolver> _skillResolver;
        private Mock<IBuffService> _buffs;
        private Mock<ICastDelayService> _castDelay;
        private FakeEntity _monster;
        private IAliveEntity _target;
        private bool _accepted;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _caster = await World.CreateSessionAsync();
            _caster.Character.MapInstance = World.GetBaseMap(TestWorld.TestMapId);
            _monster = new FakeEntity { Hp = 100, VisualType = VisualType.Monster, VisualId = 7, MapInstance = _caster.Character.MapInstance };
            _target = _monster;
            _skillResolver = World.Freeze<Mock<ISkillResolver>>();
            _skillResolver.Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), CastId)).Returns(Skill(castTime: 0));
            _buffs = World.Freeze<Mock<IBuffService>>();
            _castDelay = World.Freeze<Mock<ICastDelayService>>();
            _castDelay.Setup(d => d.WaitAsync(It.IsAny<NodaTime.Duration>())).Returns(Task.CompletedTask);
        }

        [TestMethod]
        public async Task ACastAtADeadMonsterOnlyEndsTheCast()
        {
            await new Spec("A cast at a monster that is already dead is refused with a cast cancel carrying its cast id")
                .Given(TheMonsterIsDead)
                .WhenAsync(TheCasterCasts)
                .Then(TheCastShouldBeRefused)
                .And(TheCancelShouldBe_, CancelPacketType.CancelCast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACastWhoseTargetDiesDuringTheWindUpOnlyEndsTheCast()
        {
            await new Spec("A cast whose monster dies before the wind-up ends is refused with a cast cancel")
                .Given(TheSkillHasACastTime)
                .And(TheMonsterDiesDuringTheWindUp)
                .WhenAsync(TheCasterCasts)
                .Then(TheCastShouldBeRefused)
                .And(TheCancelShouldBe_, CancelPacketType.CancelCast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStunnedCasterOnlyEndsTheCast()
        {
            await new Spec("A cast by a stunned caster is refused with a cast cancel")
                .Given(TheCasterIsStunned)
                .WhenAsync(TheCasterCasts)
                .Then(TheCastShouldBeRefused)
                .And(TheCancelShouldBe_, CancelPacketType.CancelCast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAttackAimedAtTheCasterItselfOnlyEndsTheCast()
        {
            await new Spec("An attack skill aimed at the caster itself is refused with a cast cancel")
                .Given(TheTargetIsTheCaster)
                .WhenAsync(TheCasterCasts)
                .Then(TheCastShouldBeRefused)
                .And(TheCancelShouldBe_, CancelPacketType.CancelCast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAttackOnAnNpcStopsTheAutoAttack()
        {
            await new Spec("An attack on an NPC, which can never be hit, is refused and stops the auto attack")
                .Given(TheTargetIsAnNpc)
                .WhenAsync(TheCasterCasts)
                .Then(TheCastShouldBeRefused)
                .And(TheCancelShouldBe_, CancelPacketType.CancelAutoAttack)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnknownSkillStopsTheAutoAttack()
        {
            await new Spec("A cast id that resolves to no skill is refused and stops the auto attack")
                .Given(TheCastIdResolvesToNoSkill)
                .WhenAsync(TheCasterCasts)
                .Then(TheCastShouldBeRefused)
                .And(TheCancelShouldBe_, CancelPacketType.CancelAutoAttack)
                .ExecuteAsync();
        }

        private static SkillInfo Skill(short castTime) => new(
            SkillVnum: 1, CastId: (short)CastId, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0, TargetType: 0,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>()) { CastTime = castTime };

        private void TheMonsterIsDead() => _monster.Hp = 0;

        private void TheSkillHasACastTime() =>
            _skillResolver.Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), CastId)).Returns(Skill(castTime: 10));

        private void TheMonsterDiesDuringTheWindUp() =>
            _castDelay.Setup(d => d.WaitAsync(It.IsAny<NodaTime.Duration>()))
                .Callback(() => _monster.Hp = 0)
                .Returns(Task.CompletedTask);

        private void TheCasterIsStunned() =>
            _buffs.Setup(b => b.HasEffect(It.IsAny<IAliveEntity>(), BCardEffect.SpecialAttackNoAttack)).Returns(true);

        private void TheTargetIsTheCaster() => _target = _caster.Character;

        private void TheTargetIsAnNpc() =>
            _target = new FakeEntity { Hp = 100, VisualType = VisualType.Npc, VisualId = 8, MapInstance = _caster.Character.MapInstance };

        private void TheCastIdResolvesToNoSkill() =>
            _skillResolver.Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), CastId)).Returns((SkillInfo?)null);

        private async Task TheCasterCasts() =>
            _accepted = await World.Create<GameObject.Services.BattleService.BattleService>()
                .Hit(_caster.Character, _target, new HitArguments { SkillId = CastId });

        private void TheCastShouldBeRefused() => Assert.IsFalse(_accepted);

        private void TheCancelShouldBe_(CancelPacketType type) =>
            PacketAssert.AreEqual(new CancelPacket { Type = type, TargetId = CastId },
                _caster.LastPackets.OfType<CancelPacket>().Single());
    }
}
