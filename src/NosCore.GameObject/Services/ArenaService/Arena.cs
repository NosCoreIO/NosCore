//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;

namespace NosCore.GameObject.Services.ArenaService
{
    public static class Arena
    {
        public const short IndividualArenaMapId = 2006;
        public const short ArenaOfGloryMapId = 2106;
        public const long EntryFee = 500;

        // Only a fallback: leaving an arena returns the fighter to the spot they entered from.
        public const short ExitDestinationMapId = 1;
        public const short ExitDestinationX = 50;
        public const short ExitDestinationY = 66;

        private static readonly Dictionary<short, (short X, short Y)> ExitPortals = new()
        {
            [IndividualArenaMapId] = (37, 15),
            [ArenaOfGloryMapId] = (38, 3),
        };

        public static bool IsArena(short mapId) => ExitPortals.ContainsKey(mapId);

        public static (short X, short Y)? ExitPortal(short mapId) =>
            ExitPortals.TryGetValue(mapId, out var cell) ? cell : null;

        // The n_run type picks the arena: 0 is the individual arena, 1 the Arena of Glory, which
        // costs twice as much to enter.
        public static short? MapIdForType(short type) => type switch
        {
            0 => IndividualArenaMapId,
            1 => ArenaOfGloryMapId,
            _ => null,
        };

        public static long EntryCost(short type) => EntryFee * (1 + type);

        public static (short X, short Y)? RandomFightingCell(Map.Map map)
        {
            var cells = new List<(short X, short Y)>();
            for (short y = 0; y < map.Height; y++)
            {
                for (short x = 0; x < map.Width; x++)
                {
                    if (IsFightingCell(map, x, y))
                    {
                        cells.Add((x, y));
                    }
                }
            }

            return cells.Count == 0 ? null : cells[RandomHelper.Instance.RandomNumber(0, cells.Count)];
        }

        // Players only ever fight each other inside an arena, and never a member of their own group.
        public static bool AreOpponents(ICharacterEntity attacker, ICharacterEntity defender)
        {
            if (attacker.CharacterId == defender.CharacterId) return false;
            var mapInstance = attacker.MapInstance;
            if (mapInstance == null || mapInstance.MapInstanceType != MapInstanceType.ArenaInstance) return false;
            if (defender.MapInstance?.MapInstanceId != mapInstance.MapInstanceId) return false;
            if (attacker.Group.Count > 1
                && attacker.Group.ContainsKey(new Tuple<VisualType, long>(VisualType.Player, defender.VisualId)))
            {
                return false;
            }

            return IsFightingCell(mapInstance.Map, attacker.PositionX, attacker.PositionY)
                && IsFightingCell(mapInstance.Map, defender.PositionX, defender.PositionY);
        }

        // The raised platform where the arena merchants stand is marked in the map grid; nobody can
        // hit or be hit there.
        public static bool IsFightingCell(Map.Map map, short x, short y) =>
            map.IsWalkable(x, y) && map[x, y] == 0;
    }
}
