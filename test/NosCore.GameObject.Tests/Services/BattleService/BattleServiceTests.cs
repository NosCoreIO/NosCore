//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.ShopService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wolverine;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class BattleServiceTests
    {
        private Mock<ISkillResolver> _skillResolver;
        private Mock<ITargetResolver> _targetResolver;
        private Mock<IHitQueue> _hitQueue;
        private Mock<IMessageBus> _bus;
        private Mock<ICastDelayService> _castDelay;
        private Mock<IBuffService> _buffs;
        private GameObject.Services.BattleService.BattleService _service;

        [TestInitialize]
        public void Setup()
        {
            _skillResolver = new Mock<ISkillResolver>();
            _targetResolver = new Mock<ITargetResolver>();
            _hitQueue = new Mock<IHitQueue>();
            _bus = new Mock<IMessageBus>();
            _castDelay = new Mock<ICastDelayService>();
            _castDelay.Setup(d => d.WaitAsync(It.IsAny<NodaTime.Duration>())).Returns(Task.CompletedTask);
            _buffs = new Mock<IBuffService>();
            _service = new GameObject.Services.BattleService.BattleService(
                _skillResolver.Object,
                _targetResolver.Object,
                _hitQueue.Object,
                _bus.Object,
                new Mock<GameObject.Services.BroadcastService.ISessionRegistry>().Object,
                NodaTime.SystemClock.Instance,
                new Mock<NosCore.GameObject.Services.BattleService.ICaptureService>().Object,
                _castDelay.Object,
                _buffs.Object,
                new NosCore.GameObject.Services.BattleService.SkillCastService(NodaTime.SystemClock.Instance, new Mock<NosCore.GameObject.Services.EquipmentService.IEquipmentStatsService>().Object, new Mock<NosCore.GameObject.Services.BattleService.IBuffService>().Object),
                new Mock<NosCore.GameObject.Services.BattleService.ISkillComboService>().Object,
                NullLogger<NosCore.GameObject.Services.BattleService.BattleService>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task ASkillWithACastTimeHitsOnlyOnceTheCastEnds()
        {
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            SetupLanding(origin, target, MakeSkill(TargetHitType.SingleTargetHit) with { CastTime = 10 });
            var cast = new TaskCompletionSource();
            _castDelay.Setup(d => d.WaitAsync(NodaTime.Duration.FromSeconds(1))).Returns(cast.Task);

            var hit = _service.Hit(origin, target, new HitArguments { SkillId = 1 });
            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Never);

            cast.SetResult();
            Assert.IsTrue(await hit);
            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Once);
        }

        [TestMethod]
        public async Task ACastWhoseTargetDiesMeanwhileLandsNothing()
        {
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            SetupLanding(origin, target, MakeSkill(TargetHitType.SingleTargetHit) with { CastTime = 10 });
            _castDelay.Setup(d => d.WaitAsync(It.IsAny<NodaTime.Duration>()))
                .Callback(() => target.Hp = 0)
                .Returns(Task.CompletedTask);

            Assert.IsFalse(await _service.Hit(origin, target, new HitArguments { SkillId = 1 }));
            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Never);
        }

        [TestMethod]
        public async Task AStunnedAttackerLandsNothing()
        {
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            SetupLanding(origin, target, MakeSkill(TargetHitType.SingleTargetHit));
            _buffs.Setup(b => b.HasEffect(origin, NosCore.Data.Enumerations.Buff.BCardEffect.SpecialAttackNoAttack)).Returns(true);

            Assert.IsFalse(await _service.Hit(origin, target, new HitArguments { SkillId = 1 }));
            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Never);
        }

        [TestMethod]
        public async Task AStunLandingDuringTheWindUpCancelsTheCast()
        {
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            SetupLanding(origin, target, MakeSkill(TargetHitType.SingleTargetHit) with { CastTime = 10 });
            var stunned = false;
            _buffs.Setup(b => b.HasEffect(origin, NosCore.Data.Enumerations.Buff.BCardEffect.SpecialAttackNoAttack)).Returns(() => stunned);
            _castDelay.Setup(d => d.WaitAsync(It.IsAny<NodaTime.Duration>()))
                .Callback(() => stunned = true)
                .Returns(Task.CompletedTask);

            Assert.IsFalse(await _service.Hit(origin, target, new HitArguments { SkillId = 1 }));
            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Never);
        }

        [TestMethod]
        public async Task ASkillWithoutACastTimeDoesNotWait()
        {
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            SetupLanding(origin, target, MakeSkill(TargetHitType.SingleTargetHit));

            await _service.Hit(origin, target, new HitArguments { SkillId = 1 });

            _castDelay.Verify(d => d.WaitAsync(It.IsAny<NodaTime.Duration>()), Times.Never);
        }

        [TestMethod]
        public async Task CannotAttackWhenAttackerIsDead()
        {
            var origin = new FakeEntity { Hp = 0 };
            var target = new FakeEntity { Hp = 100 };

            await _service.Hit(origin, target, new HitArguments { SkillId = 1 });

            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Never);
        }

        [TestMethod]
        public async Task CannotAttackNoAttackTarget()
        {
            // NPCs expose NoAttack=true — attempting to damage them must cancel the
            // cast without dispatching anything to the hit queue.
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 100, NoAttackOverride = true };

            await _service.Hit(origin, target, new HitArguments { SkillId = 1 });

            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Never);
        }

        [TestMethod]
        public async Task SingleTargetSkillEnqueuesOneHit()
        {
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            var skill = MakeSkill(TargetHitType.SingleTargetHit);

            _skillResolver.Setup(r => r.Resolve(origin, 1L)).Returns(skill);
            _targetResolver.Setup(r => r.Resolve(origin, target, skill)).Returns(new[] { (IAliveEntity)target });
            _hitQueue.Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 50, SuPacketHitMode.SuccessAttack, false));

            await _service.Hit(origin, target, new HitArguments { SkillId = 1 });

            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Once);
        }

        [TestMethod]
        public async Task AoeSkillEnqueuesPerTarget()
        {
            var origin = new FakeEntity { Hp = 100 };
            var t1 = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            var t2 = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            var t3 = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            var skill = MakeSkill(TargetHitType.AoeTargetHit);

            _skillResolver.Setup(r => r.Resolve(origin, 1L)).Returns(skill);
            _targetResolver.Setup(r => r.Resolve(origin, t1, skill))
                .Returns(new[] { (IAliveEntity)t1, t2, t3 });
            _hitQueue.Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 20, SuPacketHitMode.SuccessAttack, false));

            await _service.Hit(origin, t1, new HitArguments { SkillId = 1 });

            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Exactly(3));
        }

        [TestMethod]
        public async Task KillPublishesEntityDiedEvent()
        {
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 1, VisualType = VisualType.Monster };
            var skill = MakeSkill(TargetHitType.SingleTargetHit);

            _skillResolver.Setup(r => r.Resolve(origin, 1L)).Returns(skill);
            _targetResolver.Setup(r => r.Resolve(origin, target, skill)).Returns(new[] { (IAliveEntity)target });
            _hitQueue.Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 100, SuPacketHitMode.SuccessAttack, true));

            await _service.Hit(origin, target, new HitArguments { SkillId = 1 });

            _bus.Verify(b => b.PublishAsync(It.Is<EntityDiedEvent>(e => e.Victim == target && e.Killer == origin), null), Times.Once);
        }

        [TestMethod]
        public async Task UnresolvedSkillSkipsQueue()
        {
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 100 };
            _skillResolver.Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), It.IsAny<long>())).Returns((SkillInfo?)null);

            await _service.Hit(origin, target, new HitArguments { SkillId = 42 });

            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Never);
        }

        [TestMethod]
        public async Task RefusedCastsReportFalse()
        {
            var dead = new FakeEntity { Hp = 0 };
            var target = new FakeEntity { Hp = 100 };
            _skillResolver.Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), It.IsAny<long>())).Returns((SkillInfo?)null);

            Assert.IsFalse(await _service.Hit(dead, target, new HitArguments { SkillId = 1 }));
            Assert.IsFalse(await _service.Hit(new FakeEntity { Hp = 100 }, target, new HitArguments { SkillId = 1 }));
        }

        [TestMethod]
        public async Task AcceptedCastReportsTrue()
        {
            var origin = new FakeEntity { Hp = 100 };
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            SetupLanding(origin, target, MakeSkill(TargetHitType.SingleTargetHit));

            Assert.IsTrue(await _service.Hit(origin, target, new HitArguments { SkillId = 1 }));
        }

        private void SetupLanding(IAliveEntity origin, IAliveEntity target, SkillInfo skill)
        {
            _skillResolver.Setup(r => r.Resolve(origin, 1L)).Returns(skill);
            _targetResolver.Setup(r => r.Resolve(origin, target, skill)).Returns(new[] { target });
            _hitQueue.Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 50, SuPacketHitMode.SuccessAttack, false));
        }

        private static SkillInfo MakeSkill(TargetHitType hitType, byte range = 0, byte targetType = 0) => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: hitType, Range: range, TargetRange: 2, TargetType: targetType,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        internal class FakeEntity : IAliveEntity
        {
            public Entity Handle { get; set; }
            public bool IsSitting { get; set; }
            public byte Speed { get; set; }
            public byte Size { get; set; }
            public int Mp { get; set; } = 100;
            public int Hp { get; set; }
            public short Morph => 0;
            public byte MorphUpgrade => 0;
            public short MorphDesign => 0;
            public byte MorphBonus => 0;
            public bool NoAttackOverride { get; set; }
            public bool NoAttack => NoAttackOverride;
            public bool NoMove => false;
            public bool IsAlive => Hp > 0;
            public short MapX => 0;
            public short MapY => 0;
            public int MaxHp => 100;
            public int MaxMp => 100;
            public byte Level { get; set; } = 1;
            public byte HeroLevel => 0;
            public short Race => 0;
            public Shop? Shop { get; set; }
            public ConcurrentDictionary<Entity, int> HitList { get; } = new();
            public VisualType VisualType { get; set; } = VisualType.Player;
            public short VNum => 0;
            public long VisualId { get; set; }
            public byte Direction { get; set; }
            public Guid MapInstanceId { get; }
            // Off any map, which the services handle although the interface does not say so.
#nullable disable
            public NosCore.GameObject.Services.MapInstanceGenerationService.MapInstance MapInstance { get; set; }
#nullable restore
            public short PositionX { get; set; }
            public short PositionY { get; set; }
        }
    }
}
