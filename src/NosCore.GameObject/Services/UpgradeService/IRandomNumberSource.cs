//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.UpgradeService;

// Indirection so upgrade rolls are deterministic in tests. Default implementation wraps
// Random.Shared; tests inject a stub returning a fixed value.
public interface IRandomNumberSource
{
    double NextDouble();
}

public sealed class RandomNumberSource : IRandomNumberSource
{
    public double NextDouble() => System.Random.Shared.NextDouble();
}
