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
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.SpeedCalculationService
{
    [TestClass]
    public class SpeedCalculationServiceTests
    {
        private GameObject.Services.SpeedCalculationService.SpeedCalculationService? _speedCalculationService;
        private Mock<ISpeedService>? _speedService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _speedService = new Mock<ISpeedService>();
            _speedCalculationService = new GameObject.Services.SpeedCalculationService.SpeedCalculationService(_speedService.Object);
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.Swordsman)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Adventurer)]
        public async Task DefaultSpeedIsClassSpeedAsync(int characterClassInt)
        {
            var characterClass = (CharacterClassType)characterClassInt;
            _speedService!.Setup(x => x.GetSpeed(characterClass)).Returns((byte)characterClassInt);

            var session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            session.Player.SetClass(characterClass);

            var speed = _speedCalculationService!.CalculateSpeed(session.Player);
            Assert.AreEqual((byte)characterClassInt, speed);
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.Swordsman)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Adventurer)]
        public async Task VehicleSpeedOverrideDefaultSpeedAsync(int characterClassInt)
        {
            var characterClass = (CharacterClassType)characterClassInt;
            _speedService!.Setup(x => x.GetSpeed(characterClass)).Returns((byte)characterClassInt);

            var session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            session.Player.SetClass(characterClass);
            session.Player.SetVehicleSpeed(50);

            var speed = _speedCalculationService!.CalculateSpeed(session.Player);
            Assert.AreEqual((byte)50, speed);
        }

    }
}