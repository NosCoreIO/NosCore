//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Collections.Immutable;

namespace NosCore.GameObject.Ecs.Extensions;

public static class TimeSpaceAttemptExtensions
{
    public const short LoserModeFailures = 2;

    public static short RecordTimeSpaceFailure(this PlayerComponentBundle character, short timeSpaceId)
    {
        var failures = character.TimeSpaceFailures ?? ImmutableDictionary<short, short>.Empty;
        var count = (short)(failures.GetValueOrDefault(timeSpaceId) + 1);
        character.TimeSpaceFailures = failures.SetItem(timeSpaceId, count);
        return count;
    }

    // Clearing any time-space starts every count over, not only the one for the time-space cleared.
    public static void ClearTimeSpaceFailures(this PlayerComponentBundle character)
    {
        character.TimeSpaceFailures = null;
    }

    public static bool OffersLoserMode(this PlayerComponentBundle character, short timeSpaceId) =>
        character.TimeSpaceFailures?.GetValueOrDefault(timeSpaceId) >= LoserModeFailures;
}
