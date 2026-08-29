//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Ecs;

public static class DignityLevels
{
    private static volatile DignityBand[] _ladder = [];

    public static void Load(IEnumerable<DignityLevelDto> levels)
    {
        // Worst first, so the first band a value falls into is the one it belongs to.
        _ladder = levels
            .Where(level => level.MaxDignity != null)
            .OrderBy(level => level.MaxDignity)
            .Select(level => new DignityBand(level.MaxDignity!.Value, (DignityType)level.DignityLevelId))
            .ToArray();
    }

    public static DignityType FromDignity(short dignity)
    {
        foreach (var band in _ladder)
        {
            if (dignity <= band.MaxDignity)
            {
                return band.Tier;
            }
        }

        return DignityType.Default;
    }

    private readonly record struct DignityBand(short MaxDignity, DignityType Tier);
}
