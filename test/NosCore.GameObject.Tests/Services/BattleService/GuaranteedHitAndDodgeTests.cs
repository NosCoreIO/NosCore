//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // Type 16 of BCard.dat, the two subtypes that override the ordinary dodge roll:
    //
    //   11: "There is a %s%% chance that every attack hits."     232 of 253 skill declarations
    //   21: "Always dodge the target with a probability of %s%%."  none on skills, 44 on cards
    //
    // Getting either one backwards raises nothing: combat carries on and the numbers drift.
    [TestClass]
    public class GuaranteedHitAndDodgeTests
    {
        private Mock<IRandomProvider> _random = null!;
        private DamageCalculator _calculator = null!;

        // A defender built to dodge nearly always, so an attack that lands can only have landed
        // because the guarantee overrode the roll.
        private static CombatStats Defender(int guaranteedDodge = 0) => new()
        {
            Level = 1,
            DefenceDodge = 5000,
            DistanceDefenceDodge = 5000,
            GuaranteedDodgeChance = guaranteedDodge
        };

        private static CombatStats Attacker(int guaranteedHit = 0) => new()
        {
            Level = 50,
            Class = CharacterClassType.Swordsman,
            MinHit = 100,
            MaxHit = 100,
            HitRate = 1,
            Morale = 50,
            GuaranteedHitChance = guaranteedHit
        };

        private static SkillInfo Skill(byte type) => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: type, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0,
            TargetType: 0, Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        private static SkillInfo Melee => Skill(0);

        [TestInitialize]
        public void Setup()
        {
            _random = new Mock<IRandomProvider>();
            // Every roll comes back 0, which is under any threshold - the ordinary dodge
            // chance has a floor of 1% - so the ordinary dodge always fires unless something
            // skips it. That is what makes a landed hit here proof of an override.
            _random.Setup(r => r.NextDouble()).Returns(0);
            _random.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(0);
            _calculator = new DamageCalculator(_random.Object);
        }

        [TestMethod]
        public void WithoutTheGuaranteeThisDefenderDodges()
        {
            var result = _calculator.Calculate(Attacker(), Defender(), Melee);

            Assert.AreEqual(SuPacketHitMode.Miss, result.HitMode);
        }

        [TestMethod]
        public void AGuaranteedHitBeatsADefenderThatWouldAlwaysDodge()
        {
            var result = _calculator.Calculate(Attacker(guaranteedHit: 100), Defender(), Melee);

            Assert.AreNotEqual(SuPacketHitMode.Miss, result.HitMode);
            Assert.IsTrue(result.Damage > 0);
        }

        // Zero must not read as "always". The percentage is summed from the active effects, so
        // the common case is nobody carrying one at all.
        [TestMethod]
        public void AZeroChanceGuaranteeDoesNothing()
        {
            var result = _calculator.Calculate(Attacker(guaranteedHit: 0), Defender(), Melee);

            Assert.AreEqual(SuPacketHitMode.Miss, result.HitMode);
        }

        // This one needs a high roll, not the class default: the ordinary dodge chance has a
        // floor of 1%, so with a roll of 0 even a defender with no dodge at all still dodges,
        // and there would be no "would otherwise be hit" to compare against.
        [TestMethod]
        public void AGuaranteedDodgeMissesADefenderThatWouldOtherwiseBeHit()
        {
            var random = new Mock<IRandomProvider>();
            random.Setup(r => r.NextDouble()).Returns(0.99);
            var calculator = new DamageCalculator(random.Object);
            var wideOpen = new CombatStats { Level = 1, DefenceDodge = 0, DistanceDefenceDodge = 0 };

            var landed = calculator.Calculate(Attacker(), wideOpen, Melee);
            Assert.AreNotEqual(SuPacketHitMode.Miss, landed.HitMode, "setup: this one should land");

            var dodged = calculator.Calculate(Attacker(),
                wideOpen with { GuaranteedDodgeChance = 100 }, Melee);

            Assert.AreEqual(SuPacketHitMode.Miss, dodged.HitMode);
        }

        // The order is ours and not the file's, so it gets a test rather than a comment alone:
        // with both at 100 the attacker's guarantee wins.
        [TestMethod]
        public void WhenBothFireTheAttackersGuaranteeWins()
        {
            var result = _calculator.Calculate(
                Attacker(guaranteedHit: 100), Defender(guaranteedDodge: 100), Melee);

            Assert.AreNotEqual(SuPacketHitMode.Miss, result.HitMode);
        }

        // A mage's attacks never enter the dodge phase, so neither guarantee applies there.
        [TestMethod]
        public void AMagicSkillIsUntouchedByEither()
        {
            var mage = Attacker() with { Class = CharacterClassType.Mage };

            var result = _calculator.Calculate(mage, Defender(guaranteedDodge: 100),
                Skill(2));

            Assert.AreNotEqual(SuPacketHitMode.Miss, result.HitMode);
        }
    }
}
