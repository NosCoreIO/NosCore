//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Services.MovementService;

// The eight directions an entity can face, as the dir packet numbers them. Y grows downwards.
public static class Facing
{
    // Indexed by the 45° sector of a move, starting east and turning towards +Y.
    private static readonly byte[] DirectionBySector = [1, 6, 2, 7, 3, 4, 0, 5];

    public static (int X, int Y) Step(byte direction) => direction switch
    {
        0 => (0, -1),
        1 => (1, 0),
        2 => (0, 1),
        3 => (-1, 0),
        4 => (-1, -1),
        5 => (1, -1),
        6 => (1, 1),
        7 => (-1, 1),
        _ => (0, 0),
    };

    // The client turns a walker towards where it is heading without sending a dir packet.
    public static byte Towards(int dx, int dy, byte current)
    {
        if (dx == 0 && dy == 0)
        {
            return current;
        }

        var sector = (int)Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4));
        return DirectionBySector[(sector + 8) % 8];
    }
}
