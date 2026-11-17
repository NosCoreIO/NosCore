//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Map;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MapInstanceGenerationService
{
    [TestClass]
    public class MapInstanceTypeTests
    {
        private TestWorld World;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
        }

        private MapType TypeOf(MapType mapType, MapInstanceType instanceType) =>
            World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2550,
                    NameI18NKey = "testMap",
                    Data = [2, 0, 2, 0, 0, 0, 0, 0],
                    MapType = mapType
                }, Guid.NewGuid(), false, instanceType).MapType;

        [TestMethod]
        public void AMapReportsItsOwnType() =>
            Assert.AreEqual(MapType.Miniland, TypeOf(MapType.Miniland, MapInstanceType.NormalInstance));

        [TestMethod]
        public void AMapRunAsATimeSpaceRoomIsATimeSpace() =>
            Assert.AreEqual(MapType.TimeSpace, TypeOf(MapType.Normal, MapInstanceType.TimeSpaceInstance));
    }
}
