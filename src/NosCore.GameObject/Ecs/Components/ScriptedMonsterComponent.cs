//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Instances.Abstractions;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Ecs.Components;

// HpThresholds loses each threshold as it is crossed, so a monster healed above one does not run it again.
public record struct ScriptedMonsterComponent(bool IsBonus, Action<IInstanceRun>? DeathStep, Action<IInstanceRun>? HitStep,
    IReadOnlyList<HpThreshold> HpThresholds);
