//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

// When each skill a player cast, by CastId, comes off cooldown and the client is told so.
public record struct SkillResetComponent(ConcurrentDictionary<long, Instant> PendingResets);
