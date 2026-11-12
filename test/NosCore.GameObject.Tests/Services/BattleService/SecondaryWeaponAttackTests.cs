//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SecondaryWeaponAttackTests
    {
        private static CombatStats NakedStats(CharacterClassType cls, byte level)
        {
            var character = new Mock<ICharacterEntity>();
            character.SetupGet(c => c.Class).Returns(cls);
            character.SetupGet(c => c.Level).Returns(level);

            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>());
            var equipment = new Mock<IEquipmentStatsService>();
            equipment.Setup(s => s.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);

            return BattleStats.Provider(buffs.Object, equipment.Object).GetStats(character.Object);
        }

        [TestMethod]
        [DataRow(CharacterClassType.Adventurer, (byte)10, 19)]
        [DataRow(CharacterClassType.Swordsman, (byte)10, 20)]
        [DataRow(CharacterClassType.Archer, (byte)10, 21)]
        [DataRow(CharacterClassType.Mage, (byte)10, 20)]
        [DataRow(CharacterClassType.Swordsman, (byte)99, 127)]
        [DataRow(CharacterClassType.Archer, (byte)99, 137)]
        public void TheSecondaryWeaponsAttackFollowsItsOwnClassTable(CharacterClassType cls, byte level, int attack)
        {
            var stats = NakedStats(cls, level);

            Assert.AreEqual(attack, stats.MinDistance);
            Assert.AreEqual(attack, stats.MaxDistance);
        }
    }
}
