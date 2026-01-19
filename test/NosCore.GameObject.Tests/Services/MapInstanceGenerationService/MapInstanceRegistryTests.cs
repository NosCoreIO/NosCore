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

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using SpecLight;

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
