//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // BCard type 40. Three of its subtypes are used by the game's skills, and all three move
    // somebody:
    //
    //   11: Push your opponent back %s fields.        49 declarations
    //   21: Draws enemies to %s fields away from you.  64
    //   31: Charge at enemies within %s fields.        21
    //
    // The risk is not that they fail to work. It is that they work too well - that somebody ends
    // up inside a wall or off the edge of the map, where they cannot get out and where the client
    // and the server stop agreeing about where they are.
    //
    // These drive the geometry on its own. The part around it can only move a real ECS bundle, so
    // a test double would never budge and would prove nothing.
    [TestClass]
    public class ForcedMovementTests
    {
        // Eight wide, six high, clear except a vertical wall at x = 5. Off the grid is wall too,
        // the way the real map reads it.
        private static bool Walkable(short x, short y) =>
            x >= 0 && x < 8 && y >= 0 && y < 6 && x != 5;

        private static (short X, short Y) Push(short fromX, short fromY, short awayFromX,
            short awayFromY, int fields) =>
            ForcedMovement.Destination(fromX, fromY, awayFromX, awayFromY, fields,
                Math.Sign(fromX - awayFromX), Math.Sign(fromY - awayFromY), 0, Walkable);

        private static (short X, short Y) Draw(short fromX, short fromY, short towardsX,
            short towardsY, int stopAt) =>
            ForcedMovement.Destination(fromX, fromY, towardsX, towardsY, int.MaxValue,
                Math.Sign(towardsX - fromX), Math.Sign(towardsY - fromY), stopAt, Walkable);

        [TestMethod]
        public void APushSlidesTheTargetTheOtherWayFromWhoStruck()
        {
            // Attacker at (1,2), target at (2,2), pushed one field: away is +x.
            Assert.AreEqual((3, 2), Push(2, 2, 1, 2, 1));
        }

        [TestMethod]
        public void APushGoesTheWholeDistanceWhenNothingIsInTheWay()
        {
            Assert.AreEqual((4, 2), Push(1, 2, 0, 2, 3));
        }

        // The one that matters. A push of ten fields into a wall at x=5 must stop at 4, not walk
        // through it and not stop short of it.
        [TestMethod]
        public void APushStopsInFrontOfTheWallInsteadOfCrossingIt()
        {
            Assert.AreEqual((4, 2), Push(1, 2, 0, 2, 10));
        }

        [TestMethod]
        public void APushStopsAtTheEdgeOfTheMap()
        {
            // Pushed towards -x from (2,3): the edge is at 0, and -1 is not walkable.
            Assert.AreEqual((0, 3), Push(2, 3, 3, 3, 10));
        }

        [TestMethod]
        public void APushRunsDiagonallyWhenBothCoordinatesDiffer()
        {
            Assert.AreEqual((3, 4), Push(2, 3, 1, 2, 1));
        }

        // "Draws enemies to %s fields away from you": the target closes in until it is that far,
        // and no nearer. Twenty-five of the sixty-four declarations say zero.
        [TestMethod]
        public void ADrawBringsTheTargetToTheDistanceTheFileNames()
        {
            Assert.AreEqual((2, 2), Draw(4, 2, 0, 2, 2));
        }

        [TestMethod]
        public void ADrawOfZeroBringsTheTargetRightUp()
        {
            Assert.AreEqual((1, 2), Draw(4, 2, 1, 2, 0));
        }

        // A pull is stopped by a wall like everything else. The target at (6,2) cannot be drawn
        // past the wall at x=5, so it does not move at all.
        [TestMethod]
        public void ADrawCannotPullSomebodyThroughAWall()
        {
            Assert.AreEqual((6, 2), Draw(6, 2, 1, 2, 1));
        }

        [TestMethod]
        public void ADrawWithNothingBetweenGoesAllTheWay()
        {
            Assert.AreEqual((2, 2), Draw(4, 2, 1, 2, 1));
        }

        // Standing on top of the other one already: no step, and no packet either.
        [TestMethod]
        public void NobodyMovesWhenThereIsNowhereToGo()
        {
            Assert.AreEqual((4, 2), Draw(4, 2, 4, 2, 0));
        }

    }
}
