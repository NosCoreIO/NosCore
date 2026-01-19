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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.TransformationService
{
    [TestClass]
    public class TransformationServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private ITransformationService Service = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;

            Service = new GameObject.Services.TransformationService.TransformationService(
                TestHelpers.Instance.Clock,
                new Mock<IExperienceService>().Object,
                new Mock<IJobExperienceService>().Object,
                new Mock<IHeroExperienceService>().Object,
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task RemovingSpShouldResetMorphValues()
        {
            await new Spec("Removing SP should reset morph values")
                .Given(CharacterHasSpEquipped)
                .WhenAsync(RemovingSp)
                .Then(MorphShouldBeReset)
                .And(SpCooldownShouldBeSet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingVehicleShouldSetVehicledState()
        {
            await new Spec("Changing vehicle should set vehicled state")
                .WhenAsync(ChangingToVehicle)
                .Then(CharacterShouldBeVehicled)
                .And(VehicleSpeedShouldBeSet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingVehicleShouldResetState()
        {
            await new Spec("Removing vehicle should reset state")
                .GivenAsync(CharacterIsVehicled)
                .WhenAsync(RemovingVehicle)
                .Then(CharacterShouldNotBeVehicled)
                .And(VehicleSpeedShouldBeZero)
                .ExecuteAsync();
        }

        private void CharacterHasSpEquipped()
        {
            Session.Character.UseSp = true;
            Session.Character.Morph = 100;
            Session.Character.MorphUpgrade = 10;
            Session.Character.MorphDesign = 5;
        }

        private async Task RemovingSp()
        {
            await Service.RemoveSpAsync(Session.Character);
        }

        private async Task ChangingToVehicle()
        {
            var vehicleItem = new GameObject.Services.ItemGenerationService.Item.Item
            {
                VNum = 5196,
                Speed = 20,
                Morph = 2432,
                SecondMorph = 0
            };
            await Service.ChangeVehicleAsync(Session.Character, vehicleItem);
        }

        private async Task CharacterIsVehicled()
        {
            var vehicleItem = new GameObject.Services.ItemGenerationService.Item.Item
            {
                VNum = 5196,
                Speed = 20,
                Morph = 2432,
                SecondMorph = 0
            };
            await Service.ChangeVehicleAsync(Session.Character, vehicleItem);
        }

        private async Task RemovingVehicle()
        {
            await Service.RemoveVehicleAsync(Session.Character);
        }

        private void MorphShouldBeReset()
        {
            Assert.AreEqual(0, Session.Character.Morph);
            Assert.AreEqual(0, Session.Character.MorphUpgrade);
            Assert.AreEqual(0, Session.Character.MorphDesign);
        }

        private void SpCooldownShouldBeSet()
        {
            Assert.AreEqual(30, Session.Character.SpCooldown);
            Assert.IsFalse(Session.Character.UseSp);
        }

        private void CharacterShouldBeVehicled()
        {
            Assert.IsTrue(Session.Character.IsVehicled);
        }

        private void VehicleSpeedShouldBeSet()
        {
            Assert.AreEqual((byte)20, Session.Character.VehicleSpeed);
        }

        private void CharacterShouldNotBeVehicled()
        {
            Assert.IsFalse(Session.Character.IsVehicled);
        }

        private void VehicleSpeedShouldBeZero()
        {
            Assert.AreEqual((byte)0, Session.Character.VehicleSpeed);
        }
    }
}
