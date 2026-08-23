//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Map;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.MateService
{
    /// <summary>
    /// Puts a character's mates around them.
    /// </summary>
    public static class MatePlacement
    {
        // Tried in order, so the first walkable square wins and a mate against a wall tucks in
        // somewhere rather than standing in it.
        private static readonly (short X, short Y)[] Offsets =
            [(1, 1), (-1, 1), (1, -1), (-1, -1), (1, 0), (-1, 0), (0, 1), (0, -1)];

        /// <summary>
        /// Places every mate on its own walkable square around the owner.
        /// </summary>
        /// <remarks>
        /// A character can have a pet and a partner out at once, so squares are reserved as they
        /// are handed out; giving both the same offset would stack them. With nothing free the
        /// mate stands on the owner — untidy, and better than being left across the map.
        /// </remarks>
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
