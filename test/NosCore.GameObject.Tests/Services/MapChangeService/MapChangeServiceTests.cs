//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.MapChangeService
{
    [TestClass]
    public class MapChangeServiceTests
    {
        private static readonly ILogger<NosCore.GameObject.Services.MapChangeService.MapChangeService> Logger = new Mock<ILogger<NosCore.GameObject.Services.MapChangeService.MapChangeService>>().Object;
        private IMapChangeService Service = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;

            var minilandService = new Mock<IMinilandService>();
            minilandService.Setup(s => s.GetMinilandPortals(It.IsAny<long>()))
                .Returns(new System.Collections.Generic.List<Map.Portal>());

            Service = new GameObject.Services.MapChangeService.MapChangeService(
                new Mock<IExperienceService>().Object,
                new Mock<IJobExperienceService>().Object,
                new Mock<IHeroExperienceService>().Object,
                TestHelpers.Instance.MapInstanceAccessorService,
                TestHelpers.Instance.Clock,
                TestHelpers.Instance.LogLanguageLocalizer,
                minilandService.Object,
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.GameLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry,
                new Mock<Wolverine.IMessageBus>().Object);
        }

        [TestMethod]
        public async Task ChangingToNonExistentMapShouldNotChangeMapInstance()
        {
            await new Spec("Changing to non-existent map should not change map instance")
                .Given(CharacterIsOnMap)
                .WhenAsync(ChangingToNonExistentMap)
                .Then(MapInstanceShouldNotChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingMapWithNullCoordinatesShouldNotCrash()
        {
            await new Spec("Changing map with null coordinates should not crash")
                .WhenAsync(ChangingMapWithNullCoordinates)
                .Then(NoExceptionShouldBeThrown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ServiceCanBeConstructed()
        {
            await new Spec("Service can be constructed")
                .Then(ServiceShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MapAccessorReturnsValidMaps()
        {
            await new Spec("Map accessor returns valid maps")
                .Then(Map0ShouldExist)
                .And(Map1ShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingToBaseMapInstanceUpdatesMapId()
        {
            await new Spec("Changing to a BaseMap instance overwrites character.MapId with the new map id")
                .Given(CharacterIsOnMap)
                .And(CharacterMapIdIsPinnedTo_, (short)42)
                .WhenAsync(ChangingToBaseMap1)
                .Then(CharacterMapIdShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingToNonBaseInstanceKeepsMapIdUnchanged()
        {
            await new Spec("Changing to a non-base instance leaves character.MapId at its previous base-map value")
                .Given(CharacterIsOnMap)
                .And(CharacterMapIdIsPinnedTo_, (short)42)
                .WhenAsync(ChangingToMinilandInstance)
                .Then(CharacterMapIdShouldBe_, (short)42)
                .ExecuteAsync();
        }

        private void CharacterMapIdIsPinnedTo_(short mapId)
        {
            Session.Character.MapId = mapId;
        }

        private async Task ChangingToBaseMap1()
        {
            var map1 = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            await Service.ChangeMapInstanceAsync(Session, map1.MapInstanceId, 10, 10);
        }

        private async Task ChangingToMinilandInstance()
        {
            await Service.ChangeMapInstanceAsync(Session, TestHelpers.Instance.MinilandId, 5, 8);
        }

        private void CharacterMapIdShouldBe_(short expected)
        {
            Assert.AreEqual(expected, Session.Character.MapId);
        }

        private short OriginalMapId;
        private bool OperationCompleted;

        private void CharacterIsOnMap()
        {
            OriginalMapId = Session.Character.MapInstance.Map.MapId;
        }

        private async Task ChangingToNonExistentMap()
        {
            await Service.ChangeMapAsync(Session, 9999, 10, 20);
            OperationCompleted = true;
        }

        private async Task ChangingMapWithNullCoordinates()
        {
            await Service.ChangeMapAsync(Session, null, null, null);
            OperationCompleted = true;
        }

        private void MapInstanceShouldNotChange()
        {
            Assert.AreEqual(OriginalMapId, Session.Character.MapInstance.Map.MapId);
        }

        private void NoExceptionShouldBeThrown()
        {
            Assert.IsTrue(OperationCompleted);
        }

        private void ServiceShouldNotBeNull()
        {
            Assert.IsNotNull(Service);
        }

        private void Map0ShouldExist()
        {
            var map0 = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0);
            Assert.IsNotNull(map0);
        }

        private void Map1ShouldExist()
        {
            var map1 = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1);
            Assert.IsNotNull(map1);
        }
    }
}
