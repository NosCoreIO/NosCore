//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

public record struct MinigameSessionComponent(
    Guid? MinigameObjectId,
    Instant MinigameStartedAt,
    byte? MinigameRewardLevel);
