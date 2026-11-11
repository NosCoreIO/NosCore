//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class CharacterStatsPacketTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IBattleStatsProvider> _stats;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _stats = new Mock<IBattleStatsProvider>();
        }

        [TestMethod]
        public void TheWindowShowsTheStatsCombatUses()
        {
            _stats.Setup(s => s.GetStats(It.IsAny<IAliveEntity>())).Returns(default(CombatStats) with
            {
                MinHit = 120, MaxHit = 150, HitRate = 40, CriticalChance = 4, CriticalRate = 70, MeleeUpgrade = 5,
                MinDistance = 80, MaxDistance = 95, DistanceRate = 33, RangedUpgrade = 3,
                Defence = 61, DefenceDodge = 22, DistanceDefence = 58, DistanceDefenceDodge = 21,
                MagicDefence = 47, DefenceUpgrade = 4,
                FireResistance = 4, WaterResistance = 18, LightResistance = 2, DarkResistance = 1,
            });

            var sc = _session.Character.GenerateSc(_stats.Object);

            Assert.AreEqual(5, sc.MainWeaponUpgrade);
            Assert.AreEqual(120, sc.MinHit);
            Assert.AreEqual(150, sc.MaxHit);
            Assert.AreEqual(40, sc.HitRate);
            Assert.AreEqual(4, sc.CriticalHitRate);
            Assert.AreEqual(70, sc.CriticalHitMultiplier);
            Assert.AreEqual(3, sc.SecondaryWeaponUpgrade);
            Assert.AreEqual(80, sc.SecondaryMinHit);
            Assert.AreEqual(95, sc.SecondaryMaxHit);
            Assert.AreEqual(33, sc.SecondaryHitRate);
            Assert.AreEqual(4, sc.ArmorUpgrade);
            Assert.AreEqual(61, sc.Defence);
            Assert.AreEqual(22, sc.DefenceRate);
            Assert.AreEqual(58, sc.DistanceDefence);
            Assert.AreEqual(21, sc.DistanceDefenceRate);
            Assert.AreEqual(47, sc.MagicalDefence);
            Assert.AreEqual(18, sc.WaterResistance);
        }

        [TestMethod]
        [DataRow(CharacterClassType.Adventurer, AttackType.Melee, AttackType.Ranged)]
        [DataRow(CharacterClassType.Swordsman, AttackType.Melee, AttackType.Ranged)]
        [DataRow(CharacterClassType.Archer, AttackType.Ranged, AttackType.Melee)]
        [DataRow(CharacterClassType.Mage, AttackType.Magical, AttackType.Ranged)]
        public void EachClassShowsItsOwnAttackRows(CharacterClassType characterClass, AttackType main, AttackType secondary)
        {
            _session.Character.Class = characterClass;

            var sc = _session.Character.GenerateSc(_stats.Object);

            Assert.AreEqual((byte)main, sc.Type);
            Assert.AreEqual((byte)secondary, sc.Type2);
        }
    }
}
