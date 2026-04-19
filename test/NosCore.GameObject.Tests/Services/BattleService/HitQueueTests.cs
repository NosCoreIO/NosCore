//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class HitQueueTests
    {
        private static SkillInfo MakeSkill() => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0, TargetType: 0,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        [TestMethod]
        public async Task EnqueueLandedHitSubtractsDamage()
        {
            var target = new FakeBattleEntity { Hp = 100, MaxHp = 100 };
            var attacker = new FakeBattleEntity();
            var queue = BuildQueue(d => d.Damage = 40);

            var outcome = await queue.EnqueueAsync(Request(attacker, target));

            Assert.AreEqual(HitStatus.Landed, outcome.Status);
            Assert.AreEqual(40, outcome.Damage);
            Assert.AreEqual(60, target.Hp);
            Assert.IsFalse(outcome.Killed);
        }

        [TestMethod]
        public async Task ConcurrentAttackersStillConserveHp()
        {
            // Two attackers fire twenty 10-damage hits in parallel. Since the queue
            // serializes per-target, final HP must be 200 - 400 = -200 (clamped to 0),
            // with total credited damage in HitList == 200 (not more, overkill clipped).
            var target = new FakeBattleEntity { Hp = 200, MaxHp = 200 };
            var a = new FakeBattleEntity { VisualId = 1, Handle = Entity.Null };
            var b = new FakeBattleEntity { VisualId = 2, Handle = Entity.Null };
            var queue = BuildQueue(d => d.Damage = 10);

            var tasks = Enumerable.Range(0, 20).Select(async i =>
            {
                var attacker = i % 2 == 0 ? a : b;
                return await queue.EnqueueAsync(Request(attacker, target));
            }).ToArray();

            await Task.WhenAll(tasks);

            Assert.AreEqual(0, target.Hp);
            Assert.AreEqual(200, target.HitList.Values.Sum(), "sum of credited damage must equal max HP (overkill clipped)");
            Assert.IsTrue(tasks.Any(t => t.Result.Killed), "at least one hit should have dealt the killing blow");
        }

        [TestMethod]
        public async Task HitsToDeadTargetAreCancelled()
        {
            var target = new FakeBattleEntity { Hp = 0, MaxHp = 100 };
            var attacker = new FakeBattleEntity();
            var queue = BuildQueue(d => d.Damage = 50);

            var outcome = await queue.EnqueueAsync(Request(attacker, target));

            Assert.AreEqual(HitStatus.Cancelled, outcome.Status);
            Assert.AreEqual(0, outcome.Damage);
        }

        [TestMethod]
        public async Task LandedHitAppliesSkillBuffsWhenSkillHasDuration()
        {
            var target = new FakeBattleEntity { Hp = 100, MaxHp = 100 };
            var attacker = new FakeBattleEntity();
            var buffs = new Mock<IBuffService>();
            var calc = new Mock<IDamageCalculator>();
            calc.Setup(c => c.Calculate(It.IsAny<CombatStats>(), It.IsAny<CombatStats>(), It.IsAny<SkillInfo>()))
                .Returns(new DamageResult(10, SuPacketHitMode.SuccessAttack));
            var stats = new Mock<IBattleStatsProvider>();
            stats.Setup(s => s.GetStats(It.IsAny<IAliveEntity>())).Returns(new CombatStats());

            var queue = new HitQueue(calc.Object, stats.Object, buffs.Object, new Mock<ILogger>().Object);
            var skill = MakeSkill() with
            {
                SkillVnum = 7,
                Duration = 100,
                BCards = new[] { new BCardDto { Type = 3 /*AttackPower*/, FirstData = 10 } },
            };
            var request = Request(attacker, target) with { Skill = skill };

            await queue.EnqueueAsync(request);

            buffs.Verify(b => b.ApplySkillBuffAsync(target, (short)7, (short)100, skill.BCards, attacker), Times.Once);
        }

        [TestMethod]
        public async Task KillingHitSkipsBuffApplication()
        {
            var target = new FakeBattleEntity { Hp = 5, MaxHp = 100 };
            var attacker = new FakeBattleEntity();
            var buffs = new Mock<IBuffService>();
            var calc = new Mock<IDamageCalculator>();
            calc.Setup(c => c.Calculate(It.IsAny<CombatStats>(), It.IsAny<CombatStats>(), It.IsAny<SkillInfo>()))
                .Returns(new DamageResult(50, SuPacketHitMode.SuccessAttack));
            var stats = new Mock<IBattleStatsProvider>();
            stats.Setup(s => s.GetStats(It.IsAny<IAliveEntity>())).Returns(new CombatStats());

            var queue = new HitQueue(calc.Object, stats.Object, buffs.Object, new Mock<ILogger>().Object);
            var skill = MakeSkill() with { Duration = 100, BCards = new[] { new BCardDto { Type = 3 } } };

            await queue.EnqueueAsync(Request(attacker, target) with { Skill = skill });

            buffs.Verify(b => b.ApplySkillBuffAsync(It.IsAny<IAliveEntity>(), It.IsAny<short>(), It.IsAny<short>(), It.IsAny<System.Collections.Generic.IReadOnlyList<BCardDto>>(), It.IsAny<IAliveEntity>()), Times.Never);
        }

        [TestMethod]
        public async Task MissHitsDoNotAffectHpOrHitList()
        {
            var target = new FakeBattleEntity { Hp = 100, MaxHp = 100 };
            var attacker = new FakeBattleEntity();
            var queue = BuildQueue(d => { d.Damage = 0; d.HitMode = SuPacketHitMode.Miss; });

            var outcome = await queue.EnqueueAsync(Request(attacker, target));

            Assert.AreEqual(HitStatus.Missed, outcome.Status);
            Assert.AreEqual(100, target.Hp);
            Assert.AreEqual(0, target.HitList.Count);
        }

        private static HitRequest Request(IAliveEntity attacker, IAliveEntity target) => new(
            Origin: attacker,
            Target: target,
            Skill: MakeSkill(),
            IsPrimaryTarget: true,
            Completion: new TaskCompletionSource<HitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously),
            Cancellation: CancellationToken.None);

        private static HitQueue BuildQueue(Action<MutableDamage> configure)
        {
            var damageTemplate = new MutableDamage { Damage = 0, HitMode = SuPacketHitMode.SuccessAttack };
            configure(damageTemplate);
            var calc = new Mock<IDamageCalculator>();
            calc.Setup(c => c.Calculate(It.IsAny<CombatStats>(), It.IsAny<CombatStats>(), It.IsAny<SkillInfo>()))
                .Returns(() => new DamageResult(damageTemplate.Damage, damageTemplate.HitMode));

            var stats = new Mock<IBattleStatsProvider>();
            stats.Setup(s => s.GetStats(It.IsAny<IAliveEntity>())).Returns(new CombatStats());

            return new HitQueue(calc.Object, stats.Object, new Mock<IBuffService>().Object, new Mock<ILogger>().Object);
        }

        private class MutableDamage
        {
            public int Damage;
            public SuPacketHitMode HitMode;
        }

        // Minimal in-memory IAliveEntity double: tracks only what HitQueue actually reads
        // (Hp / IsAlive / HitList / Handle) plus a VisualId for disambiguation.
        private class FakeBattleEntity : IAliveEntity
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
            public bool NoAttack => false;
            public bool NoMove => false;
            public bool IsAlive => Hp > 0;
            public short MapX => 0;
            public short MapY => 0;
            public int MaxHp { get; set; }
            public int MaxMp { get; set; } = 100;
            public byte Level { get; set; } = 1;
            public byte HeroLevel => 0;
            public short Race => 0;
            public NosCore.GameObject.Services.ShopService.Shop? Shop { get; set; }
            public SemaphoreSlim HitSemaphore { get; } = new(1, 1);
            public ConcurrentDictionary<Entity, int> HitList { get; } = new();
            public VisualType VisualType => VisualType.Player;
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
