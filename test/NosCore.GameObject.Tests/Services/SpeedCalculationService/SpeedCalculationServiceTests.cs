//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.SpeedService;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.SpeedCalculationService
{
    [TestClass]
    public class SpeedCalculationServiceTests
    {
        private TestWorld World;
        private GameObject.Services.SpeedCalculationService.SpeedCalculationService SpeedCalculationService;
        private Mock<ISpeedService> SpeedService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            SpeedService = World.Freeze<Mock<ISpeedService>>();
            SpeedCalculationService = World.Create<GameObject.Services.SpeedCalculationService.SpeedCalculationService>();
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.Swordsman)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Adventurer)]
        public async Task DefaultSpeedIsClassSpeed(int characterClassInt)
        {
            var characterClass = (CharacterClassType)characterClassInt;
            SpeedService.Setup(x => x.GetSpeed(characterClass)).Returns((byte)characterClassInt);

            var character = (await World.CreateSessionAsync()).Character;
            character.Class = characterClass;

            var speed = SpeedCalculationService.CalculateSpeed(character);
            Assert.AreEqual((byte)characterClassInt, speed);
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.Swordsman)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Adventurer)]
        public async Task VehicleSpeedOverridesDefaultSpeed(int characterClassInt)
        {
            var characterClass = (CharacterClassType)characterClassInt;
            SpeedService.Setup(x => x.GetSpeed(characterClass)).Returns((byte)characterClassInt);

            var character = (await World.CreateSessionAsync()).Character;
            character.Class = characterClass;
            // Both, because a mount sets both. Setting only VehicleSpeed described a state that
            // cannot happen, and the old null check happened to answer it correctly.
            character.IsVehicled = true;
            character.VehicleSpeed = 50;

            var speed = SpeedCalculationService.CalculateSpeed(character);
            Assert.AreEqual(50, speed);
        }

        // On foot VehicleSpeed is 0, not null - the component declares it as a plain byte. Reading
        // it whenever it "is not null" therefore answered 0 for everyone, and it went unnoticed
        // because nothing called this service.
        [TestMethod]
        public async Task OnFootTheVehicleSpeedIsIgnoredRatherThanReturnedAsZero()
        {
            SpeedService.Setup(x => x.GetSpeed(CharacterClassType.Archer)).Returns((byte)12);

            var character = (await World.CreateSessionAsync()).Character;
            character.Class = CharacterClassType.Archer;
            character.IsVehicled = false;
            character.VehicleSpeed = (byte)0;

            Assert.AreEqual(12, SpeedCalculationService.CalculateSpeed(character));
        }

        [TestMethod]
        public void DefaultMonsterSpeedIsNpcMonsterSpeed()
        {
            new Spec("Default monster speed is npc monster speed")
                .Then(MonsterSpeedShouldMatchNpcMonsterDto)
                .Execute();
        }

        private void MonsterSpeedShouldMatchNpcMonsterDto()
        {
            var charMock = new Mock<INonPlayableEntity>();
            charMock.SetupGet(x => x.NpcMonster).Returns(new NpcMonsterDto
            {
                Speed = 50
            });
            var speed = SpeedCalculationService.CalculateSpeed(charMock.Object);
            Assert.AreEqual(50, speed);
        }
    }
}
