//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SkillCellsTests
    {
        // 244 "Piercing Shot", archer: the row of eight cells in front of the caster.
        //     CELL 9 10 | 0 -8 1 | 0 -7 1 | ... | 0 -1 1 | 0 0 0
        private const short PiercingShot = 244;

        // 367 "Fire Breath": thirty cells, so the list does not stop at twenty.
        private const short Fireblast = 367;

        [TestMethod]
        public void PiercingShotIsAStraightLineOfEight()
        {
            var pattern = SkillCells.Pattern(PiercingShot);
            Assert.IsNotNull(pattern);
            Assert.AreEqual(8 * 2, pattern!.Length);

            for (var i = 0; i < 8; i++)
            {
                Assert.AreEqual(0, pattern[i * 2]);
                Assert.AreEqual(-8 + i, pattern[(i * 2) + 1]);
            }
        }

        [TestMethod]
        public void FireblastKeepsAllThirtyCells()
        {
            var pattern = SkillCells.Pattern(Fireblast);
            Assert.IsNotNull(pattern);
            Assert.AreEqual(30 * 2, pattern!.Length);
        }

        [TestMethod]
        public void AimingNorthLeavesThePatternAsWritten()
        {
            var pattern = SkillCells.Pattern(PiercingShot)!;
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
            var pattern = SkillCells.Pattern(PiercingShot)!;
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
            var pattern = SkillCells.Pattern(PiercingShot)!;
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
            var pattern = SkillCells.Pattern(PiercingShot)!;
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
            var pattern = SkillCells.Pattern(PiercingShot)!;
            var cells = SkillCells.Resolve(pattern, 50, 50, 50, 50);

            Assert.AreEqual(8, cells.Count);
            Assert.IsTrue(cells.Contains((50, 42)));
        }

        [TestMethod]
        public void ASkillWithoutADrawingHasNoPattern()
        {
            // 1 is the swordsman's basic attack: no useful CELL section.
            Assert.IsNull(SkillCells.Pattern(1));
            Assert.IsFalse(SkillCells.Has(1));
        }
    }
}
