//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Services.BattleService;

// Abstraction over System.Random so dice rolls in combat are mockable. The default
// instance uses a thread-safe shared Random.
public interface IRandomProvider
{
    int Next(int minInclusive, int maxExclusive);

    double NextDouble();
}

public sealed class RandomProvider : IRandomProvider
{
    public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);

    public double NextDouble() => Random.Shared.NextDouble();
}
