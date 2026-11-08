//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.MovementService;

namespace NosCore.GameObject.Tests.Services.MovementService
{
    [TestClass]
    public class WalkPathTests
    {
        private static bool Path(string[] grid, short fromX, short fromY, short toX, short toY) =>
            WalkPath.Exists(fromX, fromY, toX, toY, (x, y) =>
                y >= 0 && y < grid.Length && x >= 0 && x < grid[y].Length && grid[y][x] == '.');

        private static readonly string[] VerticalWall =
        {
            "....#....",
            "....#....",
            "....#....",
            "....#....",
            "....#....",
            "....#....",
            "....#....",
            "....#....",
        };

        private static readonly string[] DiagonalWall =
        {
            "...#.",
            "..#..",
            ".#...",
            "#....",
        };

        private static readonly string[] Pillar =
        {
            ".....",
            ".###.",
            ".###.",
            ".###.",
            ".....",
        };

        [TestMethod]
        public void AStraightLineInTheOpenIsAPath()
        {
            var open = Enumerable.Repeat(".........", 5).ToArray();
            Assert.IsTrue(Path(open, 0, 2, 8, 2));
        }

        [TestMethod]
        public void AOneCellWallWithNoGapIsNotCrossed()
        {
            Assert.IsFalse(Path(VerticalWall, 3, 3, 5, 3));
        }

        [TestMethod]
        public void ADiagonalWallIsNotCrossedBetweenItsCorners()
        {
            Assert.IsFalse(Path(DiagonalWall, 1, 1, 2, 2));
        }

        [TestMethod]
        public void AnObstacleIsWalkedRound()
        {
            Assert.IsTrue(Path(Pillar, 0, 2, 4, 2));
        }

        [TestMethod]
        public void StandingStillIsAPath()
        {
            Assert.IsTrue(Path(Pillar, 0, 0, 0, 0));
        }
    }
}
