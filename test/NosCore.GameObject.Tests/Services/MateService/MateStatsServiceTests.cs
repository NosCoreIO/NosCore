//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.MateService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MateStatsServiceTests
    {
        private TestWorld World;
        private MateStatsService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _service = World.Create<MateStatsService>();
        }

        private static Mate Build(MateType type, byte level, NpcMonsterDto monster) =>
            new() { MateType = type, Level = level, NpcMonster = monster };

        [TestMethod]
        public void APetsMaximaGrowWithItsLevelAndIncludeTheFlatBonus()
        {
            var monster = new NpcMonsterDto { Race = 2, PetInfoVal1 = 12, PetInfoVal3 = 0, HpBonus = 500 };
            var pet = Build(MateType.Pet, 48, monster);

            _service.ApplyLevel(pet);

            Assert.AreEqual(10638, pet.MaxHp);
            Assert.AreEqual(708, pet.MaxMp);
        }

        [TestMethod]
        public void APartnersMaximaFollowItsFactors()
        {
            var monster = new NpcMonsterDto { Race = 3, PetInfoVal1 = 8, PetInfoVal3 = 6 };
            var partner = Build(MateType.Partner, 28, monster);

            _service.ApplyLevel(partner);

            Assert.AreEqual(2165, partner.MaxHp);
            Assert.AreEqual(877, partner.MaxMp);
        }

        [TestMethod]
        public void AHigherLevelMeansHigherMaxima()
        {
            var monster = new NpcMonsterDto { Race = 3, PetInfoVal1 = 8, PetInfoVal3 = 6 };
            var low = Build(MateType.Partner, 10, monster);
            var high = Build(MateType.Partner, 30, monster);

            _service.ApplyLevel(low);
            _service.ApplyLevel(high);

            Assert.IsTrue(high.MaxHp > low.MaxHp);
            Assert.IsTrue(high.MaxMp > low.MaxMp);
            Assert.IsTrue(high.XpLoad > low.XpLoad);
        }
    }
}
