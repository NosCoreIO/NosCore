//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Components;

// The extra targets an mtlist names for the skill cast that follows it.
public record struct ListedTargetsComponent(IReadOnlyList<(VisualType VisualType, long VisualId)>? ListedTargets);
