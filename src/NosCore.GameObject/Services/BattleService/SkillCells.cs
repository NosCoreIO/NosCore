//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Globalization;

namespace NosCore.GameObject.Services.BattleService;

public static class SkillCells
{
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

    public static HashSet<(short X, short Y)> Resolve(sbyte[] pattern, short casterX,
        short casterY, short targetX, short targetY)
    {
        var cells = new HashSet<(short, short)>(pattern.Length / 2);

        double dx = targetX - casterX;
        double dy = targetY - casterY;
        var len = Math.Sqrt((dx * dx) + (dy * dy));

        double ux = 0, uy = -1;
        if (len > 0.0001)
        {
            ux = dx / len;
            uy = dy / len;
        }

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
