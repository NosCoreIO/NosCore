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
using NosCore.PathFinder.Heuristic;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wolverine;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class BattleServiceTests
    {
        private Mock<ISkillResolver> _skillResolver = null!;
        private Mock<ITargetResolver> _targetResolver = null!;
        private Mock<IHitQueue> _hitQueue = null!;
        private Mock<IMessageBus> _bus = null!;
        private GameObject.Services.BattleService.BattleService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _skillResolver = new Mock<ISkillResolver>();
            _targetResolver = new Mock<ITargetResolver>();
            _hitQueue = new Mock<IHitQueue>();
            _bus = new Mock<IMessageBus>();
            _service = new GameObject.Services.BattleService.BattleService(
                _skillResolver.Object,
                _targetResolver.Object,
                _hitQueue.Object,
                _bus.Object,
                new Mock<GameObject.Services.BroadcastService.ISessionRegistry>().Object,
                NodaTime.SystemClock.Instance,
                new Mock<NosCore.GameObject.Services.BattleService.ICaptureService>().Object,
                new OctileDistanceHeuristic(),
                NullLogger<NosCore.GameObject.Services.BattleService.BattleService>.Instance);
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

            _bus.Verify(b => b.PublishAsync(It.Is<EntityDiedEvent>(e => e.Victim == target && e.Killer == origin), null!), Times.Once);
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

        [DataTestMethod]
        [DataRow(3, 3, true)]
        [DataRow(3, 4, true)]
        [DataRow(3, 5, false)]
        [DataRow(1, 2, true)]
        [DataRow(1, 3, false)]
        public async Task CharacterCastIsLimitedToSkillRangePlusOneCell(int range, int distance, bool accepted)
        {
            var origin = MakeCharacter(x: 0, y: 0);
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster, PositionX = (short)distance };
            SetupLanding(origin.Object, target, MakeSkill(TargetHitType.SingleTargetHit, (byte)range, targetType: 0));

            var result = await _service.Hit(origin.Object, target, new HitArguments { SkillId = 1 });

            Assert.AreEqual(accepted, result);
            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), accepted ? Times.Once() : Times.Never());
        }

        [TestMethod]
        public async Task DiagonalDistanceUsesTheTruncatedOctileDistance()
        {
            // (3,3) is 3 * sqrt(2) = 4.24 away: 4 truncated, within range 3 plus one cell.
            var origin = MakeCharacter(x: 0, y: 0);
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster, PositionX = 3, PositionY = 3 };
            SetupLanding(origin.Object, target, MakeSkill(TargetHitType.SingleTargetHit, range: 3, targetType: 0));

            Assert.IsTrue(await _service.Hit(origin.Object, target, new HitArguments { SkillId = 1 }));
        }

        [DataTestMethod]
        [DataRow(0, 0, DisplayName = "range zero says nothing about reach")]
        [DataRow(3, 1, DisplayName = "caster-centred skills need no target in reach")]
        public async Task SkillsWithoutAReachAreNotGated(int range, int targetType)
        {
            var origin = MakeCharacter(x: 0, y: 0);
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster, PositionX = 40 };
            SetupLanding(origin.Object, target, MakeSkill(TargetHitType.SingleTargetHit, (byte)range, (byte)targetType));

            Assert.IsTrue(await _service.Hit(origin.Object, target, new HitArguments { SkillId = 1 }));
        }

        [TestMethod]
        public async Task MonstersAreNotGatedByTheCastReach()
        {
            var origin = new FakeEntity { Hp = 100, VisualType = VisualType.Monster };
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Player, PositionX = 40 };
            SetupLanding(origin, target, MakeSkill(TargetHitType.SingleTargetHit, range: 1, targetType: 0));

            Assert.IsTrue(await _service.Hit(origin, target, new HitArguments { SkillId = 1 }));
        }

        [TestMethod]
        public async Task ReportedCastCellWithinOneStepIsTaken()
        {
            var origin = MakeCharacter(x: 0, y: 0, speed: 10);
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster, PositionX = 6 };
            SetupLanding(origin.Object, target, MakeSkill(TargetHitType.SingleTargetHit, range: 1, targetType: 0));

            // Six cells from the target is out of reach; the reported cell (5,0) is one step on
            // (5 - 1 <= 10 / 2) and leaves the caster one cell away.
            var result = await _service.Hit(origin.Object, target, new HitArguments { SkillId = 1, MapX = 5, MapY = 0 });

            Assert.IsTrue(result);
            Assert.AreEqual((short)5, origin.Object.PositionX);
        }

        [TestMethod]
        public async Task ReportedCastCellBeyondOneStepIsIgnored()
        {
            var origin = MakeCharacter(x: 0, y: 0, speed: 10);
            var target = new FakeEntity { Hp = 100, VisualType = VisualType.Monster, PositionX = 30 };
            SetupLanding(origin.Object, target, MakeSkill(TargetHitType.SingleTargetHit, range: 1, targetType: 0));

            var result = await _service.Hit(origin.Object, target, new HitArguments { SkillId = 1, MapX = 29, MapY = 0 });

            Assert.IsFalse(result);
            Assert.AreEqual((short)0, origin.Object.PositionX);
            _hitQueue.Verify(q => q.EnqueueAsync(It.IsAny<HitRequest>()), Times.Never);
        }

        private void SetupLanding(IAliveEntity origin, IAliveEntity target, SkillInfo skill)
        {
            _skillResolver.Setup(r => r.Resolve(origin, 1L)).Returns(skill);
            _targetResolver.Setup(r => r.Resolve(origin, target, skill)).Returns(new[] { target });
            _hitQueue.Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 50, SuPacketHitMode.SuccessAttack, false));
        }

        private static Mock<ICharacterEntity> MakeCharacter(short x, short y, byte speed = 10)
        {
            var character = new Mock<ICharacterEntity>();
            character.SetupAllProperties();
            character.Setup(c => c.IsAlive).Returns(true);
            character.Setup(c => c.VisualType).Returns(VisualType.Player);
            character.Setup(c => c.Speed).Returns(speed);
            character.Object.PositionX = x;
            character.Object.PositionY = y;
            return character;
        }

        private static SkillInfo MakeSkill(TargetHitType hitType, byte range = 0, byte targetType = 0) => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: hitType, Range: range, TargetRange: 2, TargetType: targetType,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        private class FakeEntity : IAliveEntity
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
            public SemaphoreSlim HitSemaphore { get; } = new(1, 1);
            public ConcurrentDictionary<Entity, int> HitList { get; } = new();
            public VisualType VisualType { get; set; } = VisualType.Player;
            public short VNum => 0;
            public long VisualId { get; set; }
            public byte Direction { get; set; }
            public Guid MapInstanceId { get; }
            public NosCore.GameObject.Services.MapInstanceGenerationService.MapInstance MapInstance { get; set; } = null!;
            public short PositionX { get; set; }
            public short PositionY { get; set; }
        }
    }
}
