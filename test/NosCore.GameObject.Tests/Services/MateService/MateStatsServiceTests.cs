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
        [DataRow((short)2682, (byte)2, 12, 0, 500, 39825, 2225)]
        [DataRow((short)535, (byte)3, 0, 0, 0, 6640, 3270)]
        public void ALevel99MateMatchesItsPublishedStats(short vNum, byte race, int hpFactor, int mpFactor, int hpBonus,
            int maxHp, int maxMp)
        {
            var monster = new NpcMonsterDto
            {
                NpcMonsterVNum = vNum, Race = race, PetInfoVal1 = hpFactor, PetInfoVal3 = mpFactor, HpBonus = hpBonus
            };
            var pet = Build(MateType.Pet, 99, monster);

            _service.ApplyLevel(pet);

            Assert.AreEqual(maxHp, pet.MaxHp);
            Assert.AreEqual(maxMp, pet.MaxMp);
        }

        [TestMethod]
        public void APetsAttackAndDefenceFollowItsLevelAndGrowth()
        {
            var monster = new NpcMonsterDto
            {
                NpcMonsterVNum = 683, Race = 2, Level = 1, AttackClass = 0, PetInfoVal1 = 12, PetInfoVal2 = 5, PetInfoVal3 = 2,
                DamageMinimumBonus = 55, DamageMaximumBonus = 110, ConcentrateBonus = 25,
                CloseDefenceBonus = 5, DistanceDefenceBonus = 2, MagicDefenceBonus = 2, DodgeBonus = 6,
                DamageMinimum = 90, CloseDefence = 22,
            };
            var pet = Build(MateType.Pet, 40, monster);

            _service.ApplyLevel(pet);

            Assert.AreEqual(314, pet.DamageMinimum);
            Assert.AreEqual(441, pet.DamageMaximum);
            Assert.AreEqual(269, pet.Concentrate);
            Assert.AreEqual(155, pet.CloseDefence);
            Assert.AreEqual(142, pet.DistanceDefence);
            Assert.AreEqual(117, pet.MagicDefence);
            Assert.AreEqual(254, pet.Dodge);
        }

        [TestMethod]
        public void APartnerKeepsItsMonstersAttackAndDefence()
        {
            var monster = new NpcMonsterDto
            {
                Race = 3, PetInfoVal1 = 8, PetInfoVal2 = 12, PetInfoVal3 = 6, DamageMinimum = 70, DamageMaximum = 90,
                Concentrate = 60, CloseDefence = 40, DistanceDefence = 41, MagicDefence = 42, DefenceDodge = 50,
            };
            var partner = Build(MateType.Partner, 60, monster);

            _service.ApplyLevel(partner);

            Assert.AreEqual(70, partner.DamageMinimum);
            Assert.AreEqual(90, partner.DamageMaximum);
            Assert.AreEqual(60, partner.Concentrate);
            Assert.AreEqual(40, partner.CloseDefence);
            Assert.AreEqual(41, partner.DistanceDefence);
            Assert.AreEqual(42, partner.MagicDefence);
            Assert.AreEqual(50, partner.Dodge);
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
