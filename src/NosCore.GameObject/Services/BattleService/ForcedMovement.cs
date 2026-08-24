//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Services.BattleService;

/// <summary>
/// The geometry behind BCard type 40: a push, a pull, a charge. All three walk an entity along
/// a line, and all three have to stop before the first wall.
/// </summary>
public static class ForcedMovement
{
    /// <summary>
    /// Where the walk ends: the geometry on its own, with no entity and no map behind it.
    /// </summary>
    /// <remarks>
    /// Separated out because this is the part that can be wrong in a way nobody notices - a step
    /// too many puts somebody inside a wall, a step too few makes a charge stop short - and
    /// because the part around it can only move a real ECS bundle, so a test double would never
    /// budge and would prove nothing.
    /// </remarks>
    public static (short X, short Y) Destination(short fromX, short fromY, short towardsX,
        short towardsY, int steps, int stepX, int stepY, int stopAt,
        Func<short, short, bool> isWalkable)
    {
        var x = fromX;
        var y = fromY;
        var closingIn = stepX == Math.Sign(towardsX - fromX) && stepY == Math.Sign(towardsY - fromY);

        for (var i = 0; i < steps; i++)
        {
            if (closingIn && Chebyshev(x, y, towardsX, towardsY) <= stopAt)
            {
                break;
            }

            var nextX = (short)(x + stepX);
            var nextY = (short)(y + stepY);

            // Zero is walkable and anything else is wall - the grid reads the opposite way round
            // from the obvious one.
            if (!isWalkable(nextX, nextY))
            {
                break;
            }

            x = nextX;
            y = nextY;
        }

        return (x, y);
    }
    private static int Chebyshev(short ax, short ay, short bx, short by) =>
        Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by));
}
