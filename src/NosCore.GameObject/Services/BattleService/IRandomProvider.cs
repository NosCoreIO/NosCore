//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Services.BattleService;

// Every roll goes through this so tests can fix the outcome.
public interface IRandomProvider
{
    int Next(int minInclusive, int maxExclusive);

    double NextDouble();
}

// The one place that rolls on the shared random; everything else goes through the interface.
#pragma warning disable NosCoreSharedRandom
public sealed class RandomProvider : IRandomProvider
{
    public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);

    public double NextDouble() => Random.Shared.NextDouble();
}
#pragma warning restore NosCoreSharedRandom
