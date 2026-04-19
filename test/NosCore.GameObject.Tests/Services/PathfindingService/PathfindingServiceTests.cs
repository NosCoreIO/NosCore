//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.PathfindingService;
using NosCore.PathFinder.Heuristic;

namespace NosCore.GameObject.Tests.Services.PathfindingService
{
    [TestClass]
    public class PathfindingServiceTests
    {
        [TestMethod]
        public void ForMapReturnsSameInstanceForSameMap()
        {
            var svc = new GameObject.Services.PathfindingService.PathfindingService(new OctileDistanceHeuristic());
            var map = TestMap(id: 1);

            var first = svc.ForMap(map);
            var second = svc.ForMap(map);

            Assert.AreSame(first, second, "pathfinder should be cached per map id");
        }

        [TestMethod]
        public void DifferentMapIdsGetDifferentPathfinders()
        {
            var svc = new GameObject.Services.PathfindingService.PathfindingService(new OctileDistanceHeuristic());
            var a = svc.ForMap(TestMap(id: 1));
            var b = svc.ForMap(TestMap(id: 2));
            Assert.AreNotSame(a, b);
        }

        // Tiny 4x4 walkable map. Map reads width/height from the first 4 bytes of Data,
        // then treats each subsequent byte as a cell; 0 is walkable by Map.IsWalkable.
        private static global::NosCore.GameObject.Map.Map TestMap(short id)
        {
            var data = new byte[4 + 4 * 4];
            data[0] = 4; // width low byte
            data[2] = 4; // height low byte
            return new global::NosCore.GameObject.Map.Map
            {
                MapId = id,
                Data = data,
                Name = new NosCore.Data.Dto.I18NString(),
            };
        }
    }
}
