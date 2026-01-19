//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.SpeedService;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Shared.Enumerations;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.SpeedCalculationService
{
    [TestClass]
    public class SpeedCalculationServiceTests
    {
        private readonly GameObject.Services.SpeedCalculationService.SpeedCalculationService SpeedCalculationService;
        private readonly Mock<ISpeedService> SpeedService;

        public SpeedCalculationServiceTests()
        {
            SpeedService = new Mock<ISpeedService>();
            SpeedCalculationService = new GameObject.Services.SpeedCalculationService.SpeedCalculationService(SpeedService.Object);
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.Swordsman)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Adventurer)]
        public void DefaultSpeedIsClassSpeed(int characterClassInt)
        {
            var characterClass = (CharacterClassType)characterClassInt;
            SpeedService.Setup(x => x.GetSpeed(characterClass)).Returns((byte)characterClassInt);

            var charMock = new Mock<ICharacterEntity>();
            charMock.SetupGet(x => x.Class).Returns(characterClass);

            var speed = SpeedCalculationService.CalculateSpeed(charMock.Object);
            Assert.AreEqual((byte)characterClassInt, speed);
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.Swordsman)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Adventurer)]
        public void VehicleSpeedOverridesDefaultSpeed(int characterClassInt)
        {
            var characterClass = (CharacterClassType)characterClassInt;
            SpeedService.Setup(x => x.GetSpeed(characterClass)).Returns((byte)characterClassInt);

            var charMock = new Mock<ICharacterEntity>();
            charMock.SetupGet(x => x.Class).Returns(characterClass);
            charMock.SetupGet(x => x.VehicleSpeed).Returns(50);

            var speed = SpeedCalculationService.CalculateSpeed(charMock.Object);
            Assert.AreEqual(50, speed);
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
