//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Map;
using NosCore.Shared.Helpers;
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

        // A mate that never stood in the Miniland gets a spot in its open middle.
        public static (short X, short Y) MinilandSpot(Mate mate, Map.Map map)
        {
            if ((mate.MapX != 0 || mate.MapY != 0) && map.IsWalkable(mate.MapX, mate.MapY))
            {
                return (mate.MapX, mate.MapY);
            }

            for (var attempt = 0; attempt < 20; attempt++)
            {
                var x = (short)RandomHelper.Instance.RandomNumber(5, 16);
                var y = (short)RandomHelper.Instance.RandomNumber(3, 14);
                if (map.IsWalkable(x, y))
                {
                    mate.MapX = x;
                    mate.MapY = y;
                    return (x, y);
                }
            }

            return (mate.MapX, mate.MapY);
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
