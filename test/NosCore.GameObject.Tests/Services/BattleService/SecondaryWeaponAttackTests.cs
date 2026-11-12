//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Tests.Shared;
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
        private static async Task<CombatStats> NakedStatsAsync(CharacterClassType cls, byte level)
        {
            var character = (await (await TestWorld.CreateAsync()).CreateSessionAsync()).Character;
            character.Class = cls;
            character.Level = level;

            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>());
            var equipment = new Mock<IEquipmentStatsService>();
            equipment.Setup(s => s.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);

            return BattleStats.Provider(buffs.Object, equipment.Object).GetStats(character);
        }

        [TestMethod]
        [DataRow(CharacterClassType.Adventurer, (byte)10, 19)]
        [DataRow(CharacterClassType.Swordsman, (byte)10, 20)]
        [DataRow(CharacterClassType.Archer, (byte)10, 21)]
        [DataRow(CharacterClassType.Mage, (byte)10, 20)]
        [DataRow(CharacterClassType.Swordsman, (byte)99, 127)]
        [DataRow(CharacterClassType.Archer, (byte)99, 137)]
        public async Task TheSecondaryWeaponsAttackFollowsItsOwnClassTable(CharacterClassType cls, byte level, int attack)
        {
            var stats = await NakedStatsAsync(cls, level);

            Assert.AreEqual(attack, stats.MinDistance);
            Assert.AreEqual(attack, stats.MaxDistance);
        }
    }
}
