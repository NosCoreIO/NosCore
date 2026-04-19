//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class NpcCombatCatalogTests
    {
        [TestMethod]
        public void SkillsBucketByMonsterVnum()
        {
            var catalog = new NpcCombatCatalog(
                new List<NpcMonsterSkillDto>
                {
                    new() { NpcMonsterSkillId = 1, NpcMonsterVNum = 10, SkillVNum = 100 },
                    new() { NpcMonsterSkillId = 2, NpcMonsterVNum = 10, SkillVNum = 200 },
                    new() { NpcMonsterSkillId = 3, NpcMonsterVNum = 20, SkillVNum = 300 },
                },
                new List<DropDto>(),
                new List<BCardDto>());

            var skills = catalog.GetSkills(10);
            Assert.AreEqual(2, skills.Count);
        }

        [TestMethod]
        public void DropsFilterOutNonMobEntries()
        {
            var catalog = new NpcCombatCatalog(
                new List<NpcMonsterSkillDto>(),
                new List<DropDto>
                {
                    new() { DropId = 1, VNum = 1, Amount = 1, DropChance = 500, MonsterVNum = 10 },
                    new() { DropId = 2, VNum = 2, Amount = 1, DropChance = 500, MonsterVNum = null }, // map-wide drop
                },
                new List<BCardDto>());

            Assert.AreEqual(1, catalog.GetDrops(10).Count, "only mob-specific drops should be indexed");
        }

        [TestMethod]
        public void UnknownVnumsReturnEmptyLists()
        {
            var catalog = new NpcCombatCatalog(new(), new(), new());
            Assert.AreEqual(0, catalog.GetSkills(999).Count);
            Assert.AreEqual(0, catalog.GetDrops(999).Count);
            Assert.AreEqual(0, catalog.GetNpcBCards(999).Count);
            Assert.AreEqual(0, catalog.GetSkillBCards(999).Count);
        }
    }
}
