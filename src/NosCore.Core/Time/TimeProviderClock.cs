//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NodaTime;

namespace NosCore.Core.Time
{
    // Reads the time from the same provider every wait and timer uses, so the two never disagree.
    public sealed class TimeProviderClock(TimeProvider time) : IClock
    {
        public Instant GetCurrentInstant() => Instant.FromDateTimeOffset(time.GetUtcNow());
    }
}
