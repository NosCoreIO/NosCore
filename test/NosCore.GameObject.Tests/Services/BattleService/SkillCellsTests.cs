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
        // 244 "Piercing Shot", archer: the row of eight cells in front of the caster.
        //     CELL 9 10 | 0 -8 1 | 0 -7 1 | ... | 0 -1 1 | 0 0 0
        private const short PiercingShot = 244;

        // 367 "Fire Breath": a solid 3x12 rectangle, so thirty-six cells. Thirty of them are
        //     in CELL, which holds no more; the last six continue in the unused tail of COST.
        private const short Fireblast = 367;

        // 1857 "Armour Piercing Round": the longest at forty, and the only one that fills the
        //     tail as well. 1175 "Reaper's Scythe" fills CELL with a closed figure and has an
        //     empty tail - proof that a full CELL does not by itself mean there is more.
        private const short ArmourPiercingRound = 1857;
        private const short ReapersScythe = 1175;

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
        public void FireblastIsAWholeRectangleAndNotAClippedOne()
        {
            var pattern = SkillCells.Pattern(Fireblast);
            Assert.IsNotNull(pattern);
            Assert.AreEqual(36 * 2, pattern!.Length);

            var cells = Pairs(pattern);
            Assert.AreEqual(3, cells.Select(c => c.dx).Distinct().Count());
            Assert.AreEqual(12, cells.Select(c => c.dy).Distinct().Count());
            Assert.AreEqual(36, cells.Distinct().Count(), "a solid rectangle, with no hole");
        }

        [TestMethod]
        public void ArmourPiercingRoundKeepsItsTip()
        {
            var cells = Pairs(SkillCells.Pattern(ArmourPiercingRound)!);
            Assert.AreEqual(40, cells.Count);

            var tip = cells.Where(c => c.dy <= -13).ToList();
            Assert.AreEqual(4, tip.Count);
            Assert.IsTrue(tip.All(c => c.dx == 0), "the tip is one cell wide");
        }

        // A "continues" flag on the last triplet CELL can hold does not mean there is more, it
        // means you cannot tell from there. This is the skill that reading the tail blindly
        // would grow cells it does not have.
        [TestMethod]
        public void ReapersScytheStopsAtThirty()
        {
            Assert.AreEqual(30 * 2, SkillCells.Pattern(ReapersScythe)!.Length);
        }

        // If the tail were something other than a continuation, appending it would collide with
        // what CELL already gave. Across every pattern it never does.
        [TestMethod]
        public void NoPatternListsTheSameCellTwice()
        {
            for (short vnum = 1; vnum < 2100; vnum++)
            {
                if (!SkillCells.Has(vnum))
                {
                    continue;
                }

                var cells = Pairs(SkillCells.Pattern(vnum)!);
                Assert.AreEqual(cells.Count, cells.Distinct().Count(), $"skill {vnum}");
                Assert.IsTrue(cells.Count <= 40, $"skill {vnum} exceeds CELL plus the tail");
            }
        }

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
