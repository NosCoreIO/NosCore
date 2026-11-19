//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading;
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
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class TauntTests
    {
        private const int BobsThreat = 500;

        private TestWorld World;
        private ClientSession _alice;
        private ClientSession _bob;
        private MonsterComponentBundle _monster;
        private IAggroService _aggro;
        private DamageResult _blow = new(0, SuPacketHitMode.NoDamageSuccess);

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _alice = await World.CreateSessionAsync();
            _bob = await World.CreateSessionAsync();
            _monster = World.GetBaseMap(TestWorld.TestMapId)
                .SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 1000 }, 5, 5, false);
            World.UseReal<IAggroService, AggroService>();
            _aggro = World.Create<IAggroService>();
            World.Freeze<Mock<IDamageCalculator>>()
                .Setup(c => c.Calculate(It.IsAny<CombatStats>(), It.IsAny<CombatStats>(), It.IsAny<SkillInfo>()))
                .Returns(() => _blow);
            World.Freeze<Mock<IBattleStatsProvider>>()
                .Setup(s => s.GetStats(It.IsAny<IAliveEntity>()))
                .Returns(new CombatStats());
        }

        [TestMethod]
        public async Task ATauntTurnsAMonsterOnTheCaster()
        {
            await new Spec("A taunt turns a monster fighting Bob onto Alice")
                .Given(TheMonsterIsFightingBob)
                .WhenAsync(AliceTauntsIt)
                .Then(TheMonsterShouldBeAfterAlice)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheTaunterTakesOverTheThreatBuiltSoFar()
        {
            await new Spec("Alice takes over the threat Bob had built")
                .Given(TheMonsterIsFightingBob)
                .WhenAsync(AliceTauntsIt)
                .Then(TheThreatOnTheMonsterShouldBe_, BobsThreat)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BobCannotWinTheMonsterBackWithASmallerHit()
        {
            await new Spec("A hit from Bob that does not beat the threat he had leaves the monster on Alice")
                .Given(TheMonsterIsFightingBob)
                .AndAsync(AliceTauntsIt)
                .When(BobHitsItFor_, BobsThreat)
                .Then(TheMonsterShouldBeAfterAlice)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATauntDrawsAMonsterThatWasLeftAlone()
        {
            await new Spec("A taunt that deals no damage still draws a monster nobody was fighting")
                .WhenAsync(AliceTauntsIt)
                .Then(TheMonsterShouldBeAfterAlice)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADebuffWithoutATauntLeavesTheTargetAlone()
        {
            await new Spec("A debuff that declares no taunt leaves the monster on Bob")
                .Given(TheMonsterIsFightingBob)
                .WhenAsync(AliceCastsADebuffWithoutATaunt)
                .Then(TheMonsterShouldStayOnBob)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMissedTauntChangesNothing()
        {
            await new Spec("A taunting attack that misses leaves the monster on Bob")
                .Given(TheMonsterIsFightingBob)
                .And(AliceWillMiss)
                .WhenAsync(AliceShootsATauntingAttack)
                .Then(TheMonsterShouldStayOnBob)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlayerCannotBeTaunted()
        {
            await new Spec("A taunt on a player changes nothing and does not fail")
                .WhenAsync(AliceTauntsBob)
                .Then(TheHitShouldLand)
                .ExecuteAsync();
        }

        private HitOutcome? _outcome;

        private void TheMonsterIsFightingBob() => _aggro.AddThreat(_monster, _bob.Character, BobsThreat);

        private void AliceWillMiss() => _blow = new DamageResult(0, SuPacketHitMode.Miss);

        private void BobHitsItFor_(int damage) => _aggro.AddThreat(_monster, _bob.Character, damage);

        private static BCardDto Taunt() => new()
        {
            Type = BCardEffect.SpecialEffects2FocusEnemy.Type(),
            SubType = BCardEffect.SpecialEffects2FocusEnemy.SubType(),
            FirstData = 1
        };

        private Task AliceTauntsIt() => CastAsync(_monster, SkillKind.Debuff, Taunt());

        private Task AliceTauntsBob() => CastAsync(_bob.Character, SkillKind.Debuff, Taunt());

        private Task AliceCastsADebuffWithoutATaunt() => CastAsync(_monster, SkillKind.Debuff);

        private Task AliceShootsATauntingAttack() => CastAsync(_monster, SkillKind.Attack, Taunt());

        private async Task CastAsync(IAliveEntity target, SkillKind kind, params BCardDto[] bCards)
        {
            var completion = new TaskCompletionSource<HitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
            _outcome = await World.Create<HitQueue>().EnqueueAsync(new HitRequest(
                Origin: _alice.Character,
                Target: target,
                Skill: new SkillInfo(230, 10, 100, 23, 527, 528, 1, TargetHitType.SingleTargetHit, 10, 0, 0, 0, 0, 30, bCards,
                    Kind: kind),
                IsPrimaryTarget: true,
                Completion: completion,
                Cancellation: CancellationToken.None));
        }

        private void TheMonsterShouldBeAfterAlice() =>
            Assert.AreEqual(_alice.Character.VisualId, _aggro.Current(_monster).TargetVisualId);

        private void TheMonsterShouldStayOnBob() =>
            Assert.AreEqual(_bob.Character.VisualId, _aggro.Current(_monster).TargetVisualId);

        private void TheThreatOnTheMonsterShouldBe_(int threat) =>
            Assert.AreEqual(threat, _aggro.Current(_monster).ThreatScore);

        private void TheHitShouldLand() => Assert.AreEqual(HitStatus.Landed, _outcome?.Status);
    }
}
