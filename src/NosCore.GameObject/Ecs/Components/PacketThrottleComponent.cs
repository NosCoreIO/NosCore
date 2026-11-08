//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

// When each throttled kind of packet was last let through, keyed by its throttle name.
public record struct PacketThrottleComponent(ConcurrentDictionary<string, Instant> LastSent);
