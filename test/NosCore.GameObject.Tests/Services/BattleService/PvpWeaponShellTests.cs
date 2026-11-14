//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.DodgeChanceService;
using NosCore.Algorithm.MonsterLevelDamageService;
using NosCore.Algorithm.UpgradeBonusService;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class PvpWeaponShellTests
    {
        private static DamageCalculator Calculator(double roll = 0.99)
        {
            var random = new Mock<IRandomProvider>();
            random.Setup(r => r.NextDouble()).Returns(roll);
            random.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(0);
            return new DamageCalculator(random.Object, new MonsterLevelDamageService(), new UpgradeBonusService(), new DodgeChanceService());
        }

        private static CombatStats Attacker(PvpShellStats shell = default) => new()
        {
            Level = 50,
            Class = CharacterClassType.Swordsman,
            MinHit = 400,
            MaxHit = 400,
            HitRate = 5000,
            Morale = 50,
            Element = 1,
            ElementRate = 100,
            IsPlayer = true,
            PvpShell = shell,
        };

        private static CombatStats Defender(bool isPlayer = true) => new()
        {
            Level = 50,
            Defence = 200,
            FireResistance = 50,
            IsPlayer = isPlayer,
        };

        private static SkillInfo Melee => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 0, TargetRange: 0,
            TargetType: 0, Element: 1, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        private static int Hit(PvpShellStats shell, bool defenderIsPlayer = true, double roll = 0.99) =>
            Calculator(roll).Calculate(Attacker(shell), Defender(defenderIsPlayer), Melee).Damage;

        [TestMethod]
        public void ReducingTheOpponentsDefenceDoesMoreDamage() =>
            Assert.IsTrue(Hit(new PvpShellStats(EnemyDefenceReduction: 50)) > Hit(default));

        [TestMethod]
        public void ReducingTheOpponentsResistanceDoesMoreDamage() =>
            Assert.IsTrue(Hit(new PvpShellStats(EnemyFireResistance: -30)) > Hit(default));

        [TestMethod]
        public void IgnoringTheResistanceWorksOnlyOnALuckyRoll()
        {
            var shell = new PvpShellStats(IgnoresFireResistance: true);

            Assert.IsTrue(Hit(shell, roll: 0.1) > Hit(default, roll: 0.1));
            Assert.AreEqual(Hit(default), Hit(shell, roll: 0.99));
        }

        [TestMethod]
        public void TheOptionsDoNothingAgainstAMonster() =>
            Assert.AreEqual(Hit(default, defenderIsPlayer: false),
                Hit(new PvpShellStats(EnemyDefenceReduction: 50, EnemyFireResistance: -30), defenderIsPlayer: false));

        [TestMethod]
        public void HittingEveryTimeLandsEvenOnARollThatWouldMiss()
        {
            var calculator = Calculator(roll: 0);
            var dodgy = Defender() with { DefenceDodge = 100000 };

            Assert.AreEqual(SuPacketHitMode.Miss, calculator.Calculate(Attacker(), dodgy, Melee).HitMode);
            Assert.AreNotEqual(SuPacketHitMode.Miss,
                calculator.Calculate(Attacker(new PvpShellStats(AlwaysHits: true)), dodgy, Melee).HitMode);
        }
    }
}
