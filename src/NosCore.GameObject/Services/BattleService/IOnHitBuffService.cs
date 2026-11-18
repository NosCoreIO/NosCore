//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

public interface IOnHitBuffService
{
    Task TriggerAsync(IAliveEntity attacker, IAliveEntity defender, bool defenderKilled, IReadOnlyList<BCardDto> skillBCards,
        bool usesSecondaryWeapon);
}
