//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Map;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.MateService
{
    public static class MatePlacement
    {
        // Tried in order: the first walkable square wins.
        private static readonly (short X, short Y)[] Offsets =
            [(1, 1), (-1, 1), (1, -1), (-1, -1), (1, 0), (-1, 0), (0, 1), (0, -1)];

        public static void Arrange(short ownerX, short ownerY, Map.Map map, IEnumerable<Mate> mates)
        {
            var taken = new HashSet<(short, short)>();
            foreach (var mate in mates)
            {
                var spot = Free(ownerX, ownerY, map, taken);
                mate.PositionX = spot.X;
                mate.PositionY = spot.Y;
                taken.Add(spot);
            }
        }

        private static (short X, short Y) Free(short ownerX, short ownerY, Map.Map map,
            HashSet<(short, short)> taken)
        {
            foreach (var offset in Offsets)
            {
                var x = (short)(ownerX + offset.X);
                var y = (short)(ownerY + offset.Y);
                if (!taken.Contains((x, y)) && map.IsWalkable(x, y))
                {
                    return (x, y);
                }
            }

            return (ownerX, ownerY);
        }
    }
}
