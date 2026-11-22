//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Components;

// The entity the player's falcon pursues (an id of 0 when none) and when it may next strike it.
public record struct FalconComponent(VisualType FalconTargetType, long FalconTargetId, Instant FalconReadyAt);
