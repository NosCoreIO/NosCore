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

// Published once at startup rather than injected: the icon is read from PlayerComponentBundle,
// a readonly struct the ECS builds without the container.
public static class ReputationLevels
{
    private static volatile ReputationBand[] _ladder = [];

    public static void Load(IEnumerable<ReputationLevelDto> levels)
    {
        _ladder = levels
            .OrderByDescending(level => level.MinReputation)
            .Select(level => new ReputationBand(level.MinReputation, (ReputationType)level.ReputationLevelId))
            .ToArray();
    }

    public static ReputationType FromReputation(long reputation)
    {
        foreach (var band in _ladder)
        {
            if (reputation >= band.MinReputation)
            {
                return band.Tier;
            }
        }

        // Reputation can go negative, below the floor of the lowest band the client declares.
        return ReputationType.GreenBeginner;
    }

    private readonly record struct ReputationBand(long MinReputation, ReputationType Tier);
}
