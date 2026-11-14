//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MateService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class PetSkillServiceTests
    {
        private const short Otter = 683;
        private const short Hoarder = 700;
        private const short Wolf = 12;
        private const short Bob = 317;
        private const long Owner = 1;

        private TestWorld World;
        private PetSkillService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = 663, SkillType = 5, CastId = 3 },
                new() { SkillVNum = 664, SkillType = 5, CastId = 2 },
                new() { SkillVNum = 665, SkillType = 5, CastId = 3 },
                new() { SkillVNum = 666, SkillType = 5, CastId = 3 },
                new() { SkillVNum = 1100, SkillType = 4, CastId = 1 }
            });
            World.Inject(new List<NpcMonsterSkillDto>
            {
                new() { NpcMonsterSkillId = 12, NpcMonsterVNum = Otter, SkillVNum = 664 },
                new() { NpcMonsterSkillId = 11, NpcMonsterVNum = Otter, SkillVNum = 663 },
                new() { NpcMonsterSkillId = 13, NpcMonsterVNum = Otter, SkillVNum = 1100 },
                new() { NpcMonsterSkillId = 20, NpcMonsterVNum = Wolf, SkillVNum = 1100 },
                new() { NpcMonsterSkillId = 30, NpcMonsterVNum = Bob, SkillVNum = 664 },
                new() { NpcMonsterSkillId = 41, NpcMonsterVNum = Hoarder, SkillVNum = 663 },
                new() { NpcMonsterSkillId = 42, NpcMonsterVNum = Hoarder, SkillVNum = 664 },
                new() { NpcMonsterSkillId = 43, NpcMonsterVNum = Hoarder, SkillVNum = 665 },
                new() { NpcMonsterSkillId = 44, NpcMonsterVNum = Hoarder, SkillVNum = 666 }
            });
            _service = World.UseReal<INpcCombatCatalog, NpcCombatCatalog>().Create<PetSkillService>();
        }

        [TestMethod]
        public void APetsBarHoldsItsCreaturesOwnSkillsInTheOrderOfItsData()
        {
            CollectionAssert.AreEqual(new short[] { 663, 664 }, Bar(TestMates.Pet(Owner, vnum: Otter)),
                "the skill an ordinary monster fights with has no place on the bar");
        }

        [TestMethod]
        public void APetWhoseCreatureHasNoSkillOfItsOwnHasAnEmptyBar()
        {
            Assert.AreEqual(0, Bar(TestMates.Pet(Owner, vnum: Wolf)).Length);
        }

        [TestMethod]
        public void TheBarHasThreeSlots()
        {
            CollectionAssert.AreEqual(new short[] { 663, 664, 665 }, Bar(TestMates.Pet(Owner, vnum: Hoarder)));
        }

        [TestMethod]
        public void APartnerHasNoPetSkillBar()
        {
            Assert.AreEqual(0, Bar(TestMates.Partner(Owner)).Length);
        }

        private short[] Bar(Mate mate) => _service.Skills(mate).Select(s => s.SkillVNum).ToArray();
    }
}
