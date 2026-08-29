//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SkillCellsTests
    {
        private const string PiercingShot = "0,-8,0,-7,0,-6,0,-5,0,-4,0,-3,0,-2,0,-1";

        private static List<(sbyte dx, sbyte dy)> Pairs(sbyte[] flat)
        {
            var cells = new List<(sbyte, sbyte)>();
            for (var i = 0; i < flat.Length; i += 2)
            {
                cells.Add((flat[i], flat[i + 1]));
            }

            return cells;
        }

        [TestMethod]
        public void AimingNorthLeavesThePatternAsWritten()
        {
            var pattern = SkillCells.Parse(PiercingShot)!;
            var cells = SkillCells.Resolve(pattern, 50, 50, 50, 40);

            Assert.AreEqual(8, cells.Count);
            for (short dy = 1; dy <= 8; dy++)
            {
                Assert.IsTrue(cells.Contains((50, (short)(50 - dy))), $"missing (50,{50 - dy})");
            }
        }

        [TestMethod]
        public void AimingSouthTurnsThePatternAround()
        {
            var pattern = SkillCells.Parse(PiercingShot)!;
            var cells = SkillCells.Resolve(pattern, 50, 50, 50, 60);

            Assert.AreEqual(8, cells.Count);
            for (short dy = 1; dy <= 8; dy++)
            {
                Assert.IsTrue(cells.Contains((50, (short)(50 + dy))), $"missing (50,{50 + dy})");
            }
        }

        [TestMethod]
        public void AimingEastPutsTheLineOnTheXAxis()
        {
            var pattern = SkillCells.Parse(PiercingShot)!;
            var cells = SkillCells.Resolve(pattern, 50, 50, 60, 50);

            Assert.AreEqual(8, cells.Count);
            for (short dx = 1; dx <= 8; dx++)
            {
                Assert.IsTrue(cells.Contains(((short)(50 + dx), 50)), $"missing ({50 + dx},50)");
            }
        }

        // Diagonals round, so demand the quadrant and the reach rather than exact cells.
        [TestMethod]
        public void AimingDiagonallyPointsTheLineAtTheTarget()
        {
            var pattern = SkillCells.Parse(PiercingShot)!;
            var cells = SkillCells.Resolve(pattern, 50, 50, 60, 40);

            Assert.IsTrue(cells.All(c => c.X >= 50 && c.Y <= 50),
                "a cell landed outside the target's quadrant");

            var furthest = cells.Max(c => System.Math.Max(System.Math.Abs(c.X - 50),
                System.Math.Abs(c.Y - 50)));
            Assert.AreEqual(6, furthest,
                "eight diagonal steps reach six cells on each axis, not eight");
        }

        // Without the zero-distance guard the normalisation divides by zero and every cell
        // collapses onto the caster, silently.
        [TestMethod]
        public void CastingOnYourOwnCellKeepsTheWrittenOrientation()
        {
            var pattern = SkillCells.Parse(PiercingShot)!;
            var cells = SkillCells.Resolve(pattern, 50, 50, 50, 50);

            Assert.AreEqual(8, cells.Count);
            Assert.IsTrue(cells.Contains((50, 42)));
        }

        // Most skills have no drawing: 1890 of the 1958. The column is null for them.
        [TestMethod]
        public void ASkillWithoutADrawingHasNoPattern()
        {
            Assert.IsNull(SkillCells.Parse(null));
            Assert.IsNull(SkillCells.Parse(""));
            Assert.IsNull(SkillCells.Parse("   "));
        }

        // A broken row must not stop a fight: the pattern decides who a skill hits, and throwing
        // here would take down the cast instead of degrading it to a single target.
        [TestMethod]
        public void AMalformedColumnIsNoPatternRatherThanAnException()
        {
            Assert.IsNull(SkillCells.Parse("0,-1,0"), "an odd count is a broken row, not a cell");
            Assert.IsNull(SkillCells.Parse("0,-1,x,2"));
            Assert.IsNull(SkillCells.Parse("0,-1,,2"));
            Assert.IsNull(SkillCells.Parse("0,-1,200,2"), "200 does not fit an sbyte");
        }

        [TestMethod]
        public void AWellFormedColumnComesBackAsPairs()
        {
            var pattern = SkillCells.Parse(PiercingShot)!;
            Assert.AreEqual(16, pattern.Length);

            var cells = Pairs(pattern);
            Assert.AreEqual(8, cells.Count);
            Assert.IsTrue(cells.All(c => c.dx == 0), "the line is one cell wide");
            Assert.AreEqual(-8, cells.Min(c => c.dy));
            Assert.AreEqual(-1, cells.Max(c => c.dy));
        }
    }
}
