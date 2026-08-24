//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Buff;
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
using Microsoft.Extensions.Logging;

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

            var queue = new HitQueue(calc.Object, stats.Object, buffs.Object, new Mock<IRegenerationService>().Object, new Mock<IVitalityService>().Object, new Mock<IInflictedCardService>().Object, new Mock<ILogger<HitQueue>>().Object);
            var skill = MakeSkill() with
            {
                SkillVnum = 7,
                Duration = 100,
                BCards = new[] { new BCardDto { Type = 3 /*AttackPower*/, FirstData = 10 } },
            };
            var request = Request(attacker, target) with { Skill = skill };

            await queue.EnqueueAsync(request);

            buffs.Verify(b => b.ApplySkillBuffAsync(target, (short)7, (short)100, skill.BCards, attacker), Times.Once);

            // The other half, and a different thing: the buff above turns the skill's own BCards
            // into a lasting effect, this inflicts the Card those BCards name by id. On the
            // entity that took the blow, which is the only reason this can run here at all.
            cards.Verify(c => c.InflictAsync(target, attacker, skill.BCards), Times.Once);
        }

        // The blow does not report itself finished until the card it carries has been applied.
        //
        // The Verify above cannot see this: it passes just as well if the call is fired and
        // forgotten, because Moq answers with an already-completed Task either way. And
        // fire-and-forget is exactly what this used to be - TryApplyHit was made async for this
        // one property, so something has to hold on to it.
        //
        // The wait can only fail in the safe direction: if the call is awaited the hit can never
        // complete, so the delay always wins; a loaded machine can make this pass when it should
        // not, never fail when it should not.
        [TestMethod]
        public async Task ALandedHitDoesNotFinishBeforeTheCardIsApplied()
        {
            var target = new FakeBattleEntity { Hp = 100, MaxHp = 100 };
            var attacker = new FakeBattleEntity();
            var calc = new Mock<IDamageCalculator>();
            calc.Setup(c => c.Calculate(It.IsAny<CombatStats>(), It.IsAny<CombatStats>(), It.IsAny<SkillInfo>()))
                .Returns(new DamageResult(10, SuPacketHitMode.SuccessAttack));
            var stats = new Mock<IBattleStatsProvider>();
            stats.Setup(s => s.GetStats(It.IsAny<IAliveEntity>())).Returns(new CombatStats());

            var applying = new TaskCompletionSource();
            var cards = new Mock<IInflictedCardService>();
            cards.Setup(c => c.InflictAsync(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(),
                    It.IsAny<System.Collections.Generic.IReadOnlyList<BCardDto>>()))
                .Returns(applying.Task);

            var queue = new HitQueue(calc.Object, stats.Object, new Mock<IBuffService>().Object,
                new Mock<IRegenerationService>().Object, cards.Object, new Mock<ILogger<HitQueue>>().Object);
            var skill = MakeSkill() with { BCards = new[] { new BCardDto { Type = 3 } } };

            var hit = queue.EnqueueAsync(Request(attacker, target) with { Skill = skill });

            Assert.AreNotSame(hit, await Task.WhenAny(hit, Task.Delay(200)),
                "the hit finished while the card was still being applied");

            applying.SetResult();
            await hit;
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

            var queue = new HitQueue(calc.Object, stats.Object, buffs.Object, new Mock<IRegenerationService>().Object, new Mock<IVitalityService>().Object, new Mock<IInflictedCardService>().Object, new Mock<ILogger<HitQueue>>().Object);
            var skill = MakeSkill() with { Duration = 100, BCards = new[] { new BCardDto { Type = 3 } } };

            await queue.EnqueueAsync(Request(attacker, target) with { Skill = skill });

            buffs.Verify(b => b.ApplySkillBuffAsync(It.IsAny<IAliveEntity>(), It.IsAny<short>(), It.IsAny<short>(), It.IsAny<System.Collections.Generic.IReadOnlyList<BCardDto>>(), It.IsAny<IAliveEntity>()), Times.Never);

            // Nor a card on a corpse: poisoning something already dead costs a packet and a buff
            // icon on an entity that is about to stop existing.
            cards.Verify(c => c.InflictAsync(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(),
                It.IsAny<System.Collections.Generic.IReadOnlyList<BCardDto>>()), Times.Never);
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

        // --- Type 37 subtype 31, "Decreases the opponent's HP by %s%%" ----------------------
        //
        // 112 of the 122 declarations on skills are this subtype, and the case was not read at
        // all. The percentage comes off MAXIMUM HP - our assumption, stated in the code, because
        // the file says only "HP by %s%%". These pin it: read off current HP every number below
        // changes, and nothing would raise.

        private static BCardDto PercentOfHp(short percent) => new()
        {
            Type = BCardEffect.RecoveryAndDamagePercentDecreaseEnemyHp.Type(),
            SubType = BCardEffect.RecoveryAndDamagePercentDecreaseEnemyHp.SubType(),
            FirstData = percent
        };

        private static HitQueue QueueDealing(int ordinaryDamage)
        {
            var calc = new Mock<IDamageCalculator>();
            calc.Setup(c => c.Calculate(It.IsAny<CombatStats>(), It.IsAny<CombatStats>(), It.IsAny<SkillInfo>()))
                .Returns(new DamageResult(ordinaryDamage, SuPacketHitMode.SuccessAttack));
            var stats = new Mock<IBattleStatsProvider>();
            stats.Setup(s => s.GetStats(It.IsAny<IAliveEntity>())).Returns(new CombatStats());
            return new HitQueue(calc.Object, stats.Object, new Mock<IBuffService>().Object,
                new Mock<IRegenerationService>().Object, new Mock<IVitalityService>().Object, new Mock<IInflictedCardService>().Object,
                new Mock<ILogger<HitQueue>>().Object);
        }

        [TestMethod]
        public async Task ThePercentageComesOffTheMaximumAndNotTheCurrentHp()
        {
            // Half of the maximum is 500. Off the current 600 it would be 300, and the target
            // would end at 290 instead of 90.
            var target = new FakeBattleEntity { Hp = 600, MaxHp = 1000 };
            var attacker = new FakeBattleEntity();

            await QueueDealing(10).EnqueueAsync(Request(attacker, target) with
            {
                Skill = MakeSkill() with { BCards = new[] { PercentOfHp(50) } }
            });

            Assert.AreEqual(90, target.Hp);
        }

        [TestMethod]
        public async Task WithoutTheEffectOnlyTheOrdinaryDamageLands()
        {
            var target = new FakeBattleEntity { Hp = 600, MaxHp = 1000 };
            var attacker = new FakeBattleEntity();

            await QueueDealing(10).EnqueueAsync(Request(attacker, target) with { Skill = MakeSkill() });

            Assert.AreEqual(590, target.Hp);
        }

        // Two slots on one skill add up: the loop must not stop at the first.
        [TestMethod]
        public async Task TwoDeclarationsOnOneSkillBothCount()
        {
            var target = new FakeBattleEntity { Hp = 1000, MaxHp = 1000 };
            var attacker = new FakeBattleEntity();

            // One point of ordinary damage, not zero: a blow that rolls zero is already a miss
            // by the guard above, and the percentage rides along with a blow that landed.
            await QueueDealing(1).EnqueueAsync(Request(attacker, target) with
            {
                Skill = MakeSkill() with { BCards = new[] { PercentOfHp(20), PercentOfHp(30) } }
            });

            Assert.AreEqual(499, target.Hp);
        }

        // It goes through the ordinary death rather than being floored short of it.
        [TestMethod]
        public async Task ThePercentageDamageCanKillAndTheDeathIsTheOrdinaryOne()
        {
            var target = new FakeBattleEntity { Hp = 200, MaxHp = 1000 };
            var attacker = new FakeBattleEntity();

            var outcome = await QueueDealing(1).EnqueueAsync(Request(attacker, target) with
            {
                Skill = MakeSkill() with { BCards = new[] { PercentOfHp(90) } }
            });

            Assert.AreEqual(0, target.Hp);
            Assert.IsTrue(outcome.Killed);
            Assert.IsFalse(target.IsAlive);
        }

        // Subtype 32 hits the caster, and no skill in the file declares it. Reading it here would
        // take the HP off the wrong entity.
        [TestMethod]
        public async Task TheSelfInflictedSubtypeIsNotReadAsIfItHitTheTarget()
        {
            var target = new FakeBattleEntity { Hp = 1000, MaxHp = 1000 };
            var attacker = new FakeBattleEntity();
            var selfInflicted = new BCardDto
            {
                Type = BCardEffect.RecoveryAndDamagePercentDecreaseSelfHp.Type(),
                SubType = BCardEffect.RecoveryAndDamagePercentDecreaseSelfHp.SubType(),
                FirstData = 50
            };

            await QueueDealing(1).EnqueueAsync(Request(attacker, target) with
            {
                Skill = MakeSkill() with { BCards = new[] { selfInflicted } }
            });

            // Only the ordinary point of damage: the self-inflicted subtype took nothing here.
            Assert.AreEqual(999, target.Hp);
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

            return new HitQueue(calc.Object, stats.Object, new Mock<IBuffService>().Object, new Mock<IRegenerationService>().Object, new Mock<IVitalityService>().Object, new Mock<IInflictedCardService>().Object, new Mock<ILogger<HitQueue>>().Object);
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
