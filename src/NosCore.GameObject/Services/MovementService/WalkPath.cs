//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.MovementService;

public static class WalkPath
{
    // The client walks around corners inside one packet, so the cells in between do not have
    // to lie on a straight line - only within a few steps of one.
    private const int Detour = 4;

    public static int Steps(short fromX, short fromY, short toX, short toY) =>
        Math.Max(Math.Abs(toX - fromX), Math.Abs(toY - fromY));

    public static bool Exists(short fromX, short fromY, short toX, short toY, Func<short, short, bool> isWalkable)
    {
        var maxSteps = Steps(fromX, fromY, toX, toY) + Detour;
        var visited = new HashSet<(short, short)> { (fromX, fromY) };
        var frontier = new List<(short X, short Y)> { (fromX, fromY) };

        for (var step = 0; step < maxSteps && frontier.Count > 0; step++)
        {
            var next = new List<(short X, short Y)>();
            foreach (var (x, y) in frontier)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        var nx = (short)(x + dx);
                        var ny = (short)(y + dy);
                        if (!isWalkable(nx, ny)
                            || (dx != 0 && dy != 0 && !isWalkable(nx, y) && !isWalkable(x, ny))
                            || Steps(nx, ny, toX, toY) > maxSteps - step - 1
                            || !visited.Add((nx, ny)))
                        {
                            continue;
                        }

                        if (nx == toX && ny == toY)
                        {
                            return true;
                        }

                        next.Add((nx, ny));
                    }
                }
            }

            frontier = next;
        }

        return fromX == toX && fromY == toY;
    }
}
