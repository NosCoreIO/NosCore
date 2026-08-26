//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Globalization;

namespace NosCore.GameObject.Services.BattleService;

// A skill's cell pattern, rotated towards the target.
//
// 67 of the 68 skills with a CELL section declare AOE with radius zero, because the area IS
// the pattern. Reading the radius makes them hit a single target and nothing says so.
//
// Rotation is by the angle to the target, exact on the cardinals and rounded on the
// diagonals, where a one-cell row can come out as a staircase with a gap at its side.
public static class SkillCells
{
    // A malformed value yields null rather than throwing: the pattern decides who a skill hits.
    public static sbyte[]? Parse(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        var parts = pattern.Split(',');

        // Pairs, so an odd count is a broken row, not a pattern with a spare coordinate.
        if (parts.Length == 0 || parts.Length % 2 != 0)
        {
            return null;
        }

        var cells = new sbyte[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!sbyte.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out cells[i]))
            {
                return null;
            }
        }

        return cells;
    }

    // The absolute cells hit. Caster and target on one cell leaves no direction: kept facing north.
    public static HashSet<(short X, short Y)> Resolve(sbyte[] pattern, short casterX,
        short casterY, short targetX, short targetY)
    {
        var cells = new HashSet<(short, short)>(pattern.Length / 2);

        double dx = targetX - casterX;
        double dy = targetY - casterY;
        var len = Math.Sqrt((dx * dx) + (dy * dy));

        // At zero distance there is no direction: north, as authored.
        double ux = 0, uy = -1;
        if (len > 0.0001)
        {
            ux = dx / len;
            uy = dy / len;
        }

        // Authored basis is right = (1,0), forward = (0,-1); forward maps onto (ux, uy) and right
        // onto (-uy, ux).
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
