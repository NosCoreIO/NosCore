//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using Arch.Core;

namespace NosCore.GameObject.Ecs.Components;

// Damage each attacker has landed, shared out as rewards when the entity dies.
public record struct HitListComponent(ConcurrentDictionary<Entity, int> HitList);
