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

    public static short? Maximum { get; private set; }

    public static void Load(IEnumerable<DignityLevelDto> levels)
    {
        var imported = levels.ToList();
        Maximum = imported.FirstOrDefault(level => level.DignityLevelId == (byte)DignityType.Default)?.MaxDignity;

        // Worst first, so the first band a value falls into is the one it belongs to.
        _ladder = imported
            .SelectMany(level => level.MaxDignity is { } max ? new[] { new DignityBand(max, (DignityType)level.DignityLevelId) } : [])
            .OrderBy(band => band.MaxDignity)
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
