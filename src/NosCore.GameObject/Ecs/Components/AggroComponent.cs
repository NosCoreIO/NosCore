//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Components;

// Aggro bookkeeping for monsters/pets. TargetVisualId 0 means "no target". UntilLeash is
// the moment aggro drops if the target has not been seen/hit again, giving the AI a
// natural cooldown without a separate timer.
public record struct AggroComponent(
    VisualType TargetVisualType,
    long TargetVisualId,
    int ThreatScore,
    Instant UntilLeash);
