//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.BattleService;

// A skill's cell pattern, rotated towards the target.
//
// Sixty-eight skills in Skill.dat carry a CELL section: an explicit list of the cells hit,
// relative to the caster and drawn facing north. Of those, 67 are declared as area (HitType 3)
// with an area radius of ZERO - because the area IS the pattern, not the radius. Reading only the
// radius makes those skills hit a single target: no exception, no log, just less damage than
// there should be. 244 "Piercing Shot" is the row of eight cells in front; 367 "Fire Breath" is a
// rectangle of thirty.
//
// The pattern is authored facing north and cast towards the target, so it is rotated by the angle
// between the two rather than snapped to eight directions: on the four cardinals the result is
// exact (sine and cosine are 0 or +-1), on the diagonals it is the nearest approximation.
//
// What stays approximate, and is worth stating: rotating by an arbitrary angle and rounding can
// turn a row one cell wide into a staircase with the odd gap at its side, and somebody half a cell
// off the line can slip through it. On the cardinals - the overwhelming majority of casts, since
// the caster lines up with the target - it does not arise.
public static class SkillCells
{
    // A skill's pattern, or null if it has none.
    public static sbyte[]? Pattern(short skillVnum)
    {
        return SkillCellTable.Patterns.TryGetValue(skillVnum, out var cells) ? cells : null;
    }

    public static bool Has(short skillVnum) => SkillCellTable.Patterns.ContainsKey(skillVnum);

    // The absolute cells hit, with the pattern rotated from the caster towards the target.
    //
    // Caster and target on the same cell leaves no direction to rotate from, so the drawing is
    // kept as authored, facing north. That is the limiting case of hitting yourself.
    public static HashSet<(short X, short Y)> Resolve(sbyte[] pattern, short casterX,
        short casterY, short targetX, short targetY)
    {
        var cells = new HashSet<(short, short)>(pattern.Length / 2);

        double dx = targetX - casterX;
        double dy = targetY - casterY;
        var len = Math.Sqrt((dx * dx) + (dy * dy));

        // The direction the pattern points. At zero distance there is none: north, as authored.
        double ux = 0, uy = -1;
        if (len > 0.0001)
        {
            ux = dx / len;
            uy = dy / len;
        }

        // The pattern is written in a "right = (1,0), forward = (0,-1)" basis. Bringing it into the
        // basis of the cast means sending "forward" onto (ux, uy) and "right" onto the perpendicular
        // that preserves the sense of rotation, that is (-uy, ux).
        foreach (var (cx, cy) in Pairs(pattern))
        {
            double right = cx;
            double forward = -cy;

            var x = (right * -uy) + (forward * ux);
            var y = (right * ux) + (forward * uy);

            cells.Add(((short)(casterX + Math.Round(x, MidpointRounding.AwayFromZero)),
                (short)(casterY + Math.Round(y, MidpointRounding.AwayFromZero))));
        }

        return cells;
    }

    private static IEnumerable<(sbyte X, sbyte Y)> Pairs(sbyte[] pattern)
    {
        for (var i = 0; i + 1 < pattern.Length; i += 2)
        {
            yield return (pattern[i], pattern[i + 1]);
        }
    }
}
