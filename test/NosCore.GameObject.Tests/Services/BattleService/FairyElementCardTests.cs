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
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // BCard type 96, subtypes 0 and 1: "Increases the equipped fairy's element by %s" and its
    // decreasing half. Card 4408 (the Fairy Attack Potion's) carries +5, the Elemental Blessing
    // cards up to +40, and gloves, bracelets and titles carry it while worn.
    [TestClass]
    public class FairyElementCardTests
    {
        private const int FairyRate = 30;

        private EquipmentStats Gear = EquipmentStats.None;
        private readonly List<BuffInstance> Buffs = new();
        private CombatStats Stats;

        [TestMethod]
        public async Task ABuffRaisesTheWornFairysElement()
        {
            await new Spec("An effect on the equipped fairy's element raises the element rate a character fights with")
                .Given(AWornFairyOfRate_, FairyRate)
                .And(ABuffWith_Of_, BCardEffect.Type96IncreaseFairyElement, (short)5)
                .When(ResolvingTheCharactersStats)
                .Then(TheElementRateShouldBe_, FairyRate + 5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDecreasingSubtypeLowersIt()
        {
            await new Spec("The decreasing subtype takes the value off the fairy's element")
                .Given(AWornFairyOfRate_, FairyRate)
                .And(ABuffWith_Of_, BCardEffect.Type96DecreaseFairyElement, (short)10)
                .When(ResolvingTheCharactersStats)
                .Then(TheElementRateShouldBe_, FairyRate - 10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WornGearCarryingTheEffectCountsToo()
        {
            await new Spec("Gloves carrying the effect raise the fairy's element while worn")
                .Given(AWornFairyOfRate_, FairyRate)
                .And(WornGearWith_Of_, BCardEffect.Type96IncreaseFairyElement, (short)10)
                .When(ResolvingTheCharactersStats)
                .Then(TheElementRateShouldBe_, FairyRate + 10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutAFairyTheEffectHasNothingToRaise()
        {
            await new Spec("Without a fairy there is no fairy element to raise")
                .Given(NoFairy)
                .And(ABuffWith_Of_, BCardEffect.Type96IncreaseFairyElement, (short)5)
                .When(ResolvingTheCharactersStats)
                .Then(TheElementRateShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheFairyBoosterAddsThirtyToTheWornFairy()
        {
            await new Spec("The Fairy Booster's card has no BCards but raises a worn fairy's element by 30")
                .Given(AWornFairyOfRate_, FairyRate)
                .And(TheFairyBoosterIsActive)
                .When(ResolvingTheCharactersStats)
                .Then(TheElementRateShouldBe_, FairyRate + 30)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheFairyBoosterNeedsAFairy()
        {
            await new Spec("Without a fairy the booster has nothing to raise")
                .Given(NoFairy)
                .And(TheFairyBoosterIsActive)
                .When(ResolvingTheCharactersStats)
                .Then(TheElementRateShouldBe_, 0)
                .ExecuteAsync();
        }

        private void TheFairyBoosterIsActive() =>
            Buffs.Add(new BuffInstance(CardId: FairyBooster.CardId, BuffType: BuffType.Good, Caster: null,
                StartedAt: Instant.MinValue, ExpiresAt: Instant.MaxValue, BCards: Array.Empty<BCardDto>(), Static: true));

        private void AWornFairyOfRate_(int rate) =>
            Gear = EquipmentStats.None with { ElementRate = rate, Element = ElementType.Fire, FairyWorn = true };

        private void NoFairy() => Gear = EquipmentStats.None;

        private void ABuffWith_Of_(BCardEffect effect, short value) =>
            Buffs.Add(new BuffInstance(CardId: 4408, BuffType: BuffType.Good, Caster: null,
                StartedAt: Instant.MinValue, ExpiresAt: Instant.MaxValue, BCards: new[] { Card(effect, value) }));

        private void WornGearWith_Of_(BCardEffect effect, short value) =>
            Gear = Gear with { BCards = new[] { Card(effect, value) } };

        private void ResolvingTheCharactersStats()
        {
            var character = new Mock<ICharacterEntity>();
            character.SetupGet(c => c.Level).Returns((byte)50);
            character.SetupGet(c => c.Class).Returns(CharacterClassType.Swordsman);

            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(Buffs);
            var equipment = new Mock<IEquipmentStatsService>();
            equipment.Setup(e => e.Resolve(It.IsAny<IAliveEntity>())).Returns(Gear);

            Stats = new BattleStatsProvider(buffs.Object, equipment.Object).GetStats(character.Object);
        }

        private void TheElementRateShouldBe_(int expected) => Assert.AreEqual(expected, Stats.ElementRate);

        private static BCardDto Card(BCardEffect effect, short value) => new()
        {
            Type = effect.Type(),
            SubType = effect.SubType(),
            FirstData = value,
        };
    }
}
