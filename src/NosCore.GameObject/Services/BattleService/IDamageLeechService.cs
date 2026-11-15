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

public interface IDamageLeechService
{
    Task ApplyAsync(IAliveEntity attacker, int damageDealt);

    // Type 21 subtypes 3 to 5: HP or MP taken from the target and given to the attacker.
    Task DrainAsync(IAliveEntity attacker, IAliveEntity target, IReadOnlyList<BCardDto> skillCards);
}
