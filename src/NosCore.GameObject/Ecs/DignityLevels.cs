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

// Imported from the client by DignityLevelParser. Published once at startup rather than injected
// for the same reason as ReputationLevels: the icon is read from a readonly ECS struct.
public static class DignityLevels
{
    // Worst first, so the first band a value falls into is the one it belongs to.
    private static readonly DignityBand[] ClientLadder =
    [
        new(-801, DignityType.Failed),
        new(-601, DignityType.Useless),
        new(-401, DignityType.Unqualified),
        new(-201, DignityType.Dreadful),
        new(-100, DignityType.Dubious)
    ];

    private static volatile DignityBand[] _ladder = ClientLadder;

    public static void Load(IEnumerable<DignityLevelDto> levels)
    {
        var ladder = levels
            .Where(level => level.MaxDignity != null)
            .OrderBy(level => level.MaxDignity)
            .Select(level => new DignityBand(level.MaxDignity!.Value, (DignityType)level.DignityLevelId))
            .ToArray();

        if (ladder.Length == 0)
        {
            return;
        }

        _ladder = ladder;
    }

    public static void ResetToClientLadder()
    {
        _ladder = ClientLadder;
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
