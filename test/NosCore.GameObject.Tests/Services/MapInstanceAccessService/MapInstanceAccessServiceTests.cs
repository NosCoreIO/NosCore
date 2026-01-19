//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.MapInstanceAccessService
{
    [TestClass]
    public class MapInstanceAccessServiceTests
    {
        private IMapInstanceAccessorService Service = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Service = TestHelpers.Instance.MapInstanceAccessorService;
        }

        [TestMethod]
        public async Task GetBaseMapByIdShouldReturnMapForValidId()
        {
            await new Spec("Get base map by ID should return map for valid ID")
                .When(GettingMapById0)
                .Then(MapShouldNotBeNull)
                .And(MapIdShouldBe0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetBaseMapByIdShouldReturnNullForInvalidId()
        {
            await new Spec("Get base map by ID should return null for invalid ID")
                .When(GettingMapByInvalidId)
                .Then(MapShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetMapInstanceShouldReturnNullForInvalidGuid()
        {
            await new Spec("Get map instance should return null for invalid GUID")
                .When(GettingMapInstanceByInvalidGuid)
                .Then(MapInstanceShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetMapInstanceShouldReturnMapForValidGuid()
        {
            await new Spec("Get map instance should return map for valid GUID")
                .Given(MapInstanceIdIsKnown)
                .When(GettingMapInstanceByValidGuid)
                .Then(MapInstanceShouldNotBeNull)
                .ExecuteAsync();
        }

        private GameObject.Services.MapInstanceGenerationService.MapInstance? ResultMap;
        private Guid KnownMapInstanceId;

        private void GettingMapById0()
        {
            ResultMap = Service.GetBaseMapById(0);
        }

        private void GettingMapByInvalidId()
        {
            ResultMap = Service.GetBaseMapById(9999);
        }

        private void GettingMapInstanceByInvalidGuid()
        {
            ResultMap = Service.GetMapInstance(Guid.NewGuid());
        }

        private void MapInstanceIdIsKnown()
        {
            var map = Service.GetBaseMapById(0);
            if (map != null)
            {
                KnownMapInstanceId = map.MapInstanceId;
            }
        }

        private void GettingMapInstanceByValidGuid()
        {
            ResultMap = Service.GetMapInstance(KnownMapInstanceId);
        }

        private void MapShouldNotBeNull()
        {
            Assert.IsNotNull(ResultMap);
        }

        private void MapShouldBeNull()
        {
            Assert.IsNull(ResultMap);
        }

        private void MapIdShouldBe0()
        {
            Assert.AreEqual((short)0, ResultMap?.Map.MapId);
        }

        private void MapInstanceShouldBeNull()
        {
            Assert.IsNull(ResultMap);
        }

        private void MapInstanceShouldNotBeNull()
        {
            Assert.IsNotNull(ResultMap);
        }
    }
}
