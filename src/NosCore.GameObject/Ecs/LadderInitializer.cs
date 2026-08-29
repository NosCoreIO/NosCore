//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using System.Collections.Generic;

namespace NosCore.GameObject.Ecs;

public sealed class LadderInitializer
{
    public LadderInitializer(List<ReputationLevelDto> reputationLevels, List<DignityLevelDto> dignityLevels)
    {
        ReputationLevels.Load(reputationLevels);
        DignityLevels.Load(dignityLevels);
    }
}
