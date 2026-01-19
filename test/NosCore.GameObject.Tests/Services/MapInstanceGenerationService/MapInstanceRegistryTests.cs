//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using SpecLight;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.MapInstanceGenerationService
{
    [TestClass]
    public class MapInstanceRegistryTests
    {
        private IMapInstanceRegistry Registry = null!;

        [TestInitialize]
        public void Setup()
        {
            Registry = new MapInstanceRegistry();
        }

        [TestMethod]
        public async Task GetByIdShouldReturnNullForUnknownId()
        {
            await new Spec("Get by ID should return null for unknown ID")
                .When(GettingUnknownMapInstance)
                .Then(ResultShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetBaseMapByIdShouldReturnNullForUnknownMapId()
        {
            await new Spec("Get base map by ID should return null for unknown map ID")
                .When(GettingUnknownBaseMap)
                .Then(ResultShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAllShouldReturnEmptyForEmptyRegistry()
        {
            await new Spec("Get all should return empty for empty registry")
                .When(GettingAllMapInstances)
                .Then(CountShouldBeZero)
                .ExecuteAsync();
        }

        private MapInstance? ResultMapInstance;
        private int MapInstanceCount;

        private void GettingUnknownMapInstance()
        {
            ResultMapInstance = Registry.GetById(Guid.NewGuid());
        }

        private void GettingUnknownBaseMap()
        {
            ResultMapInstance = Registry.GetBaseMapById(9999);
        }

        private void GettingAllMapInstances()
        {
            MapInstanceCount = Registry.GetAll().Count();
        }

        private void ResultShouldBeNull()
        {
            Assert.IsNull(ResultMapInstance);
        }

        private void CountShouldBeZero()
        {
            Assert.AreEqual(0, MapInstanceCount);
        }
    }
}
