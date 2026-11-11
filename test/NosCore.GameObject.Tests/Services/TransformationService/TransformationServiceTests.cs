//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.Algorithm.SpeedService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.TransformationService
{
    [TestClass]
    public class TransformationServiceTests
    {
        private TestWorld World;
        private static readonly ILogger<NosCore.GameObject.Services.TransformationService.TransformationService> Logger = new Mock<ILogger<NosCore.GameObject.Services.TransformationService.TransformationService>>().Object;
        private ITransformationService Service;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(1);

            Service = new GameObject.Services.TransformationService.TransformationService(
                World.Clock,
                new Mock<IExperienceService>().Object,
                new Mock<IJobExperienceService>().Object,
                new Mock<IHeroExperienceService>().Object,
                Logger,
                World.LogLanguageLocalizer,
                World.WorldConfiguration,
                World.Create<GameObject.Services.SpeedCalculationService.SpeedCalculationService>(),
                new Mock<GameObject.Services.SkillService.ISkillService>().Object,
                new Mock<NosCore.GameObject.Services.BattleService.IBattleStatsProvider>().Object);
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
                .And(TheSpeedTheClientSeesIsTheVehicles)
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
                .And(TheSpeedTheClientSeesIsBackOnFoot)
                .ExecuteAsync();
        }

        // VehicleSpeed alone changes nothing the player can feel: `cond` carries Speed, and Speed
        // was written once at login from the class table. Asserting only VehicleSpeed is what let
        // a mount that adds no speed look correct.
        private void TheSpeedTheClientSeesIsTheVehicles()
        {
            Assert.AreEqual(20, Session.Character.Speed);
            Assert.AreEqual(20, Session.Character.GenerateCond().Speed);
        }

        private void TheSpeedTheClientSeesIsBackOnFoot()
        {
            Assert.AreEqual(new SpeedService().GetSpeed(Session.Character.Class),
                Session.Character.Speed);
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
            await Service.RemoveSpAsync(Session);
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
            await Service.ChangeVehicleAsync(Session, vehicleItem);
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
            await Service.ChangeVehicleAsync(Session, vehicleItem);
        }

        private async Task RemovingVehicle()
        {
            await Service.RemoveVehicleAsync(Session);
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
