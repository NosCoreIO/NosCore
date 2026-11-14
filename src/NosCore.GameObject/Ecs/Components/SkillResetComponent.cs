//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

// When each skill that was cast comes off cooldown and the client is told so: a player's by CastId, a
// pet's by the slot of its skill bar.
public record struct SkillResetComponent(ConcurrentDictionary<long, Instant> PendingResets);
