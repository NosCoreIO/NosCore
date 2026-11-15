//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Instances.Abstractions;
using System;

namespace NosCore.GameObject.Ecs.Components;

public record struct ScriptedMonsterComponent(bool IsBonus, Action<IInstanceRun>? DeathStep);
