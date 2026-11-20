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
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class ChargeSkillTests
    {
        private const long ChargeSkillId = 1537;
        private const long AttackSkillId = 200;
        private const long DebuffSkillId = 230;

        // Meditation: "Melee attack is increased by 200".
        private static readonly IReadOnlyList<BCardDto> Meditation =
            new[] { new BCardDto { Type = BCardType.CardType.AttackPower, SubType = 21, FirstData = 200 } };

        private TestWorld World;
        private ClientSession _session;
        private MapInstance _map;
        private MonsterComponentBundle _monster;
        private Mock<IBuffService> _buffs;
        private readonly List<HitRequest> _hits = new();

        private PlayerComponentBundle Caster => _session.Character;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _map = World.GetBaseMap(TestWorld.TestMapId);
            _monster = _map.SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 1000 }, 5, 5, false);

            var skills = World.Freeze<Mock<ISkillResolver>>();
            skills.Setup(s => s.Resolve(It.IsAny<IAliveEntity>(), ChargeSkillId)).Returns(Skill(1537, SkillReach.SelfSkill, 4, Meditation));
            skills.Setup(s => s.Resolve(It.IsAny<IAliveEntity>(), AttackSkillId)).Returns(Skill(200, SkillReach.TargetedSkill, 0, Array.Empty<BCardDto>()));
            skills.Setup(s => s.Resolve(It.IsAny<IAliveEntity>(), DebuffSkillId))
                .Returns(Skill(230, SkillReach.TargetedSkill, 1, Array.Empty<BCardDto>()) with { Kind = SkillKind.Debuff });
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
            _buffs = World.Freeze<Mock<IBuffService>>();
        }

        private static SkillInfo Skill(short vnum, byte targetType, byte type, IReadOnlyList<BCardDto> cards) =>
            new(vnum, vnum, 0, 0, 0, 0, type, TargetHitType.SingleTargetHit, 1, 0, targetType, 0, 0, 0, cards);

        [TestMethod]
        public async Task CastingAChargeStoresItAndShowsItOnTheCaster()
        {
            await new Spec("A charge skill is kept on the caster and drawn as card 0 carrying its value")
                .WhenAsync(TheCasterUses_On_, ChargeSkillId, "caster")
                .Then(TheCasterShouldHoldTheCharge_, true)
                .And(TheMapShouldSeeTheCharge_, 200)
                .And(TheChargeCardsShouldReachNobody)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheNextAttackCarriesTheCharge()
        {
            await new Spec("The attack after a charge carries the charge's cards into its damage")
                .GivenAsync(TheCasterUses_On_, ChargeSkillId, "caster")
                .WhenAsync(TheCasterUses_On_, AttackSkillId, "monster")
                .Then(TheHit_ShouldCarryTheCharge_, 0, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheChargeIsGoneOnceSpent()
        {
            await new Spec("A spent charge is taken off the caster and off the screen")
                .GivenAsync(TheCasterUses_On_, ChargeSkillId, "caster")
                .WhenAsync(TheCasterUses_On_, AttackSkillId, "monster")
                .Then(TheCasterShouldHoldTheCharge_, false)
                .And(TheMapShouldSeeTheCharge_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheAttackAfterTheChargedOneIsPlain()
        {
            await new Spec("Only the first attack after a charge carries it")
                .GivenAsync(TheCasterUses_On_, ChargeSkillId, "caster")
                .AndAsync(TheCasterUses_On_, AttackSkillId, "monster")
                .WhenAsync(TheCasterUses_On_, AttackSkillId, "monster")
                .Then(TheHit_ShouldCarryTheCharge_, 1, false)
                .And(NoChargeShouldBeShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAttackWithoutAChargeCarriesNone()
        {
            await new Spec("An attack that no charge came before carries none")
                .WhenAsync(TheCasterUses_On_, AttackSkillId, "monster")
                .Then(TheHit_ShouldCarryTheCharge_, 0, false)
                .And(NoChargeShouldBeShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADebuffDoesNotSpendTheCharge()
        {
            await new Spec("A skill that only inflicts a debuff leaves the charge for the next blow")
                .GivenAsync(TheCasterUses_On_, ChargeSkillId, "caster")
                .WhenAsync(TheCasterUses_On_, DebuffSkillId, "monster")
                .Then(TheCasterShouldHoldTheCharge_, true)
                .And(TheHit_ShouldCarryTheCharge_, 0, false)
                .ExecuteAsync();
        }

        private async Task TheCasterUses_On_(long skillId, string target)
        {
            _map.LastPackets.Clear();
            IAliveEntity recipient = target == "caster" ? Caster : _monster;
            Assert.IsTrue(await World.Create<GameObject.Services.BattleService.BattleService>()
                .Hit(Caster, recipient, new HitArguments { SkillId = skillId }));
        }

        private void TheCasterShouldHoldTheCharge_(bool held) =>
            Assert.AreEqual(held, Caster.ChargeBCards == Meditation);

        private void TheMapShouldSeeTheCharge_(int value) =>
            PacketAssert.AreEqual(new BfPacket
            {
                VisualType = VisualType.Player,
                VisualId = Caster.VisualId,
                Buff = new BfPacket.BuffElementSubPacket { ChargeValue = value, BuffId = 0, Duration = value },
                BuffLevel = Caster.Level,
            }, _map.LastPackets.OfType<BfPacket>().Single());

        private void NoChargeShouldBeShown() => Assert.IsFalse(_map.LastPackets.OfType<BfPacket>().Any());

        private void TheChargeCardsShouldReachNobody()
        {
            _buffs.Verify(b => b.InflictCardsAsync(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), Meditation), Times.Never);
            Assert.AreEqual(0, _hits.Count);
        }

        private void TheHit_ShouldCarryTheCharge_(int index, bool carried) =>
            Assert.AreEqual(carried, _hits[index].Skill.ChargeBCards == Meditation);
    }
}
