//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

public interface IDebuffResistanceService
{
    // Percentage points added to the chance of a bad card landing: negative when the target
    // resists, positive when something makes it more exposed.
    int ChanceModifier(IAliveEntity target, CardDto card, IReadOnlyCollection<BuffInstance> activeBuffs);
}
