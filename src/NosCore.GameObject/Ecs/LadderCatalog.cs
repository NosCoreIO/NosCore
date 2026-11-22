//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Ecs;

public sealed class LadderCatalog : ISingletonService
{
    private readonly ReputationBand[] _reputation;
    private readonly DignityBand[] _dignity;

    public LadderCatalog(List<ReputationLevelDto> reputationLevels, List<DignityLevelDto> dignityLevels)
    {
        _reputation = reputationLevels
            .OrderByDescending(level => level.MinReputation)
            .Select(level => new ReputationBand(level.MinReputation, (ReputationType)level.ReputationLevelId))
            .ToArray();

        MaxDignity = dignityLevels.FirstOrDefault(level => level.DignityLevelId == (byte)DignityType.Default)?.MaxDignity;

        // Worst first, so the first band a value falls into is the one it belongs to.
        _dignity = dignityLevels
            .SelectMany(level => level.MaxDignity is { } max ? new[] { new DignityBand(max, (DignityType)level.DignityLevelId) } : [])
            .OrderBy(band => band.MaxDignity)
            .ToArray();
    }

    public short? MaxDignity { get; }

    public ReputationType FromReputation(long reputation)
    {
        foreach (var band in _reputation)
        {
            if (reputation >= band.MinReputation)
            {
                return band.Tier;
            }
        }

        // Reputation can go negative, below the floor of the lowest band the client declares.
        return ReputationType.GreenBeginner;
    }

    public DignityType FromDignity(short dignity)
    {
        foreach (var band in _dignity)
        {
            if (dignity <= band.MaxDignity)
            {
                return band.Tier;
            }
        }

        return DignityType.Default;
    }

    private readonly record struct ReputationBand(long MinReputation, ReputationType Tier);

    private readonly record struct DignityBand(short MaxDignity, DignityType Tier);
}
