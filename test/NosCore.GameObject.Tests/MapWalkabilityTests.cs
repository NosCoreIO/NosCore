//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.PathFinder.Heuristic;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class MapWalkabilityTests
    {
        private static GameObject.Map.Map OpenMap(byte width, byte height)
        {
            var data = new byte[4 + width * height];
            data[0] = width;
            data[2] = height;
            return new GameObject.Map.Map { MapId = 1, Data = data };
        }

        [TestMethod]
        public void TheCellsOnTheMapAreWalkable()
        {
            var map = OpenMap(3, 2);

            Assert.IsTrue(map.IsWalkable(0, 0));
            Assert.IsTrue(map.IsWalkable(2, 1));
        }

        [TestMethod]
        public void TheCellsJustPastTheMapEdgeAreNot()
        {
            var map = OpenMap(3, 2);

            Assert.IsFalse(map.IsWalkable(3, 0), "x == width would read the next row's first cell");
            Assert.IsFalse(map.IsWalkable(3, 1), "x == width on the last row would read past the data");
            Assert.IsFalse(map.IsWalkable(0, 2), "y == height would read past the data");
            Assert.IsFalse(map.IsWalkable(-1, 0));
            Assert.IsFalse(map.IsWalkable(0, -1));
        }

        [TestMethod]
        public void ThePathfinderStaysOnAMapWhoseEdgeIsOpen()
        {
            var map = OpenMap(8, 8);
            var pathfinder = new GameObject.Services.PathfindingService.PathfindingService(new OctileDistanceHeuristic()).ForMap(map);

            var path = pathfinder.FindPath((1, 1), (6, 7)).ToList();

            Assert.IsTrue(path.Count > 1);
            Assert.AreEqual(((short)6, (short)7), path.Last());
        }
    }
}
