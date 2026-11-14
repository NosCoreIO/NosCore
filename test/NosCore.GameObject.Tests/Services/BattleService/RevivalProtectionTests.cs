//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class RevivalProtectionTests
    {
        private TestWorld World;
        private PlayerComponentBundle _protected;
        private PlayerComponentBundle _other;
        private BuffService _buffs;
        private HitQueue _queue;
        private HitOutcome _outcome;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _protected = (await World.CreateSessionAsync()).Character;
            _other = (await World.CreateSessionAsync()).Character;
            foreach (var player in new[] { _protected, _other })
            {
                player.MaxHp = 1000;
                player.Hp = 1000;
            }

            var catalog = new Mock<ICardCatalog>();
            catalog.Setup(c => c.GetCard(RevivalProtectionCard.CardId)).Returns(RevivalProtectionCard.Card);
            catalog.Setup(c => c.GetCardBCards(RevivalProtectionCard.CardId)).Returns(RevivalProtectionCard.BCards);
            _buffs = new BuffService(World.Clock, catalog.Object, new Mock<IRandomProvider>().Object, new Mock<IDebuffResistanceService>().Object, new Mock<Wolverine.IMessageBus>().Object);

            var calculator = new Mock<IDamageCalculator>();
            calculator.Setup(c => c.Calculate(It.IsAny<CombatStats>(), It.IsAny<CombatStats>(), It.IsAny<SkillInfo>()))
                .Returns(new DamageResult(100, SuPacketHitMode.SuccessAttack));
            var stats = new Mock<IBattleStatsProvider>();
            stats.Setup(s => s.GetStats(It.IsAny<IAliveEntity>())).Returns(new CombatStats());
            _queue = new HitQueue(calculator.Object, stats.Object, _buffs,
                new Mock<IRegenerationService>().Object, new Mock<IVitalityService>().Object,
                new Mock<ILogger<HitQueue>>().Object, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task AProtectedPlayerTakesNoDamage()
        {
            await new Spec("A player under card 169 loses no HP to another player's blow")
                .GivenAsync(ThePlayerHoldsTheRevivalProtection)
                .WhenAsync(TheOtherPlayerHitsTheProtectedOne)
                .Then(TheBlowShouldMiss)
                .And(TheProtectedPlayerShouldHave_Hp, 1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AProtectedPlayerDealsNoDamage()
        {
            await new Spec("A player under card 169 cannot hurt anyone either")
                .GivenAsync(ThePlayerHoldsTheRevivalProtection)
                .WhenAsync(TheProtectedPlayerHitsTheOtherOne)
                .Then(TheBlowShouldMiss)
                .And(TheOtherPlayerShouldHave_Hp, 1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheProtectionLastsUntilItsFiveSecondsAreUp()
        {
            await new Spec("Card 169 still protects four seconds in")
                .GivenAsync(ThePlayerHoldsTheRevivalProtection)
                .And(Seconds_Pass, 4)
                .WhenAsync(TheOtherPlayerHitsTheProtectedOne)
                .Then(TheProtectedPlayerShouldHave_Hp, 1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheProtectionEndsAfterFiveSeconds()
        {
            await new Spec("Card 169 lasts 50 deciseconds; after that the player is hit as usual")
                .GivenAsync(ThePlayerHoldsTheRevivalProtection)
                .And(Seconds_Pass, 5)
                .WhenAsync(TheOtherPlayerHitsTheProtectedOne)
                .Then(TheProtectedPlayerShouldHave_Hp, 950)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheLifeTickRemovesTheProtectionOnceItRunsOut()
        {
            await new Spec("The buff tick drops card 169 after five seconds")
                .GivenAsync(ThePlayerHoldsTheRevivalProtection)
                .And(Seconds_Pass, 5)
                .WhenAsync(TheBuffsAreTicked)
                .Then(TheProtectedPlayerShouldNoLongerHoldTheCard)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AttackingDoesNotEndTheProtection()
        {
            await new Spec("Card 169 has no effect that ends it early, so swinging leaves it in place")
                .GivenAsync(ThePlayerHoldsTheRevivalProtection)
                .AndAsync(TheProtectedPlayerHitsTheOtherOne)
                .WhenAsync(TheOtherPlayerHitsTheProtectedOne)
                .Then(TheProtectedPlayerShouldStillHoldTheCard)
                .And(TheProtectedPlayerShouldHave_Hp, 1000)
                .ExecuteAsync();
        }

        private Task ThePlayerHoldsTheRevivalProtection() =>
            _buffs.ApplyAsync(_protected, RevivalProtectionCard.Card, RevivalProtectionCard.BCards, _protected);

        private void Seconds_Pass(int seconds) =>
            World.Clock.Advance(Duration.FromSeconds(seconds));

        private async Task TheOtherPlayerHitsTheProtectedOne() =>
            _outcome = await _queue.EnqueueAsync(Request(_other, _protected));

        private async Task TheProtectedPlayerHitsTheOtherOne() =>
            _outcome = await _queue.EnqueueAsync(Request(_protected, _other));

        private Task TheBuffsAreTicked() => _buffs.TickAsync(_protected);

        private void TheBlowShouldMiss() =>
            Assert.AreEqual(HitStatus.Missed, _outcome.Status);

        private void TheProtectedPlayerShouldHave_Hp(int hp) => Assert.AreEqual(hp, _protected.Hp);

        private void TheOtherPlayerShouldHave_Hp(int hp) => Assert.AreEqual(hp, _other.Hp);

        private void TheProtectedPlayerShouldStillHoldTheCard() =>
            Assert.IsTrue(_buffs.HasBuff(_protected, RevivalProtectionCard.CardId));

        private void TheProtectedPlayerShouldNoLongerHoldTheCard() =>
            Assert.IsFalse(_buffs.HasBuff(_protected, RevivalProtectionCard.CardId));

        private static HitRequest Request(IAliveEntity attacker, IAliveEntity target) => new(
            Origin: attacker,
            Target: target,
            Skill: new SkillInfo(
                SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
                Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0, TargetType: 0,
                Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>()),
            IsPrimaryTarget: true,
            Completion: new TaskCompletionSource<HitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously),
            Cancellation: CancellationToken.None);
    }
}
